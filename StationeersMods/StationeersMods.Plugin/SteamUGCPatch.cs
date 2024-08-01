using System.Threading.Tasks;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace StationeersMods.Plugin
{
    [HarmonyPatch]
    public static class SteamUGCPatch
    {
        //postfix for  public static async Task<bool> DeleteFileAsync(PublishedFileId fileId)
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SteamUGC), "DeleteFileAsync")]
        public static bool DeleteFileAsyncPrefix(ref Task<bool> __result)
        {
            Debug.Log("Override deleting from workshop. Nothing will be deleted.");
            __result = Task.Run(() => { return true; });
            //don't call original method
            return false;
        }
    }
}