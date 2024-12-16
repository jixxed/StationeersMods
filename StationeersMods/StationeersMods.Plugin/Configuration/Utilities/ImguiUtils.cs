using System;
using System.Collections.Generic;
using UnityEngine;

namespace StationeersMods.Plugin.Configuration.Utilities
{
    internal static class ImguiUtils
    {

        private static Texture2D _tooltipBg;
        private static Texture2D _windowBackground;

        public static void DrawWindowBackground(Rect position)
        {
            if (!_windowBackground)
            {
                var windowBackground = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                windowBackground.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.5f, 1));
                windowBackground.Apply();
                _windowBackground = windowBackground;
            }

            GUI.Box(position, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = _windowBackground } });
        }

        public static void DrawContolBackground(Rect position, Color color = default)
        {
            if (!_tooltipBg)
            {
                var background = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                background.SetPixel(0, 0, Color.black);
                background.Apply();
                _tooltipBg = background;
            }

            GUI.Box(position, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = _tooltipBg } });
        }
    }
}