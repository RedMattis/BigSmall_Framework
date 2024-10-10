using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public static class SettingsWidgets
    {
        public static void CreateSettingsSlider(Listing_Standard listingStandard, string labelName, ref float value, float min=0, float max=10, Func<float,string> valueFormatter = null)
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

            // Draw the label, slider, and value on the respective Rects
            Widgets.Label(labelRect, labelName);
            value = Widgets.HorizontalSlider(sliderRect, value, min, max, true);
            if (valueFormatter != null)
            {
                Widgets.Label(valueRect, valueFormatter(value));
            }
            else
            {
                Widgets.Label(valueRect, $"{value:F1}");
            }
        }

        public static void CreateSettingCheckbox(Listing_Standard listingStandard, string labelName, ref bool value)
        {
            Rect fullRow = listingStandard.GetRect(Text.LineHeight);
            // Divide the row into two segments for the label and the checkbox
            float labelWidth = fullRow.width * 0.90f;
            float checkboxWidth = fullRow.width * 0.1f; 
            Rect labelRect = new(fullRow.x, fullRow.y, labelWidth, fullRow.height);
            Rect checkboxRect = new(labelRect.xMax, fullRow.y, checkboxWidth, fullRow.height);

            Widgets.Label(labelRect, labelName);
            Widgets.Checkbox(checkboxRect.position, ref value);
        }
    }
}
