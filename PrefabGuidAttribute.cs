using UnityEngine;

namespace Bastard
{
    /// <summary>
    /// Marks a string field as storing an asset GUID selected from a specific project folder.
    /// </summary>
    public class PrefabGuidAttribute : PropertyAttribute
    {
        public string RootPath { get; }

        public PrefabGuidAttribute(string rootPath)
        {
            RootPath = rootPath;
        }
    }
}
