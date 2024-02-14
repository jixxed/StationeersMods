using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using StationeersMods.Shared;
using UnityEngine;

namespace StationeersMods
{
    internal class AssetBundleResource : Resource
    {
        private bool _canLoad;

        public AssetBundleResource(string name, string path, string modDirectory) : base(name, modDirectory)
        {
            this.path = path;

            _canLoad = false;

            GetAssetPaths();
        }

        public string path { get; }

        public AssetBundle assetBundle { get; private set; }

        public ReadOnlyCollection<string> assetPaths { get; private set; }

        public override bool canLoad => _canLoad;

        protected override IEnumerator LoadResources()
        {
            assetBundle = AssetBundle.LoadFromFile(path);

            yield break;
        }

        protected override IEnumerator LoadResourcesAsync()
        {
            var assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(path);

            while (!assetBundleCreateRequest.isDone)
            {
                loadProgress = assetBundleCreateRequest.progress;
                yield return null;
            }

            assetBundle = assetBundleCreateRequest.assetBundle;
        }

        protected override void UnloadResources()
        {
            if (assetBundle != null)
                assetBundle.Unload(true);

            assetBundle = null;
        }

        private void GetAssetPaths()
        {
            var assetPaths = new List<string>();

            this.assetPaths = assetPaths.AsReadOnly();

            if (string.IsNullOrEmpty(path))
                return;

            if (!File.Exists(path))
                return;

            var manifestPath = path + ".manifest";

            if (!File.Exists(manifestPath))
            {
                LogUtility.LogWarning(name + " manifest missing");
                return;
            }

            _canLoad = true;

            //TODO: long lines in manifest are formatted?
            var lines = File.ReadAllLines(manifestPath);

            var start = Array.IndexOf(lines, "Assets:") + 1;

            for (var i = start; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("- "))
                    break;

                var assetPath = lines[i].Substring(2);

                //Note: if the asset is a scene, we only need the name
                if (assetPath.EndsWith(".unity"))
                    assetPath = Path.GetFileNameWithoutExtension(assetPath);

                assetPaths.Add(assetPath);
            }
        }
    }
}