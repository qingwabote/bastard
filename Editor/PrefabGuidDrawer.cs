using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bastard
{
    [CustomPropertyDrawer(typeof(PrefabGuidAttribute))]
    public class PrefabGuidDrawer : PropertyDrawer
    {
        private sealed class OptionCache
        {
            public string[] Guids;
            public string[] DisplayNames;
        }

        private sealed class ResolvedOptions
        {
            public string[] Guids;
            public string[] DisplayNames;
            public int CurrentIndex;
        }

        private static readonly Dictionary<string, OptionCache> CacheByRootPath = new();

        static PrefabGuidDrawer()
        {
            EditorApplication.projectChanged += () => CacheByRootPath.Clear();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                return new UnityEngine.UIElements.HelpBox("Use PrefabGuidPicker with string fields only.", UnityEngine.UIElements.HelpBoxMessageType.Error);
            }

            var picker = (PrefabGuidAttribute)attribute;
            var resolvedOptions = ResolveOptions(picker.RootPath, property.stringValue);
            var guidChoices = resolvedOptions.Guids.ToList();

            string FormatGuid(string guid)
            {
                var index = guidChoices.IndexOf(guid);
                if (index >= 0 && index < resolvedOptions.DisplayNames.Length)
                {
                    return resolvedOptions.DisplayNames[index];
                }

                return string.IsNullOrEmpty(guid) ? "<None>" : $"<Missing> {guid}";
            }

            var popup = new PopupField<string>(property.displayName, guidChoices, resolvedOptions.CurrentIndex)
            {
                tooltip = property.tooltip,
                formatListItemCallback = FormatGuid,
                formatSelectedValueCallback = FormatGuid
            };

            popup.BindProperty(property);

            return popup;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "Use PrefabGuidPicker with string fields only.", MessageType.Error);
                return;
            }

            var picker = (PrefabGuidAttribute)attribute;
            var resolvedOptions = ResolveOptions(picker.RootPath, property.stringValue);
            var displayedOptions = resolvedOptions.DisplayNames.Select(displayName => new GUIContent(displayName)).ToArray();
            EditorGUI.BeginChangeCheck();
            var selectedIndex = EditorGUI.Popup(position, label, resolvedOptions.CurrentIndex, displayedOptions);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = resolvedOptions.Guids[selectedIndex];
            }
        }

        private static ResolvedOptions ResolveOptions(string rootPath, string currentGuid)
        {
            var options = GetOptions(rootPath);
            var guids = options.Guids;
            var displayNames = options.DisplayNames;
            var currentIndex = Array.IndexOf(guids, currentGuid);

            if (!string.IsNullOrEmpty(currentGuid) && currentIndex < 0)
            {
                Array.Resize(ref guids, guids.Length + 1);
                Array.Resize(ref displayNames, displayNames.Length + 1);
                currentIndex = guids.Length - 1;
                guids[currentIndex] = currentGuid;
                displayNames[currentIndex] = $"<Missing> {currentGuid}";
            }
            else if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            return new ResolvedOptions
            {
                Guids = guids,
                DisplayNames = displayNames,
                CurrentIndex = currentIndex
            };
        }

        private static OptionCache GetOptions(string rootPath)
        {
            if (CacheByRootPath.TryGetValue(rootPath, out var cached))
            {
                return cached;
            }

            var assetGuids = AssetDatabase.FindAssets("t:Prefab", new[] { rootPath });
            var entries = assetGuids
                .Select(guid =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    return new
                    {
                        Guid = guid,
                        Name = System.IO.Path.GetFileNameWithoutExtension(path),
                        RelativePath = path.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) ? path[rootPath.Length..] : path
                    };
                })
                .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var nameCounts = entries
                .GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            var guids = new List<string> { string.Empty };
            var displayNames = new List<string> { "<None>" };

            foreach (var entry in entries)
            {
                guids.Add(entry.Guid);

                if (nameCounts[entry.Name] == 1)
                {
                    displayNames.Add(entry.Name);
                }
                else
                {
                    displayNames.Add($"{entry.Name} ({entry.RelativePath.TrimStart('/', '\\')})");
                }
            }

            var built = new OptionCache
            {
                Guids = guids.ToArray(),
                DisplayNames = displayNames.ToArray()
            };

            CacheByRootPath[rootPath] = built;
            return built;
        }
    }
}
