using System.Xml;
using Verse;

namespace BigAndSmall.Utilities;

public class HediffChance
{
    public HediffDef hediff;
    public float chance;
    
    public HediffChance()
    {
    }

    public HediffChance(HediffDef hediff, float chance)
    {
        this.hediff = hediff;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "hediff", xmlRoot.Name, null, null, null);
        chance = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
    }
}