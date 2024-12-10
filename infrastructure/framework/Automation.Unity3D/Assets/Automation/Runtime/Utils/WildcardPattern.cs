
using System.Text.RegularExpressions;

namespace Automation.Runtime.Utils
{
    public class WildcardPattern
    {
        //-------------------------------------------------------------------------
        private Regex regex;
        
        //-------------------------------------------------------------------------
        public WildcardPattern(string pattern)
        {
            pattern = "^" + pattern.Replace(".", @"\.").Replace("?", ".").Replace("*", ".*") + "$";
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
        }
        
        //-------------------------------------------------------------------------
        public bool IsMatch(string text)
        {
            return regex.IsMatch(text);
        }
    }
}