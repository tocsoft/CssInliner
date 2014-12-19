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
}
