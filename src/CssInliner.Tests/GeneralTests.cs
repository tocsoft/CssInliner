using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NUnit.Framework;

namespace Tocsoft.CssInliner.Tests
{
    [TestFixture]
    public class GeneralTests
    {
        private InlinerConfig config;
        private HtmlProcessor processor;

        [SetUp]
        public void SetUp()
        {
            config = new InlinerConfig();
        }

        IEnumerable<TestCase> TestCases
        {
            get
            {
                var sources = Directory.EnumerateFiles(".\\Tests\\", "*.*");

                return sources.Select(x => TestCase.Load(x));
            }
        }

        [Test]
        [TestCaseSource("TestCases")]
        public async Task Run(TestCase test)
        {

            var processor = new StringProcessor(test.Source, config);
            var result = await processor.Process();

            AreSame(test.Exprected, result);
        }

        public class TestCase
        {
            public string Name { get; set; }
            public string Source { get; set; }
            public string Exprected { get; set; }

            public static TestCase Load(string path)
            {
                List<string> parts = new List<string>();
                using (var r = File.OpenText(path))
                {
                    var line = "";
                    StringBuilder sb = new StringBuilder();
                    while ((line = r.ReadLine()) != null)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.Any() && trimmed.All(x => x == '='))
                        {
                            parts.Add(sb.ToString());
                            sb.Clear();
                        }
                        else
                        {
                            sb.AppendLine(line);
                        }
                    }
                    if (sb.Length > 0)
                    {
                        parts.Add(sb.ToString());
                    }
                }
                parts.Reverse();


                TestCase res = new TestCase();
                res.Exprected = parts.First();
                res.Source = parts.Skip(1).First();

                if (parts.Count == 3)
                {
                    res.Name = parts.Last().Trim();
                }
                if (string.IsNullOrWhiteSpace(res.Name))
                {
                    res.Name = Path.GetFileNameWithoutExtension(path);
                }

                return res;
            }

            public override string ToString()
            {
                return Name;
            }
        }


        private void AreSame(string expected, string actual)
        {
            Assert.AreEqual(NormalizeHtml(expected), NormalizeHtml(actual));
        }

        private string NormalizeHtml(string html)
        {
            var doc = new HtmlDocument();

            doc.OptionOutputAsXml = true;
            doc.OptionOutputUpperCase = true;


            doc.LoadHtml(html);

            //strip spacing from style tags and attributes

            var styleTags = doc.DocumentNode.SelectNodes("descendant-or-self::style");

            var p = new ExCSS.Parser();
            if (styleTags != null)
            {

                foreach (HtmlNode n in styleTags)
                {
                    var stylesheet = p.Parse(n.InnerText);

                    n.ParentNode.ReplaceChild(HtmlNode.CreateNode("<style>" + stylesheet.ToString(false, 0) + "<style>"), n);
                }
            }
            var styledElements = doc.DocumentNode.SelectNodes("descendant-or-self::*[@style]");
            if (styledElements != null)
            {
                foreach (HtmlNode n in styledElements)
                {
                    var style = "* {" + n.Attributes["style"].Value + "}";

                    var stylesheet = p.Parse(style);
                    n.Attributes["style"].Value = stylesheet.StyleRules.Single().ToString(false, 0);
                }
            }

            //remove "empty" text nodes
            var allNodes = doc.DocumentNode.DescendantsAndSelf();
            if (allNodes != null)
            {
                var txtNodes = allNodes.Where(x => x.Name == "#text");
                var emptyNodes = txtNodes.Where(x => string.IsNullOrWhiteSpace(x.InnerText)).ToList();

                foreach (HtmlNode n in emptyNodes)
                {
                    n.Remove();
                }
            }

            var sb = new StringBuilder();
            using (var tw = new StringWriter(sb))
            {
                doc.Save(tw);
            }
            var finalhtml = sb.ToString();

            return finalhtml;
        }
    }
}
