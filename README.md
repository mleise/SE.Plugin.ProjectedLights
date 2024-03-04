# Projected Lights

This Space Engineers plugin works on all the interior light class blocks to give them projected light qualities like seen in the spot light. This includes the interior and corner lights, light panel, lit passage and air duct as well as the inset kitchen and aquarium.

The idea came while playing 2014's Alien: Isolation. It is generally agreed upon, that it is an immersive game with great atmosphere and many are surprised how well the visuals of the environments held up. This is in part due to the audio design, the 3D assets, texture work, shaders, particles and peculiar post-processing, but the most immediate difference to Space Engineers would be the lighting. Like most games it uses shadow casting lights sparingly and in fact there may never be more than one active at the same time. (Remember this released on the PS3 and XBox 360 as well.) Instead they are used artistically for spot lights down a corridor, overhead lights to emphasize a location or star light shining in. On top of that, their baked lighting is very low resolution and doesn't do much to make the scene look interesting. What really gives the game its distinctive look is the attenuation. Unlike Space Engineers, where most lights are the simple point lights that bleed through walls, Alien: Isolation mostly uses attenuated lights that only shine in one direction. The technique for this is the same as for the spot lights and helmet lamp in Space Engineers: A grayscale texture is projected onto the scenery in front of the light. This gives the added benefit of being able to add grates, dirt or other non-uniformity to the light and make it more interesting. In the example below, note how the lights have texture to them and the cage around the light bulb is projected onto the walls of the air duct and the shotgun. The glossy reflection is also dimmer in the shaded areas.
![Projected Lights in Alien Isolation](https://github.com/mleise/SE.Plugin.ProjectedLights/assets/609447/e1a03a47-a771-4d5c-9f86-a842e3c44fee)

### Screenshots of version 1.0.0.0

DerMakrat's Speedy Interplanetary Multipurpose Frigate MK 1.1
![hangar1](https://github.com/mleise/SE.Plugin.ProjectedLights/assets/609447/da5d255c-b16f-4d19-bece-194e41ac9091)
WeeZy's NCC 1701 USS Enterprise
![hall](https://github.com/mleise/SE.Plugin.ProjectedLights/assets/609447/8f802db8-e439-4269-953a-079671f4de12)
![reacor_room](https://github.com/mleise/SE.Plugin.ProjectedLights/assets/609447/517408ba-7c80-4817-98db-0d675191b659)
My own Anvil C8X Pisces Expedition
![pisces](https://github.com/mleise/SE.Plugin.ProjectedLights/assets/609447/3182de5a-c2d0-47d6-8856-2046af1ff62d)

## What is changed?

As mentioned, this plugin essentially turns every point light into a spot light with texture, but without activating the shadow casting by default. I removed the 32 spot lights limit entirely, but kept the maximum of 4 shadow casters. Shadow activation is strictly distance based, so there will be flickering while moving around more than 4 active shadow casters. That said, you will find that once the light doesn't shine backwards through walls as much any more, a lot of the need for shadows is alleviated. (Fun fact: Shadows from point light sources require 360° shadow mapping, which isn't implemented anyways.)

The light cone when casting shadows is limited to 136° (instead of 179° without) as I noticed occasional flickering when going above that angle. Some of the light textures are then drawn as a tiny spot, so I switch to a different default (a big bright spot) for some lights when shadow casting is enabled. You can still override the texture in any case.

Vanilla Space Engineers has a very limited range of bloom coming from emissive materials like the battery charge indicators or magnetic boots. I found that for light fixtures, that are - virtue of their nature - in a bright environment, it is not enough to make them stand out and look like they are turned on, which gives a strange disconnect between the light source and the illuminated area. To fix this, I increased the gain on emissive materials in the renderer, while simultaneously turning down all the emissive material intensities by the same amount. Then I increased the emissivity individually for each interior light to a believable level.

To change the settings, I added new terminal controls to all interior lights. Changes from the default are stored as an INI section in the Custom Data of the block.

## Installation

1. You will need the [Space Engineers Launcher](https://github.com/sepluginloader/SpaceEngineersLauncher) installed. Instructions are in the link.
  
2. Optionally, if you want to keep launching Space Engineers directly through Steam, do the following:
  
  1. Right-click on Space Engineers and select "Properties..."
    
  2. At the bottom of the "General" tab you'll find a text box labeled "Advanced users may choose to enter modifications to their launch options". We are advanced users.
    
  3. Type the full path to the launcher into that box, optionally add `-skipintro` to skip over the company logo video, and add `%command%`. So the full line may read like this:
    `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64\SpaceEngineersLauncher.exe -skipintro %command%`
    The `%command%` part will be expanded into the original Space Engineers executable path and is just required for Steam to understand that we are replacing the full command line, not just appending to it.
    
3. Once in the game, click on the new "Plugins" button in the main menu.
  
4. Type "Projected Lights" in the search bar, then check the box next to the search result.
  
5. Press Esc or click the X in the top right, then click on "Apply" and answer "Yes" to restart the game.
  

The plugin is now installed and active and will be updated automatically.

## Development

Use Visual Studio (Community) 2022 to open the project file. It is configured to use the original installation location of Space Engineers to automatically find dependencies and launch the Space Engineers Launcher when debugging (F5). If you have moved Space Engineers afterwards, you can update the registry key `InstallLocation` under `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 244850` and reload the project.
