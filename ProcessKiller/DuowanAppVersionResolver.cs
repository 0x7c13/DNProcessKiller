
namespace ProcessKiller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using HtmlAgilityPack;
    using System.Text.RegularExpressions;

    public class DuowanAppVersionResolver : IAppVersionResolver
    {
        private readonly string _pageUrl;

        public DuowanAppVersionResolver(string pageUrl)
        {
            _pageUrl = pageUrl;
        }

        public Version GetLatestVersion()
        {
            var doc = new HtmlWeb().Load(_pageUrl);
            var title = doc?.DocumentNode?.SelectSingleNode("//head/title")?.InnerHtml;
            if (title != null)
            {
                return _parseVersionFromTitle(title);
            }
            else
            {
                throw new Exception("Cannot find title node.");
            }
        }

        private Version _parseVersionFromTitle(string title)
        {
            var versionString = title.Split(new char[] {'[', ']'}, StringSplitOptions.RemoveEmptyEntries).First(t => t.StartsWith("V", StringComparison.CurrentCultureIgnoreCase));
            return new Version(versionString.Substring(1));
        }
    }
}
