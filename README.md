
[![Image](doc/discord_button.png)](https://discord.gg/AEmQR3XCGm)

# StationeersMods

This is a modding framework to create Stationeers mods. 

## Usage
If you are here to download the latest release for your gameplay you need the following:

### Installation

StationeersMods is a Bepinex plugin, so it is installed just like any other. StationeersMods will load mods you subscribed to from the workshop.
- If you haven't installed Bepinex (5.4.23.2 is the latest supported at time of writing. 6.0 is not yet supported) for the game: [Install BepInEx](https://docs.bepinex.dev/articles/user_guide/installation/index.html)
- Download the latest `StationeersMods.zip` from [Releases](https://github.com/jixxed/StationeersMods/releases) and extract the zip to your `BepInEx/plugins/` folder
- Install your mods from the workshop and play the game

If you have questions you can ask them on [Discord](https://discord.gg/AEmQR3XCGm).

## Mod Development

There are multiple flavors of mods supported:
- unity mod: create anything you want in the game. New components with your own 3d models. (Requires unity)
- simple mod: a simple mod like Bepinex or Addons. Create an simple patching mod and and compile. (Use Visual Studio or Rider)
- bepinex mod: StationeersMods can automatically load bepinex mods from the workshop. Mods need to add a blank About\bepinex file to indicate this mod loads properly with StationeersMods.  

for both the unity and simple mods, tutorials and examples are provided in the doc folder. Image overlays are available to indicate clearly to the user that the mod will we loaded with StationeersMods.

### Manual project setup for unity mod (doc guide and example preferred, so only continue reading if you really want to)

To create mods in unity you can use the StationeersMods Exporter.
- Download the latest `StationeersMods-exporter.zip` from [Releases](https://github.com/jixxed/StationeersMods/releases)
- copy `Assets/` from the zip to your Unity project
- Check that a `StationeersMods` menu has appeared in the Unity editor.

### The Export Window

Assetmods are exported via the `StationeersMods/Export Mod` menu:

![](docs/media/readme/editor_window.png)

- **Mod Name** : The name of the assetmod
- **Author** : Who should get credit for the assetmod
- **Version** : What version of the assetmod is being exported
- **Startup Prefab** : Instantiated into the scene after assetmod is loaded
- **Description** : What this mod does
- **Log Level** : The log level if you use `StationeersMods.Shared.LogUtility`
- **Output Directory** : Where the mod should be exported to

A lot of that is probably self-explanatory besides the **Startup Prefab**.

### The Startup Prefab

A normal Bepin plugin must define a class inheriting `BaseUnityPlugin` to be booted.

Assetmods however are booted by loading a specific prefab from your assetbundle into the scene. You can specify any prefab you want, and it can have whatever scripts on it that you'd like to start with your mod.

Your prefab can also contain child objects with their own components and children. This is useful for automatically bringing in a large structure of objects into the scene, such as a UI panel containing a number of windows.

![](docs/media/readme/startup_prefab.png)

### ContentHandler and ModBehaviour

Every assetmod has an associated `ContentHandler` instance which contains a reference to its prefabs, scenes, and the `Mod` instance itself. In order for you to receive and instance of your assetmod's `ContentHandler` you should put a `ModBehaviour` on your Startup Prefab.

`ModBehaviour` is a subclass of `MonoBehaviour` and you can use it on any of your prefabs to get an instance to the `ContentHandler` via the `ModBehavior.contentHandler` instance field.

#### OnLoaded

Any script that inherits `ModBehaviour` can override the `OnLoaded(ContentHandler contentHandler)` method which is called when the assetmod has been fully loaded.

**You should not access prefabs or scenes via the `ContentHandler` before `OnLoaded` is called.**

#### Scenes and Prefabs

Your assetmod's `ContentHandler` has `prefabs` and `scenes` fields which can be used to access those assets within your mod. You should use `ContentHandler.Instantiate` to instantiate them. This will ensure that any objects your assetmod creates can be properly destroyed when your assetmod is unloaded.

Currently StationeersMods has no way of specifying what assets should be included in the assetmod's assetbundle. **Every asset in your project is added to your assetbundle.**

### Assetmod Assemblies

The default DLL assembly name that Unity builds all your scripts into is called `Assembly-CSharp`. The game the assetmods are being used with already contains an assembly DLL called `Assembly-CSharp`, as it too was built with Unity.

This is a problem. StationeersMods can't let Unity put your assetmod's scripts into an assembly called `Assembly-CSharp` since it conflicts with the main game assembly. To solve this problem, StationeersMods requires you to use **Assembly Definition** assets so that your code ends up in a different assembly. (Why yes, it would be easier if Unity just let you change the default assembly name...)

#### Assembly Definition Assets

Assembly Definition assets or, **Asmdefs** for short, are a built-in Unity asset type that you can create from the asset creation menu:

![](docs/media/readme/create_asmdef.png)

Asmdef are **simple**. The easiest thing is to overthink them. Let me tell you what Asmdefs are:

    You put them in a folder. All scripts in that folder and below are compiled into a different assembly.

That's it. If the Asmdef's name is `Foobar` then you'll get `Foobar.dll` containing all the scripts below the Asmdef in the asset folder structure.

*"But what do I name my asmdef? Where do I put the scripts? etc etc"* I hear you ask.

It literally doesn't matter. You can have as many Asmdefs you'd like, and you can organize your code underneath them however you like. You need to remember one fact:

    Any scripts not captured by an Asmdef wont make it into your assetmod.

Because those scripts will ends up in `Assembly-CSharp.dll` and we can't include that in your assetmod.

#### Asmdef Settings

There are a number of settings for an Asmdef so let's take a look at some important ones. Here is the Asmdef for the ExampleMod:

![](docs/media/readme/asmdef_settings.png)

- **Name** : This determines the name of the assembly DLL filename `Foo` => `Foo.dll`
- **Auto Referenced** : Should be **true**. We want Unity to load this assembly and consider it part of the project.
- **Override References** : True if you want to utilize any pre-compiled assemblies. That is, any assemblies Unity is not responsible for compiling. The ExampleMod uses quite a few. The `Assembly-CSharp.dll` is the one from the game we're modding, so we can refer to its classes in our own code. `StationeersMods.Interface.dll` there is so that we can refer to `StationeersMods.Interface.ModBehaviour` and `StationeersMods.Interface.ContentHandler` in our code.
- **Assembly References** : Will only appear if `Override References` is true. Use this to name the precompiled-assemblies that your assetmod depends on.
- **Platforms** : Should always be exactly as shown for all assetmod Asmdefs.

## Exported Assetmod Content

Once you've exported your assetmod you should find the following content:

![](docs/media/readme/exported_content.png)

- **ExampleMod.dll** : The assembly containing all your scripts. You'll get one for each Asmdef you defined.
- **ExampleMod.info** : Metadata describing your assetmod
- **Windows** : Contains the assetbundle for the Windows platform (the only currently supported platform)

Inside you'll find:

![](docs/media/readme/exported_assets.png)

### Installing Assetmods

Installing assetmods are as easy as dropping them into your `%userprofile%\Documents\My Games\Stationeers\mods\` folder.

## Building this solution

If you are going to build the solution, be sure to look at `StationeersMods.VS.props`, setting those two properties will make it so the package builds without having to track down all of the dependencies manually (In Visual Studio)