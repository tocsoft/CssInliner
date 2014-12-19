using ExCSS.Model;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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

                var rules = await LoadCss();

                var matches = MatchStylesToNodes(rules);

                ApplyStylesToNodes(matches);

                if (_config.StripStyleTags)
                {
                    StripMatchingTags("descendant-or-self::style");
                }

                if (_config.StripLinkTags)
                {
                    StripMatchingTags("descendant-or-self::link[@type = 'text/css' and (@href)] | descendant-or-self::link[@rel = 'stylesheet' and (@href)]");
                }
            }

            return _document;
        }

        private void StripMatchingTags(string xpath)
        {
            var nodes = _document.DocumentNode.SelectNodes(xpath);

            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    n.Remove();
                }
            }
        }

        private IDictionary<HtmlNode, IEnumerable<ScopedProperty>> MatchStylesToNodes(IEnumerable<ScopedProperty> rules)
        {
            var allElements = _document.DocumentNode.SelectNodes("descendant-or-self::*");

            var matches = new Dictionary<HtmlNode, List<ScopedProperty>>();

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

            return matches.ToDictionary(x => x.Key, x => (IEnumerable<ScopedProperty>)x.Value);
        }

        private static void ApplyStylesToNodes(IDictionary<HtmlNode, IEnumerable<ScopedProperty>> matches)
        {
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

        private async Task<IEnumerable<ScopedProperty>> LoadCss()
        {
            Uri baseUri = GetBaseUri();

            List<ScopedProperty> results = new List<ScopedProperty>();
            var styleTags = _document.DocumentNode.SelectNodes("descendant-or-self::link[@type = 'text/css' and (@href)] | descendant-or-self::link[@rel = 'stylesheet' and (@href)] | descendant-or-self::style");

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

        private Uri GetBaseUri()
        {
            var rootBase = new Uri(_config.OverrideBaseDirectory, UriKind.RelativeOrAbsolute);
            if (!rootBase.IsAbsoluteUri)
            {
                rootBase = new Uri(Path.GetFullPath(_config.OverrideBaseDirectory) + "\\");
            }
            //lets load base tasg
            var baseTag = _document.DocumentNode.SelectSingleNode("descendant-or-self::base");
            if (baseTag != null)
            {
                var attrib = baseTag.Attributes.FirstOrDefault(x => x.Name.ToUpper() == "HREF");
                var relativeUri = "";
                if (attrib == null)
                {
                    relativeUri = attrib.Value;
                    rootBase = new Uri(rootBase, relativeUri);
                }
            }
            return rootBase;
        }
    }
}
