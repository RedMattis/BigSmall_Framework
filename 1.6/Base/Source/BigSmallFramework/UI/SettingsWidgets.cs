using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class SettingsWidgets
    {
        private static Dictionary<string, string> inputBuffers = new();

        public static bool NearlyEquals(this float a, float b, float tolerance = 0.01f)
        {
            return Math.Abs(a - b) < tolerance;
        }

        private static string SanitizeNumericInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var chars = new List<char>(input.Length);
            int i = 0;

            // Allow leading minus
            if (input[0] == '-') chars.Add(input[i++]);

            bool hasDecimal = false;
            for (; i < input.Length; i++)
            {
                char c = input[i];
                if (char.IsDigit(c))
                    chars.Add(c);
                else if ((c == '.' || c == ',') && !hasDecimal)
                {
                    chars.Add('.');
                    hasDecimal = true;
                }
            }
            return new string(chars.ToArray());
        }

        //public static void CreateSettingsSlider(Listing_Standard listingStandard, string labelName, ref float value,
        //    float min = 0, float max = 10, Func<float, string> valueFormatter = null, string bufferKey = null)
        //{
        //    static string SetBufferFromValue(float value, Func<float, string> valueFormatter) =>
        //        valueFormatter != null ? valueFormatter(value) : value.ToString("F1");


        //    bufferKey ??= labelName;

        //    if (!inputBuffers.TryGetValue(bufferKey, out var buffer))
        //        buffer = SetBufferFromValue(value, valueFormatter);

        //    if (buffer == null)
        //    {
        //        buffer = SetBufferFromValue(value, valueFormatter);
        //    }
        //    else if (!float.TryParse(buffer, out float parsedValue) || !NearlyEquals(parsedValue, value))
        //    {
        //        buffer = SetBufferFromValue(value, valueFormatter);
        //    }

        //    // Define a total rect for one row of slider and label
        //    Rect fullRow = listingStandard.GetRect(Text.LineHeight);

        //    // Divide the row into segments for the label, the slider, and the value text
        //    float labelWidth = fullRow.width * 0.46f;
        //    float sliderWidth = fullRow.width * 0.45f;
        //    float valueWidth = fullRow.width * 0.09f;

        //    Rect labelRect = new(fullRow.x, fullRow.y, labelWidth, fullRow.height);
        //    Rect sliderRect = new(labelRect.xMax, fullRow.y, sliderWidth, fullRow.height);
        //    Rect valueRect = new(sliderRect.xMax, fullRow.y, valueWidth, fullRow.height);

        //    Widgets.Label(labelRect, labelName);
        //    value = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
        //    Widgets.TextFieldNumeric<float>(valueRect, ref value, ref buffer, min: min, max: max);

        //    inputBuffers[bufferKey] = buffer;
        //}



        //public static void CreateSettingsSlider(Listing_Standard listingStandard, string labelName, ref float value, float min = 0, float max = 10, Func<float, string> valueFormatter = null)
        //{
        //    // Define a total rect for one row of slider and label
        //    Rect fullRow = listingStandard.GetRect(Text.LineHeight);

        //    // Divide the row into segments for the label, the slider, and the value text
        //    float labelWidth = fullRow.width * 0.46f;
        //    float sliderWidth = fullRow.width * 0.45f;
        //    float valueWidth = fullRow.width * 0.09f;

        //    Rect labelRect = new(fullRow.x, fullRow.y, labelWidth, fullRow.height);
        //    Rect sliderRect = new(labelRect.xMax, fullRow.y, sliderWidth, fullRow.height);
        //    Rect valueRect = new(sliderRect.xMax, fullRow.y, valueWidth, fullRow.height);

        //    // Draw the label, slider, and value on the respective Rects
        //    Widgets.Label(labelRect, labelName);
        //    value = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
        //    if (valueFormatter != null)
        //    {
        //        Widgets.Label(valueRect, valueFormatter(value));
        //    }
        //    else
        //    {
        //        Widgets.Label(valueRect, $"{value:F1}");
        //    }
        //}

        public static void CreateSettingsSlider(Listing_Standard listingStandard, string labelName, ref float value, ref string buffer, float min = 0, float max = 10, Func<float, string> valueFormatter = null)
        {
            // Define a total rect for one row of slider and label
            Rect fullRow = listingStandard.GetRect(Text.LineHeight);

            // Divide the row into segments for the label, the slider, and the value text
            float labelWidth = fullRow.width * 0.46f;
            float sliderWidth = fullRow.width * 0.45f;
            float valueWidth = fullRow.width * 0.09f;

            Rect labelRect = new(fullRow.x, fullRow.y, labelWidth, fullRow.height);
            Rect sliderRect = new(labelRect.xMax, fullRow.y, sliderWidth, fullRow.height);
            Rect valueRect = new(sliderRect.xMax, fullRow.y, valueWidth, fullRow.height);

            Widgets.Label(labelRect, labelName);
            value = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
            TextFieldNumericFloat(valueRect, ref value, ref buffer, min: min, max: max, valueFormatter: valueFormatter);
        }

        public static void TextFieldNumericFloat(Rect rect, ref float val, ref string buffer, float min = 0f, float max = 1E+09f, Func<float, string> valueFormatter = null)
        {
            static string SetBufferFromValue(float value, Func<float, string> valueFormatter) =>
                valueFormatter != null ? valueFormatter(value) : value.ToString("F1");
            buffer ??= val.ToString();
            GUI.SetNextControlName("TextField" + rect.y.ToString("F0") + rect.x.ToString("F0"));

            if (!float.TryParse(buffer, out float parsedValue1) || !NearlyEquals(parsedValue1, val))
                buffer = val.ToString("F2");
            string displayText = SetBufferFromValue(val, valueFormatter);
            bool percent = displayText.EndsWith('%');
            string editText = TextField(rect, displayText);
            string cleanText = editText.Replace("%", "");

            if (cleanText != buffer && float.TryParse(buffer, out float parsedValue))
            {
                buffer = cleanText;
                ResolveParseNow(cleanText, ref val, ref buffer, percent, min, max);
            }

            static void ResolveParseNow(string edited, ref float val, ref string buffer, bool percent, float min, float max)
            {
                if (edited.NullOrEmpty())
                {
                    ResetValue(edited, ref val, ref buffer, min, max);
                }
                else if (float.TryParse(edited, out var result2))
                {
                    if (percent) result2 = result2 /= 100f;
                    val = Mathf.Clamp(result2, min, max);
                    buffer = val.ToString();
                }
            }
            static void ResetValue(string edited, ref float val, ref string buffer, float min, float max)
            {
                val = default;
                if (min > 0f) val = Mathf.RoundToInt(min);
                if (max < 0f) val = Mathf.RoundToInt(max);
                buffer = val.ToString();
            }
        }

        public static string TextField(Rect rect, string text)
        {
            text ??= "";
            return GUI.TextField(rect, text, Text.CurTextFieldStyle);
        }

        public static void CreateSettingCheckbox(Listing_Standard listingStandard, string labelName, ref bool value, bool disabled=false)
        {
            Rect fullRow = listingStandard.GetRect(Text.LineHeight);
            // Divide the row into two segments for the label and the checkbox
            float labelWidth = fullRow.width * 0.90f;
            float checkboxWidth = fullRow.width * 0.1f; 
            Rect labelRect = new(fullRow.x, fullRow.y, labelWidth, fullRow.height);
            Rect checkboxRect = new(labelRect.xMax, fullRow.y, checkboxWidth, fullRow.height);

            Widgets.Label(labelRect, labelName);
            Widgets.Checkbox(checkboxRect.position, ref value, disabled: disabled);
        }

        public static void CreateRadioButtonsTwoOptions(Listing_Standard lst, string labelName, ref bool value, string optionTrue, string optionFalse)
        {
            const float labelWidth = 0.55f;
            const float radioWidth = 0.45f;

            lst.GapLine();
            Rect radioRect = lst.GetRect(Text.LineHeight * 2);

            Rect firstRow = new(radioRect.x, radioRect.y, radioRect.width, Text.LineHeight);
            Rect labelRect = new(firstRow.x, firstRow.y, firstRow.width * labelWidth, firstRow.height);
            Rect radioTrueRect = new(labelRect.xMax, firstRow.y, firstRow.width * radioWidth, firstRow.height);
            Widgets.Label(labelRect, labelName);
            Widgets.Label(radioTrueRect, optionTrue);
            if (Widgets.RadioButton(radioTrueRect.x-32, radioTrueRect.y, value))
            {
                value = true;
            }

            // Second line
            Rect secondRow = new(radioRect.x, firstRow.yMax + lst.verticalSpacing, radioRect.width, Text.LineHeight);
            Rect dummyLabelRect = new(secondRow.x, secondRow.y, secondRow.width * labelWidth, secondRow.height);
            Rect radioFalseRect = new(dummyLabelRect.xMax, secondRow.y, secondRow.width * radioWidth, secondRow.height);
            Widgets.Label(radioFalseRect, optionFalse);
            if (Widgets.RadioButton(radioFalseRect.x-32, radioFalseRect.y, !value))
            {
                value = false;
            }
            lst.GapLine();
        }
    }
}
