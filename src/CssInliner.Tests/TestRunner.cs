using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NUnit.Framework;
using Tocsoft.CssInliner.ResourceLoaders;
#if NET40
using Task = System.Threading.Tasks.TaskEx;
#endif

namespace Tocsoft.CssInliner.Tests
{
    public class FileMockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Uri _directoryUri;
        private readonly Uri _host;

        public FileMockHttpMessageHandler(string directory, Uri host)
        {
            _host = host;
            _directoryUri = new Uri(Path.GetFullPath(directory) + "\\");
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var relative = _host.MakeRelativeUri(request.RequestUri);

            var final = new Uri(_directoryUri, relative);
            var path = final.LocalPath;

            if (File.Exists(path))
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new System.Net.Http.ByteArrayContent(File.ReadAllBytes(path))
                });
            }
            else
            {
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
        }
    }

    [TestFixture]
    public class GeneralTests
    {

        IEnumerable<string> TestCaseFiles
        {
            get
            {
                return Directory.EnumerateFiles(".\\Tests\\", "*.html");
            }
        }

        IEnumerable<object[]> TestCases
        {
            get
            {
                return TestCaseFiles.Select(x => new object[] { TestCase.Load(x, new FileMockHttpMessageHandler(".\\www", new Uri("http://localhost"))) }).ToList();
            }
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void Run(TestCase test)
        {
            var processor = new StringProcessor(test.Source, test.Config);
            var result = processor.Process().GetAwaiter().GetResult();

            HtmlAssert.AreSame(test.Exprected, result);
        }
    }


    public static class HtmlAssert
    {

        public static void AreSame(string expected, string actual)
        {
            var cleanExpected = NormalizeHtml(expected);
            var cleanActual = NormalizeHtml(actual);
            Assert.AreEqual(cleanExpected, cleanActual);
        }


        private static string NormalizeHtml(string html)
        {
            var doc = new HtmlDocument();

            doc.OptionOutputUpperCase = true;


            doc.LoadHtml(html);

            //strip spacing from style tags and attributes

            var styleTags = doc.DocumentNode.SelectNodes("descendant-or-self::style");

            var p = new ExCSS.Parser();
            if (styleTags != null)
            {

                foreach (HtmlNode n in styleTags)
                {
                    var stylesheet = p.Parse(n.InnerHtml);
                    var minCss = stylesheet.ToString(false, 0);
                    n.InnerHtml = minCss;
                    //var newNode = HtmlNode.CreateNode("<style>" + minCss + "<style>");
                    //n.ParentNode.ReplaceChild(newNode, n);
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

    public static class ObjectExtensions
    {
        public static void SetProperty(this object obj, string propertyName, string value)
        {
            var p = obj.GetType().GetProperties().FirstOrDefault(x => x.Name.ToUpper() == propertyName.Trim().ToUpper());
            if (p != null)
            {
                object finalValue = null;
                if (p.PropertyType == typeof(bool))
                {
                    finalValue = bool.Parse(value);
                }
                else if (p.PropertyType == typeof(string))
                {
                    finalValue = value.Trim();
                }

                p.SetValue(obj, finalValue, new object[0]);
            }
        }

    }

    public class TestCase
    {
        public string Description { get; set; }
        public string FilePath { get; set; }
        public string Source { get; set; }
        public string Exprected { get; set; }
        public InlinerConfig Config { get; set; }

        public static TestCase Load(string path, HttpMessageHandler msghandler)
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

            if (parts.Count > 2)
            {
                res.Description = parts.Last().Trim();
            }

            res.FilePath = path;
            res.Config = new InlinerConfig();
            if (parts.Count == 4)
            {
                var configSettings = parts[2].Trim().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                var settings = configSettings.Select(x =>
                    {
                        var p = x.Split(new[] { ':' }, 2);
                        return new
                        {
                            propertyName = p[0],
                            value = p[1]
                        };
                    });

                foreach (var s in settings)
                {
                    res.Config.SetProperty(s.propertyName, s.value);
                }
            }


            res.Config.ResourceLoaders.OfType<HttpResourceLoader>().Single().Client = new System.Net.Http.HttpClient(msghandler);

            return res;
        }

        public override string ToString()
        {
            return Path.GetFileName(FilePath) + " - " + Description;
        }
    }
}
