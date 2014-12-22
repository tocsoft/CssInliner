using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
}
