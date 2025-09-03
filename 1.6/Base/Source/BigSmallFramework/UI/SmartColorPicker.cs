using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.Sound;
using Verse;
using static Verse.Widgets;
using RimWorld;
using Verse.Noise;
using Mono.Security.X509.Extensions;

namespace BigAndSmall
{
    [StaticConstructorOnStartup]
    public static class SmartColorWidgets
    {
        public static Texture2D BrightnessTexture { get => field; set; } = ContentFinder<Texture2D>.Get("BS_UI/BrightnessGradient", true);
        public static Texture2D SliderHandle { get => field; set; } = ContentFinder<Texture2D>.Get("UI/Buttons/SliderHandle");

        public static List<Color> GreyScale5Palette { get => field; set; } =
        [
            new Color(.05f,.05f, .05f), // near-black
            new Color(0.2f, 0.2f, 0.2f), // dark gray
            new Color(0.5f, 0.5f, 0.5f), // medium gray
            new Color(0.8f, 0.8f, 0.8f), // light gray
            new Color(.99f,.99f, .99f),   // near-white
        ];

        // Fairly pastel colors since those will look best in-game.
        public static List<Color> ColorPalette { get => field; set; } =
        [
            // Red
            new Color(0.4f, 0.1f, 0.1f), // dark
            new Color(0.6f, 0.2f, 0.2f), // medium
            new Color(0.8f, 0.2f, 0.3f), // light

            // Orange
            new Color(0.4f, 0.25f, 0.1f),
            new Color(0.6f, 0.4f, 0.2f),
            new Color(0.8f, 0.5f, 0.2f),

            // Yellow
            new Color(0.4f, 0.4f, 0.1f),
            new Color(0.6f, 0.6f, 0.2f),
            new Color(0.8f, 0.8f, 0.2f),

            // Green
            new Color(0.1f, 0.4f, 0.1f),
            new Color(0.2f, 0.6f, 0.2f),
            new Color(0.2f, 0.8f, 0.3f),

            // Cyan
            new Color(0.1f, 0.4f, 0.4f),
            new Color(0.2f, 0.6f, 0.6f),
            new Color(0.2f, 0.8f, 0.8f),

            // Blue
            new Color(0.1f, 0.1f, 0.4f),
            new Color(0.2f, 0.2f, 0.6f),
            new Color(0.2f, 0.3f, 0.8f),

            // Purple
            new Color(0.25f, 0.1f, 0.4f),
            new Color(0.4f, 0.2f, 0.6f),
            new Color(0.5f, 0.2f, 0.8f),

            // Magenta
            new Color(0.4f, 0.1f, 0.4f),
            new Color(0.6f, 0.2f, 0.6f),
            new Color(0.8f, 0.2f, 0.8f),
        ];

        public static List<Color> FullColorPalette { get => field; set; } =
        [
            ..GreyScale5Palette,
            ..ColorPalette,
        ];

        public static List<Color> PaletteWithSkinClrs { get => field; set; } =
            [
            new Color(0.949f, 0.929f, 0.878f),
            new Color(1.000f, 0.937f, 0.835f),
            new Color(1.000f, 0.937f, 0.788f),
            new Color(1.000f, 0.937f, 0.741f),
            new Color(0.976f, 0.859f, 0.647f),
            new Color(0.949f, 0.780f, 0.549f),
            new Color(0.894f, 0.620f, 0.353f),
            new Color(0.510f, 0.357f, 0.188f),
            new Color(0.388f, 0.275f, 0.141f),
            ..GreyScale5Palette,
            ..ColorPalette,
            ];

        public static List<Color> MiniPalette { get => field; set; } =
            [
            new Color(.05f,.05f, .05f), // near-black
            new Color(0.2f, 0.2f, 0.2f), // dark gray
            new Color(0.5f, 0.5f, 0.5f), // medium gray
            new Color(0.8f, 0.8f, 0.8f), // light gray
            new Color(.95f,.95f, .95f),   // near-white

            new Color(0.6f, 0.2f, 0.2f), // medium red
            new Color(0.6f, 0.5f, 0.2f), // medium orange
            new Color(0.6f, 0.6f, 0.2f), // medium yellow
            new Color(0.2f, 0.6f, 0.2f), // medium green
            new Color(0.2f, 0.6f, 0.6f), // medium cyan
            new Color(0.2f, 0.2f, 0.6f), // medium blue
            new Color(0.5f, 0.2f, 0.6f), // medium purple
            new Color(0.5f, 0.2f, 0.2f), // medium magenta
            ];

        public static Color? MakeColorPicker(Rect inRect, Color color, ref bool draggingSlider, ref bool draggingHSV)
        {
            Rect rect = new(inRect);

            // We're making a layout with a wheel on the left and a brightness slider on the right and palette boxes right below the slider.
            const float wheelPct = 0.30f;
            float wheelWidth = rect.width * wheelPct;
            float wheelHeight = rect.height-8;
            float wheelSize = Mathf.Min(wheelWidth, wheelHeight);
            float sliderHeight = 18;
            float padding = 16;

            Rect wheelRect = new(rect.x, rect.y, wheelSize, wheelSize);
            Rect copyPasteRect = new(wheelRect.x, wheelRect.yMax, wheelRect.width, 30);
            Rect sliderRect = new(rect.x + wheelRect.xMax + padding, rect.y, rect.width - wheelSize - padding, sliderHeight);
            Rect paletteRect = new(sliderRect.x, sliderRect.yMax + 10, sliderRect.width, rect.height - sliderRect.height - 10);

            Color pasteColor = color;
            CopyPasteUI.DoCopyPasteButtons(copyPasteRect, () => AddColorToClipboard(color), () => PasteToColor(ref pasteColor));
            if (!pasteColor.IndistinguishableFromExact(color))
            {
                return pasteColor;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                draggingSlider = false;
                draggingHSV = false;
            }

            bool wasHsvChanged = false;
            Color.RGBToHSV(color, out float hue, out float sat, out float val);

            if (MakeBrightnessSlider(sliderRect, val, ref draggingSlider) is float newBrightness)
            {
                wasHsvChanged = true;
                val = newBrightness;
            }

            if (HSVColorWheel(wheelRect, ref hue, ref sat, ref val, ref draggingHSV))
            {
                wasHsvChanged = true;
            }

            if (GetPaletteColors(color, PaletteWithSkinClrs, paletteRect) is Color newColorFromPalette)
            {
                return newColorFromPalette;
            }

            else if (wasHsvChanged)
            {
                Color newColor = Color.HSVToRGB(hue, sat, val);
                return newColor;
            }
            return null;
        }

        private static void PasteToColor(ref Color color)
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            string[] parts = clipboard.Split(',');
            if (parts.Length >= 3
                && float.TryParse(parts[0], out float r)
                && float.TryParse(parts[1], out float g)
                && float.TryParse(parts[2], out float b))
            {
                float a = 1f;
                if (parts.Length >= 4)
                {
                    float.TryParse(parts[3], out a);
                }
                color = new Color(r, g, b, a);
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        private static void AddColorToClipboard(Color color)
        {
            string colorString = $"{color.r},{color.g},{color.b},{color.a}";
            GUIUtility.systemCopyBuffer = colorString;
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }

        private static float? MakeBrightnessSlider(Rect inRect, float brightness, ref bool dragging)
        {
            float newBrightness = brightness;
            GUI.DrawTexture(inRect, BrightnessTexture, ScaleMode.StretchToFill, alphaBlend: true);

            float handleSize = 22;
            float handleX = Mathf.Lerp(inRect.x - handleSize / 2, inRect.xMax - handleSize/2, brightness);
            float handleY = inRect.center.y - handleSize / 2f;
            GUI.DrawTexture(new Rect(handleX, handleY, handleSize, handleSize), Widgets.ColorSelectionCircle);
            var paddedRect = inRect.ExpandedBy(4f);
            if ((Event.current.type == EventType.MouseDown && inRect.Contains(Event.current.mousePosition))
                || (dragging && Event.current.type == EventType.MouseDrag && paddedRect.Contains(Event.current.mousePosition)))
            {
                float mouseX = Mathf.Clamp(Event.current.mousePosition.x, inRect.x, inRect.xMax);
                newBrightness = (mouseX - inRect.x) / inRect.width;
                newBrightness = Mathf.Clamp(newBrightness, 0.01f, 0.99f); // Avoid pure black or pure white so we don't lose the hue/sat info.
                dragging = true;
                Event.current.Use();
            }
            
            if (brightness == newBrightness)
            {
                return null;
            }
            return newBrightness;
        }

        public static bool HSVColorWheel(Rect inRect, ref float hue, ref float sat, ref float val, ref bool dragging, float? colorValueOverride = null, string controlName = null)
        {
            if (inRect.width != inRect.height)
            {
                throw new ArgumentException("HSV color wheel must be drawn in a square rect.");
            }
            float num = colorValueOverride ?? val;
            GUI.DrawTexture(inRect, Widgets.HSVColorWheelTex, ScaleMode.ScaleToFit, alphaBlend: true, 1f, Color.HSVToRGB(0f, 0f, num), 0f, 0f);
            float newHue = (hue + 0.25f) * 2f * MathF.PI;
            Vector2 vector = new Vector2(Mathf.Cos(newHue), 0f - Mathf.Sin(newHue)) * sat * inRect.width / 2f;
            Widgets.DrawColorSelectionCircle(inRect, Vector2Int.RoundToInt(vector + inRect.center), (num > 0.5f) ? Color.black : Color.white);
            var paddedRect = inRect.ExpandedBy(4f);
            if ((Event.current.type == EventType.MouseDown && inRect.Contains(Event.current.mousePosition))
                || (dragging && Event.current.type == EventType.MouseDrag && paddedRect.Contains(Event.current.mousePosition)))
            {
                GUI.FocusControl(controlName);
                Vector2 vector2 = (Event.current.mousePosition - inRect.center) / (inRect.size / 2f);
                newHue = Mathf.Atan2(0f - vector2.y, vector2.x) / (MathF.PI * 2f);
                newHue += 1.75f;
                newHue %= 1f;
                float newSat = Mathf.Clamp01(vector2.magnitude);
                dragging = true;

                Event.current.Use();
                if (newHue != hue || newSat != sat)
                {
                    hue = newHue;
                    sat = newSat;
                    return true;
                }
            }
            return false;
        }

        private static Color? GetPaletteColors(Color currClr, List<Color> palette, Rect inRect)
        {
            Color color = currClr;

            Widgets.ColorSelector(inRect, ref color, palette, out float height, null, 22, 2, ColorSelecterExtraOnGUI);
            float x = inRect.x;
            if (!color.IndistinguishableFromExact(currClr))
            {
                return color;
            }
            return null;
        }

        private static void ColorSelecterExtraOnGUI(Color color, Rect boxRect)
        {
            Texture2D texture2D = null;
            TaggedString taggedString = null;
            if (texture2D != null)
            {
                Rect position = boxRect.ContractedBy(4f);
                GUI.color = Color.black.ToTransparent(0.2f);
                GUI.DrawTexture(new Rect(position.x + 2f, position.y + 2f, position.width, position.height), texture2D);
                GUI.color = Color.white.ToTransparent(0.8f);
                GUI.DrawTexture(position, texture2D);
                GUI.color = Color.white;
            }
            if (!taggedString.NullOrEmpty())
            {
                TooltipHandler.TipRegion(boxRect, taggedString);
            }
        }


    }

}
