using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Assets.Scripts;
using HarmonyLib;

namespace StationeersMods.Plugin
{
    [HarmonyPatch]
    public static class WorldManagerPatch
    {

        [HarmonyPatch(typeof(DataCollection), "Register")]
        [HarmonyPrefix]
        private static bool Register(DataCollection __instance, ref DataCollection dataCollection)
        {
            if (dataCollection == null || string.IsNullOrEmpty(dataCollection.Id))
            {
                return false;
            }

            var field = typeof(DataCollection).GetField("_dataCollectionDictionary", BindingFlags.NonPublic | BindingFlags.Static);
            var dictionary = (Dictionary<int, DataCollection>)field.GetValue(null);

            if (dictionary.ContainsKey(dataCollection.IdHash))
            {

                var existingDataCollection = DataCollection.Get<DataCollection>(dataCollection.IdHash);

                if (existingDataCollection != null)
                {

                    Type dataCollectionType = dataCollection.GetType();

                    var tasks = new List<Task>();

                    foreach (var prop in dataCollectionType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (!prop.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase))
                        {
                            var currentDataCollection = dataCollection; // Capture the dataCollection instance

                            tasks.Add(Task.Run(() =>
                            {
                                var value = prop.GetValue(currentDataCollection);

                                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(prop.PropertyType) && prop.PropertyType != typeof(string))
                                {
                                    var listValues = string.Join(", ", (System.Collections.IEnumerable)value);
                                }

                                prop.SetValue(existingDataCollection, value);
                            }));
                        }
                    }

                    foreach (var fieldInfo in dataCollectionType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (!fieldInfo.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase))
                        {
                            var currentDataCollection = dataCollection; // Capture the dataCollection instance

                            tasks.Add(Task.Run(() =>
                            {
                                var value = fieldInfo.GetValue(currentDataCollection);

                                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(fieldInfo.FieldType) && fieldInfo.FieldType != typeof(string))
                                {
                                    var listValues = string.Join(", ", (System.Collections.IEnumerable)value);
                                }

                                fieldInfo.SetValue(existingDataCollection, value);
                            }));
                        }
                    }

                    Task.WhenAll(tasks).ContinueWith(t =>
                    {
                        var method = dataCollectionType.GetMethod("OnRegistered", BindingFlags.NonPublic | BindingFlags.Instance);
                        method.Invoke(existingDataCollection, null);
                    }).Wait();

                    return false;
                }
            }

            dictionary.Add(dataCollection.IdHash, dataCollection);
            var newMethod = dataCollection.GetType().GetMethod("OnRegistered", BindingFlags.NonPublic | BindingFlags.Instance);
            newMethod.Invoke(dataCollection, null);
            return false;
        }
    }
}