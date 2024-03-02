using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using VRage.Render.Scene.Components;
using VRageRender;
using VRageRender.Messages;

namespace mleise.ProjectedLightsPlugin
{
	// Space Engineers can render up to 4 shadows. This patch contains heuristics to ensure the important spot lights are rendered first.
	[HarmonyPatch]
	internal static class Patch_MyLightsRendering_CullSpotLights
	{
		private static Type myLightsRenderingType = AccessTools.TypeByName("VRage.Render11.LightingStage.MyLightsRendering");
		private static FieldInfo resultsField = AccessTools.DeclaredField("VRage.Render11.Culling.MyCullQuery:Results");
		private static FieldInfo spotLightsField = AccessTools.DeclaredField("VRage.Render11.Culling.MyCullResults:SpotLights");
		private static FieldInfo viewerDistanceSquaredFastField = AccessTools.DeclaredField("VRage.Render11.Scene.Components.MyLightComponent:ViewerDistanceSquaredFast");
		private static FieldInfo outputListField = AccessTools.DeclaredField(myLightsRenderingType, "m_outputList");
		private static FieldInfo maxPointLightsField = AccessTools.DeclaredField(myLightsRenderingType, "m_maxPointLights");
		private static MethodBase sortFunction, removeRangeFunction;
		private static object sortComparer;

		internal static MethodBase TargetMethod()
		{
			return myLightsRenderingType.GetMethod("CullSpotLights", BindingFlags.Static | BindingFlags.NonPublic);
		}

		internal static void Prepare()
		{
			var methods = outputListField.FieldType.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (var method in methods)
			{
				if (method.Name == "Sort" && method.GetParameters().Length == 1)
				{
					sortFunction = method;
					break;
				}
			}
			sortComparer = AccessTools.DeclaredField("VRage.Render11.Scene.Components.MyLightComponent:SortComparer").GetValue(null);
			removeRangeFunction = outputListField.FieldType.GetMethod("RemoveRange", new Type[] { typeof(int), typeof(int) });
		}

		internal static bool Prefix(object query)
		{
			var dataField = typeof(MyLightComponent).GetField("m_data", BindingFlags.Instance | BindingFlags.NonPublic);
			var originalDataField = typeof(MyLightComponent).GetField("m_originalData", BindingFlags.Instance | BindingFlags.NonPublic);

			// Sort spot lights by distance and turn shadows off.
			var queryResults = resultsField.GetValue(query);
			var spotLights = (IList)spotLightsField.GetValue(queryResults);
			foreach (MyLightComponent myLightComponent in spotLights)
			{
				viewerDistanceSquaredFastField.SetValue(myLightComponent, myLightComponent.Owner.CalculateCameraDistanceSquaredFast());
				var data = (UpdateRenderLightData)originalDataField.GetValue(myLightComponent);
				data.CastShadows = false;
				originalDataField.SetValue(myLightComponent, data);
			}
			sortFunction.Invoke(spotLights, new object[] { sortComparer });

			// Turn shadows on again for the closest 4 (except if the light is so close to the camera that shadows wont show).
			int shadowCasters = 0;
			foreach (MyLightComponent myLightComponent in spotLights)
			{
				var castShadows = ((UpdateRenderLightData)dataField.GetValue(myLightComponent)).CastShadows;
				if (castShadows && (float)viewerDistanceSquaredFastField.GetValue(myLightComponent) >= 0.08f)
				{
					var data = (UpdateRenderLightData)originalDataField.GetValue(myLightComponent);
					data.CastShadows = castShadows;
					originalDataField.SetValue(myLightComponent, data);
					if (++shadowCasters == 4) break;
				}
			}

			// Remove any excess lights from the end of the list.
			var maxLights = (int)maxPointLightsField.GetValue(null);
			if (spotLights.Count > maxLights)
			{
				removeRangeFunction.Invoke(spotLights, new object[] { maxLights, spotLights.Count - maxLights });
			}
			return false;
		}
	}

	// This patch removes the 32 instances limit from the spotlight renderer.
	[HarmonyPatch]
	internal static class Patch_MyLightsRendering_RenderSpotlights
	{
		private static Type myLightsRenderingType = AccessTools.TypeByName("VRage.Render11.LightingStage.MyLightsRendering");

		internal static MethodBase TargetMethod()
		{
			return myLightsRenderingType.GetMethod("RenderSpotlights", BindingFlags.Static | BindingFlags.NonPublic);
		}

		internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var codes = (List<CodeInstruction>)instructions;
			for (var i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 32)
				{
					codes.RemoveAt(i + 2);
					break;
				}
			}
			return codes;
		}
	}

	// The render target texture used for emissive materials is 8-bit, so the amount of bloom we get is hard capped by the
	// renderers emissive bloom multiplier. In order to get a realistically blinding light without resorting to the glare
	// sprites we need to bump up this bloom response a lot.
	[HarmonyPatch(typeof(MyPostprocessSettings), nameof(MyPostprocessSettings.GetProcessedData))]
	internal static class Patch_MyPostprocessSettings_GetProcessedData
	{
		internal static void Postfix(ref MyPostprocessSettings.Layout __result)
		{
			__result.BloomEmissiveness *= LightDefinition.EMISSIVE_BOOST;
		}
	}

	// Now since everything blooms out more than desired, we have to write smaller values into the emissive render target.
	[HarmonyPatch]
	internal static class Patch_MyRender11_ProcessMessageInternal
	{
		internal static void Prepare()
		{
			AccessTools.Field("VRage.Render11.Scene.Components.MyModelProperties:DefaultEmissivity").SetValue(null, LightDefinition.EMISSIVE_BOOST_INV);

			var myInstanceMaterialType = AccessTools.TypeByName("VRage.Render11.GeometryStage2.Instancing.MyInstanceMaterial");
			var defaultField = AccessTools.Field(myInstanceMaterialType, "Default");
			var material = defaultField.GetValue(null);
			AccessTools.PropertySetter(myInstanceMaterialType, "Emissivity").Invoke(material, new object[] { LightDefinition.EMISSIVE_BOOST_INV });
			defaultField.SetValue(null, material);
		}

		internal static MethodBase TargetMethod() => AccessTools.Method("VRageRender.MyRender11:ProcessMessageInternal");

		internal static void Prefix(MyRenderMessageBase message)
		{
			switch (message.MessageType)
			{
				case MyRenderMessageEnum.UpdateColorEmissivity: // Used for example by batteries, spammed by H2/O2 generators.
					((MyRenderMessageUpdateColorEmissivity)message).Emissivity *= LightDefinition.EMISSIVE_BOOST_INV;
					break;
				case MyRenderMessageEnum.UpdateModelProperties: // Used when lights change their emissive texture.
					((MyRenderMessageUpdateModelProperties)message).Emissivity *= LightDefinition.EMISSIVE_BOOST_INV;
					break;
			}
		}
	}
}