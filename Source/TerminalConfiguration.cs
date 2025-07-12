using HarmonyLib;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;

namespace mleise.ProjectedLightsPlugin
{
	static class IniHandler
	{
		private const string INI_SECTION = "ProjectedLights";

		private struct ParseResult
		{
			internal LightDefinition definition;
			internal MyFunctionalBlock block;
			internal MyIni ini;
		}

		private static ref ParseResult Parse(MyFunctionalBlock block, out ParseResult result, bool forUpdating = false)
		{
			var ini = new MyIni();
			result = new ParseResult
			{
				block = block,
				definition = LightDefinition.s_dict.TryGetValue(block.BlockDefinition.Id.SubtypeName, out var definition) ? definition : LightDefinition.s_generic,
				ini = (forUpdating ? ini.TryParse(block.CustomData) : ini.TryParse(block.CustomData, INI_SECTION)) ? ini : null
			};
			return ref result;
		}

		internal static LightDefinition GetFullDefinition(MyFunctionalBlock block)
		{
			Parse(block, out var pr);
			pr.definition.Disabled = !GetBool(ref pr, "Enabled", DefaultEnabled(ref pr));
			pr.definition.CastShadows = GetBool(ref pr, "CastShadows", DefaultCastShadows(ref pr));
			pr.definition.ShadowRange = GetFloat(ref pr, "ShadowRange", DefaultShadowRange(ref pr));
			pr.definition.Texture = GetString(ref pr, "Texture", pr.definition.CastShadows ? pr.definition.SpotTexture : pr.definition.Texture);
			pr.definition.SpotTexture = pr.definition.Texture;
			pr.definition.ConeAngle = Math.Min(GetFloat(ref pr, "ConeAngle", DefaultConeAngle(ref pr)), pr.definition.CastShadows ? 136 : 180);
			pr.definition.Bloom = GetFloat(ref pr, "Bloom", DefaultBloom(ref pr));
			pr.definition.Intensity = GetFloat(ref pr, "Intensity", pr.definition.Intensity);
			pr.definition.Rotation = GetFloat(ref pr, "Rotation", pr.definition.Rotation);
			pr.definition.Forward = GetFloat(ref pr, "Forward", pr.definition.Forward);
			pr.definition.Left = GetFloat(ref pr, "Left", pr.definition.Left);
			pr.definition.Up = GetFloat(ref pr, "Up", pr.definition.Up);
			pr.definition.TextureRotation = GetFloat(ref pr, "TextureRotation", DefaultTextureRotation(ref pr));
			pr.definition.Mix = GetFloat(ref pr, "Mix", pr.definition.Mix);
			return pr.definition;
		}

		private static bool GetBool(ref ParseResult pr, string name, bool fallback)
		{
			return (pr.ini != null && pr.ini.Get(INI_SECTION, name).TryGetBoolean(out var value)) ? value : fallback;
		}

		private static float GetFloat(ref ParseResult pr, string name, float fallback)
		{
			return (pr.ini != null && pr.ini.Get(INI_SECTION, name).TryGetSingle(out var value)) ? value : fallback;
		}

		private static string GetString(ref ParseResult pr, string name, string fallback)
		{
			return (pr.ini != null && pr.ini.Get(INI_SECTION, name).TryGetString(out var value)) ? value : fallback;
		}

		private static bool SetBool(ref ParseResult pr, string name, bool fallback, bool value)
		{
			if (pr.ini == null) return false;

			if (SetHelper(ref pr, name, fallback, value) && !pr.ini.Get(INI_SECTION, name).TryGetBoolean(out fallback) || fallback != value)
			{
				pr.ini.Set(INI_SECTION, name, value);
				pr.block.CustomData = pr.ini.ToString();
			}
			return true;
		}

		private static bool SetFloat(ref ParseResult pr, string name, float fallback, float value)
		{
			if (pr.ini == null) return false;

			if (Math.Abs(fallback - value) < 0.001f)
			{
				value = fallback;
			}
			if (SetHelper(ref pr, name, fallback, value) && !pr.ini.Get(INI_SECTION, name).TryGetSingle(out fallback) || Math.Abs(fallback - value) >= 0.0009f)
			{
				pr.ini.Set(INI_SECTION, name, value.ToString("F3", CultureInfo.InvariantCulture));
				pr.block.CustomData = pr.ini.ToString();
			}
			return true;
		}

		private static bool SetString(ref ParseResult pr, string name, string fallback, string value)
		{
			if (pr.ini == null) return false;

			if (SetHelper(ref pr, name, fallback, value) && !pr.ini.Get(INI_SECTION, name).TryGetString(out fallback) || fallback != value)
			{
				pr.ini.Set(INI_SECTION, name, value);
				pr.block.CustomData = pr.ini.ToString();
			}
			return true;
		}

		private static bool SetHelper<T>(ref ParseResult pr, string name, T fallback, T value)
		{
			if (fallback.Equals(value))
			{
				if (pr.ini.ContainsKey(INI_SECTION, name))
				{
					pr.ini.Delete(INI_SECTION, name);
					DeleteSectionIfEmpty(pr.ini);
					pr.block.CustomData = pr.ini.ToString();
				}
				return false;
			}
			return true;
		}

		private static void DeleteSectionIfEmpty(MyIni ini)
		{
			var keys = new List<MyIniKey>();
			ini.GetKeys(INI_SECTION, keys);
			if (keys.Count == 0)
			{
				ini.DeleteSection(INI_SECTION);
			}
		}

		private static bool DefaultEnabled(ref ParseResult pr) => typeof(MyInteriorLight).IsAssignableFrom(pr.block.GetType()) && !pr.definition.Disabled;
		internal static bool GetEnabled(MyFunctionalBlock block) => GetBool(ref Parse(block, out var pr), "Enabled", DefaultEnabled(ref pr));
		internal static bool SetEnabled(MyFunctionalBlock block, bool value) => SetBool(ref Parse(block, out var pr, true), "Enabled", DefaultEnabled(ref pr), value);

		private static bool DefaultCastShadows(ref ParseResult pr) => pr.definition.CastShadows;
		internal static bool GetCastShadows(MyFunctionalBlock block) => GetBool(ref Parse(block, out var pr), "CastShadows", DefaultCastShadows(ref pr));
		internal static void SetCastShadows(MyFunctionalBlock block, bool value) => SetBool(ref Parse(block, out var pr, true), "CastShadows", DefaultCastShadows(ref pr), value);

		private static float DefaultShadowRange(ref ParseResult pr) => pr.definition.ShadowRange;
		internal static float GetDefaultShadowRange(MyFunctionalBlock block) => DefaultShadowRange(ref Parse(block, out _));
		internal static float GetShadowRange(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "ShadowRange", DefaultShadowRange(ref pr));
		internal static void SetShadowRange(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "ShadowRange", DefaultShadowRange(ref pr), value);

		private static float DefaultConeAngle(ref ParseResult pr) => pr.definition.ConeAngle;
		internal static float GetDefaultConeAngle(MyFunctionalBlock block) => DefaultConeAngle(ref Parse(block, out _));
		internal static float GetConeAngle(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "ConeAngle", DefaultConeAngle(ref pr));
		internal static void SetConeAngle(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "ConeAngle", DefaultConeAngle(ref pr), value);

		private static float DefaultForward(ref ParseResult pr) => pr.definition.Forward;
		internal static float GetDefaultForward(MyFunctionalBlock block) => DefaultForward(ref Parse(block, out _));
		internal static float GetForward(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Forward", DefaultForward(ref pr));
		internal static void SetForward(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Forward", DefaultForward(ref pr), value);

		private static float DefaultLeft(ref ParseResult pr) => pr.definition.Left;
		internal static float GetDefaultLeft(MyFunctionalBlock block) => DefaultLeft(ref Parse(block, out _));
		internal static float GetLeft(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Left", DefaultLeft(ref pr));
		internal static void SetLeft(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Left", DefaultLeft(ref pr), value);

		private static float DefaultUp(ref ParseResult pr) => pr.definition.Up;
		internal static float GetDefaultUp(MyFunctionalBlock block) => DefaultUp(ref Parse(block, out _));
		internal static float GetUp(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Up", DefaultUp(ref pr));
		internal static void SetUp(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Up", DefaultUp(ref pr), value);

		private static float DefaultRotation(ref ParseResult pr) => pr.definition.Rotation;
		internal static float GetDefaultRotation(MyFunctionalBlock block) => DefaultRotation(ref Parse(block, out _));
		internal static float GetRotation(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Rotation", DefaultRotation(ref pr));
		internal static void SetRotation(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Rotation", DefaultRotation(ref pr), value);

		private static float DefaultBloom(ref ParseResult pr) => pr.definition.Bloom;
		internal static float GetDefaultBloom(MyFunctionalBlock block) => DefaultBloom(ref Parse(block, out _));
		internal static float GetBloom(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Bloom", DefaultBloom(ref pr));
		internal static void SetBloom(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Bloom", DefaultBloom(ref pr), value);

		private static float DefaultIntensity(ref ParseResult pr) => pr.definition.Intensity;
		internal static float GetDefaultIntensity(MyFunctionalBlock block) => DefaultIntensity(ref Parse(block, out _));
		internal static float GetIntensity(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Intensity", DefaultIntensity(ref pr));
		internal static void SetIntensity(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Intensity", DefaultIntensity(ref pr), value);

		private static float DefaultMix(ref ParseResult pr) => pr.definition.Mix;
		internal static float GetDefaultMix(MyFunctionalBlock block) => DefaultMix(ref Parse(block, out _)) * 100;
		internal static float GetMix(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "Mix", DefaultMix(ref pr)) * 100;
		internal static void SetMix(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Mix", DefaultMix(ref pr), value / 100);

		internal static string GetTexture(MyFunctionalBlock block) => GetString(ref Parse(block, out _), "Texture", "");
		internal static void SetTexture(MyFunctionalBlock block, string value) => SetString(ref Parse(block, out _, true), "Texture", "", value);

		private static float DefaultTextureRotation(ref ParseResult pr) => pr.definition.TextureRotation;
		internal static float GetDefaultTextureRotation(MyFunctionalBlock block) => DefaultTextureRotation(ref Parse(block, out _));
		internal static float GetTextureRotation(MyFunctionalBlock block) => GetFloat(ref Parse(block, out var pr), "TextureRotation", DefaultTextureRotation(ref pr));
		internal static void SetTextureRotation(MyFunctionalBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "TextureRotation", DefaultTextureRotation(ref pr), value);
	}

	// Here we update make sure that any configuration done in the custom data of a lighting block is applied after a
	// change. I chose "SetCustomData_Internal()" instead of patching the "CustomData" setter, because the latter is
	// inlined on release mode.
	[HarmonyPatch(typeof(MyTerminalBlock), "SetCustomData_Internal")]
	static class Patch_MyTerminalBlock_SetCustomData_Internal
	{
		internal static void Postfix(MyTerminalBlock __instance)
		{
			if (__instance is MyLightingBlock)
			{
				// All the different lighting blocks (interior, spot, search, heat vent) conveniently have a private
				// "m_lightingLogic" field somewhere that encapsulates the bulk of work dealing with 3D models and their light
				// fixtures as well as reacting to changes to the sliders in the terminal.
				var lightingLogicField = __instance.GetType().GetField("m_lightingLogic", BindingFlags.Instance | BindingFlags.NonPublic);
				var logic = (MyLightingLogic)lightingLogicField?.GetValue(__instance);
				if (logic != null)
				{
					Patch_MyLightingLogic_RecreateLights.FixupLightingLogic(logic, false);
					__instance.RaisePropertiesChanged();
				}
			}
		}
	}

	static class TerminalControls
	{
		internal static readonly KeyValuePair<MyStringId, string>[] TEXTURES;

		internal static readonly List<IMyTerminalControl> s_terminalControls = new List<IMyTerminalControl>();

		static TerminalControls()
		{
			var textures = new List<KeyValuePair<MyStringId, string>>
			{
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Default"), ""),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Narrow Spot"), @"Textures\SunGlare\SunFlareWhiteAnamorphic.DDS"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Medium Spot"), @"Textures\Particles\AnamorphicFlare.DDS"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Wide Spot"), @"Textures\Particles\Firefly.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Soft Circle"), @"Textures\SunGlare\SunCircle.DDS"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Hard Circle"), @"Textures\GUI\Indicators\EnemyIndicator02.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Soft Glare"), @"Textures\Particles\GlareLsInteriorLight.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Hard Glare"), @"Textures\Particles\particle_glare.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Rays"), @"Textures\Particles\LightRay.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Barred"), @"Textures\Lights\reflector_large.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Grated"), @"Textures\Lights\reflector_2.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Two Spots Merged"), @"Textures\Lights\dual_reflector.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Two Spots Refracted"), @"Textures\Lights\dual_reflector_2.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Two Spots"), @"Textures\Lights\dual_reflector_3.dds"),
				new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Directional"), @"Textures\Particles\SciFiEngineThrustMiddle.DDS"),
				new KeyValuePair<MyStringId, string>(MySpaceTexts.DisplayName_Faction_SpacePirates_Translation, @"Textures\FactionLogo\PirateIcon.dds"),
				new KeyValuePair<MyStringId, string>(MySpaceTexts.DisplayName_Faction_SpaceSpiders_Translation, @"Textures\FactionLogo\Spiders.dds"),
				new KeyValuePair<MyStringId, string>(MySpaceTexts.DisplayName_Faction_Factorum_Translation, @"Textures\FactionLogo\Factorum.dds"),
				new KeyValuePair<MyStringId, string>(MySpaceTexts.DisplayName_Faction_Unknown_Translation, @"Textures\FactionLogo\Unknown.dds"),
			};
			for (int i = 1; i <= 4; i++)
			{
				textures.Add(new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Miner " + i.ToString()), @"Textures\FactionLogo\Miners\MinerIcon_" + i.ToString() + ".dds"));
			}
			for (int i = 1; i <= 5; i++)
			{
				textures.Add(new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Trader " + i.ToString()), @"Textures\FactionLogo\Traders\TraderIcon_" + i.ToString() + ".dds"));
			}
			for (int i = 1; i <= 16; i++)
			{
				textures.Add(new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Builder " + i.ToString()), @"Textures\FactionLogo\Builders\BuilderIcon_" + i.ToString() + ".dds"));
			}
			for (int i = 1; i <= 33; i++)
			{
				if (i != 25)
				{
					textures.Add(new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("Other " + i.ToString()), @"Textures\FactionLogo\Others\OtherIcon_" + i.ToString() + ".dds"));
				}
			}
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Cross, @"Textures\Models\Cross.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Danger, @"Textures\Models\DangerZone.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Construction, @"Textures\Models\UnderConstruction.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Angry, @"Textures\Models\Emotes\Angry.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Annoyed, @"Textures\Models\Emotes\Annoyed.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Confused, @"Textures\Models\Emotes\Confused.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Crying, @"Textures\Models\Emotes\Crying.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Dead, @"Textures\Models\Emotes\Dead.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Evil, @"Textures\Models\Emotes\Evil.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Happy, @"Textures\Models\Emotes\Happy.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Love, @"Textures\Models\Emotes\Love.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Neutral, @"Textures\Models\Emotes\Neutral.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Sad, @"Textures\Models\Emotes\Sad.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Shocked, @"Textures\Models\Emotes\Shocked.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Skeptical, @"Textures\Models\Emotes\Skeptical.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Sleepy, @"Textures\Models\Emotes\Sleepy.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Suspicious_Left, @"Textures\Models\Emotes\Suspicious_Left.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Suspicious_Right, @"Textures\Models\Emotes\Suspicious_Right.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MySpaceTexts.LCD_Emote_Wink, @"Textures\Models\Emotes\Wink.dds"));
			textures.Add(new KeyValuePair<MyStringId, string>(MyStringId.GetOrCompute("(Customized)"), ""));
			TEXTURES = textures.ToArray();

			s_terminalControls.Add(new MyTerminalControlSeparator<MyFunctionalBlock>());

			s_terminalControls.Add(new MyTerminalControlOnOffSwitch<MyFunctionalBlock>("ProjectedLightsEnabled", MySpaceTexts.DisplayName_Block_ReflectorLight)
			{
				Getter = IniHandler.GetEnabled,
				Setter = (x, v) => { if (IniHandler.SetEnabled(x, v)) { x.RaisePropertiesChanged(); } },
			});

			var mixSlider = new MyTerminalControlSlider<MyFunctionalBlock>("Mix", MySpaceTexts.ToolbarAction_Decrease, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultMix,
				Getter = IniHandler.GetMix,
				Setter = IniHandler.SetMix,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetMix(x), 0).Append("%"),
			};
			mixSlider.SetLimits(0, 100);
			s_terminalControls.Add(mixSlider);

			var bloomSlider = new MyTerminalControlSlider<MyFunctionalBlock>("Bloom", MyStringId.GetOrCompute("Bloom"), MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultBloom,
				Getter = IniHandler.GetBloom,
				Setter = IniHandler.SetBloom,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetBloom(x), 1),
			};
			bloomSlider.SetLogLimits(0.1f, 50);
			s_terminalControls.Add(bloomSlider);

			var coneAngleSlider = new MyTerminalControlSlider<MyFunctionalBlock>("ConeAngle", MySpaceTexts.BlockPropertiesText_MotorCurrentAngle, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultConeAngle,
				Getter = IniHandler.GetConeAngle,
				Setter = IniHandler.SetConeAngle,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetConeAngle(x), 1).Append(" °"),
			};
			coneAngleSlider.SetLimits(0, 180);
			s_terminalControls.Add(coneAngleSlider);

			s_terminalControls.Add(new MyTerminalControlCombobox<MyFunctionalBlock>("Texture", MySpaceTexts.BlockPropertyTitle_LCDScreenDefinitionsTextures, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				ComboBoxContent = (list) =>
				{
					for (int i = 0; i < TEXTURES.Length; i++)
					{
						list.Add(new VRage.ModAPI.MyTerminalControlComboBoxItem { Key = i, Value = TEXTURES[i].Key });
					}
				},
				Getter = (x) =>
				{
					var texturePath = IniHandler.GetTexture(x);
					if (texturePath == "") return 0;
					for (int i = 1; i < TEXTURES.Length - 1; i++)
					{
						if (TEXTURES[i].Value == texturePath)
						{
							return i;
						}
					}
					return TEXTURES.Length - 1;
				},
				Setter = (x, v) =>
				{
					if (v != TEXTURES.Length - 1)
					{
						IniHandler.SetTexture(x, TEXTURES[v].Value);
					}
				},
			});

			var rotationSlider = new MyTerminalControlSlider<MyFunctionalBlock>("Rotation", MySpaceTexts.HelpScreen_ControllerRotation_Pitch, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultRotation,
				Getter = IniHandler.GetRotation,
				Setter = IniHandler.SetRotation,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetRotation(x), 1).Append(" °"),
			};
			rotationSlider.SetLimits(-180, +180);
			s_terminalControls.Add(rotationSlider);

			var forwardSlider = new MyTerminalControlSlider<MyFunctionalBlock>("Forward", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetZ, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultForward,
				Getter = IniHandler.GetForward,
				Setter = IniHandler.SetForward,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetForward(x), 2).Append(" m"),
			};
			forwardSlider.SetLimits(-5, +5);
			s_terminalControls.Add(forwardSlider);

			var leftSlider = new MyTerminalControlSlider<MyFunctionalBlock>("Left", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetX, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultLeft,
				Getter = IniHandler.GetLeft,
				Setter = IniHandler.SetLeft,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetLeft(x), 2).Append(" m"),
			};
			leftSlider.SetLimits(-5, +5);
			s_terminalControls.Add(leftSlider); ;

			var upSlider = new MyTerminalControlSlider<MyFunctionalBlock>("Up", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetY, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultUp,
				Getter = IniHandler.GetUp,
				Setter = IniHandler.SetUp,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetUp(x), 2).Append(" m"),
			};
			upSlider.SetLimits(-5, +5);
			s_terminalControls.Add(upSlider); ;

			var textureRotationSlider = new MyTerminalControlSlider<MyFunctionalBlock>("TextureRotation", MySpaceTexts.HelpScreen_ControllerRotation_Roll, MySpaceTexts.Blank)
			{
				Enabled = IniHandler.GetEnabled,
				DefaultValueGetter = IniHandler.GetDefaultTextureRotation,
				Getter = IniHandler.GetTextureRotation,
				Setter = IniHandler.SetTextureRotation,
				Writer = (x, result) => result.AppendDecimal(IniHandler.GetTextureRotation(x), 1).Append(" °"),
			};
			textureRotationSlider.SetLimits(-180, +180);
			s_terminalControls.Add(textureRotationSlider);

			s_terminalControls.Add(new MyTerminalControlOnOffSwitch<MyFunctionalBlock>("CastShadows", MySpaceTexts.PlayerCharacterColorDefault)
			{
				Enabled = IniHandler.GetEnabled,
				Getter = IniHandler.GetCastShadows,
				Setter = IniHandler.SetCastShadows,
			});

			var shadowRangeSlider = new MyTerminalControlSlider<MyFunctionalBlock>("ShadowRange", MySpaceTexts.Beacon_SafeZone_RangeSlider, MySpaceTexts.Blank)
			{
				Enabled = (x) => IniHandler.GetEnabled(x) && IniHandler.GetCastShadows(x),
				DefaultValueGetter = IniHandler.GetDefaultShadowRange,
				Getter = IniHandler.GetShadowRange,
				Setter = IniHandler.SetShadowRange,
				Writer = (x, result) => result.AppendDecimal((float)IniHandler.GetShadowRange(x), 1).Append(" m"),
			};
			shadowRangeSlider.SetLogLimits(1, 3000);
			s_terminalControls.Add(shadowRangeSlider);
		}

		internal static void AddTerminalControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
		{
			if (block is MyInteriorLight)
			{
				controls.AddList(s_terminalControls);
			}
		}
	}
}
