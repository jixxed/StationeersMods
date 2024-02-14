using HarmonyLib;
using StationeersMods.Interface;

namespace examplepatchmod
{
    class ExamplePatchMod : ModBehaviour
    {
        public override void OnLoaded(ContentHandler contentHandler)
        {
            //READ THE README FIRST! 
            
            // Harmony harmony = new Harmony("ExamplePatchMod");
            // harmony.PatchAll();
            UnityEngine.Debug.Log("ExamplePatchMod Loaded!");
        }
    }
}