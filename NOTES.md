#### Intensity changes

When the game loads, the `MyLightingLogic` has a `CurrentLightPower` of `0` that increases by 5% each frame. The logic's parent light block (except for the heat vent) register a callback to a private method, always named `UpdateIntensity()` that the logic then calls to put the new resulting intensity into effect.
Overall the following properties may be overwritten by a change in `CurrentLightPower`:

- `Logic.Lights[n].Intensity`

- `Logic.Lights[n].ReflectorIntensity` (only spot/search lights)

- `Logic.GlareIntensity`

- `Logic.GlareSize` (only spot/search lights)

- `Logic.BulbColor`

Likewise, setting a new intensity or falloff via the sliders in the terminal will result in a call to the same callback, although the falloff isn't technically involved in any computations. Furthermore, when the block changes its working state (turned on/off, damaged, repaired, power loss) the `CurrentLightPower` also goes up/down accordingly.

When we change the settings via custom data, we should mirror the light block terminal sliders and call:

- `MyLightingLogic.UpdateIntensity()`
  Which will invoke the different light blocks' `UpdateIntensity()` methods that we should `Postfix()` to make changes based on vanilla outcomes.

- `MyLightingLogic.UpdateLightProperties()`
  This method transfers additional properties of the `MyLightingLogic` to its `MyLight` list. The properties are:
  
  - `Light.Range            = Logic.Radius` 
  
  - `Light.ReflectorRange   = Logic.ReflectorRadius`
  
  - `Light.Color            = Logic.Color`
  
  - `Light.ReflectorColor   = Logic.Color`
  
  - `Light.Falloff          = Logic.Falloff`
  
  - `Light.PointLightOffset = Logic.Offset`

Afterwards it pushes the update to the renderer.

#### Glare changes

When an lighting block changes its `IsPreview` state, the glare values are updated. Glare is also turned back on when messing with the blinking interval.

**Action:** If we want to use the glare values to cache INI settings, we need to disable the `MyInteriorLight.UpdateGlare()` method and overwrite the `MyInteriorLight.UpdateEnabled()` method to toggle the spot light instead.

#### Repurposing unused fields in the light class

Since a lot of configuration happens in the INI file and we don't want to parse it over and over to find the modifiers for changes in intensity and others, we can repurpose some of the fields that are both unused and don't get overwritten after a `MyLight` has been initially configured. This table lists those fields and their new purpose:

| Field         | Type                     | New Meaning                                                                                                    |
| ------------- | ------------------------ | -------------------------------------------------------------------------------------------------------------- |
| `LightType`   | `enum MyLightType : int` | Only set up initially and never used. Negate integer value to mean our plugin code is working with this light. |
| `GlossFactor` | `float`                  | Repurposed to hold a gloss suppression factor, so some lights don't have such glaringly bright reflections.    |

#### Things that don't work

- Texture is read as "Default" when it is overwritten, but matches the default. This is inconsistent if someone set that texture specifically, but the next time looking into the terminal it says "Default", even though the "CustomData" clearly has it overridden.
- Sometimes when flipping many lights from new lighting code to vanilla, they break. Sometimes they have very high emissive values and sometimes they seem to turn off completely.

#### Things that need investigating

#### Things that could be done in the future

- Import light intensity data from light fixture manufacturers to provide 360° light masks.

- Apply some settings to existing spotlights (SG spot, LG spot, SG rotating, LG rotating, car spot) as they too could benefit from some bloom upgrade, but may not work as we disable glare and I like the light cones on the reflector lights, plus people often use them to add a sense of dust of humidity to a scene.

- Lights with very large point light offsets suggest they are for glow effects, e.g. the point light is supposed to create a glow on the outside with the light fixture hidden away. For these to be exempt, I'd need to switch from On/Off to Off/Auto/On for the lights where the default is always "Auto" instead of "On" or "Off" depending on type of light.
