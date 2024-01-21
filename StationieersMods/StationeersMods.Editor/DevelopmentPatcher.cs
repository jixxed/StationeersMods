using StationeersMods.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace StationeersMods.Editor
{
    public class DevelopmentPatcher
    {
        public bool? DevelopmentModeEnabled { get; private set; }

        public string PathToBootConfig(ExportSettings settings) => Path.Combine(settings.StationeersDirectory, "rocketstation_Data", "boot.config");
        public string PathToPlayerDirectory => Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines/windowsstandalonesupport/Variations");
        public string PathToStationeersExecutable(ExportSettings settings) => Path.Combine(settings.StationeersDirectory, "rocketstation.exe");
        public string PathToStationeersPlayer(ExportSettings settings) => Path.Combine(settings.StationeersDirectory, "UnityPlayer.dll");
        public string PathToStationeersMono(ExportSettings settings) => Path.Combine(settings.StationeersDirectory, "MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll");

        public string DevelopmentExecutablePath => Path.Combine(PathToPlayerDirectory, "win64_player_development_mono/WindowsPlayer.exe");
        public string DevelopmentPlayerPath => Path.Combine(PathToPlayerDirectory, "win64_player_development_mono/UnityPlayer.dll");
        public string DevelopmentMonoPath => Path.Combine(PathToPlayerDirectory, "win64_player_development_mono/MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll");

        public string ReleaseExecutablePath => Path.Combine(PathToPlayerDirectory, "win64_player_nondevelopment_mono/WindowsPlayer.exe");
        public string ReleasePlayerPath => Path.Combine(PathToPlayerDirectory, "win64_player_nondevelopment_mono/UnityPlayer.dll");
        public string ReleaseMonoPath => Path.Combine(PathToPlayerDirectory, "win64_player_nondevelopment_mono/MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll");

        private void ValidateDirectory(ExportSettings settings)
        {
            if (string.IsNullOrEmpty(settings.StationeersDirectory))
            {
                DevelopmentModeEnabled = null;
                throw new ArgumentException("The Stationeers directory is not set");
            }

            if (!Directory.Exists(settings.StationeersDirectory))
            {
                DevelopmentModeEnabled = null;
                throw new ArgumentException("The Stationeers directory is not valid");
            }
        }

        private bool CheckIsIdentical(string fileA, string fileB)
        {
            var fileInfoA = new FileInfo(fileA);
            var fileInfoB = new FileInfo(fileB);

            // Check the filesize, and then check the timestamp - if they match, we're good
            return fileInfoA.LastWriteTime == fileInfoB.LastWriteTime && fileInfoA.Length == fileInfoB.Length;
        }

        public void CheckDevelopmentMode(ExportSettings settings)
        {
            ValidateDirectory(settings);

                // Check the player connection option is enabled
            var configLines = File.ReadAllLines(PathToBootConfig(settings));
            if (!configLines.Contains("player-connection-debug=1"))
            {
                // Config option is not set
                DevelopmentModeEnabled = false;
                return;
            }

            // Check if we're currently using the development executable, development player dll, and development mono
            if (!CheckIsIdentical(PathToStationeersExecutable(settings), DevelopmentExecutablePath) ||
                !CheckIsIdentical(PathToStationeersPlayer(settings), DevelopmentPlayerPath) ||
                !CheckIsIdentical(PathToStationeersMono(settings), DevelopmentMonoPath))
            {
                DevelopmentModeEnabled = false;
                return;
            }

            // Both files are good, so debugging should work
            DevelopmentModeEnabled = true;
        }

        public void SetDevelopmentMode(ExportSettings settings, bool enabled)
        {
            ValidateDirectory(settings);

            // Change player connection line
            var configLines = File.ReadAllLines(PathToBootConfig(settings));
            var newConfigLines = configLines.Where(line => !line.Contains("player-connection-debug")).ToList();
            newConfigLines.Add($"player-connection-debug={(enabled ? 1 : 0)}");
            File.WriteAllLines(PathToBootConfig(settings), newConfigLines);

            // Change files
            // Note that the nondevelopment executable isn't 100% identical to the stationeers executable since it has no icon
            File.Copy(enabled ? DevelopmentExecutablePath : ReleaseExecutablePath, PathToStationeersExecutable(settings), true);
            File.Copy(enabled ? DevelopmentMonoPath : ReleaseMonoPath, PathToStationeersMono(settings), true);
            File.Copy(enabled ? DevelopmentPlayerPath : ReleasePlayerPath, PathToStationeersPlayer(settings), true);

            // Verify change
            CheckDevelopmentMode(settings);
        }
    }
}
