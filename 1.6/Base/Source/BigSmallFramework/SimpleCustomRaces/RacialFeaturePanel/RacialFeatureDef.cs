using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace BigAndSmall
{
    public interface IRacialFeature
    {
        public string Label { get; }
        public string DescriptionFull { get; }
        public Texture2D Icon { get; }
        public Color IconColor { get; }
    }

    public class RacialFeatureDef : Def, IRacialFeature
    {
        [NoTranslate]
        public string iconPath = "BS_DefaultIcon";
        public Color? iconColor = Color.white;
        private string cachedDescription;

        [Unsaved(false)]
        private Texture2D cachedIcon;

        public HediffDef hediffDescriptionSource = null;
        public string Label => label;
        public Texture2D Icon
        {
            get
            {
                if (cachedIcon == null)
                {
                    if (iconPath.NullOrEmpty())
                    {
                        cachedIcon = BaseContent.BadTex;
                    }
                    else
                    {
                        cachedIcon = ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                    }
                }
                return cachedIcon;
            }
        }

        public Color IconColor => iconColor.HasValue ? iconColor.Value : Color.white;

        public string DescriptionFull => cachedDescription ??= GetDescriptionFull();


        protected string GetDescriptionFull()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(description);
            stringBuilder.AppendLine();
            if (hediffDescriptionSource != null)
            {
                stringBuilder.AppendLine(hediffDescriptionSource.Description);
            }
            return stringBuilder.ToString();
        }
    }

    public class RacialFeature : IRacialFeature
    {
        public string label = "Unnamed";
        public string description = "No description available.";
        public HediffDef hediffDescriptionSource = null;
        public string iconPath = "BS_DefaultIcon";
        public Color? iconColor = Color.white;
        private string cachedDescription;
        private Texture2D cachedIcon;


        public string Label => label;
        public string DescriptionFull => cachedDescription ??= GetDescriptionFull();
        public Texture2D Icon
        {
            get
            {
                if (cachedIcon == null)
                {
                    if (iconPath.NullOrEmpty())
                    {
                        cachedIcon = BaseContent.BadTex;
                    }
                    else
                    {
                        cachedIcon = ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                    }
                }
                return cachedIcon;
            }
        }
        public Color IconColor => iconColor.HasValue ? iconColor.Value : Color.white;
        protected string GetDescriptionFull()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(description);
            stringBuilder.AppendLine();
            if (hediffDescriptionSource != null)
            {
                stringBuilder.AppendLine(hediffDescriptionSource.Description);
            }
            return stringBuilder.ToString();
        }
    }
}
