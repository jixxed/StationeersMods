# Creating a code mod

This guide will help you build your first mod and load it in the game. You can adapt it afterwards to your needs.
This guide is written Unity version 2021.2.13f1.

## Extract template
Download the [ExamplePatchMod](https://github.com/StationeersMods/ExamplePatchMod). This contains a working starter mod you can easily adapt.

- Copy the project into a project folder (ex. ExamplePatchMod)
- ExamplePatchMod.VS.props contains a property key pointing to the Stationeers game folder. Set this to the correct path so required dll's are resolved automatically.
## Preparation steps
In the zip is a readme. follow the instructions defined there.

Replace ExamplePatchMod **EVERYWHERE** with your own mod name. This is for filenames, as well as references inside the files. use you own namespace.

the following files contains ExamplePatchMod: ExamplePatchMod.sln, ExamplePatchMod,info, ExamplePatchModcsproj, ExamplePatchMod.cs, About/About.xml

In the SLN file set a custom GUID for your project instead of 00000000-0000-0000-0000-000000000000. You can generate a GUID online: https://duckduckgo.com/?q=guid

After this you can import your mod into your favourite IDE. The buildscript is configured to build directly to the local mods folder.

## Modifications
The word `ExamplePatchMod` should **NOT** appear anywhere before you publish. 

there is a `stationeersmods` file in the `About` folder. This file is mandatory for StationeersMods mods as a way to recognize the mod needs to be processed as a StationeersMods mod.

An overlay image is provided for you to use to create your own thumbnail.

# Upload to Steam Workshop

To upload your mod to steam, go to the workshop menu in the main menu of the game. Select your mod and click Publish.
After you have published your mod, the `About.xml` file in the local mods folder will be updated with the workshop handle. You can also find this handle in the workshop url.
Copy this handle and add it in the `About.xml` inside your project.

WARNING: If you don't do this, the next time you publish, the mod will be uploaded as a new mod!