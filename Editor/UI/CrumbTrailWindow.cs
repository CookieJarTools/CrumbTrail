using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.CrumbTrail.Editor
{
	/// <summary>
	/// The list, in the editor, next to the work it is about.
	/// <para>
	/// Two things shape this window. Writing a task down takes one field and one key, because
	/// anything slower and the thought is gone before it is written. And a task can point at the
	/// object it concerns, which is the one thing a note here can do that a comment in the code or
	/// a ticket in a tracker cannot.
	/// </para>
	/// </summary>
	public class CrumbTrailWindow : EditorWindow
	{
		private const string NoFilter = "Any priority";

		private readonly HashSet<ToDoTask> open = new HashSet<ToDoTask>();
		private readonly HashSet<ToDoTask> editing = new HashSet<ToDoTask>();

		private int current;
		private string search = string.Empty;
		private TaskPriority? priorityFilter;

		private VisualElement root;
		private VisualElement tabs;
		private VisualElement tasks;
		private TextField capture;

		private static CrumbTrailData Data => CrumbTrailData.instance;

		private ToDoList CurrentList =>
			current >= 0 && current < Data.Lists.Count ? Data.Lists[current] : null;

		[MenuItem("Window/CookieJarTools/CrumbTrail")]
		public static void ShowWindow()
		{
			var window = GetWindow<CrumbTrailWindow>();
			window.titleContent = new GUIContent("CrumbTrail", EditorGUIUtility.IconContent("d_UnityEditor.VersionControl").image);
			window.minSize = new Vector2(280, 200);
		}

		public void CreateGUI()
		{
			root = rootVisualElement;
			root.Clear();
			root.AddToClassList("crumb-root");

			var sheet = Stylesheet();
			if (sheet != null)
			{
				root.styleSheets.Add(sheet);
			}

			tabs = new VisualElement();
			tabs.AddToClassList("crumb__tabs");
			root.Add(tabs);

			BuildCapture();
			BuildSearch();

			tasks = new ScrollView();
			tasks.AddToClassList("crumb__tasks");
			root.Add(tasks);

			Refresh();
		}

		private void BuildCapture()
		{
			var box = new VisualElement();
			box.AddToClassList("crumb__capture");
			root.Add(box);

			capture = new TextField { value = string.Empty };
			capture.AddToClassList("crumb__capture-field");
			capture.textEdition.placeholder = "Write a task down";
			capture.textEdition.hidePlaceholderOnFocus = false;
			box.Add(capture);

			var hint = new Label("Enter");
			hint.AddToClassList("crumb__capture-hint");
			box.Add(hint);

			// USS has no :focus-within, so the box is told when the field inside it has focus.
			capture.RegisterCallback<FocusInEvent>(_ => box.AddToClassList("crumb__capture--on"));
			capture.RegisterCallback<FocusOutEvent>(_ => box.RemoveFromClassList("crumb__capture--on"));
			capture.RegisterCallback<KeyDownEvent>(OnCaptureKey);
		}

		private void OnCaptureKey(KeyDownEvent evt)
		{
			if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
			{
				return;
			}

			// Read it a tick later, and do not stop the key getting through.
			//
			// The field syncs what was typed into its value as part of handling Return, so reading
			// it inside this callback sees whatever was there beforehand, which on the first press
			// is nothing. That is what made it take two presses: the first press only committed
			// the text, and the second one found it. Letting the key finish and reading afterwards
			// works whichever way round the field happens to do it.
			EditorApplication.delayCall += Capture;
		}

		/// <summary>Writes down whatever is in the field, and leaves the field ready for the next one.</summary>
		private void Capture()
		{
			var text = capture.value?.Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			if (CurrentList == null)
			{
				Data.Lists.Add(new ToDoList());
				current = Data.Lists.Count - 1;
			}

			CurrentList.Tasks.Insert(0, new ToDoTask { Text = text });
			Data.Touch();
			capture.value = string.Empty;
			capture.Focus();
			Refresh();
		}

		private void BuildSearch()
		{
			var bar = new VisualElement();
			bar.AddToClassList("crumb__bar");
			root.Add(bar);

			var box = new VisualElement();
			box.AddToClassList("crumb__search");
			bar.Add(box);

			var field = new TextField { value = search };
			field.AddToClassList("crumb__search-field");
			field.textEdition.placeholder = "Search";
			field.textEdition.hidePlaceholderOnFocus = false;
			field.RegisterValueChangedCallback(evt =>
			{
				search = evt.newValue;
				RefreshTasks();
			});
			box.Add(field);

			var more = new Button(ShowOptions) { text = "⋯" };
			more.AddToClassList("crumb__more");
			more.tooltip = "What is shown";
			bar.Add(more);
		}

		/// <summary>
		/// The things that change what is shown, folded into one menu. On a to-do list a row of
		/// toggles is the clutter that stops it being opened.
		/// </summary>
		private void ShowOptions()
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Show finished"), Data.ShowCompleted, () =>
			{
				Data.ShowCompleted = !Data.ShowCompleted;
				RefreshTasks();
			});
			menu.AddItem(new GUIContent("Compact rows"), Data.CompactMode, () =>
			{
				Data.CompactMode = !Data.CompactMode;
				RefreshTasks();
			});
			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent(NoFilter), priorityFilter == null, () => Filter(null));
			foreach (TaskPriority value in System.Enum.GetValues(typeof(TaskPriority)))
			{
				var captured = value;
				menu.AddItem(new GUIContent(value.ToString()), priorityFilter == captured, () => Filter(captured));
			}

			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent("Clear the finished ones"), false, ClearDone);
			menu.AddItem(new GUIContent("Delete this list"), false, DeleteList);
			menu.ShowAsContext();
		}

		private void Filter(TaskPriority? value)
		{
			priorityFilter = value;
			RefreshTasks();
		}

		private void Refresh()
		{
			RefreshTabs();
			RefreshTasks();
		}

		private void RefreshTabs()
		{
			tabs.Clear();
			for (var i = 0; i < Data.Lists.Count; i++)
				{
				tabs.Add(BuildTab(Data.Lists[i], i));
			}

			var add = new Button(AddList) { text = "+" };
			add.AddToClassList("crumb__tab-add");
			add.tooltip = "A new list";
			tabs.Add(add);
		}

		private VisualElement BuildTab(ToDoList list, int index)
		{
			// Not a Button: a Button draws its own text, and anything added to it is laid over
			// the top, so the count ends up sitting on the name.
			var tab = new VisualElement();
			tab.AddToClassList("crumb__tab");
			tab.EnableInClassList("crumb__tab--on", index == current);

			var name = new Label(list.Name);
			name.AddToClassList("crumb__tab-name");
			tab.Add(name);

			var count = new Label(list.Remaining.ToString());
			count.AddToClassList("crumb__tab-count");
			tab.Add(count);

			tab.RegisterCallback<ClickEvent>(_ => Choose(index));
			tab.RegisterCallback<ContextClickEvent>(_ => RenameList(list));
			return tab;
		}

		private void Choose(int index)
		{
			current = index;
			Refresh();
		}

		private void AddList()
		{
			Data.Lists.Add(new ToDoList());
			current = Data.Lists.Count - 1;
			Data.Touch();
			Refresh();
		}

		private void RenameList(ToDoList list)
		{
			var menu = new GenericMenu();
			menu.AddItem(new GUIContent("Rename"), false, () =>
			{
				var name = EditorInputDialog.Ask("Rename list", list.Name);
				if (string.IsNullOrEmpty(name))
				{
					return;
				}

				list.Name = name;
				Data.Touch();
				Refresh();
			});
			menu.ShowAsContext();
		}

		private void DeleteList()
		{
			var list = CurrentList;
			if (list == null)
			{
				return;
			}

			if (!EditorUtility.DisplayDialog("CrumbTrail", "Delete “" + list.Name + "” and its "
					+ list.Tasks.Count + " tasks?", "Delete", "Keep it"))
			{
				return;
			}

			Data.Lists.Remove(list);
			current = Mathf.Clamp(current - 1, 0, Mathf.Max(0, Data.Lists.Count - 1));
			Data.Touch();
			Refresh();
		}

		private void ClearDone()
		{
			var list = CurrentList;
			if (list == null)
			{
				return;
			}

			list.Tasks.RemoveAll(task => task.Done);
			Data.Touch();
			Refresh();
		}

		private void RefreshTasks()
		{
			tasks.Clear();
			var list = CurrentList;
			if (list == null)
			{
				tasks.Add(Empty("No list yet", "Write a task above and one is made for you."));
				return;
			}

			var shown = 0;
			foreach (var task in list.Tasks)
			{
				if (!Shown(task))
				{
					continue;
				}

				tasks.Add(BuildTask(list, task));
				shown++;
			}

			if (shown == 0)
			{
				tasks.Add(EmptyFor(list));
			}
		}

		private bool Shown(ToDoTask task)
		{
			if (task.Done && !Data.ShowCompleted)
			{
				return false;
			}

			if (priorityFilter != null && task.Priority != priorityFilter.Value)
			{
				return false;
			}

			if (string.IsNullOrEmpty(search))
			{
				return true;
			}

			var needle = search.ToLowerInvariant();
			return (task.Text ?? string.Empty).ToLowerInvariant().Contains(needle)
				|| (task.Description ?? string.Empty).ToLowerInvariant().Contains(needle);
		}

		private VisualElement BuildTask(ToDoList list, ToDoTask task)
		{
			// A task is a line and, when it is opened, the note under it. The two live in one box
			// so the note moves with the line rather than floating next to it.
			var box = new VisualElement();
			box.AddToClassList("crumb__task-box");

			var row = new VisualElement();
			row.AddToClassList("crumb__task");
			row.EnableInClassList("crumb__task--done", task.Done);
			box.Add(row);

			var toggle = new Toggle { value = task.Done };
			toggle.AddToClassList("crumb__task-toggle");
			toggle.RegisterValueChangedCallback(evt =>
			{
				task.Done = evt.newValue;
				Data.Touch();
				row.EnableInClassList("crumb__task--done", evt.newValue);
				RefreshTabs();
			});
			row.Add(toggle);

			var priority = new VisualElement();
			priority.AddToClassList("crumb__task-priority");
			priority.AddToClassList("crumb__task-priority--" + task.Priority.ToString().ToLowerInvariant());
			priority.tooltip = task.Priority + " priority";
			row.Add(priority);

			var text = new TextField { value = task.Text };
			text.AddToClassList("crumb__task-text");
			text.RegisterValueChangedCallback(evt =>
			{
				task.Text = evt.newValue;
				Data.Touch();
			});
			row.Add(text);

			if (task.Link != null)
			{
				row.Add(BuildLink(task));
			}

			row.Add(BuildTaskActions(list, task, box));

			if (open.Contains(task))
			{
				box.Add(BuildNote(task));
			}

			return box;
		}

		/// <summary>
		/// The note under a task, written in Markdown.
		/// <para>
		/// It shows as drawn text and turns into the raw Markdown when clicked, which is the way
		/// round that suits reading. A note is read far more often than it is written, so the
		/// drawn version is what you get by default.
		/// </para>
		/// </summary>
		private VisualElement BuildNote(ToDoTask task)
		{
			var note = new VisualElement();
			note.AddToClassList("crumb__note");

			if (editing.Contains(task))
			{
				var field = new TextField { value = task.Description, multiline = true };
				field.AddToClassList("crumb__note-edit");
				field.RegisterValueChangedCallback(evt =>
				{
					task.Description = evt.newValue;
					Data.Touch();
				});
				field.RegisterCallback<FocusOutEvent>(_ =>
				{
					editing.Remove(task);
					RefreshTasks();
				});
				note.Add(field);
				field.schedule.Execute(() => field.Focus()).ExecuteLater(1);
				return note;
			}

			if (string.IsNullOrWhiteSpace(task.Description))
			{
				var empty = new Label("Click to write a note. Markdown works, and [[Page]] links to the CookBook.");
				empty.AddToClassList("crumb__note-empty");
				empty.RegisterCallback<ClickEvent>(_ => Edit(task));
				note.Add(empty);
				return note;
			}

			var drawn = MarkdownBlock.Build(task.Description);
			note.Add(drawn);

			var edit = new Button(() => Edit(task)) { text = "Edit" };
			edit.AddToClassList("crumb__note-editbutton");
			note.Add(edit);
			return note;
		}

		private void Edit(ToDoTask task)
		{
			editing.Add(task);
			RefreshTasks();
		}

		private void ToggleOpen(ToDoTask task)
		{
			if (!open.Remove(task))
			{
				open.Add(task);
			}

			editing.Remove(task);
			RefreshTasks();
		}

		private VisualElement BuildLink(ToDoTask task)
		{
			var badge = new Label(task.Link.name);
			badge.AddToClassList("crumb__task-link");
			badge.tooltip = "Show " + task.Link.name;
			badge.RegisterCallback<ClickEvent>(_ => EditorGUIUtility.PingObject(task.Link));
			return badge;
		}

		private VisualElement BuildTaskActions(ToDoList list, ToDoTask task, VisualElement box)
		{
			var actions = new VisualElement();
			actions.AddToClassList("crumb__task-actions");

			// The one action that stays visible, because a task carrying a note should say so
			// without being hovered over first.
			var expand = new Button(() => ToggleOpen(task)) { text = open.Contains(task) ? "▾" : "▸" };
			expand.AddToClassList("crumb__task-button");
			expand.AddToClassList("crumb__task-expand");
			expand.EnableInClassList("crumb__task-expand--has-note", !string.IsNullOrWhiteSpace(task.Description));
            expand.tooltip = "The note on this task";
			actions.Add(expand);

			var link = new Button(() => LinkSelection(task)) { text = "◉" };
			link.AddToClassList("crumb__task-button");
			link.tooltip = "Point this at whatever is selected";
			actions.Add(link);

			var priority = new Button(() => CyclePriority(task)) { text = "▲" };
			priority.AddToClassList("crumb__task-button");
			priority.tooltip = "Change how much it matters";
			actions.Add(priority);

			var remove = new Button(() => Remove(list, task)) { text = "✕" };
			remove.AddToClassList("crumb__task-button");
			remove.tooltip = "Remove";
			actions.Add(remove);
			return actions;
		}

		/// <summary>Points the task at whatever is selected, or lets it go if nothing is.</summary>
		private void LinkSelection(ToDoTask task)
		{
			task.Link = Selection.activeObject;
			Data.Touch();
			RefreshTasks();
		}

		private void CyclePriority(ToDoTask task)
		{
			task.Priority = task.Priority switch
			{
				TaskPriority.Low => TaskPriority.Medium,
				TaskPriority.Medium => TaskPriority.High,
				_ => TaskPriority.Low
			};

			Data.Touch();
			RefreshTasks();
		}

		private void Remove(ToDoList list, ToDoTask task)
		{
			list.Tasks.Remove(task);
			Data.Touch();
			Refresh();
		}

		private VisualElement EmptyFor(ToDoList list)
		{
			if (list.Tasks.Count == 0)
			{
				return Empty("Nothing on this list", "Write one above. It saves itself.");
			}

			return Empty("Nothing matches", "Every task here is hidden by the search or the filter.");
		}

		private VisualElement Empty(string title, string line)
		{
			var box = new VisualElement();
			box.AddToClassList("crumb__empty");

			var frame = new VisualElement();
			frame.AddToClassList("crumb__empty-frame");
			box.Add(frame);

			var icon = new Image { image = EditorGUIUtility.IconContent("d_UnityEditor.VersionControl").image };
			icon.AddToClassList("crumb__empty-icon");
			frame.Add(icon);

			var heading = new Label(title);
			heading.AddToClassList("crumb__empty-title");
			box.Add(heading);

			var detail = new Label(line);
			detail.AddToClassList("crumb__empty-line");
			box.Add(detail);
			return box;
		}

		private void OnDisable()
		{
			// Anything typed in the last tick still reaches the file.
			Data.Flush();
		}

		private static StyleSheet Stylesheet()
		{
			foreach (var guid in AssetDatabase.FindAssets("CrumbTrail t:StyleSheet"))
			{
				var found = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guid));
				if (found != null)
				{
					return found;
				}
			}

			return null;
		}
	}
}
