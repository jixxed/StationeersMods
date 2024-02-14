using System;

namespace StationeersMods.Cecil
{
    /// <summary>
    ///     Filter mode for finding Assemblies.
    /// </summary>
    [Flags]
    public enum AssemblyFilter
    {
        ApiAssemblies = 1,
        ModToolAssemblies = 2,
        ModAssemblies = 4
    }
}