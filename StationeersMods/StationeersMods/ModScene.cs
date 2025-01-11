using System.Collections;
using System.IO;
using StationeersMods.Interface;
using UnityEngine.SceneManagement;

namespace StationeersMods
{
    /// <summary>
    ///     Represents a Scene that is included in a Mod.
    /// </summary>
    public class ModScene : Resource
    {
        /// <summary>
        ///     Initialize a new ModScene with a Scene name and a Mod
        /// </summary>
        /// <param name="name">The scene's name</param>
        /// <param name="mod">The Mod this ModScene belongs to.</param>
        public ModScene(string name, AssemblyMod mod, string modBasePath, string path) : base(name, modBasePath, path)
        {
            this.mod = mod;
            scene = null;
        }

        /// <summary>
        ///     This ModScene's Scene.
        /// </summary>
        public Scene? scene { get; private set; }

        /// <summary>
        ///     The Mod this scene belongs to.
        /// </summary>
        public AssemblyMod mod { get; }

        /// <summary>
        ///     Can the scene be loaded? False if this scene's Mod is not loaded.
        /// </summary>
        public override bool canLoad => mod.loadState == ResourceLoadState.Loaded;

        protected override IEnumerator LoadResources()
        {
            //NOTE: Loading a scene synchronously prevents the scene from being initialized, so force async loading.
            yield return LoadResourcesAsync();
        }

        protected override IEnumerator LoadResourcesAsync()
        {
            var loadOperation = SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < .9f)
            {
                loadProgress = loadOperation.progress;
                yield return null;
            }

            loadOperation.allowSceneActivation = true;

            yield return loadOperation;

            scene = SceneManager.GetSceneByName(name);

            SetActive();
        }

        protected override void UnloadResources()
        {
            if (scene.HasValue)
                scene.Value.Unload();

            scene = null;
        }

        /// <summary>
        ///     Set this ModScene's Scene as the active scene.
        /// </summary>
        public void SetActive()
        {
            if (scene.HasValue)
                SceneManager.SetActiveScene(scene.Value);
        }

        protected override void OnLoaded()
        {
            if(mod is Mod m)
            foreach (var modHandler in GetComponentsInScene<IModHandler>())
                modHandler.OnLoaded(m.contentHandler);

            base.OnLoaded();
        }

        /// <summary>
        ///     Returns the first Component of type T in this Scene.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T GetComponentInScene<T>()
        {
            if (loadState != ResourceLoadState.Loaded)
                return default(T);

            return scene.Value.GetComponentInScene<T>();
        }

        /// <summary>
        ///     Returns all Components of type T in this Scene.
        /// </summary>
        /// <typeparam name="T">The Component that will be looked for.</typeparam>
        /// <returns>An array of found Components of Type T.</returns>
        public T[] GetComponentsInScene<T>()
        {
            if (loadState != ResourceLoadState.Loaded)
                return new T[0];

            return scene.Value.GetComponentsInScene<T>();
        }
    }
}