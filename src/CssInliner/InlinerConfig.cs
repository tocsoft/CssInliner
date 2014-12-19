using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tocsoft.CssInliner.ResourceLoaders;

namespace Tocsoft.CssInliner
{
    public class InlinerConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InlinerConfig"/> class.
        /// </summary>
        public InlinerConfig()
        {
            ResourceLoaders = new List<IResourceLoader>()
            {
                new FileResourceLoader(),
                new HttpResourceLoader()
            };

            OverrideBaseDirectory = Path.GetFullPath(".");

            StripStyleTags = false;
            StripLinkTags = false;
        }

        /// <summary>
        /// Gets or sets the resource loaders.
        /// </summary>
        /// <value>
        /// The resource loaders.
        /// </value>
        public ICollection<IResourceLoader> ResourceLoaders { get; private set; }

        /// <summary>
        /// Gets or sets the base directory.
        /// </summary>
        /// <value>
        /// The base directory.
        /// </value>
        public string OverrideBaseDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [strip style tags].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [strip style tags]; otherwise, <c>false</c>.
        /// </value>
        public bool StripStyleTags { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [strip link tags].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [strip link tags]; otherwise, <c>false</c>.
        /// </value>
        public bool StripLinkTags { get; set; }
    }

}
