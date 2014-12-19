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
    public class HtmlProcessor
    {
        private readonly InlinerConfig _config;

        private readonly ExCSS.Parser cssParser = new ExCSS.Parser();
        private readonly HtmlDocument _document;

        bool processed = false;

        public HtmlProcessor(HtmlAgilityPack.HtmlDocument document, InlinerConfig config)
        {
            _config = config;

            _document = document;
        }

        public async Task<HtmlDocument> Process()
        {
            if (!processed)
            {
                Uri baseUri = GetBaseUri(_document);

                var rules = await LoadCss(baseUri, _document);

                var allElements = _document.DocumentNode.SelectNodes("descendant-or-self::*");

                Dictionary<HtmlNode, List<ScopedProperty>> matches = new Dictionary<HtmlNode, List<ScopedProperty>>();

                foreach (var n in allElements)
                {
                    foreach (var r in rules)
                    {
                        if (n.IsMatch(r.Scope.Selector))
                        {
                            if (!matches.ContainsKey(n))
                            {
                                matches.Add(n, new List<ScopedProperty>());
                            }
                            matches[n].Add(r);
                        }

                        //TODO update the appending css rule to make sure the correctly prioritised rule is used
                        if (n.Attributes.Contains("style"))
                        {
                            var inlineStyles = n.Attributes["style"].Value;

                            var inline = cssParser.Parse("* { " + inlineStyles + "}")
                                .StyleRules.SelectMany(x => ConvertInline(x.Declarations));

                            if (!matches.ContainsKey(n))
                            {
                                matches.Add(n, new List<ScopedProperty>());
                            }
                            matches[n].AddRange(inline);
                        }
                    }
                }

                //go through and update all styles
                foreach (var nodeRules in matches)
                {
                    var node = nodeRules.Key;

                    var orderd = nodeRules.Value.OrderByDescending(x => x.Scope).ToList();
                    var declerations = nodeRules.Value.GroupBy(x => x.Property.Name).Select(x => x.OrderByDescending(r => r.Scope).First());

                    StringBuilder stylesSB = new StringBuilder();
                    foreach (var d in declerations)
                    {
                        stylesSB.Append(d.Property.ToString());
                        stylesSB.Append(";");
                    }

                    if (!node.Attributes.Contains("style"))
                    {
                        node.Attributes.Add("style", stylesSB.ToString());
                    }
                    else
                    {
                        node.Attributes["style"].Value = stylesSB.ToString();
                    }
                }
            }

            return _document;
        }

        private async Task<IEnumerable<ScopedProperty>> LoadCss(Uri baseUri, HtmlDocument doc)
        {
            List<ScopedProperty> results = new List<ScopedProperty>();
            var styleTags = doc.DocumentNode.SelectNodes("descendant-or-self::link[@type = 'text/css' and (@href)] | descendant-or-self::link[@rel = 'stylesheet' and (@href)] | descendant-or-self::style");

            if (styleTags != null)
            {
                var styleTasks = styleTags.Select(x => LoadCss(baseUri, x)).ToArray();

                await Task.WhenAll(styleTasks);

                var rules = styleTasks.SelectMany(x => x.Result);

                foreach (var r in rules)
                {
                    var props = Convert(r, results.Count);
                    results.AddRange(props);
                }
            }
            return results;
        }

        private IEnumerable<ScopedProperty> Convert(ExCSS.StyleRule rule, int startIdx)
        {
            return Convert(rule.Selector, rule.Declarations, startIdx);
        }

        private IEnumerable<ScopedProperty> ConvertInline(ExCSS.StyleDeclaration declaration)
        {
            foreach (var p in declaration)
            {
                yield return new ScopedProperty(p);
            }
        }

        private IEnumerable<ScopedProperty> Convert(ExCSS.BaseSelector selector, ExCSS.StyleDeclaration declaration, int startIdx)
        {
            var multiSelectorList = selector as ExCSS.MultipleSelectorList;
            if (multiSelectorList != null)
            {

                foreach (var sel in multiSelectorList)
                {
                    var result = Convert(sel, declaration, startIdx);
                    foreach (var r in result)
                    {
                        startIdx++;
                        yield return r;
                    }
                }
            }
            else
            {
                foreach (var p in declaration)
                {
                    yield return new ScopedProperty(p, selector, startIdx++);
                }
            }
        }

        private async Task<IEnumerable<ExCSS.StyleRule>> LoadCss(Uri baseUri, HtmlNode node)
        {
            var css = "";
            if (node.Name.ToUpper() == "STYLE")
            {
                //inline 
                css = node.InnerText;
            }
            else
            {
                var hrefAttrib = node.Attributes.FirstOrDefault(x => x.Name.ToUpper() == "HREF");
                if (hrefAttrib != null)
                {
                    var uri = new Uri(baseUri, hrefAttrib.Value);

                    var loader = _config.ResourceLoaders.FirstOrDefault(x => x.CanLoad(uri));
                    if (loader != null)
                    {
                        css = await loader.Load(uri);
                    }
                }
            }

            var stylesheet = cssParser.Parse(css);

            return stylesheet.StyleRules;
        }

        private Uri GetBaseUri(HtmlDocument doc)
        {
            string uriBaseString = _config.OverrideBaseDirectory;

            //lets load base tasg
            var baseTag = doc.DocumentNode.SelectSingleNode("descendant-or-self::base");
            if (baseTag != null)
            {
                var attrib = baseTag.Attributes.FirstOrDefault(x => x.Name.ToUpper() == "HREF");
                if (attrib == null)
                {
                    uriBaseString = attrib.Value;
                }
            }

            var baseUri = new Uri(uriBaseString);
            return baseUri;
        }
    }
}
