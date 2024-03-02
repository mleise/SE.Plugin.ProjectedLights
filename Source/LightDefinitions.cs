using System.Collections.Generic;

namespace mleise.ProjectedLightsPlugin
{
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
				ConeAngle = 163,
				Forward = +0.813f,
				Rotation = 0,
				Bloom = 15,
				Intensity = 7,
				Mix = 0.06f,
			},
			["SmallLight"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\Firefly.dds",
				ConeAngle = 157,
				Forward = -1.122f,
				Bloom = 20,
				Intensity = 5,
				Mix = 0.1f,
			},
			["LargeBlockLight_1corner"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\particle_glare.dds",
				ConeAngle = 173,
				Rotation = -45,
				TextureRotation = 28,
				Forward = -1.54f,
				Bloom = 10,
				Intensity = 5,
				Mix = 0.03f,
			},
			["LargeBlockLight_2corner"] = new LightDefinition(true)
			{
				Texture = @"Textures\Lights\dual_reflector_2.dds",
				ConeAngle = 170,
				Forward = -1.249f,
				TextureRotation = 0,
				Bloom = 10,
				Intensity = 7,
				Mix = 0.15f,
			},
			["LargeLightPanel"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
				ConeAngle = 178,
				Forward = -1.155f,
				Bloom = 3,
				Intensity = 9,
			},
			["PassageSciFiLight"] = new LightDefinition(true)
			{
				Texture = @"Textures\Lights\reflector_2.dds",
				SpotTexture = @"Textures\Lights\reflector_2.dds",
				ConeAngle = 141,
				Forward = -1.032f,
				Bloom = 5,
				Intensity = 6,
			},
			["AirDuctLight"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\particle_glare.dds",
				SpotTexture = @"Textures\Particles\particle_glare.dds",
				ConeAngle = 154,
				Forward = -0.249f,
				Rotation = 90,
				TextureRotation = 0,
				Bloom = 100,
				Intensity = 10,
				Mix = 1,
			},
			["LargeBlockInsetAquarium"] = new LightDefinition(true)
			{
				SpotTexture = @"Textures\SunGlare\SunCircle.DDS",
				Texture = @"Textures\SunGlare\SunCircle.DDS",
				ConeAngle = 150,
				Forward = -0.96f,
				Left = -0.75f,
				Rotation = 50,
				Bloom = 0.5f,
				Intensity = 2.5f,
				Mix = 0.4f,
				CastShadows = true,
			},
			["LargeBlockInsetKitchen"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
				ConeAngle = 179,
				Forward = -0.265f,
				Left = -0.95f,
				Rotation = -3,
				Bloom = 3,
				Intensity = 3,
				Mix = 0.25f,
			},
			// Small block
			["SmallBlockInsetLight"] = new LightDefinition(true)
			{
				Texture = @"Textures\Lights\reflector_2.dds",
				ConeAngle = 163,
				Forward = +0.13f,
				Rotation = -2,
				Bloom = 15,
				Intensity = 3,
				Mix = 0.1f,
			},
			["SmallBlockSmallLight"] = new LightDefinition(true)
			{
				Texture = @"Textures\Lights\reflector_2.dds",
				ConeAngle = 163,
				Forward = -1.123f,
				Bloom = 10,
				Intensity = 3,
				Mix = 0.1f,
			},
			["SmallBlockLight_1corner"] = new LightDefinition(true)
			{
				Texture = @"Textures\Lights\reflector_2.dds",
				ConeAngle = 163,
				Rotation = 45,
				Forward = -0.27f,
				Bloom = 21.3f,
				Intensity = 4,
			},
			["SmallBlockLight_2corner"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
				ConeAngle = 178,
				Forward = -0.249f,
				Bloom = 13,
				Intensity = 1.5f,
				Mix = 0.1f,
			},
			["OffsetLight"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\LightRay.dds",
				SpotTexture = @"Textures\Particles\LightRay.dds",
				ConeAngle = 114,
				Forward = -0.249f,
				Bloom = 200,
				Intensity = 10,
				Mix = 0.05f,
			},
			["SmallLightPanel"] = new LightDefinition(true)
			{
				Texture = @"Textures\Particles\GlareLsInteriorLight.dds",
				ConeAngle = 175.8f,
				Forward = -0.249f,
				TextureRotation = 130,
				Bloom = 4.6f,
				Intensity = 9,
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
}
