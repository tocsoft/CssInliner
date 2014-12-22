using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tocsoft.CssInliner.ResourceLoaders;

namespace Tocsoft.CssInliner.Tests
{

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
