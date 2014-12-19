using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner
{
    public static class Processor
    {
        #region static
        public static InlinerConfig Default { get; set; }

        static Processor()
        {
            //default 
            Default = new InlinerConfig();
        }
        #endregion

        public static Task<string> Process(string htmlContents)
        {
            return Process(htmlContents, Default);
        }

        public static Task<string> Process(string htmlContents, InlinerConfig config)
        {
            var processor = new StringProcessor(htmlContents, config);

            return processor.Process();
        }
    }
}
