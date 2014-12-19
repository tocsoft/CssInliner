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

        IEnumerable<object[]> TestCases
        {
            get
            {
                var sources = Directory.EnumerateFiles(".\\Tests\\", "*.*");

                return sources.Select(x => new object[] { TestCase.Load(x) }).ToList();
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
