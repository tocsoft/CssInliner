using System;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner
{
    public interface IResourceLoader
    {
        bool CanLoad(Uri uri);
        Task<string> Load(Uri uri);
    }
}