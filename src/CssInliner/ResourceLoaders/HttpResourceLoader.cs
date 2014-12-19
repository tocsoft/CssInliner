using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner.ResourceLoaders
{
    public class HttpResourceLoader : IResourceLoader
    {
        public System.Net.Http.HttpClient Client { get; set; }

        private System.Net.Http.HttpClient GetClient()
        {
            return Client ?? new System.Net.Http.HttpClient();
        }

        private string[] supportedSchemes = new[] {
            "HTTP",
            "HTTPS"
        };

        public bool CanLoad(Uri uri)
        {
            return supportedSchemes.Contains(uri.Scheme.ToUpper());
        }

        public async Task<string> Load(Uri uri)
        {
            var response = await GetClient().GetAsync(uri);

            return await response.Content.ReadAsStringAsync();
        }
    }
}
