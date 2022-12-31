using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGuiNET;

namespace ImGui.Forms
{
    public static class Style
    {
        private const Theme DefaultTheme_ = Theme.Dark;

        private static readonly IDictionary<Theme, IDictionary<ImGuiCol, Color>> Colors = new Dictionary<Theme, IDictionary<ImGuiCol, Color>>();
        private static readonly IDictionary<Theme, IDictionary<ImGuiStyleVar, object>> Styles = new Dictionary<Theme, IDictionary<ImGuiStyleVar, object>>();

        // HINT: Set to true initially, so ApplyStyle gets triggered by the first frame and sets every default style accordingly
        private static bool _hasChanges = true;

        public static Theme Theme { get; private set; } = DefaultTheme_;

        public static void ChangeTheme(Theme theme)
        {
            if (Theme == theme)
                return;

            Theme = theme;
            _hasChanges = true;
        }

        #region Style changes

        public static void SetStyle(ImGuiStyleVar style, float value)
        {
            SetStyle(Theme, style, (object)value);
        }

        public static void SetStyle(ImGuiStyleVar style, Vector2 value)
        {
            SetStyle(Theme, style, (object)value);
        }

        public static void SetStyle(Theme theme, ImGuiStyleVar style, float value)
        {
            SetStyle(theme, style, (object)value);
        }

        public static void SetStyle(Theme theme, ImGuiStyleVar style, Vector2 value)
        {
            SetStyle(theme, style, (object)value);
        }

        public static float GetStyleFloat(ImGuiStyleVar style)
        {
            return (float)GetStyle(Theme, style);
        }

        public static Vector2 GetStyleVector2(ImGuiStyleVar style)
        {
            return (Vector2)GetStyle(Theme, style);
        }

        public static float GetStyleFloat(Theme theme, ImGuiStyleVar style)
        {
            return (float)GetStyle(theme, style);
        }

        public static Vector2 GetStyleVector2(Theme theme, ImGuiStyleVar style)
        {
            return (Vector2)GetStyle(theme, style);
        }

        private static void SetStyle(Theme theme, ImGuiStyleVar style, object value)
        {
            if (!Styles.ContainsKey(theme))
                Styles[theme] = new Dictionary<ImGuiStyleVar, object>();

            Styles[theme][style] = value;
            _hasChanges = true;
        }

        private static object GetStyle(Theme theme, ImGuiStyleVar style)
        {
            if (!Styles.ContainsKey(theme) || !Styles[theme].ContainsKey(style))
            {
                var stylePtr = ImGuiNET.ImGui.GetStyle();
                switch (style)
                {
                    case ImGuiStyleVar.ScrollbarRounding:
                        return stylePtr.ScrollbarRounding;

                    case ImGuiStyleVar.ScrollbarSize:
                        return stylePtr.ScrollbarSize;

                    case ImGuiStyleVar.WindowPadding:
                        return stylePtr.WindowPadding;
                }
            }

            return Styles[theme][style];
        }

        #endregion

        #region Color changes

        public static void SetColor(ImGuiCol colorIndex, Color color)
        {
            SetColor(Theme, colorIndex, color);
        }

        public static void SetColor(Theme theme, ImGuiCol colorIndex, Color color)
        {
            if (!Colors.ContainsKey(theme))
                Colors[theme] = new Dictionary<ImGuiCol, Color>();

            Colors[theme][colorIndex] = color;
            _hasChanges = true;
        }

        public static Color GetColor(ImGuiCol colorIndex)
        {
            return GetColor(Theme, colorIndex);
        }

        public static Color GetColor(Theme theme, ImGuiCol colorIndex)
        {
            if (!Colors.ContainsKey(theme) || !Colors[theme].ContainsKey(colorIndex))
            {
                var style = ImGuiNET.ImGui.GetStyle();
                return style.Colors[(int)colorIndex].ToColor();
            }

            return Colors[theme][colorIndex];
        }

        #endregion

        internal static void ApplyStyle()
        {
            if (!_hasChanges)
                return;

            switch (Theme)
            {
                case Theme.Light:
                    ImGuiNET.ImGui.StyleColorsLight();
                    break;

                case Theme.Dark:
                    ImGuiNET.ImGui.StyleColorsDark();
                    break;
            }

            var stylePtr = ImGuiNET.ImGui.GetStyle();

            if (Colors.ContainsKey(Theme))
            {
                foreach (var color in Colors[Theme])
                    stylePtr.Colors[(int)color.Key] = color.Value.ToVector4();
            }

            if (Styles.ContainsKey(Theme))
            {
                foreach (var style in Styles[Theme])
                {
                    switch (style.Key)
                    {
                        case ImGuiStyleVar.ScrollbarRounding:
                            stylePtr.ScrollbarRounding = (float)style.Value;
                            break;

                        case ImGuiStyleVar.ScrollbarSize:
                            stylePtr.ScrollbarSize = (float)style.Value;
                            break;

                        case ImGuiStyleVar.WindowPadding:
                            stylePtr.WindowPadding = (Vector2)style.Value;
                            break;
                    }
                }
            }

            _hasChanges = false;
        }
    }
}
