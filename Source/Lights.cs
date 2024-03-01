using HarmonyLib;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.Lights;
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
using VRageMath;
using VRageRender;
using VRageRender.Messages;

struct LightDefinition
{
	public LightDefinition(bool enabled)
	{
		Disabled = !enabled;
		Texture = @"Textures\Particles\GlareLsInteriorLight.dds";
		SpotTexture = @"Textures\SunGlare\SunCircle.DDS";
		TextureRotation = 90;
		ConeAngle = 178;
		Bloom = 5;
		Intensity = 3;
		Rotation = Forward = Left = 0;
		Mix = 0;
		CastShadows = false;
	}

	internal static LightDefinition s_generic = new LightDefinition(true);
	internal static Dictionary<string, LightDefinition> s_dict = new Dictionary<string, LightDefinition>()
	{
		// Large block
		["LargeBlockInsetLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Lights\reflector_2.dds",
			ConeAngle = 163, Forward = +0.813f, Rotation = 0,
			Bloom = 15, Intensity = 7, Mix = 0.06f,
		},
		["SmallLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\Firefly.dds",
			ConeAngle = 157, Forward = -1.122f,
			Bloom = 20, Intensity = 5, Mix = 0.1f,
		},
		["LargeBlockLight_1corner"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\particle_glare.dds",
			ConeAngle = 173, Rotation = -45, TextureRotation = 28, Forward = -1.54f,
			Bloom = 10, Intensity = 5, Mix = 0.03f,
		},
		["LargeBlockLight_2corner"] = new LightDefinition(true)
		{
			Texture = @"Textures\Lights\dual_reflector_2.dds",
			ConeAngle = 170, Forward = -1.249f, TextureRotation = 0,
			Bloom = 10, Intensity = 7, Mix = 0.15f,
		},
		["LargeLightPanel"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
			ConeAngle = 178, Forward = -1.155f,
			Bloom = 3, Intensity = 9,
		},
		["PassageSciFiLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Lights\reflector_2.dds", SpotTexture = @"Textures\Lights\reflector_2.dds",
			ConeAngle = 141, Forward = -1.032f,
			Bloom = 5, Intensity = 6,
		},
		["AirDuctLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\particle_glare.dds", SpotTexture = @"Textures\Particles\particle_glare.dds",
			ConeAngle = 154, Forward = -0.249f, Rotation = 90, TextureRotation = 0,
			Bloom = 100, Intensity = 10, Mix = 1,
		},
		["LargeBlockInsetAquarium"] = new LightDefinition(true)
		{
			SpotTexture = @"Textures\SunGlare\SunCircle.DDS", Texture = @"Textures\SunGlare\SunCircle.DDS",
			ConeAngle = 150, Forward = -0.96f, Left = -0.75f, Rotation = 50,
			Bloom = 0.5f, Intensity = 2.5f, Mix = 0.4f, CastShadows = true,
		},
		["LargeBlockInsetKitchen"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
			ConeAngle = 179, Forward = -0.265f, Left = -0.95f, Rotation = -3,
			Bloom = 3, Intensity = 3, Mix = 0.25f,
		},
		// Small block
		["SmallBlockInsetLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Lights\reflector_2.dds",
			ConeAngle = 163, Forward = +0.13f, Rotation = -2,
			Bloom = 15, Intensity = 3, Mix = 0.1f,
		},
		["SmallBlockSmallLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Lights\reflector_2.dds",
			ConeAngle = 163, Forward = -1.123f,
			Bloom = 10, Intensity = 3, Mix = 0.1f,
		},
		["SmallBlockLight_1corner"] = new LightDefinition(true)
		{
			Texture = @"Textures\Lights\reflector_2.dds",
			ConeAngle = 163, Rotation = 45, Forward = -0.27f,
			Bloom = 21.3f, Intensity = 4,
		},
		["SmallBlockLight_2corner"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
			ConeAngle = 178, Forward = -0.249f,
			Bloom = 13, Intensity = 1.5f, Mix = 0.1f,
		},
		["OffsetLight"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\LightRay.dds", SpotTexture = @"Textures\Particles\LightRay.dds",
			ConeAngle = 114, Forward = -0.249f,
			Bloom = 200, Intensity = 10, Mix = 0.05f,
		},
		["SmallLightPanel"] = new LightDefinition(true)
		{
			Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
			ConeAngle = 175.8f, Forward = -0.249f, TextureRotation = 130,
			Bloom = 4.6f, Intensity = 9,
		},
	};

	/// <summary>Marker flag that just disables processing by this plugin for a light.</summary>
	internal bool Disabled;
	/// <summary>Relative projected texture file name inside the Content directory. Only the red channel is used.</summary>
	internal string Texture;
	/// <summary>Texture used when casting shadows. Usually this one will have a bigger light spot.</summary>
	internal string SpotTexture;
	/// <summary>How much the texture itself is rotated.</summary>
	internal float TextureRotation;
	/// <summary>Angle of the projected light cone. 136° is the largest that renders reliably when shadows are cast.</summary>
	internal float ConeAngle;
	/// <summary>Bloom multiplier.</summary>
	internal float Bloom;
	/// <summary>Extra intensity coefficient</summary>
	internal float Intensity;
	/// <summary>How far the light source is moved forward to prevent the model itself from casting shadows.</summary>
	internal float Forward;
	/// <summary>How far the light source is moved left to prevent the model itself from casting shadows.</summary>
	internal float Left;
	/// <summary>How far the light is rotated. Useful for corner lights, to point them 45° away from "forward".</summary>
	internal float Rotation;
	/// <summary>How much of the point light we want to keep (linear interpolation 0 to 1). Setting to 0 removes point light for performance.</summary>
	internal float Mix;
	/// <summary>Whether this light should cast shadows by default.</summary>
	internal bool CastShadows;

	/// <summary>Multiplier for the bloom caused by emissive materials. Setting this higher than ~17.4, will cause some LCDs to turn dark due to 8-bit rounding to 0.</summary>
	internal const float EMISSIVE_BOOST = 17.4f;
	internal const float EMISSIVE_BOOST_INV = 1 / EMISSIVE_BOOST;
	/// <summary>Drops the intensity of the gloss effect.</summary>
	internal const float GLOSS_FACTOR = 0.2f;
}

enum LightFlags : uint {
	handledByUs = 0x80000000,
}

internal static class MyExtensions
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

internal static class IniHandler
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
		result = new ParseResult {
			block = block,
			definition = LightDefinition.s_dict.TryGetValue(block.BlockDefinition.Id.SubtypeName, out var definition) ? definition : LightDefinition.s_generic,
			ini = (forUpdating ? ini.TryParse(block.CustomData) : ini.TryParse(block.CustomData, INI_SECTION)) ? ini : null
		};
		return ref result;
	}

	internal static LightDefinition GetFullDefinition(MyLightingBlock block, MyLightingLogic logic)
	{
		Parse(block, out var pr);
		pr.definition.Disabled = !GetBool(ref pr, "Enabled", DefaultEnabled(ref pr));
		pr.definition.CastShadows = GetBool(ref pr, "CastShadows", DefaultCastShadows(ref pr));
		pr.definition.Texture = GetString(ref pr, "Texture", pr.definition.CastShadows ? pr.definition.SpotTexture : pr.definition.Texture);
		pr.definition.SpotTexture = pr.definition.Texture;
		pr.definition.TextureRotation = GetFloat(ref pr, "TextureRotation", DefaultTextureRotation(ref pr));
		pr.definition.ConeAngle = Math.Min(GetFloat(ref pr, "ConeAngle", DefaultConeAngle(ref pr)), pr.definition.CastShadows ? 136 : 360);
		pr.definition.Bloom = GetFloat(ref pr, "Bloom", DefaultBloom(ref pr));
		pr.definition.Intensity = GetFloat(ref pr, "Intensity", pr.definition.Intensity);
		pr.definition.Forward = GetFloat(ref pr, "Forward", pr.definition.Forward);
		pr.definition.Left = GetFloat(ref pr, "Left", pr.definition.Left);
		pr.definition.Rotation = GetFloat(ref pr, "Rotation", pr.definition.Rotation);
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
	internal static bool GetEnabled(MyLightingBlock block) => GetBool(ref Parse(block, out var pr), "Enabled", DefaultEnabled(ref pr));
	internal static bool SetEnabled(MyLightingBlock block, bool value) => SetBool(ref Parse(block, out var pr, true), "Enabled", DefaultEnabled(ref pr), value);

	private static bool DefaultCastShadows(ref ParseResult pr) => pr.definition.CastShadows;
	internal static bool GetCastShadows(MyLightingBlock block) => GetBool(ref Parse(block, out var pr), "CastShadows", DefaultCastShadows(ref pr));
	internal static void SetCastShadows(MyLightingBlock block, bool value) => SetBool(ref Parse(block, out var pr, true), "CastShadows", DefaultCastShadows(ref pr), value);

	private static float DefaultConeAngle(ref ParseResult pr) => pr.definition.ConeAngle;
	internal static float GetDefaultConeAngle(MyLightingBlock block) => DefaultConeAngle(ref Parse(block, out _));
	internal static float GetConeAngle(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "ConeAngle", DefaultConeAngle(ref pr));
	internal static void SetConeAngle(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "ConeAngle", DefaultConeAngle(ref pr), value);

	private static float DefaultForward(ref ParseResult pr) => pr.definition.Forward;
	internal static float GetDefaultForward(MyLightingBlock block) => DefaultForward(ref Parse(block, out _));
	internal static float GetForward(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "Forward", DefaultForward(ref pr));
	internal static void SetForward(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Forward", DefaultForward(ref pr), value);

	private static float DefaultLeft(ref ParseResult pr) => pr.definition.Left;
	internal static float GetDefaultLeft(MyLightingBlock block) => DefaultLeft(ref Parse(block, out _));
	internal static float GetLeft(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "Left", DefaultLeft(ref pr));
	internal static void SetLeft(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Left", DefaultLeft(ref pr), value);

	private static float DefaultRotation(ref ParseResult pr) => pr.definition.Rotation;
	internal static float GetDefaultRotation(MyLightingBlock block) => DefaultRotation(ref Parse(block, out _));
	internal static float GetRotation(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "Rotation", DefaultRotation(ref pr));
	internal static void SetRotation(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Rotation", DefaultRotation(ref pr), value);

	private static float DefaultBloom(ref ParseResult pr) => pr.definition.Bloom;
	internal static float GetDefaultBloom(MyLightingBlock block) => DefaultBloom(ref Parse(block, out _));
	internal static float GetBloom(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "Bloom", DefaultBloom(ref pr));
	internal static void SetBloom(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Bloom", DefaultBloom(ref pr), value);

	private static float DefaultIntensity(ref ParseResult pr) => pr.definition.Intensity;
	internal static float GetDefaultIntensity(MyLightingBlock block) => DefaultIntensity(ref Parse(block, out _));
	internal static float GetIntensity(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "Intensity", DefaultIntensity(ref pr));
	internal static void SetIntensity(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Intensity", DefaultIntensity(ref pr), value);

	private static float DefaultMix(ref ParseResult pr) => pr.definition.Mix;
	internal static float GetDefaultMix(MyLightingBlock block) => DefaultMix(ref Parse(block, out _));
	internal static float GetMix(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "Mix", DefaultMix(ref pr)) * 100;
	internal static void SetMix(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "Mix", DefaultMix(ref pr), value / 100);

	internal static string GetTexture(MyLightingBlock block) => GetString(ref Parse(block, out _), "Texture", "");
	internal static void SetTexture(MyLightingBlock block, string value) => SetString(ref Parse(block, out _, true), "Texture", "", value);

	private static float DefaultTextureRotation(ref ParseResult pr) => pr.definition.TextureRotation;
	internal static float GetDefaultTextureRotation(MyLightingBlock block) => DefaultTextureRotation(ref Parse(block, out _));
	internal static float GetTextureRotation(MyLightingBlock block) => GetFloat(ref Parse(block, out var pr), "TextureRotation", DefaultTextureRotation(ref pr));
	internal static void SetTextureRotation(MyLightingBlock block, float value) => SetFloat(ref Parse(block, out var pr, true), "TextureRotation", DefaultTextureRotation(ref pr), value);
}

internal static class TerminalControls
{
	internal static readonly KeyValuePair<string, string>[] TEXTURES = {
		new KeyValuePair<string, string>("Default", ""),
		new KeyValuePair<string, string>("Narrow Spot", @"Textures\SunGlare\SunFlareWhiteAnamorphic.DDS"),
		new KeyValuePair<string, string>("Medium Spot", @"Textures\Particles\AnamorphicFlare.DDS"),
		new KeyValuePair<string, string>("Wide Spot", @"Textures\Particles\Firefly.dds"),
		new KeyValuePair<string, string>("Soft Circle", @"Textures\SunGlare\SunCircle.DDS"),
		new KeyValuePair<string, string>("Hard Circle", @"Textures\GUI\Indicators\EnemyIndicator02.dds"),
		new KeyValuePair<string, string>("Soft Glare", @"Textures\Particles\GlareLsInteriorLight.dds"),
		new KeyValuePair<string, string>("Hard Glare", @"Textures\Particles\particle_glare.dds"),
		new KeyValuePair<string, string>("Rays", @"Textures\Particles\LightRay.dds"),
		new KeyValuePair<string, string>("Grated", @"Textures\Lights\reflector_large.dds"),
		new KeyValuePair<string, string>("Two Spots Merged", @"Textures\Lights\dual_reflector.dds"),
		new KeyValuePair<string, string>("Two Spots Refracted", @"Textures\Lights\dual_reflector_2.dds"),
		new KeyValuePair<string, string>("Two Spots", @"Textures\Lights\dual_reflector_3.dds"),
		new KeyValuePair<string, string>("Directional", @"Textures\Particles\SciFiEngineThrustMiddle.DDS"),
		new KeyValuePair<string, string>("(Customized)", ""),
	};

	internal static readonly List<IMyTerminalControl> s_terminalControls = new List<IMyTerminalControl>();

	static TerminalControls()
	{
		s_terminalControls.Add(new MyTerminalControlOnOffSwitch<MyInteriorLight>("ProjectedLightsEnabled", MySpaceTexts.DisplayName_Block_ReflectorLight)
		{
			Getter = IniHandler.GetEnabled,
			Setter = (x, v) => { if (IniHandler.SetEnabled(x, v)) { x.RaisePropertiesChanged(); } },
		});

		var mixSlider = new MyTerminalControlSlider<MyInteriorLight>("Mix", MySpaceTexts.BlockPropertyTitle_Scale, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultMix,
			Getter = IniHandler.GetMix,
			Setter = IniHandler.SetMix,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetMix(x), 0).Append("%"),
		};
		mixSlider.SetLimits(0, 100);
		s_terminalControls.Add(mixSlider);

		var bloomSlider = new MyTerminalControlSlider<MyInteriorLight>("Bloom", MyStringId.GetOrCompute("Bloom"), MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultBloom,
			Getter = IniHandler.GetBloom,
			Setter = IniHandler.SetBloom,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetBloom(x), 1),
		};
		bloomSlider.SetLogLimits(0.5f, 200);
		s_terminalControls.Add(bloomSlider);

		s_terminalControls.Add(new MyTerminalControlOnOffSwitch<MyInteriorLight>("CastShadows", MySpaceTexts.PlayerCharacterColorDefault)
		{
			Enabled = IniHandler.GetEnabled,
			Getter = IniHandler.GetCastShadows,
			Setter = IniHandler.SetCastShadows,
		});

		var coneAngleSlider = new MyTerminalControlSlider<MyInteriorLight>("ConeAngle", MySpaceTexts.BlockPropertiesText_MotorCurrentAngle, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultConeAngle,
			Getter = IniHandler.GetConeAngle,
			Setter = IniHandler.SetConeAngle,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetConeAngle(x), 1).Append(" °"),
		};
		coneAngleSlider.SetLimits(0, 180);
		s_terminalControls.Add(coneAngleSlider);

		s_terminalControls.Add(new MyTerminalControlCombobox<MyInteriorLight>("Texture", MySpaceTexts.BlockPropertyTitle_LCDScreenDefinitionsTextures, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			ComboBoxContent = (list) =>
			{
				for (int i = 0; i < TEXTURES.Length; i++)
				{
					list.Add(new VRage.ModAPI.MyTerminalControlComboBoxItem { Key = i, Value = MyStringId.GetOrCompute(TEXTURES[i].Key) });
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

		var textureRotationSlider = new MyTerminalControlSlider<MyInteriorLight>("TextureRotation", MySpaceTexts.HelpScreen_ControllerRotation_Roll, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultTextureRotation,
			Getter = IniHandler.GetTextureRotation,
			Setter = IniHandler.SetTextureRotation,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetTextureRotation(x), 1).Append(" °"),
		};
		textureRotationSlider.SetLimits(-180, +180);
		s_terminalControls.Add(textureRotationSlider);

		var rotationSlider = new MyTerminalControlSlider<MyInteriorLight>("Rotation", MySpaceTexts.HelpScreen_ControllerRotation_Pitch, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultRotation,
			Getter = IniHandler.GetRotation,
			Setter = IniHandler.SetRotation,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetRotation(x), 1).Append(" °"),
		};
		rotationSlider.SetLimits(-180, +180);
		s_terminalControls.Add(rotationSlider);

		var forwardSlider = new MyTerminalControlSlider<MyInteriorLight>("Forward", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetZ, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultForward,
			Getter = IniHandler.GetForward,
			Setter = IniHandler.SetForward,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetForward(x), 2).Append(" m"),
		};
		forwardSlider.SetLimits(-5, +5);
		s_terminalControls.Add(forwardSlider);

		var leftSlider = new MyTerminalControlSlider<MyInteriorLight>("Left", MySpaceTexts.BlockPropertyTitle_ProjectionOffsetX, MySpaceTexts.Blank)
		{
			Enabled = IniHandler.GetEnabled,
			DefaultValueGetter = IniHandler.GetDefaultLeft,
			Getter = IniHandler.GetLeft,
			Setter = IniHandler.SetLeft,
			Writer = (x, result) => result.AppendDecimal(IniHandler.GetLeft(x), 2).Append(" m"),
		};
		leftSlider.SetLimits(-5, +5);
		s_terminalControls.Add(leftSlider); ;
	}

	internal static void AddTerminalControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
	{
		if (block is MyInteriorLight)
		{
			controls.AddList(s_terminalControls);
		}
	}
}

// Here we update make sure that any configuration done in the custom data of a lighting block is applied after a
// change. I chose "SetCustomData_Internal()" instead of patching the "CustomData" setter, because the latter is
// inlined on release mode.
[HarmonyPatch(typeof(MyTerminalBlock), "SetCustomData_Internal")]
internal static class Patch_MyTerminalBlock_SetCustomData_Internal
{
	internal static void Postfix(MyTerminalBlock __instance)
	{
		if (__instance is MyLightingBlock || __instance is MySearchlight || __instance is MyHeatVentBlock)
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

// Pasting grids or welding projected interior lights normally enables the glare, so we disable this method.
[HarmonyPatch(typeof(MyInteriorLight), "UpdateGlare")]
internal static class Patch_MyInteriorLight_UpdateGlare
{
	internal static bool Prefix(MyInteriorLight __instance)
	{
		return !__instance.LightingLogic().IsLightHandledByUs();
	}
}

// Here we clone the original, but omit the glare and make the light cone brighter the more focused it is.
// We also update the emissive material, because we now incorporate the lights intensity into emissivity.
[HarmonyPatch(typeof(MyInteriorLight), "UpdateIntensity")]
internal static class Patch_MyInteriorLight_UpdateIntensity
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

[HarmonyPatch(typeof(MyLightingLogic), nameof(MyLightingLogic.ComputeBulbColor))]
internal static class Patch_MyLightingLogic_ComputeBulbColor
{
	internal static bool Prefix(MyLightingLogic __instance, ref Color __result)
	{
		if (__instance.IsLightHandledByUs())
		{
			var color = __instance.Color;
			byte r = (byte)(Math.Sqrt(color.R / 255.0) * 101 + 1);
			byte g = (byte)(Math.Sqrt(color.G / 255.0) * 101 + 1);
			byte b = (byte)(Math.Sqrt(color.B / 255.0) * 101 + 1);
			__result = new Color(r, g, b);
			return false;
		}
		return true;
	}
}

[HarmonyPatch(typeof(MyLightingLogic), "UpdateLightProperties")]
internal static class Patch_MyLightingLogic_UpdateLightProperties
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
		AccessTools.PropertySetter(myInstanceMaterialType, "Emissivity").Invoke(material, new object[] {LightDefinition.EMISSIVE_BOOST_INV });
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

// Finally, we patch the light logic to set higher emissive values on demand.
[HarmonyPatch(typeof(MyLightingLogic), "UpdateEmissiveMaterial", new Type[] { typeof(uint) })]
internal static class Patch_MyLightingLogic_UpdateEmissiveMaterial
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
				var intensity = (blinkOn ? __instance.CurrentLightPower * __instance.Intensity * __instance.Lights[0].GlareMaxDistance : 0);
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
internal static class Patch_MyLightingLogic_RecreateLights
{
	private static FieldInfo s_blockField = AccessTools.DeclaredField(typeof(MyLightingLogic), "m_block");

	internal static void FixupLightingLogic(MyLightingLogic logic, bool isInitialCreation)
	{
		var block = (MyFunctionalBlock)s_blockField.GetValue(logic);
		var definition = IniHandler.GetFullDefinition((MyLightingBlock)block, logic);

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
			logic.Initialize();
			logic.UpdateParents();
			logic.IsEmissiveMaterialDirty = true;
			logic.UpdateEmissiveMaterial();
			logic.UpdateLightProperties();
		}
	}

	internal static void Postfix(MyLightingLogic __instance)
	{
		FixupLightingLogic(__instance, true);
	}
}

// This patch replaces the turn on/off logic for when the light is blinking.
[HarmonyPatch(typeof(MyInteriorLight), "UpdateEnabled")]
internal static class Patch_MyInteriorLight_UpdateEnabled
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
