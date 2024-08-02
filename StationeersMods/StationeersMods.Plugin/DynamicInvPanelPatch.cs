using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.Objects;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace StationeersMods.Plugin
{
    [HarmonyPatch]
    public static class DynamicInvPanelPatch
    {
        private static readonly Color[] color = new Color[2]
        {
            new Color(1f, 1f, 1f, 1f),
            new Color(0.2f, 0.2f, 0.2f, 1f)
        };
        //original method
        // private void AddDynamicItem(string name)
        // {
        //     DynamicItem dynamicItem = UnityEngine.Object.Instantiate<DynamicItem>(this.DynamicItemPrefab, this.Content, true);
        //     this.AllDynamicItems.Add(dynamicItem);
        //     dynamicItem.name = "~Spawn" + name;
        //     dynamicItem.Name.text = name;
        //     dynamicItem.Transform.localScale = new Vector3(1f, 1f, 1f);
        //     dynamicItem.Background.sprite = UnityEngine.Resources.Load<Sprite>("UI/Thumbnails/" + name);
        //     dynamicItem.Background.color = this.color[(bool) (UnityEngine.Object) dynamicItem.Background.sprite ? 0 : 1];
        //     dynamicItem.Toggle.group = this._group;
        // }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DynamicInvPanel), "AddDynamicItem")]
        public static void AddDynamicItemPostfix()
        {
            //get the last item that was added to DynamicInvPanel.Instance.AllDynamicItems
            var item = DynamicInvPanel.Instance.AllDynamicItems.Last();

            if ((bool)(UnityEngine.Object)item.Background.sprite) return;
            
            var name = item.Name.text;
            Debug.Log("Sprite empty for " + item.Name.text + ". Attempt to use thumbnail instead for dynamic spawn menu.");
            var thumbnail = Prefab.AllPrefabs.Find(thing => thing.name == name)?.Thumbnail;
            item.Background.sprite = thumbnail;
            item.Background.color = color[(bool)(UnityEngine.Object)item.Background.sprite ? 0 : 1];
        }
    }
}