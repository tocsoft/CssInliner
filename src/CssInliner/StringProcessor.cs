using ExCSS.Model;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner
{
    public class StringProcessor
    {
        string _result;
        private readonly string _contents;
        private readonly InlinerConfig _config;

        public StringProcessor(string contents, InlinerConfig config)
        {
            _contents = contents;
            _config = config;
        }

        public async Task<string> Process()
        {
            if (_result == null)
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(_contents);
                var htmlProcessor = new HtmlProcessor(doc, _config);
                var res = await htmlProcessor.Process();
                var sb = new StringBuilder();
                using (var tw = new System.IO.StringWriter(sb))
                {
                    res.Save(tw);
                }
                _result = sb.ToString();
            }

            return _result;
        }
    }
}
