using ExCSS.Model;
using HtmlAgilityPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CssInliner
{
    public class Inliner
    {
		public static string InlineCssIntoHtml(string html)
		{
			HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
			doc.LoadHtml(html);


			var styleTags = doc.DocumentNode.SelectNodes("descendant-or-self::style");


			var cssParser = new ExCSS.StylesheetParser();
			var styles = styleTags.Select(x => cssParser.Parse(x.InnerText));


			var allElements = doc.DocumentNode.SelectNodes("descendant-or-self::*");


			Dictionary<HtmlNode, List<ScopedDeclaration>> matches = new Dictionary<HtmlNode, List<ScopedDeclaration>>();

			foreach (var n in allElements)
			{
				foreach (var r in styles.SelectMany(x => x.RuleSets))
				{
					var matchedSelectors = n.Matches(r.Selectors);


					if (matchedSelectors.Any())
					{

						if (!matches.ContainsKey(n))
						{
							matches.Add(n, new List<ScopedDeclaration>());
						}
						
						var matchedDeclerations = r.Declarations.SelectMany(x => matchedSelectors.Select(y => new ScopedDeclaration(x,y)));
						matches[n].AddRange(matchedDeclerations);

						//TODO update the appending css rule to make sure the correctly prioritised rule is used
						if (n.Attributes.Contains("style")){
							var inlineStyles = n.Attributes["style"].Value;
							
							var inline = cssParser.Parse("* { " + inlineStyles + "}")
											.RuleSets.Single()
											.Declarations
											.Select(x => new ScopedDeclaration(x));
							
							matches[n].AddRange(inline);
						}
					}
				}
			}

			//go through and update all styles
			foreach (var nodeRules in matches)
			{
				var node = nodeRules.Key;

				var declerations = nodeRules.Value.GroupBy(x => x.Decleration.Name).Select(x => x.OrderBy(r => r.Scope).First()).OrderBy(r => r.Scope);


				StringBuilder stylesSB = new StringBuilder();
				foreach (var d in declerations)
				{
					stylesSB.Append(d.Decleration.ToString());
					stylesSB.Append(";");
				}

				if (!node.Attributes.Contains("style"))
				{
					node.Attributes.Add("style", stylesSB.ToString());
				}
				else { 
					node.Attributes["style"].Value = stylesSB.ToString();
				}
			}


			var sb = new StringBuilder();
			using (var tw = new System.IO.StringWriter(sb))
			{

				doc.Save(tw);
			}

			return sb.ToString();
		}
    }

	internal class ScopedDeclaration
	{
		public ScopedDeclaration(Declaration decleration, Selector selector)
		{
			Decleration = decleration;
			Scope = new ScopedDeclarationRank(selector, decleration.Important);
		}

		public ScopedDeclaration(Declaration decleration)
		{
			Decleration = decleration;
			Scope = new ScopedDeclarationRank(decleration.Important);
		}

		public ScopedDeclarationRank Scope { get; private set; }
		public Declaration Decleration { get; private set; }
	}

	internal class ScopedDeclarationRank : IComparable
	{
		public ScopedDeclarationRank(Selector selector, bool IsImportant)
		{
			foreach (var s in selector.SimpleSelectors)
				Add(s);
			if (IsImportant)
				Important++;
		}

		public ScopedDeclarationRank(bool IsImportant)
		{
			Inline++;
			if (IsImportant)
				Important++;
		}

		private void Add(SimpleSelector addSel)
		{
			if (addSel.Child != null)
				Add(addSel.Child);

			if (!string.IsNullOrEmpty(addSel.Class))
				Class++;

			if (addSel.Attribute != null)
				Attribute++;

			if (addSel.Pseudo != null)
				Attribute++;

			if (!string.IsNullOrEmpty(addSel.ElementName))
				Element++;

			if (!string.IsNullOrEmpty(addSel.ID))
				Id++;
		}

		public int Important { get; set; }
		public int Inline { get; set; }
		public int Id { get; set; }
		public int Element { get; set; }
		public int Class {get;set;}
		public int Attribute {get;set;}

		internal int Total {
			get {
				return Important * 10000 +
					Inline * 1000 +
					Id * 100 +
					Class * 10 +
					Attribute * 10 +
					Element ;
			}
		}

		public int CompareTo(object obj)
		{
			var t = obj as ScopedDeclarationRank;
			if (t == null)
				return -1;

			return Total.CompareTo(t.Total);
		}
	}

	internal static class NodeExtensions
	{
		internal static bool IsMatch(this HtmlAgilityPack.HtmlNode node, IEnumerable<Selector> selectors)
		{
			return Matches(node, selectors).Any();
		}

		internal static IEnumerable<Selector> Matches(this HtmlAgilityPack.HtmlNode node, IEnumerable<Selector> selectors)
		{
			return selectors.Where(x => node.IsMatch(x.SimpleSelectors));
		}

		internal static bool IsMatch(this HtmlAgilityPack.HtmlNode node, IEnumerable<SimpleSelector> selectors)
		{

			var stack = new Stack<SimpleSelector>(selectors);
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

		internal static bool IsMatch(this HtmlAgilityPack.HtmlNode node, SimpleSelector selector, Stack<SimpleSelector> remainingStack)
		{
			if (!string.IsNullOrEmpty(selector.ElementName) && node.Name != selector.ElementName)
				return false;

			if (!string.IsNullOrEmpty(selector.ID) && node.Id != selector.ID)
				return false;

			if (!string.IsNullOrEmpty(selector.Class))
			{
				var classString = node.Attributes.Contains("class") ? node.Attributes["class"].Value : "";
				if (!classString.Split(' ').Contains(selector.Class))
					return false;
			}
			if (!string.IsNullOrEmpty(selector.Pseudo)) {
				if (!node.IsPseudoMatch(selector, remainingStack))
					return false;
			}


			if (selector.Combinator != null && remainingStack.Any())
			{
				var nextSel = remainingStack.Pop();
				
				switch (selector.Combinator.Value)
				{
					case Combinator.ChildOf:

						if (node.ParentNode == null)//has to be a child of something
							return false;
						if(!node.ParentNode.IsMatch(nextSel, remainingStack))
							return false;
						break;

					case Combinator.Namespace:
						//we are not going to support this until I see a valid use case
						return false;
					case Combinator.PrecededBy:

						if(!node.PreviousSiblingsNotText().Any(x=>x.IsMatch(nextSel, remainingStack)))
							return false;

						break;
					case Combinator.PrecededImmediatelyBy:
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
			if (selector.Attribute != null)
			{
				if (!node.IsMatch(selector.Attribute))
					return false;
			}
			if (selector.Child != null)
			{
				return node.IsMatch(selector.Child, remainingStack);
			}

			return true;
		}


		internal static IEnumerable<HtmlNode> PreviousSiblingsNotText(this HtmlAgilityPack.HtmlNode node)
		{
			var prev = node.PreviousSibling;
			while (prev != null)
			{
				if (!(prev is HtmlAgilityPack.HtmlTextNode))
					yield return prev;

				prev = prev.PreviousSibling;
			}
		}
		internal static HtmlNode PreviousSiblingNotText(this HtmlAgilityPack.HtmlNode node)
		{
			return node.PreviousSiblingsNotText().FirstOrDefault();
		}

		internal static bool IsPseudoMatch(this HtmlAgilityPack.HtmlNode node, SimpleSelector selector, Stack<SimpleSelector> remainingStack)
		{
			//by default we fail out any rule using a pseudo selector
			return false;	
		}

		internal static bool IsMatch(this HtmlAgilityPack.HtmlNode node, ExCSS.Model.Attribute attribute)
		{
			var attr = node.Attributes[attribute.Operand];
			if (attr != null)
			{
				var value = attr.Value;
				var test = (attribute.Value ?? "").Trim(' ', '\'', '"');
				switch (attribute.Operator)
				{
					case AttributeOperator.BeginsWith:
						return value.StartsWith(test);
					case AttributeOperator.Contains:
						return value.Contains(test);
					case AttributeOperator.EndsWith:
						return value.EndsWith(test);
					case AttributeOperator.Equals:
						return value == test;
					case AttributeOperator.Hyphenated:
						return value.Split('-').Contains(test);
					case AttributeOperator.InList:
						return value.Split(' ').Contains(test);
					case AttributeOperator.None:
						return true;
				}
			}

			return false;
		}
	}
}
