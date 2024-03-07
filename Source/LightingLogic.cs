using HarmonyLib;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Lights;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.ModAPI;
using VRageMath;
using VRageRender;

namespace mleise.ProjectedLightsPlugin
{
	enum LightFlags : uint
	{
		handledByUs = 0x80000000,
	}

	static class MyExtensions
	{
		private static FieldInfo s_lightingLogicFieldInfo = AccessTools.DeclaredField(typeof(MyLightingBlock), "m_lightingLogic");

		internal static MyLightingLogic LightingLogic(this MyLightingBlock block)
		{
			return (MyLightingLogic)s_lightingLogicFieldInfo.GetValue(block);
		}

		internal static bool IsLightHandledByUs(this MyLightingLogic logic)
		{
			return logic.Lights.Count == 0 || ((uint)logic.Lights[0].LightType & (uint)LightFlags.handledByUs) != 0;
		}
	}

	// Pasting grids or welding projected interior lights normally enables the glare, so we disable this method.
	[HarmonyPatch(typeof(MyInteriorLight), "UpdateGlare")]
	static class Patch_MyInteriorLight_UpdateGlare
	{
		internal static bool Prefix(MyInteriorLight __instance)
		{
			return !__instance.LightingLogic().IsLightHandledByUs();
		}
	}

	// Here we clone the original, but omit the glare and make the light cone brighter the more focused it is.
	// We also update the emissive material, because we now incorporate the lights intensity into emissivity.
	[HarmonyPatch(typeof(MyInteriorLight), "UpdateIntensity")]
	static class Patch_MyInteriorLight_UpdateIntensity
	{
		internal static bool Prefix(MyInteriorLight __instance)
		{
			var logic = __instance.LightingLogic();
			if (logic.IsLightHandledByUs())
			{
				float vanillaIntensity = 2 * logic.CurrentLightPower * __instance.Intensity;
				foreach (MyLight light in logic.Lights)
				{
					light.Intensity = vanillaIntensity * light.GlareIntensity;
					light.ReflectorIntensity = vanillaIntensity * LightDefinition.GLOSS_FACTOR * light.GlareQueryFreqMinMs * (1 - light.GlareIntensity);
				}

				logic.BulbColor = logic.ComputeBulbColor();
				return false;
			}
			return true;
		}

		internal static void Postfix(MyInteriorLight __instance)
		{
			var logic = __instance.LightingLogic();
			if (logic.IsLightHandledByUs())
			{
				logic.IsEmissiveMaterialDirty = true;
				logic.UpdateEmissiveMaterial();
			}
		}
	}

	// We replace the light bulb color with a direct copy of the color values.
	[HarmonyPatch(typeof(MyLightingLogic), nameof(MyLightingLogic.ComputeBulbColor))]
	static class Patch_MyLightingLogic_ComputeBulbColor
	{
		internal static bool Prefix(MyLightingLogic __instance, ref Color __result)
		{
			if (__instance.IsLightHandledByUs())
			{
				__result = __instance.Color;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MyLightingLogic), "UpdateLightProperties")]
	static class Patch_MyLightingLogic_UpdateLightProperties
	{
		internal static bool Prefix(MyLightingLogic __instance)
		{
			if (__instance.IsLightHandledByUs())
			{
				foreach (var light in __instance.Lights)
				{
					light.ReflectorColor = __instance.Color;
					light.ReflectorFalloff = 0.52f;
					light.PointLightOffset = __instance.Offset - light.GlareQueryFreqRndMs;
					light.Range = __instance.Radius;
					light.ReflectorRange = __instance.Radius + light.PointLightOffset;
					light.Falloff = __instance.Falloff;
					light.UpdateLight();
				}
				return false;
			}
			else
			{
				return true;
			}
		}
	}

	[HarmonyPatch(typeof(MyLightingLogic), "UpdateEmissiveMaterial", new Type[] { typeof(uint) })]
	static class Patch_MyLightingLogic_UpdateEmissiveMaterial
	{
		private static FieldInfo s_blinkOnFieldInfo = AccessTools.DeclaredField(typeof(MyLightingLogic), "m_blinkOn");
		private static FieldInfo s_pointLightEmissiveMaterialFieldInfo = AccessTools.DeclaredField(typeof(MyLightingLogic), "m_pointLightEmissiveMaterial");
		private static FieldInfo s_spotLightEmissiveMaterialFieldInfo = AccessTools.DeclaredField(typeof(MyLightingLogic), "m_spotLightEmissiveMaterial");

		internal static bool Prefix(MyLightingLogic __instance, uint renderId)
		{
			if (__instance.IsLightHandledByUs())
			{
				if (renderId != uint.MaxValue)
				{
					var blinkOn = (bool)s_blinkOnFieldInfo.GetValue(__instance);
					var intensity = blinkOn ? __instance.CurrentLightPower * __instance.Intensity * __instance.Lights[0].GlareMaxDistance : 0;
					var pointLightEmissiveMaterial = (string)s_pointLightEmissiveMaterialFieldInfo.GetValue(__instance);
					var spotLightEmissiveMaterial = (string)s_spotLightEmissiveMaterialFieldInfo.GetValue(__instance);
					MyRenderProxy.UpdateModelProperties(renderId, pointLightEmissiveMaterial, 0, 0, __instance.BulbColor, intensity);
					if (!string.IsNullOrEmpty(spotLightEmissiveMaterial))
					{
						MyRenderProxy.UpdateModelProperties(renderId, spotLightEmissiveMaterial, 0, 0, __instance.BulbColor, intensity);
					}
				}
				return false;
			}
			else
			{
				return true;
			}
		}
	}

	// This is where MyLightingLogic creates its lights using its LightLocalDatas array and some fixed data from the block definition.
	[HarmonyPatch(typeof(MyLightingLogic), nameof(MyLightingLogic.RecreateLights))]
	static class Patch_MyLightingLogic_RecreateLights
	{
		private static FieldInfo s_blockField = AccessTools.DeclaredField(typeof(MyLightingLogic), "m_block");

		internal static void FixupLightingLogic(MyLightingLogic logic, bool isInitialCreation)
		{
			var block = (MyFunctionalBlock)s_blockField.GetValue(logic);
			var definition = IniHandler.GetFullDefinition(block);

			// Processing is enabled for all interior light type blocks that aren't explicitly disabled in the definition.
			if (!definition.Disabled)
			{
				List<MyLightingLogic.LightLocalData> lightLocalDatas = logic.LightLocalDatas;
				List<MyLight> lights = logic.Lights;

				foreach (var light in lights)
				{
					// Mark as handled by us and disable glare so we can use its fields for other purposes.
					light.LightType = (MyLightType)((uint)light.LightType | (uint)LightFlags.handledByUs);
					light.GlareOn = false;
					light.GlossFactor = 0;
					// Hijack glare intensity as blend factor between spot light and point light.
					light.GlareIntensity = definition.Mix;
					// Hijacked to store a bloom intensity coefficient.
					light.GlareMaxDistance = definition.Bloom;
					// Hijacked to store an intensity coefficient.
					light.GlareQueryFreqMinMs = definition.Intensity;
					// Cast shadows only if explicitly asked for and limit cone accordingly
					light.CastShadows = definition.CastShadows;
					// Turn projected texture on and set its cone angle.
					light.ReflectorOn = definition.Mix < 1;
					light.ReflectorTexture = definition.Texture;
					light.ReflectorConeDegrees = definition.ConeAngle;
					light.ReflectorGlossFactor = 1;
					light.ReflectorDiffuseFactor = light.DiffuseFactor / LightDefinition.GLOSS_FACTOR;
				}

				// We first reset the position, then add our offset.
				logic.UpdateLightData();
				for (int i = 0; i < lightLocalDatas.Count; i++)
				{
					var lightData = lightLocalDatas[i];
					var oldTranslation = lightData.LocalMatrix.Translation;
					lightData.LocalMatrix = Matrix.Multiply(lightData.LocalMatrix, Matrix.CreateFromAxisAngle(lightData.LocalMatrix.Right, MathHelper.ToRadians(definition.Rotation)));
					var leftBeforeRoll = lightData.LocalMatrix.Left;
					lightData.LocalMatrix = Matrix.Multiply(lightData.LocalMatrix, Matrix.CreateFromAxisAngle(lightData.LocalMatrix.Forward, MathHelper.ToRadians(definition.TextureRotation)));
					lightData.LocalMatrix.Translation = lightData.LocalMatrix.Forward * definition.Forward + leftBeforeRoll * definition.Left;
					// Hijacked to fix point light offset after we moved the origin.
					lights[i].GlareQueryFreqRndMs = Vector3.Dot(lightData.LocalMatrix.Forward, lightData.LocalMatrix.Translation - oldTranslation);
				}
				logic.IsPositionDirty = true;
				logic.UpdateLightPosition();
				logic.IsEmissiveMaterialDirty = true;
				logic.UpdateEmissiveMaterial();
				typeof(MyLightingLogic).GetMethod("UpdateIntensity", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(logic, null);
				logic.UpdateLightProperties();
			}
			else if (!isInitialCreation)
			{
				logic.UpdateLightData();
				logic.NeedsRecreateLights = true;
				block.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
			}
		}

		internal static void Postfix(MyLightingLogic __instance)
		{
			FixupLightingLogic(__instance, true);
		}
	}

	// This patch replaces the turn on/off logic for when the light is blinking.
	[HarmonyPatch(typeof(MyInteriorLight), "UpdateEnabled")]
	static class Patch_MyInteriorLight_UpdateEnabled
	{
		internal static bool Prefix(MyInteriorLight __instance, bool state)
		{
			var logic = __instance.LightingLogic();
			if (logic.IsLightHandledByUs())
			{
				foreach (var light in logic.Lights)
				{
					// Glare intensity is where we store the LERP factor between spot light and point light.
					if (light.GlareIntensity > 0)
					{
						light.LightOn = state;
					}
					if (light.GlareIntensity < 1)
					{
						light.ReflectorOn = state;
					}
				}
				return false;
			}
			else
			{
				return true;
			}
		}
	}
}