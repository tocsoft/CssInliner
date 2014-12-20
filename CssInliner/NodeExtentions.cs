using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExCSS;
using HtmlAgilityPack;

namespace Tocsoft.CssInliner
{
    public static class NodeExtentions
    {
        internal static bool IsMatch(this HtmlNode node, BaseSelector selector)
        {
            var selectorList = selector as AggregateSelectorList;

            var complex = selector as ComplexSelector;

            if (selectorList != null)
            {
                return selectorList.All(x => node.IsMatch(x));
            }
            else if (complex != null)
            {
                return node.IsMatch(complex);
            }
            else
            {
                return node.IsSimpleMatch(selector);
            }
        }

        internal static bool IsMatch(this HtmlNode node, ComplexSelector selector)
        {
            var stack = new Stack<CombinatorSelector>(selector);
            var currentRule = stack.Pop();

            if (!node.IsMatch(currentRule, stack))
                return false;

            //no more selectors, its a match
            if (!stack.Any())
                return true;

            var currentNode = node.ParentNode;
            currentRule = stack.Pop();

            while (currentNode != null)
            {
                if (currentNode.IsMatch(currentRule, stack))
                {
                    if (stack.Any())
                    {
                        currentRule = stack.Pop();
                    }
                    else
                    {
                        //no more rules left to match ad we still have ode context then return true	
                        return true;
                    }

                }

                //keep moving up the dom
                currentNode = currentNode.ParentNode;
            }
            //We must still have unmatched css selectors

            return false;
        }

        internal static bool IsSimpleMatch(this HtmlNode node, ElementSelector selector)
        {
            if (selector != null)
            {
                if (node.Name.ToUpper() != selector.Element.ToUpper())
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        internal static bool IsSimpleMatch(this HtmlNode node, IdSelector selector)
        {
            if (selector != null)
            {
                if (node.Id != selector.Id)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        internal static bool IsSimpleMatch(this HtmlNode node, ClassSelector selector)
        {
            if (selector != null)
            {
                var classes = (node.Attributes.Contains("class") ? node.Attributes["class"].Value : "").Split(' ');
                if (!classes.Contains(selector.Class))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        internal static bool IsSimpleMatch(this HtmlNode node, PseudoElementSelector selector)
        {
            return false;
        }

        internal static bool IsSimpleMatch(this HtmlNode node, PseudoClassSelector selector)
        {
            return false;
        }


        internal static bool IsSimpleMatch(this HtmlAgilityPack.HtmlNode node, ExCSS.AttributeSelector selector)
        {
            if (selector != null)
            {
                var attr = node.Attributes[selector.Attribute];
                if (attr != null)
                {
                    var value = attr.Value;

                    var test = selector.Value;
                    switch (selector.Operand)
                    {
                        case AttributeOperator.StartsWith:
                            return value.StartsWith(test);
                        case AttributeOperator.Contains:
                            return value.Contains(test);
                        case AttributeOperator.EndsWith:
                            return value.EndsWith(test);
                        case AttributeOperator.Match:
                            return value == test;
                        case AttributeOperator.NegatedMatch:
                            return value != test;
                        case AttributeOperator.DashSeparated:
                            return value.Split('-').Contains(test);
                        case AttributeOperator.SpaceSeparated:
                            return value.Split(' ').Contains(test);
                        case AttributeOperator.Unmatched:
                            return true;
                    }
                }
            }
            return false;
        }

        internal static bool IsSimpleMatch(this HtmlNode node, BaseSelector selector)
        {
            var agg = selector as AggregateSelectorList;
            if (agg == null)
            {

                return
                    selector is UniveralSelector
                    || node.IsSimpleMatch(selector as ElementSelector)
                    || node.IsSimpleMatch(selector as IdSelector)
                    || node.IsSimpleMatch(selector as ClassSelector)
                    || node.IsSimpleMatch(selector as PseudoClassSelector)
                    || node.IsSimpleMatch(selector as PseudoElementSelector)
                    || node.IsSimpleMatch(selector as AttributeSelector);
            }

            return agg.All(x => node.IsSimpleMatch(x));
        }

        internal static bool IsMatch(this HtmlNode node, CombinatorSelector selector, Stack<CombinatorSelector> remainingStack)
        {
            if (!node.IsSimpleMatch(selector.Selector))
            {
                return false;
            }


            if (remainingStack.Any())
            {
                var nextSel = remainingStack.Pop();

                switch (nextSel.Delimiter)
                {
                    case Combinator.Child:

                        if (node.ParentNode == null)//has to be a child of something
                            return false;
                        if (!node.ParentNode.IsMatch(nextSel, remainingStack))
                            return false;
                        break;

                    case Combinator.Namespace:
                        //we are not going to support this until I see a valid use case
                        return false;
                    case Combinator.Sibling:

                        if (!node.PreviousSiblingsNotText().Any(x => x.IsMatch(nextSel, remainingStack)))
                            return false;

                        break;
                    case Combinator.AdjacentSibling:
                        var sib = node.PreviousSiblingNotText();
                        if (sib == null)//has to be a child of something
                            return false;
                        if (!sib.IsMatch(nextSel, remainingStack))
                            return false;

                        break;
                    default:
                        break;
                }
            }
            return true;
        }


        internal static IEnumerable<HtmlNode> PreviousSiblingsNotText(this HtmlNode node)
        {
            var prev = node.PreviousSibling;
            while (prev != null)
            {
                if (!((prev is HtmlTextNode)))
                    yield return prev;

                prev = prev.PreviousSibling;
            }
        }
        internal static HtmlNode PreviousSiblingNotText(this HtmlNode node)
        {
            return node.PreviousSiblingsNotText().FirstOrDefault();
        }

    }
}
