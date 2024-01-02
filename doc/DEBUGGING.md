# Debugging

To be able to attach to the game, you need to have an IDE installed with the Unity debugger extensions. For Visual Studio, this is the Visual Studio Tools for Unity (VSTU).

The first step is to set Stationeers to development mode. The easiest way to do this is to use the StationeersMods menu in Unity to enable development mode automatically. The steps to do it manually are as follows:
- Edit `[game folder]/rocketstation_Data/boot.config` and add the line: `player-connection-debug=1`
- Copy the following files from `C:\Program Files\Unity 2021.2.13f1\Editor\Data\PlaybackEngines\windowsstandalonesupport\Variations\win64_player_development_mono`:
  - `WindowsPlayer.exe` (to `[game folder]/rocketstation.exe`)
  - `UnityPlayer.dll` (to `[game folder]/UnityPlayer.dll`)
  - `MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll` (to `[game folder]/MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll`)

After the game has been set to development mode, you should be able to attach. However, you need symbols to be able to actually debug.

To debug your own mod code when building **with** Unity:
- Make sure "Include PDBs" is checked in the export settings.

To debug your own mod code when building a standalone code mod **without** Unity:
- Make sure you're building with debug information and `project properties -> Build -> Advanced... -> Debugging information` is set to "Portable". This is the only format supported by VSTU. 
- Make sure the generated .pdb is copied along with the .dll to the exported mod folder.

To debug code from the game:
- Find `[game folder]/rocketstation_Data/Managed/Assembly-CSharp.dll`. This file contains (most of) the game's code.
- Install dotPeek and load up `Assembly-CSharp.dll`. After it loads, right-click Assembly-CSharp in the Assembly Exporer and select "Generate Pdb...". Export anywhere.
- After exporting, copy the exported .pdb file (either works) next to `Assembly-CSharp.dll` in the Stationeers directory.
- When debugging, you should now be able to step through and see any Stationeers code.

> *Note that this seems to work only with dotPeek since other tools (ILSpy, dnSpy, etc) do not export the .pdb in the correct (portable with external code) format.*