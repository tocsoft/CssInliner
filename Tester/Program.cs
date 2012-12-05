using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tester
{
	class Program
	{
		static void Main(string[] args)
		{
			var html = File.ReadAllText("sample.html");
			
			var cleanHtml = CssInliner.Inliner.InlineCssIntoHtml(html);
			File.WriteAllText("sampleOutput.html", cleanHtml);
		}
	}
}
