using System;
using BepInEx;

namespace StationeersMods.Interface;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class StationeersMod : BepInPlugin
{
    public StationeersMod(string GUID, string Name, string Version) : base(GUID, Name, Version)
    {
    }
}