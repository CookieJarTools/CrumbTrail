using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// Draws the Markdown in a task note.
	/// <para>
	/// This is CrumbTrail's own small renderer rather than the CookBook's, because a package cannot
	/// lean on another package that may not be installed. It covers what a note actually uses:
	/// headings, lists, quotes, code, and the inline marks. Anything it does not know how to draw
	/// is shown as the text that was typed, so nothing is ever swallowed.
	/// </para>
	/// </summary>
	public static class MarkdownBlock
	{
		private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
		private static readonly Regex Italic = new Regex(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);
		private static readonly Regex Code = new Regex(@"`([^`]+)`", RegexOptions.Compiled);
		private static readonly Regex Link = new Regex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled);
		private static readonly Regex PageLink = new Regex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);

		/// <summary>Builds the elements for a note. Returns an empty container for empty text.</summary>
		public static VisualElement Build(string markdown)
		{
			var box = new VisualElement();
			box.AddToClassList("crumb__md");
			if (string.IsNullOrWhiteSpace(markdown))
			{
				return box;
			}

			var lines = markdown.Replace("\r\n", "\n").Split('\n');
			var index = 0;
			while (index < lines.Length)
			{
				index = AddBlock(box, lines, index);
			}

			return box;
		}

		/// <summary>Adds whatever block starts at this line, and says which line comes next.</summary>
		private static int AddBlock(VisualElement box, string[] lines, int index)
		{
			var line = lines[index];
			if (line.TrimStart().StartsWith("```"))
			{
				return AddFence(box, lines, index);
			}

			if (string.IsNullOrWhiteSpace(line))
			{
				return index + 1;
			}

			var trimmed = line.TrimStart();
			if (trimmed.StartsWith("#"))
			{
				AddHeading(box, trimmed);
				return index + 1;
			}

			if (trimmed.StartsWith("> "))
			{
				AddQuote(box, trimmed.Substring(2));
				return index + 1;
			}

			if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
			{
				AddBullet(box, "•", trimmed.Substring(2));
				return index + 1;
			}

			var numbered = Regex.Match(trimmed, @"^(\d+)\.\s+(.*)$");
			if (numbered.Success)
			{
				AddBullet(box, numbered.Groups[1].Value + ".", numbered.Groups[2].Value);
				return index + 1;
			}

			AddParagraph(box, line);
			return index + 1;
		}

		private static int AddFence(VisualElement box, string[] lines, int index)
		{
			var code = new StringBuilder();
			var at = index + 1;
			while (at < lines.Length && !lines[at].TrimStart().StartsWith("```"))
			{
				code.AppendLine(lines[at]);
				at++;
			}

			var block = new Label(code.ToString().TrimEnd());
			block.AddToClassList("crumb__md-code-block");
			block.selection.isSelectable = true;
			box.Add(block);
			return at + 1;
		}

		private static void AddHeading(VisualElement box, string line)
		{
			var level = 0;
			while (level < line.Length && line[level] == '#')
			{
				level++;
			}

			var label = Rich(line.Substring(level).TrimStart());
			label.AddToClassList("crumb__md-heading");
			label.AddToClassList("crumb__md-heading--" + Mathf.Clamp(level, 1, 3));
			box.Add(label);
		}

		private static void AddQuote(VisualElement box, string text)
		{
			var row = new VisualElement();
			row.AddToClassList("crumb__md-quote");
			row.Add(Rich(text));
			box.Add(row);
		}

		private static void AddBullet(VisualElement box, string marker, string text)
		{
			var row = new VisualElement();
			row.AddToClassList("crumb__md-item");

			var dot = new Label(marker);
			dot.AddToClassList("crumb__md-marker");
			row.Add(dot);

			var body = Rich(text);
			body.AddToClassList("crumb__md-item-text");
			row.Add(body);
			box.Add(row);
		}

		private static void AddParagraph(VisualElement box, string text)
		{
			var label = Rich(text);
			label.AddToClassList("crumb__md-paragraph");
			box.Add(label);
		}

		/// <summary>
		/// One line of text with the inline marks turned into rich text.
		/// <para>
		/// Links are drawn as part of the text rather than as separate elements so a sentence is
		/// not broken into pieces that wrap badly. The whole line carries the click, and which
		/// link was clicked is worked out from where in the text the pointer was.
		/// </para>
		/// </summary>
		private static Label Rich(string text)
		{
			var targets = new List<string>();

			var marked = PageLink.Replace(text, match =>
			{
				targets.Add("page:" + match.Groups[1].Value);
				return "<color=#c3c9f7><u>" + match.Groups[1].Value + "</u></color>";
			});

			marked = Link.Replace(marked, match =>
			{
				targets.Add(match.Groups[2].Value);
				return "<color=#c3c9f7><u>" + match.Groups[1].Value + "</u></color>";
			});

			marked = Bold.Replace(marked, "<b>$1</b>");
			marked = Italic.Replace(marked, "<i>$1</i>");
			marked = Code.Replace(marked, "<color=#e0a33f>$1</color>");

			var label = new Label(marked) { enableRichText = true };
			label.selection.isSelectable = true;
			if (targets.Count > 0)
			{
				label.AddToClassList("crumb__md-has-link");
				label.tooltip = targets.Count == 1 ? Describe(targets[0]) : "Opens what is linked here";
				label.RegisterCallback<ClickEvent>(_ => Follow(targets[0]));
			}

			return label;
		}

		private static string Describe(string target)
		{
			return target.StartsWith("page:") ? "Open “" + target.Substring(5) + "” in the CookBook" : target;
		}

		/// <summary>
		/// Follows a link. A page link goes to the CookBook when it is installed, and says so
		/// plainly when it is not, rather than doing nothing and looking broken.
		/// </summary>
		private static void Follow(string target)
		{
			if (!target.StartsWith("page:"))
			{
				Application.OpenURL(target);
				return;
			}

			var page = target.Substring(5);
			if (CookbookBridge.OpenPage == null)
			{
				EditorUtility.DisplayDialog("CrumbTrail",
					"“" + page + "” is a CookBook page, and the CookBook is not in this project.\n\n"
					+ "Install it and this link will open the page.", "Right");
				return;
			}

			CookbookBridge.OpenPage(page);
		}
	}
}
