using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Moq;
using NUnit.Framework;
using Tocsoft.CssInliner.ResourceLoaders;

namespace Tocsoft.CssInliner.Tests
{
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
        public async Task Run(TestCase test)
        {



            var processor = new StringProcessor(test.Source, test.Config);
            var result = await processor.Process();

            HtmlAssert.AreSame(test.Exprected, result);
        }
    }
}
