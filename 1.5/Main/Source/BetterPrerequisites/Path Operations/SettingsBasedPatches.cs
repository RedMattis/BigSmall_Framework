using System.Text.RegularExpressions;
using System.Xml;
using Verse;

namespace BigAndSmall
{
    public abstract class PatchOp_IfSettings : PatchOperation
    {
        protected readonly PatchOperation match;
        protected readonly PatchOperation nomatch;
        abstract protected bool ShouldApply();
        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (ShouldApply())
            {
                if (match != null)
                {
                    return match.Apply(xml);
                }
            }
            else if (nomatch != null)
            {
                return nomatch.Apply(xml);
            }
            return true;
        }
    }

    public class PatchOp_AddBionics : PatchOp_IfSettings { protected override bool ShouldApply() => BigSmallMod.settings.surgeryAndBionics; }
    public class PatchOp_FantasyEnabled : PatchOp_IfSettings { protected override bool ShouldApply() => BigSmallMod.settings.useFantasyNames; }



    public class PatchOp_ReplaceText : PatchOperationPathed
    {
        private string oldText;
        private string newText;
        private bool wholeWordsOnly = false;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            bool result = false;
            // Select nodes matching the XPath
            XmlNodeList nodes = xml.SelectNodes(xpath);
            if (nodes == null || nodes.Count == 0) return false;

            string oldTextLower = oldText.ToLower();

            foreach (XmlNode node in nodes)
            {
                if (node.NodeType == XmlNodeType.Element || node.NodeType == XmlNodeType.Text)
                {
                    // Replace text content in the node
                    if (!string.IsNullOrEmpty(node.InnerText) && node.InnerText.ToLower().Contains(oldTextLower))
                    {

                        string pattern = wholeWordsOnly
                        ? @"\b" + Regex.Escape(oldTextLower) + @"\b"  // Regex pattern for whole word replacement
                        : Regex.Escape(oldTextLower);  // Simple substring replacement pattern

                        node.InnerText = Regex.Replace(node.InnerText, pattern, matchingV =>
                        {
                            string matchedText = matchingV.Value;
                            // Check if the first letter of matched text was uppercase
                            if (char.IsUpper(matchedText[0]))
                            {
                                // Capitalize the first letter of new text
                                return char.ToUpper(newText[0]) + newText.Substring(1);
                            }
                            else
                            {
                                // Otherwise, use the new text as-is
                                return newText;
                            }
                        }, RegexOptions.IgnoreCase);

                        result = true;
                    }
                }
            }
            return result;
        }
    }

}
