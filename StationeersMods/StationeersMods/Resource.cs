using System;
using System.Collections;
using System.IO;
using System.Linq;
using Assets.Scripts.Serialization;
using StationeersMods.Interface;
using UnityEngine;

namespace StationeersMods
{
    /// <summary>
    ///     A class that supports async loading of various resources.
    /// </summary>
    public abstract class Resource : IResource
    {
        private float _loadProgress;

        private LoadState _loadState;

        public String modBaseDirectory { get; protected set; }
        public String modDirectory { get; protected set; }
        /// <summary>
        ///     Initialize a Resource with a name.
        /// </summary>
        /// <param name="name">The Resource's name</param>
        protected Resource(string name, string modBaseDirectory, string modDirectory)
        {
            this.modDirectory = modDirectory;
            this.modBaseDirectory = modBaseDirectory;
            this.name = name;
            _loadState = new UnloadedState(this);
        }

        /// <summary>
        ///     Is this Resource busy loading?
        /// </summary>
        public virtual bool isBusy => _loadState.isBusy;

        /// <summary>
        ///     Can this Resource be loaded?
        /// </summary>
        public virtual bool canLoad => true;

        /// <summary>
        ///     This Resource's current load state.
        /// </summary>
        public ResourceLoadState loadState => _loadState.loadState;

        /// <summary>
        ///     What is the Resource's load progress.
        /// </summary>
        public float loadProgress
        {
            get => _loadProgress;
            protected set
            {
                if (value == _loadProgress)
                    return;

                _loadProgress = value;
                LoadProgress?.Invoke(_loadProgress);
            }
        }

        /// <summary>
        ///     This Resource's name.
        /// </summary>
        public string name { get; }

        /// <summary>
        ///     Load this Resource.
        /// </summary>
        public void Load()
        {
            Dispatcher.instance.Enqueue(LoadCoroutine());
        }

        /// <summary>
        ///     Load this Resource asynchronously.
        /// </summary>
        public void LoadAsync()
        {
            Dispatcher.instance.Enqueue(LoadAsyncCoroutine());
        }

        /// <summary>
        ///     Unload this Resource.
        /// </summary>
        public void Unload()
        {
            _loadState.Unload();
        }

        /// <summary>
        ///     Occurs when this Resource has been loaded.
        /// </summary>
        public event Action<Resource> Loaded;

        /// <summary>
        ///     Occurs when this Resource has been unloaded.
        /// </summary>
        public event Action<Resource> Unloaded;

        /// <summary>
        ///     Occurs when this Resource's async loading has been cancelled.
        /// </summary>
        public event Action<Resource> LoadCancelled;

        /// <summary>
        ///     Occurs when this Resources async loading has been resumed.
        /// </summary>
        public event Action<Resource> LoadResumed;

        /// <summary>
        ///     Occurs when this Resource's loadProgress changes.
        /// </summary>
        public event Action<float> LoadProgress;

        /// <summary>
        ///     Coroutine that loads this Resource.
        /// </summary>
        public IEnumerator LoadCoroutine()
        {
            //only load if mods are enabled
            ModConfig config = !File.Exists(WorkshopMenu.ConfigPath)
                ? new ModConfig()
                : XmlSerialization.Deserialize<ModConfig>(WorkshopMenu.ConfigPath, "");
            if (hasNoModConfig(config) || hasModConfigAndIsEnabled(config))
            {
                yield return _loadState.Load();
            }
        }

        private bool hasNoModConfig(ModConfig config)
        {
            return config.Mods.Count == 0 || config.Mods.All(modData => String.IsNullOrEmpty(modData.DirectoryPath) || String.Compare(
                Path.GetFullPath(modData.DirectoryPath).TrimEnd('\\'),
                Path.GetFullPath(modDirectory).TrimEnd('\\'),
                StringComparison.InvariantCultureIgnoreCase) != 0);
        }

        private bool hasModConfigAndIsEnabled(ModConfig config)
        {
            return config.Mods.Any(modData => !String.IsNullOrEmpty(modData.DirectoryPath) && String.Compare(
                Path.GetFullPath(modData.DirectoryPath).TrimEnd('\\'),
                Path.GetFullPath(modDirectory).TrimEnd('\\'),
                StringComparison.InvariantCultureIgnoreCase) == 0 && modData.Enabled);
        }

        /// <summary>
        ///     Coroutine that loads this Resource asynchronously.
        /// </summary>
        public IEnumerator LoadAsyncCoroutine()
        {
            yield return _loadState.LoadAsync();
        }

        /// <summary>
        ///     Finalize the current LoadState.
        /// </summary>
        protected void End()
        {
            _loadState.End();
        }

        /// <summary>
        ///     Use this to implement anything that should happen before unloading this Resource.
        /// </summary>
        protected virtual void PreUnLoadResources()
        {
        }

        /// <summary>
        ///     Use this to implement unloading this Resource.
        /// </summary>
        protected abstract void UnloadResources();

        /// <summary>
        ///     Use this to implement loading this Resource.
        /// </summary>
        protected abstract IEnumerator LoadResources();

        /// <summary>
        ///     Use this to implement loading this Resource asynchronously.
        /// </summary>
        protected abstract IEnumerator LoadResourcesAsync();

        /// <summary>
        ///     Handle end of loading.
        /// </summary>
        protected virtual void OnLoaded()
        {
            loadProgress = 1;
            Loaded?.Invoke(this);
        }

        /// <summary>
        ///     Handle end of unloading.
        /// </summary>
        protected virtual void OnUnloaded()
        {
            _loadProgress = 0;
            Unloaded?.Invoke(this);
        }

        /// <summary>
        ///     Handle load cancelling.
        /// </summary>
        protected virtual void OnLoadCancelled()
        {
            LoadCancelled?.Invoke(this);
        }

        /// <summary>
        ///     Handle load resuming.
        /// </summary>
        protected virtual void OnLoadResumed()
        {
            LoadResumed?.Invoke(this);
        }

        private abstract class LoadState
        {
            protected readonly Resource resource;

            protected LoadState(Resource resource)
            {
                this.resource = resource;
            }

            public virtual bool isBusy => false;

            public abstract ResourceLoadState loadState { get; }

            public virtual IEnumerator Load()
            {
                yield break;
            }

            public virtual IEnumerator LoadAsync()
            {
                yield break;
            }

            public virtual void Unload()
            {
            }

            public virtual void End()
            {
            }
        }

        private class UnloadedState : LoadState
        {
            public UnloadedState(Resource resource) : base(resource)
            {
            }

            public override ResourceLoadState loadState => ResourceLoadState.Unloaded;

            public override IEnumerator Load()
            {
                if (resource.canLoad)
                {
                    resource._loadState = new LoadingState(resource);
                    yield return resource.LoadResources(); //TODO: this skips a frame
                    resource.End();
                }
            }

            public override IEnumerator LoadAsync()
            {
                if (resource.canLoad)
                {
                    resource._loadState = new LoadingState(resource);
                    yield return resource.LoadResourcesAsync();
                    resource.End();
                }
            }
        }

        private class LoadingState : LoadState
        {
            public LoadingState(Resource resource) : base(resource)
            {
            }

            public override bool isBusy => true;

            public override ResourceLoadState loadState => ResourceLoadState.Loading;

            public override void End()
            {
                resource._loadState = new LoadedState(resource);
                resource.OnLoaded();
            }

            public override void Unload()
            {
                resource._loadState = new CancellingState(resource);
            }
        }

        private class LoadedState : LoadState
        {
            public LoadedState(Resource resource) : base(resource)
            {
            }

            public override ResourceLoadState loadState => ResourceLoadState.Loaded;

            public override void Unload()
            {
                if (resource.isBusy)
                {
                    resource.PreUnLoadResources();
                    resource._loadState = new UnloadingState(resource);
                }
                else
                {
                    resource.PreUnLoadResources();
                    resource.UnloadResources();
                    resource._loadState = new UnloadedState(resource);
                    resource.OnUnloaded();
                }
            }
        }

        private class CancellingState : LoadState
        {
            public CancellingState(Resource resource) : base(resource)
            {
            }

            public override bool isBusy => true;

            public override ResourceLoadState loadState => ResourceLoadState.Cancelling;

            public override IEnumerator Load()
            {
                resource.OnLoadResumed();
                resource._loadState = new LoadingState(resource);
                yield break;
            }

            public override IEnumerator LoadAsync()
            {
                resource.OnLoadResumed();
                resource._loadState = new LoadingState(resource);
                yield break;
            }

            public override void End()
            {
                resource._loadState = new UnloadedState(resource);
                resource.PreUnLoadResources();
                resource.UnloadResources();
                resource.OnLoadCancelled();
            }
        }

        private class UnloadingState : LoadState
        {
            public UnloadingState(Resource resource) : base(resource)
            {
            }

            public override bool isBusy => true;

            public override ResourceLoadState loadState => ResourceLoadState.Unloading;

            public override IEnumerator Load()
            {
                resource._loadState = new LoadedState(resource);
                resource.OnLoaded();
                yield break;
            }

            public override IEnumerator LoadAsync()
            {
                resource._loadState = new LoadedState(resource);
                resource.OnLoaded();
                yield break;
            }

            public override void End()
            {
                resource.PreUnLoadResources();
                resource.UnloadResources();
                resource._loadState = new UnloadedState(resource);
                resource.OnUnloaded();
            }
        }
    }
}
