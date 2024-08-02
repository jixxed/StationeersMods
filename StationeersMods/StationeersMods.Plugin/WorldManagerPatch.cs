using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace StationeersMods.Plugin
{
    [HarmonyPatch]
    public static class WorldManagerPatch
    {
     
        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorldManager), "LoadDataFilesAtPath")]
        public static void LoadDataFilesAtPathPostfix()
        {
           Localization.OnLanguageChanged.Invoke();
        }
    }
}