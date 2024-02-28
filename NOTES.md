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

TODO:

- sliders

- elumdat

- apply some settings to existing spotlights (SG spot, LG spot, SG rotating, LG rotating, car spot)
