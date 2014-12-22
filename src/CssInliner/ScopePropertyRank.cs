using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tocsoft.CssInliner
{
    internal class ScopePropertyRank : IComparable
    {
        public ScopePropertyRank(ExCSS.BaseSelector selector, bool IsImportant, int selectorIndex)
        {
            Selector = selector;


            Add(selector);

            if (IsImportant)
                Important++;

            Index = selectorIndex;
        }
        public ExCSS.BaseSelector Selector { get; private set; }

        public ScopePropertyRank(bool IsImportant)
        {
            Inline++;
            if (IsImportant)
                Important++;
        }

        private void Add(ExCSS.BaseSelector selector)
        {
            var complex = selector as ExCSS.ComplexSelector;
            var agg = selector as ExCSS.AggregateSelectorList;
            if (complex != null)
            {
                foreach (var s in complex)
                    Add(s.Selector);
            }else if (agg != null)
            {
                foreach (var s in agg)
                    Add(s);
            }
            else 
            {
                if (selector is ExCSS.ClassSelector) {
                    Class++;
                }

                if (selector is ExCSS.AttributeSelector
                    || selector is ExCSS.PseudoClassSelector
                    || selector is ExCSS.PseudoElementSelector)
                {
                    Attribute++;
                }

                if (selector is ExCSS.ElementSelector)
                {
                    Element++;
                }

                if (selector is ExCSS.IdSelector)
                {
                    Id++;
                }                
            }
        }

        public int Important { get; private set; }
        public int Inline { get; private set; }
        public int Id { get; private set; }
        public int Element { get; private set; }
        public int Class { get; private set; }
        public int Attribute { get; private set; }

        public int Index { get; private set; }

        internal int Total
        {
            get
            {
                return Important * 10000 +
                    Inline * 1000 +
                    Id * 100 +
                    Class * 10 +
                    Attribute * 10 +
                    Element;
            }
        }

        public int CompareTo(object obj)
        {
            var t = obj as ScopePropertyRank;
            if (t == null)
                return -1;

            //return the comparitor for the first filed that doesn't excatly match... using the default of int for 0;
            return InnerCompareTo(t).Where(x => x != 0).FirstOrDefault();
        }

        private IEnumerable<int> InnerCompareTo(ScopePropertyRank comparer)
        {
            yield return this.Important.CompareTo(comparer.Important);
            yield return this.Inline.CompareTo(comparer.Inline);
            yield return this.Id.CompareTo(comparer.Id);
            yield return this.Class.CompareTo(comparer.Class);
            yield return this.Attribute.CompareTo(comparer.Attribute);
            yield return this.Element.CompareTo(comparer.Element);
            yield return this.Index.CompareTo(comparer.Index);
        }
    }
}
