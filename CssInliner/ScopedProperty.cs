using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner
{
    internal class ScopedProperty
    {
        public ScopedProperty(ExCSS.Property property, ExCSS.BaseSelector selector, int selectorIndex)
        {
            Property = property;
            Scope = new ScopePropertyRank(selector, property.Important, selectorIndex);
        }

        public ScopedProperty(ExCSS.Property property)
        {
            Property = property;
            Scope = new ScopePropertyRank(property.Important);
        }

        public ScopePropertyRank Scope { get; private set; }

        public ExCSS.Property Property { get; private set; }

    }
}
