using System;
using System.Collections.Generic;

namespace StationeersMods
{
    public class DependencyException : Exception
    {
        public DependencyException(string message) : base(message)
        {
        }
    }
}