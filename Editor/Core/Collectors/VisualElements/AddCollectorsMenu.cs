using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Simulation
{
    class AddCollectorsMenu : VisualElement
    {
        const string k_DefaultDirectoryText = "Collectors";
        string m_CurrentPath = string.Empty;
        VisualElement m_DirectoryChevron;
        TextElement m_DirectoryLabelText;
        Dictionary<string, HashSet<string>> m_MenuDirectories = new Dictionary<string, HashSet<string>>();
        VisualElement m_MenuElements;
        List<MenuItem> m_MenuItems = new List<MenuItem>();
        Dictionary<string, List<MenuItem>> m_MenuItemsMap = new Dictionary<string, List<MenuItem>>();

        CollectorsList m_CollectorsList;

        string m_SearchString = string.Empty;

        public AddCollectorsMenu(VisualElement parentElement, VisualElement button, CollectorsList collectorsList)
        {
            m_CollectorsList = collectorsList;
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                $"{CoreProfilerUtils.uxmlDir}/Collectors/AddCollectorsMenu.uxml");
            template.CloneTree(this);
            style.position = new StyleEnum<Position>(Position.Absolute);

            var buttonPosition = button.worldBound.position;
            var top = Math.Min(buttonPosition.y, parentElement.worldBound.height - 300);
            style.top = top;
            style.left = buttonPosition.x;

            focusable = true;
            RegisterCallback<FocusOutEvent>(evt =>
            {
                if (evt.relatedTarget == null || ((VisualElement)evt.relatedTarget).FindCommonAncestor(this) != this)
                    ExitMenu();
            });

            var directoryLabel = this.Q<VisualElement>("directory-label");
            directoryLabel.RegisterCallback<MouseUpEvent>(evt => { AscendDirectory(); });
            m_DirectoryLabelText = this.Q<TextElement>("directory-label-text");
            m_DirectoryChevron = this.Q<VisualElement>("directory-chevron");

            var searchBar = this.Q<TextField>("search-bar");
            searchBar.schedule.Execute(() => searchBar.ElementAt(0).Focus());
            searchBar.RegisterValueChangedCallback(evt => searchString = evt.newValue);

            m_MenuElements = this.Q<VisualElement>("menu-options");

            CreateMenuItems();
            DrawDirectoryItems();
        }

        string currentPath
        {
            get => m_CurrentPath;
            set
            {
                m_CurrentPath = value;
                DrawDirectoryItems();
            }
        }

        string currentPathName
        {
            get
            {
                if (m_CurrentPath == string.Empty)
                    return k_DefaultDirectoryText;
                var pathItems = m_CurrentPath.Split(Path.AltDirectorySeparatorChar);
                return pathItems[pathItems.Length - 1];
            }
        }

        string searchString
        {
            get => m_SearchString;
            set
            {
                m_SearchString = value;
                if (m_SearchString == string.Empty)
                    DrawDirectoryItems();
                else
                    DrawSearchItems();
            }
        }

        string directoryText
        {
            set
            {
                m_DirectoryLabelText.text = value;
                m_DirectoryChevron.style.visibility = value == k_DefaultDirectoryText
                    ? new StyleEnum<Visibility>(Visibility.Hidden)
                    : new StyleEnum<Visibility>(Visibility.Visible);
            }
        }

        void ExitMenu()
        {
            parent.Remove(this);
        }

        void AddCollector(Type collectorType)
        {
            m_CollectorsList.AddCollector(collectorType);
            ExitMenu();
        }

        void AscendDirectory()
        {
            var pathItems = m_CurrentPath.Split(Path.AltDirectorySeparatorChar);
            var path = pathItems[0];
            for (var i = 1; i < pathItems.Length - 1; i++)
                path = $"{path}/{pathItems[i]}";
            currentPath = path;
        }

        void DrawDirectoryItems()
        {
            directoryText = currentPathName;
            m_MenuElements.Clear();

            if (m_MenuDirectories.ContainsKey(currentPath))
            {
                var directories = m_MenuDirectories[currentPath];
                foreach (var directory in directories)
                    m_MenuElements.Add(new MenuDirectoryElement(directory, this));
            }

            if (m_MenuItemsMap.ContainsKey(currentPath))
            {
                var menuItems = m_MenuItemsMap[currentPath];
                foreach (var menuItem in menuItems)
                    m_MenuElements.Add(new MenuItemElement(menuItem, this));
            }
        }

        void DrawSearchItems()
        {
            directoryText = k_DefaultDirectoryText;
            m_MenuElements.Clear();

            var upperSearchString = searchString.ToUpper();
            foreach (var menuItem in m_MenuItems)
                if (menuItem.itemName.ToUpper().Contains(upperSearchString))
                    m_MenuElements.Add(new MenuItemElement(menuItem, this));
        }

        void CreateMenuItems()
        {
            var rootList = new List<MenuItem>();
            m_MenuItemsMap.Add(string.Empty, rootList);

            var collectorTypeSet = new HashSet<Type>();
            foreach (var collector in m_CollectorsList.simulationProfiler.collectors)
                collectorTypeSet.Add(collector.GetType());

            foreach (var collectorType in CoreProfilerUtils.collectorTypes)
            {
                if (collectorTypeSet.Contains(collectorType))
                    continue;
                var menuAttribute = (AddCollectorMenuAttribute)Attribute.GetCustomAttribute(
                    collectorType, typeof(AddCollectorMenuAttribute));
                if (menuAttribute != null)
                {
                    var pathItems = menuAttribute.menuPath.Split('/');
                    if (pathItems.Length > 1)
                    {
                        var path = string.Empty;
                        var itemName = pathItems[pathItems.Length - 1];
                        for (var i = 0; i < pathItems.Length - 1; i++)
                        {
                            var childPath = $"{path}/{pathItems[i]}";
                            if (i < pathItems.Length - 1)
                            {
                                if (!m_MenuDirectories.ContainsKey(path))
                                    m_MenuDirectories.Add(path, new HashSet<string>());
                                m_MenuDirectories[path].Add(childPath);
                            }

                            path = childPath;
                        }

                        if (!m_MenuItemsMap.ContainsKey(path))
                            m_MenuItemsMap.Add(path, new List<MenuItem>());

                        var item = new MenuItem(collectorType, itemName);
                        m_MenuItems.Add(item);
                        m_MenuItemsMap[path].Add(item);
                    }
                    else
                    {
                        if (pathItems.Length == 0)
                            throw new AssertionException("Empty collector menu path");
                        var item = new MenuItem(collectorType, pathItems[0]);
                        m_MenuItems.Add(item);
                        rootList.Add(item);
                    }
                }
                else
                {
                    rootList.Add(new MenuItem(collectorType, collectorType.Name));
                }
            }

            m_MenuItems.Sort((item1, item2) => item1.itemName.CompareTo(item2.itemName));
        }

        class MenuItem
        {
            public string itemName;
            public Type collectorType;

            public MenuItem(Type collectorType, string itemName)
            {
                this.collectorType = collectorType;
                this.itemName = itemName;
            }
        }

        sealed class MenuItemElement : TextElement
        {
            public MenuItemElement(MenuItem menuItem, AddCollectorsMenu menu)
            {
                text = menuItem.itemName;
                AddToClassList("collector__add-menu-directory-item");
                RegisterCallback<MouseUpEvent>(evt => menu.AddCollector(menuItem.collectorType));
            }
        }

        sealed class MenuDirectoryElement : VisualElement
        {
            public MenuDirectoryElement(string directory, AddCollectorsMenu menu)
            {
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    $"{CoreProfilerUtils.uxmlDir}/Collectors/MenuDirectoryElement.uxml").CloneTree(this);
                var pathItems = directory.Split('/');
                this.Q<TextElement>("directory").text = pathItems[pathItems.Length - 1];
                RegisterCallback<MouseUpEvent>(evt => menu.currentPath = directory);
            }
        }
    }
}
