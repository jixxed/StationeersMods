using System;
using System.Collections.Generic;
using StationeersMods.Plugin;

namespace StationeersMods
{
    public class MissingDependencyException : DependencyException
    {
        public List<ModVersion> Missing;
        public MissingDependencyException(string message, List<ModVersion> missing) : base(message)
        {
            Missing = missing;
        }
    }
}