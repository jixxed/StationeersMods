using UnityEngine;

namespace StationeersMods.Editor
{
    public class EditorScriptableSingleton<T> where T : ScriptableObject
    {
        private static T _instance;

        public EditorScriptableSingleton(T instance = null)
        {
            if (instance != null)
                _instance = instance;

            if (_instance == null)
                _instance = GetInstance();
        }
        //Note: Unity versions 5.6 and earlier fail to load ScriptableObject assets for Types that are defined in an editor assembly 
        //and derive from a Type defined in a non-editor assembly.

        public T instance
        {
            get
            {
                if (_instance == null)
                    GetInstance();

                return _instance;
            }
        }

        private T GetInstance()
        {
            Debug.Log("Getting export settings.");

            if (_instance == null)
            {
                Debug.Log("Instance was null, loading resource " + typeof(T).Name);
                _instance = Resources.Load<T>(typeof(T).Name);
            }
            //  Debug.Log("settings found " + _instance.ToString());

            if (_instance == null)
            {
                Debug.Log("Creating asset resource.");
                _instance = ScriptableObject.CreateInstance<T>();
                AssetUtility.CreateAsset(_instance);
            }
            return _instance;
        }
    }
}