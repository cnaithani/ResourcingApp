using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResourcingToolKit.Classes
{
    internal static class Utils
    {
        public static string EscapeXmlCharacters(this string input)
        {
            switch (input)
            {
                case null: return null;
                case "": return "";
                default:
                    {
                        input = input.Replace("&", "&amp;")
                            .Replace("'", "&apos;")
                            .Replace("\"", "&quot;")
                            .Replace(">", "&gt;")
                            .Replace("<", "&lt;");

                        return input;
                    }
            }
        }
    }
}
