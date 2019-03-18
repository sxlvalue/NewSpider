using System.Collections.Generic;
using System.Text.RegularExpressions;
using DotnetSpider.Downloader;

namespace DotnetSpider.Data.Processor
{
    public class RegexPageFilter : IPageFilter
    {
        private readonly List<string> _patterns;
        private readonly List<string> _excludePatterns;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="patterns">需要匹配的正则</param>
        public RegexPageFilter(params string[] patterns) : this(patterns, null)
        {
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="patterns">需要匹配的正则</param>
        /// <param name="excludePatters">需要排除匹配的正则</param>
        public RegexPageFilter(IEnumerable<string> patterns, IEnumerable<string> excludePatters = null)
        {
            _patterns = patterns == null ? new List<string>() : new List<string>(patterns);
            _excludePatterns = excludePatters == null ? new List<string>() : new List<string>(excludePatters);
        }

        public bool Check(Request request)
        {
            if (_patterns.Count == 0 && _excludePatterns.Count == 0) return true;

            foreach (var pattern in _excludePatterns)
            {
                if (Regex.IsMatch(request.Url, pattern))
                {
                    return false;
                }
            }

            foreach (var pattern in _patterns)
            {
                if (Regex.IsMatch(request.Url, pattern))
                {
                    return true;
                }
            }

            return false;
        }
    }
}