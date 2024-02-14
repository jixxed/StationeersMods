using System.IO;
using UnityEngine;

namespace StationeersMods
{
    public class VersionHelper
    {
        private static string Version { get; set; }

        public static string GameVersion()
        {
            if (Version != null)
            {
                return Version;
            }

            var filename = "version.ini";
            if (!File.Exists(Application.streamingAssetsPath + "/" + filename))
                return "0";
            string str1 = File.ReadAllText(Application.streamingAssetsPath + "/" + filename);
            string str2 = "UPDATEVERSION=Update ";
            int startIndex1 = str1.IndexOf(str2);
            if (-1 != startIndex1)
            {
                int num = str1.IndexOf("\r", startIndex1);
                if (-1 != num)
                {
                    Version = str1.Substring(startIndex1 + str2.Length, num - startIndex1 - str2.Length);
                    return Version;
                }
            }

            return "0";
        }
    }
}