using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CookieJarTools.CrumbTrail.Editor
{
    public class ToDoListWindow : EditorWindow
    {
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private ToDoListDatabase database;

        private List<ToDoList> Lists => database != null ? database.Lists : new List<ToDoList>();

        private CookieJarTools.CrumbTrail.Editor.UI.TabView tabView;
        private readonly Dictionary<ToDoList, ToolbarToggle> listToTabMap = new();

        [MenuItem("Tools/ToDo Lists")]
        public static void ShowWindow()
        {
            var window = GetWindow<ToDoListWindow>();
            window.titleContent = new GUIContent("ToDo Lists");
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(styleSheet);

            var background = new VisualElement();
            background.AddToClassList("window-background");
            rootVisualElement.Add(background);

            LoadOrCreateDatabase();

            var addListButton = new Button(() => 
            {
                AddList(null);
                EditorUtility.SetDirty(database);
            }) { text = "+ Add List" };
            addListButton.AddToClassList("add-list-button");
            background.Add(addListButton);

            tabView = new CookieJarTools.CrumbTrail.Editor.UI.TabView();
            tabView.AddToClassList("tab-view");
            background.Add(tabView);

            foreach (var list in Lists.ToArray())
            {
                AddList(list);
            }
        }

        private void LoadOrCreateDatabase()
        {
            if (database != null) return;

            database = AssetDatabase.LoadAssetAtPath<ToDoListDatabase>("Assets/ToDoListDatabase.asset");
            if (database != null) return;
            
            database = CreateInstance<ToDoListDatabase>();
            AssetDatabase.CreateAsset(database, "Assets/ToDoListDatabase.asset");
            AssetDatabase.SaveAssets();
        }

        private void AddList(ToDoList existing)
        {
            if (existing != null && listToTabMap.ContainsKey(existing)) return;

            var todo = existing ?? new ToDoList();
            if (!Lists.Contains(todo))
            {
                Lists.Add(todo);
            }

            EditorUtility.SetDirty(database);

            var listBox = new VisualElement();
            listBox.AddToClassList("todo-list");

            var header = new TextField { value = todo.Name };
            header.RegisterValueChangedCallback(evt => 
            {
                todo.Name = evt.newValue;
            });
            header.AddToClassList("list-title");
            listBox.Add(header);

            var description = new TextField { multiline = true, value = todo.Description };
            description.RegisterValueChangedCallback(evt => 
            {
                todo.Description = evt.newValue;
            });
            description.AddToClassList("list-description");
            listBox.Add(description);

            var optionsBox = new VisualElement();
            optionsBox.AddToClassList("options-box");
            listBox.Add(optionsBox);

            var searchRow = new VisualElement();
            searchRow.AddToClassList("options-row");

            var searchField = new TextField("Search");
            searchField.AddToClassList("search-field");
            searchRow.Add(searchField);

            var priorityOptions = new List<TaskPriority>
            {
                TaskPriority.All,
                TaskPriority.Low,
                TaskPriority.Medium,
                TaskPriority.High
            };

            var priorityFilter = new PopupField<TaskPriority>("Priority", priorityOptions, TaskPriority.All)
            {
                formatListItemCallback = val => val == TaskPriority.All ? "All" : val.ToString(),
                formatSelectedValueCallback = val => val == TaskPriority.All ? "All" : val.ToString()
            };
            priorityFilter.AddToClassList("priority-filter");
            searchRow.Add(priorityFilter);
            optionsBox.Add(searchRow);

            var flagsRow = new VisualElement();
            flagsRow.AddToClassList("options-row");

            flagsRow.Add(WrapToggle("Names Only", out var searchInNamesOnly, true));
            flagsRow.Add(WrapToggle("Case Sensitive", out var searchCaseSensitive, false));
            flagsRow.Add(WrapToggle("Regex", out var searchRegex, false));

            optionsBox.Add(flagsRow);

            var scrollView = new ScrollView();
            scrollView.AddToClassList("task-scrollview");
            listBox.Add(scrollView);

            var addTaskButton = new Button(() => 
            {
                AddTask(todo, scrollView);
            }) { text = "+ Add Task" };
            addTaskButton.AddToClassList("add-task-button");
            listBox.Add(addTaskButton);

            var footerOptions = new VisualElement();
            footerOptions.AddToClassList("options-row");
            footerOptions.Add(WrapToggle("Compact Mode", out var compactMode, database.CompactMode));
            footerOptions.Add(WrapToggle("Show Completed", out var showCompleted, false));
            listBox.Add(footerOptions);

            void Refresh() => RefreshTasks(todo, scrollView, showCompleted.value, priorityFilter.value,
                searchField.value, searchInNamesOnly.value, searchCaseSensitive.value, searchRegex.value,
                compactMode.value);

            searchField.RegisterValueChangedCallback(_ => Refresh());
            searchInNamesOnly.RegisterValueChangedCallback(_ => Refresh());
            searchCaseSensitive.RegisterValueChangedCallback(_ => Refresh());
            searchRegex.RegisterValueChangedCallback(_ => Refresh());
            showCompleted.RegisterValueChangedCallback(_ => Refresh());
            priorityFilter.RegisterValueChangedCallback(_ => Refresh());
            compactMode.RegisterValueChangedCallback(_ =>
            {
                database.CompactMode = compactMode.value;
                Refresh();
            });

            var deleteButton = new Button(() => 
            {
                if (EditorUtility.DisplayDialog("Delete To-Do List",
                    $"Are you sure you want to delete the list '{todo.Name}'?", "Delete", "Cancel"))
                {
                    DeleteList(todo);
                }
            }) { text = "Delete List" };
            deleteButton.AddToClassList("delete-list-button");
            listBox.Add(deleteButton);

            var tabToggle = tabView.AddTab(todo.Name, listBox);
            listToTabMap[todo] = tabToggle;

            tabToggle.AddManipulator(new ContextualMenuManipulator(evt => 
            {
                evt.menu.AppendAction("Delete List", _ => DeleteList(todo));
            }));

            header.RegisterValueChangedCallback(evt => 
            {
                todo.Name = evt.newValue;
                if (listToTabMap.TryGetValue(todo, out var tab))
                    tab.text = evt.newValue;
            });

            Refresh();
        }

        private VisualElement WrapToggle(string label, out Toggle toggle, bool initialValue)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginRight = 20f
                }
            };

            toggle = new Toggle { value = initialValue };
            var labelElement = new Label(label)
            {
                style =
                {
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Normal
                }
            };

            container.Add(toggle);
            container.Add(labelElement);

            return container;
        }

        private void AddTask(ToDoList list, VisualElement container)
        {
            list.Tasks.Add(new ToDoTask());
            RefreshTasks(list, container, false, TaskPriority.All, "", true, false, false, false);
        }

        private void RefreshTasks(ToDoList list, VisualElement container, bool showCompleted, TaskPriority filterPriority,
            string search, bool nameOnly, bool caseSensitive, bool useRegex, bool compact)
        {
            container.Clear();

            Regex regex = null;
            if (!string.IsNullOrEmpty(search))
            {
                var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                try { regex = useRegex ? new Regex(search, options) : new Regex(Regex.Escape(search), options); }
                catch { regex = null; }
            }

            foreach (var task in list.Tasks.ToArray())
            {
                if (filterPriority != TaskPriority.All && task.Priority != filterPriority)
                    continue;

                if (!showCompleted && task.Done) continue;

                if (regex != null)
                {
                    var haystack = nameOnly ? task.Text : task.Text + "\n" + task.Description;
                    if (!regex.IsMatch(haystack)) continue;
                }

                var taskBox = new VisualElement();
                taskBox.AddToClassList("task-box");
                if (compact) taskBox.AddToClassList("compact");

                var taskRow = new VisualElement();
                taskRow.AddToClassList("task-row");

                var toggle = new Toggle { value = task.Done };
                toggle.RegisterValueChangedCallback(evt => 
                {
                    task.Done = evt.newValue;
                    if (list.Tasks.Count > 0 && list.Tasks.TrueForAll(t => t.Done))
                    {
                        if (EditorUtility.DisplayDialog("All Tasks Complete",
                                $"All tasks in '{list.Name}' are done. Delete this list?", "Yes", "No"))
                        {
                            DeleteList(list);
                            return;
                        }
                    }
                    RefreshTasks(list, container, showCompleted, filterPriority, search, nameOnly, caseSensitive, useRegex, compact);
                });

                var field = new TextField { value = task.Text };
                field.RegisterValueChangedCallback(evt => 
                {
                    task.Text = evt.newValue;
                });
                field.AddToClassList("task-text");

                var deleteTaskButton = new Button(() =>
                {
                    if (!EditorUtility.DisplayDialog("Delete Task",
                            $"Are you sure you want to delete the task '{task.Text}'?", "Delete", "Cancel")) return;
                    
                    list.Tasks.Remove(task);
                    EditorUtility.SetDirty(database);
                    RefreshTasks(list, container, showCompleted, filterPriority, search, nameOnly, caseSensitive, useRegex, compact);
                }) { text = "✕" };
                deleteTaskButton.AddToClassList("delete-task-button");

                taskRow.Add(toggle);
                taskRow.Add(field);
                taskRow.Add(deleteTaskButton);
                taskBox.Add(taskRow);

                var description = new TextField("Description") { multiline = true, value = task.Description };
                description.RegisterValueChangedCallback(evt => 
                {
                    task.Description = evt.newValue;
                });
                description.AddToClassList("task-description");

                var priorityField = new EnumField("Priority", task.Priority);
                priorityField.RegisterValueChangedCallback(evt => 
                {
                    task.Priority = (TaskPriority)evt.newValue;
                    taskBox.style.backgroundColor = GetPriorityColor(task.Priority);
                    var styleBorderBottomColor = GetPriorityBorderColor(task.Priority);
                    taskBox.style.borderBottomColor = styleBorderBottomColor;
                    taskBox.style.borderTopColor = styleBorderBottomColor;
                    taskBox.style.borderRightColor = styleBorderBottomColor;
                    taskBox.style.borderLeftColor = styleBorderBottomColor;
                    RefreshTasks(list, container, showCompleted, filterPriority, search, nameOnly, caseSensitive, useRegex, compact);
                });
                priorityField.AddToClassList("task-priority");

                taskBox.style.backgroundColor = GetPriorityColor(task.Priority);
                var styleBorderBottomColor = GetPriorityBorderColor(task.Priority);
                taskBox.style.borderBottomColor = styleBorderBottomColor;
                taskBox.style.borderTopColor = styleBorderBottomColor;
                taskBox.style.borderRightColor = styleBorderBottomColor;
                taskBox.style.borderLeftColor = styleBorderBottomColor;

                description.style.display = compact ? DisplayStyle.None : DisplayStyle.Flex;
                priorityField.style.display = compact ? DisplayStyle.None : DisplayStyle.Flex;

                taskBox.Add(description);
                taskBox.Add(priorityField);

                container.Add(taskBox);
            }
        }

        private void DeleteList(ToDoList list)
        {
            Lists.Remove(list);
            if (listToTabMap.TryGetValue(list, out var tab))
            {
                tab.parent.Remove(tab);
                listToTabMap.Remove(list);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(database);

            tabView.ClearTabs();
            listToTabMap.Clear();

            var remainingLists = Lists.ToArray();
            foreach (var toDoList in remainingLists)
            {
                AddList(toDoList);
            }
        }
        
        private Color GetPriorityColor(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => new Color(0.15f, 0.25f, 0.15f),
                TaskPriority.Medium => new Color(0.25f, 0.25f, 0.1f),
                TaskPriority.High => new Color(0.35f, 0.15f, 0.15f),
                _ => Color.clear
            };
        }
        
        private Color GetPriorityBorderColor(TaskPriority priority)
        {
            return priority switch
            {
                TaskPriority.Low => new Color(0.23f, 0.47f, 0.23f),
                TaskPriority.Medium => new Color(0.56f, 0.56f, 0.28f),
                TaskPriority.High => new Color(0.67f, 0.2f, 0.2f),
                _ => Color.clear
            };
        }
    }

    
}