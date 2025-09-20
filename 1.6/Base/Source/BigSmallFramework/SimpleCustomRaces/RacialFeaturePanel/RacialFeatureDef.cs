using System;
using System.Collections.Generic;
using System.Linq;
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
        public string iconPath = "BS_Traits/DisguisedDemon";
        public Color? iconColor = Color.white;
        private string cachedDescription;

        public string Label => label;
        public Texture2D Icon
        {
            get
            {
                if (field == null)
                {
                    if (iconPath.NullOrEmpty())
                    {
                        field = BaseContent.BadTex;
                    }
                    else
                    {
                        field = ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                    }
                }
                return field;
            }
        }

        public Color IconColor => iconColor ?? Color.white;

        public string DescriptionFull => cachedDescription ??= GetDescriptionFull();


        protected string GetDescriptionFull()
        {
            StringBuilder stringBuilder = new();
            if (description.NullOrEmpty() == false)
            {
                stringBuilder.AppendLine(description);
            }
            
            return stringBuilder.ToString();
        }
    }

    public class RacialFeature : IRacialFeature
    {
        public string label = "Unnamed";
        public string description = "No description available.";
        public HediffDef hediffDescriptionSource = null;
        public string iconPath = "BS_Traits/Disguised";
        public Color? iconColor = Color.white;
        private string cachedDescription;

        public string Label => label;
        public string DescriptionFull => cachedDescription ??= GetDescriptionFull();
        public Texture2D Icon
        {
            get
            {
                if (field == null)
                {
                    if (iconPath.NullOrEmpty())
                    {
                        field = BaseContent.BadTex;
                    }
                    else
                    {
                        field = ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                    }
                }
                return field;
            }
        }
        public Color IconColor => iconColor ?? Color.white;
        protected string GetDescriptionFull()
        {
            StringBuilder stringBuilder = new();
            if (hediffDescriptionSource != null)
            {
                stringBuilder.AppendLine(hediffDescriptionSource.Description);
            }
            stringBuilder.AppendLine(description);
            
            return stringBuilder.ToString();
        }

        public RacialFeature SetupFromThis(List<PawnExtension> extensions)
        {
            var rf = new RacialFeature
            {
                label = label,
                description = description,
                iconPath = iconPath,
                iconColor = iconColor,
                hediffDescriptionSource = hediffDescriptionSource,
            };
            try
            {
                if (extensions.TryGetDescription(out string content))
                {
                    rf.cachedDescription = content + "\n\n" + rf.cachedDescription;
                }
            }
            catch (Exception e)
            {
                Log.ErrorOnce($"Caught Exception in RacialFeatureDef.SetupFromThis: {e.Message}\n{e.StackTrace}", 423589);
            }
            return rf;
        }
    }
}
