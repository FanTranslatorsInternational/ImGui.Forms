using System;
using System.Collections.Generic;
using System.Numerics;
using ImGui.Forms.Extensions;
using ImGui.Forms.Models;
using ImGuiNET;
using SixLabors.ImageSharp;

namespace ImGui.Forms
{
    public static class Style
    {
        private const Theme DefaultTheme_ = Theme.Dark;

        private static readonly Dictionary<Theme, Dictionary<ImGuiCol, Color>> Colors = [];
        private static readonly Dictionary<Theme, Dictionary<ImGuiStyleVar, object>> Styles = [];

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
                Styles[theme] = [];

            Styles[theme][style] = value;
            _hasChanges = true;
        }

        private static object GetStyle(Theme theme, ImGuiStyleVar style)
        {
            if (Styles.TryGetValue(theme, out Dictionary<ImGuiStyleVar, object>? styles)
                && styles.TryGetValue(style, out object? value))
                return value;

            ImGuiStylePtr stylePtr = ImGuiNET.ImGui.GetStyle();
            return style switch
            {
                ImGuiStyleVar.Alpha => stylePtr.Alpha,
                ImGuiStyleVar.DisabledAlpha => stylePtr.DisabledAlpha,
                ImGuiStyleVar.WindowPadding => stylePtr.WindowPadding,
                ImGuiStyleVar.WindowRounding => stylePtr.WindowRounding,
                ImGuiStyleVar.WindowBorderSize => stylePtr.WindowBorderSize,
                ImGuiStyleVar.WindowMinSize => stylePtr.WindowMinSize,
                ImGuiStyleVar.WindowTitleAlign => stylePtr.WindowTitleAlign,
                ImGuiStyleVar.ChildRounding => stylePtr.ChildRounding,
                ImGuiStyleVar.ChildBorderSize => stylePtr.ChildBorderSize,
                ImGuiStyleVar.PopupRounding => stylePtr.PopupRounding,
                ImGuiStyleVar.PopupBorderSize => stylePtr.PopupBorderSize,
                ImGuiStyleVar.FramePadding => stylePtr.FramePadding,
                ImGuiStyleVar.FrameRounding => stylePtr.FrameRounding,
                ImGuiStyleVar.FrameBorderSize => stylePtr.FrameBorderSize,
                ImGuiStyleVar.ItemSpacing => stylePtr.ItemSpacing,
                ImGuiStyleVar.ItemInnerSpacing => stylePtr.ItemInnerSpacing,
                ImGuiStyleVar.IndentSpacing => stylePtr.IndentSpacing,
                ImGuiStyleVar.CellPadding => stylePtr.CellPadding,
                ImGuiStyleVar.ScrollbarSize => stylePtr.ScrollbarSize,
                ImGuiStyleVar.ScrollbarRounding => stylePtr.ScrollbarRounding,
                ImGuiStyleVar.GrabMinSize => stylePtr.GrabMinSize,
                ImGuiStyleVar.GrabRounding => stylePtr.GrabRounding,
                ImGuiStyleVar.TabRounding => stylePtr.TabRounding,
                ImGuiStyleVar.TabBorderSize => stylePtr.TabBorderSize,
                ImGuiStyleVar.TabBarBorderSize => stylePtr.TabBarBorderSize,
                ImGuiStyleVar.TabBarOverlineSize => stylePtr.TabBarOverlineSize,
                ImGuiStyleVar.TableAngledHeadersAngle => stylePtr.TableAngledHeadersAngle,
                ImGuiStyleVar.TableAngledHeadersTextAlign => stylePtr.TableAngledHeadersTextAlign,
                ImGuiStyleVar.ButtonTextAlign => stylePtr.ButtonTextAlign,
                ImGuiStyleVar.SelectableTextAlign => stylePtr.TabRounding,
                ImGuiStyleVar.SeparatorTextBorderSize => stylePtr.SelectableTextAlign,
                ImGuiStyleVar.SeparatorTextAlign => stylePtr.SeparatorTextAlign,
                ImGuiStyleVar.SeparatorTextPadding => stylePtr.SeparatorTextPadding,
                ImGuiStyleVar.DockingSeparatorSize => stylePtr.DockingSeparatorSize,
                _ => throw new InvalidOperationException($"Unknown style variable {style}.")
            };
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
                Colors[theme] = [];

            Colors[theme][colorIndex] = color;
            _hasChanges = true;
        }

        public static Color GetColor(ImGuiCol colorIndex)
        {
            return GetColor(Theme, colorIndex);
        }

        public static Color GetColor(Theme theme, ImGuiCol colorIndex)
        {
            if (Colors.TryGetValue(theme, out Dictionary<ImGuiCol, Color>? colors)
                && colors.TryGetValue(colorIndex, out Color color))
                return color;

            ImGuiStylePtr style = ImGuiNET.ImGui.GetStyle();
            return style.Colors[(int)colorIndex].ToColor();
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

                default:
                    throw new InvalidOperationException($"Unknown theme {Theme}.");
            }

            ImGuiStylePtr stylePtr = ImGuiNET.ImGui.GetStyle();

            if (Colors.TryGetValue(Theme, out Dictionary<ImGuiCol, Color>? themeColors))
            {
                foreach (ImGuiCol imGuiColor in themeColors.Keys)
                    stylePtr.Colors[(int)imGuiColor] = themeColors[imGuiColor].ToVector4();
            }

            if (Styles.TryGetValue(Theme, out Dictionary<ImGuiStyleVar, object>? themeStyles))
            {
                foreach (ImGuiStyleVar imGuiStyle in themeStyles.Keys)
                {
                    switch (imGuiStyle)
                    {
                        case ImGuiStyleVar.Alpha: stylePtr.Alpha = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.DisabledAlpha: stylePtr.DisabledAlpha = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.WindowPadding: stylePtr.WindowPadding = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.WindowRounding: stylePtr.WindowRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.WindowBorderSize: stylePtr.WindowBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.WindowMinSize: stylePtr.WindowMinSize = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.WindowTitleAlign: stylePtr.WindowTitleAlign = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ChildRounding: stylePtr.ChildRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ChildBorderSize: stylePtr.ChildBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.PopupRounding: stylePtr.PopupRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.PopupBorderSize: stylePtr.PopupBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.FramePadding: stylePtr.FramePadding = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.FrameRounding: stylePtr.FrameRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.FrameBorderSize: stylePtr.FrameBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ItemSpacing: stylePtr.ItemSpacing = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ItemInnerSpacing: stylePtr.ItemInnerSpacing = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.IndentSpacing: stylePtr.IndentSpacing = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.CellPadding: stylePtr.CellPadding = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ScrollbarSize: stylePtr.ScrollbarSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ScrollbarRounding: stylePtr.ScrollbarRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.GrabMinSize: stylePtr.GrabMinSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.GrabRounding: stylePtr.GrabRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.TabRounding: stylePtr.TabRounding = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.TabBorderSize: stylePtr.TabBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.TabBarBorderSize: stylePtr.TabBarBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.TabBarOverlineSize: stylePtr.TabBarOverlineSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.TableAngledHeadersAngle: stylePtr.TableAngledHeadersAngle = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.TableAngledHeadersTextAlign: stylePtr.TableAngledHeadersTextAlign = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.ButtonTextAlign: stylePtr.ButtonTextAlign = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.SelectableTextAlign: stylePtr.SelectableTextAlign = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.SeparatorTextBorderSize: stylePtr.SeparatorTextBorderSize = (float)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.SeparatorTextAlign: stylePtr.SeparatorTextAlign = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.SeparatorTextPadding: stylePtr.SeparatorTextPadding = (Vector2)themeStyles[imGuiStyle]; break;
                        case ImGuiStyleVar.DockingSeparatorSize: stylePtr.DockingSeparatorSize = (float)themeStyles[imGuiStyle]; break;
                        default: throw new InvalidOperationException($"Unknown style variable {imGuiStyle}.");
                    }
                }
            }

            _hasChanges = false;
        }
    }

    public readonly struct ThemedColor
    {
        private readonly bool _hasColors;

        private readonly ImGuiCol? _colIndex;

        private readonly Color _lightColor;
        private readonly Color _darkColor;

        public ThemedColor(Color lightColor, Color darkColor)
        {
            _colIndex = null;

            _lightColor = lightColor;
            _darkColor = darkColor;

            _hasColors = true;
        }

        private ThemedColor(ImGuiCol colIndex)
        {
            _colIndex = colIndex;

            _lightColor = _darkColor = Color.Transparent;

            _hasColors = true;
        }

        public bool IsEmpty => GetColor() == Color.Transparent;

        private Color GetColor()
        {
            if (!_hasColors)
                return Color.Transparent;

            if (_colIndex.HasValue)
                return Style.GetColor(_colIndex.Value);

            return Style.Theme switch
            {
                Theme.Light => _lightColor,
                Theme.Dark => _darkColor,
                _ => Color.Transparent
            };
        }

        public static implicit operator ThemedColor(Color c) => new(c, c);
        public static implicit operator ThemedColor(ImGuiCol c) => new(c);
        public static implicit operator Color(ThemedColor c) => c.GetColor();
    }
}
