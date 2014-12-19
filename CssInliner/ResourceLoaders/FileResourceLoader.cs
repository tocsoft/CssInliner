using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner.ResourceLoaders
{
    public class FileResourceLoader : IResourceLoader
    {
        public bool CanLoad(Uri uri)
        {
            return uri.Scheme.ToUpper().StartsWith("FILE");
        }

        public Task<string> Load(Uri uri)
        {
            var path = uri.LocalPath;

            var f = new FileInfo(path);
            using (var s = f.OpenRead())
            using (var sr = new StreamReader(s))
            {
                return sr.ReadToEndAsync();
            }
        }
    }
}
