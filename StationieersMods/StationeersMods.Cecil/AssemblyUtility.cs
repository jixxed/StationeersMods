using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using StationeersMods.Shared;

namespace StationeersMods.Cecil
{
    /// <summary>
    ///     Utility for finding Assemblies.
    /// </summary>
    public class AssemblyUtility
    {
        /// <summary>
        ///     Find dll files in a directory and its sub directories.
        /// </summary>
        /// <param name="path">The directory to search in.</param>
        /// <returns>A List of paths to found Assemblies.</returns>
        public static List<string> GetAssemblies(string path, AssemblyFilter assemblyFilter)
        {
            var assemblies = new List<string>();

            GetAssemblies(assemblies, path, assemblyFilter);

            return assemblies;
        }

        public static void GetAssemblies(List<string> assemblies, string path, AssemblyFilter assemblyFilter)
        {
            var assemblyFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories);

            foreach (var assembly in assemblyFiles)
            {
                AssemblyDefinition assemblyDefinition;

                try
                {
                    assemblyDefinition = AssemblyDefinition.ReadAssembly(assembly);
                }
                catch
                {
                    LogUtility.LogDebug($"Couldn't read assembly: {assembly}");
                    continue;
                }

                var name = assemblyDefinition.Name.Name;

                assemblyDefinition.Dispose();

                if (name == "StationeersMods" || name.StartsWith("StationeersMods."))
                {
                    if ((assemblyFilter & AssemblyFilter.ModToolAssemblies) != 0)
                    {
                        LogUtility.LogDebug($"Adding assembly: {name}");
                        assemblies.Add(assembly);
                    }

                    continue;
                }

                if (name.Contains("Mono.Cecil"))
                {
                    if ((assemblyFilter & AssemblyFilter.ModToolAssemblies) != 0)
                    {
                        LogUtility.LogDebug($"Adding assembly: {name}");
                        assemblies.Add(assembly);
                    }

                    continue;
                }

                //if(CodeSettings.apiAssemblies.Contains(name))
                //{
                //    if((assemblyFilter & AssemblyFilter.ApiAssemblies) != 0)
                //    {
                //        LogUtility.LogDebug($"Adding assembly: {name}");
                //        assemblies.Add(assembly);
                //    }

                //    continue;
                //}

                if ((assemblyFilter & AssemblyFilter.ModAssemblies) != 0)
                {
                    LogUtility.LogDebug($"Adding assembly: {name}");
                    assemblies.Add(assembly);
                }
            }
        }
    }
}