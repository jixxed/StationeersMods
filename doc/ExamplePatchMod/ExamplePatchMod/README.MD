This is a Harmony patch template for StationeersMods. You can use this template without Unity.

To properly use this template you need to do the following:
Configure the StationeersDirectory in ExamplePatchMod.VS.props 
Replace ExamplePatchMod EVERYWHERE with your own mod name. This is for filenames, as well as references inside the files. use you own namespace.
In the SLN file set a custom GUID for your project instead of 00000000-0000-0000-0000-000000000000. You can generate a GUID online: https://duckduckgo.com/?q=guid

When you build the project, all the files are automatically copied to the Stationeers local mod folder.
Keep this project outside of the mods folder. 

When you want to publish your mod make sure you:
- Update the About.xml with the necessary information. 
- Keep the warnings in the description fields in About.xml. One description is for steam, the other ingame. This allows for separate markup on the steam workshop and ingame.
- After publish check About.xml in the local mods folder. It will have a workshophandle added. Copy this tag to the About.xml file in this project! It allows you to safely update your mod. 
  ALL THE FILES IN THE LOCAL MOD FOLDER GET OVERWRITTEN EACH TIME YOU BUILD
- Update the Preview.png and thumb.png with your own image.

The stationeersmods file is an empty marker file, so StationeersMods knows it should load this mod.
The .info file is also used by StationeersMods to load in your mod.

The bare minimum your built mod should contain is:
Mod
- About
  - About.xml
  - Preview.png
  - stationeersmods
  - thumb.png
- GameData
  - dont.remove (of course you can remove this file if you add custom language or recipes to this folder)
- Mod.dll
- Mod.info

There is a release and a debug build. The debug build includes a PDB file for debugging purposes.