using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace CookieJarTools.CrumbTrail.Editor.UI
{
    public class TabView : VisualElement
    {
        private ToolbarToggle currentTab;
        private VisualElement tabButtons;
        private VisualElement tabContent;

        public TabView()
        {
            var tabContainer = new VisualElement();
            tabContainer.AddToClassList("tab-buttons-container");

            var tabScrollView = new ScrollView(ScrollViewMode.Horizontal);
            tabScrollView.name = "tab-scroll";
            tabScrollView.AddToClassList("tab-scroll");

            tabButtons = new VisualElement { name = "tab-buttons" };
            tabButtons.AddToClassList("tab-buttons");
            tabButtons.style.flexDirection = FlexDirection.Row;

            tabScrollView.Add(tabButtons);

            tabContent = new VisualElement { name = "tab-content" };
            tabContent.AddToClassList("tab-content");

            tabContainer.Add(tabScrollView);
            Add(tabContainer);
            Add(tabContent);
        }

        public ToolbarToggle AddTab(string title, VisualElement content)
        {
            var toggle = new ToolbarToggle { text = title };
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    if (currentTab != null)
                        currentTab.SetValueWithoutNotify(false);
                    currentTab = toggle;
                    tabContent.Clear();
                    tabContent.Add(content);
                }
            });

            if (currentTab == null)
            {
                toggle.value = true;
                currentTab = toggle;
                tabContent.Add(content);
            }

            tabButtons.Add(toggle);
            return toggle;
        }

        public void ClearTabs()
        {
            tabButtons.Clear();
            tabContent.Clear();
            currentTab = null;
        }
    }
}