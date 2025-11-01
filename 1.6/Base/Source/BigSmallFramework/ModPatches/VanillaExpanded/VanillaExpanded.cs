using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace BigAndSmall
{
    public static class VanillaExpanded
    {
        private static bool? _veActive = null;
        /// <summary>
        /// Checks if Vanlla Expanded is loaded.
        /// </summary>
        public static bool VEActive => _veActive ??= ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");
    }
}