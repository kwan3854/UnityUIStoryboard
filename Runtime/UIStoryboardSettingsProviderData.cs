
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.kwanjoong.unityuistoryboard
{
    /// <summary>
    /// Singleton accessor for the hidden ScriptableObject (UIStoryboardSettingsAsset).
    /// Automatically creates and stores the asset in a safe location if it does not exist yet.
    /// </summary>
    internal static class UIStoryboardSettingsProviderData
    {
        // Change these constants if you want to store the asset in a different folder or file name.
        private const string AssetFolderName = "Assets/Editor/UIStoryboard";
        private const string AssetFileName   = "UIStoryboardSettingsAsset.asset";

        private static UIStoryboardSettingsAsset _instance;

        /// <summary>
        /// Provides a singleton instance of the UIStoryboardSettingsAsset.
        /// </summary>
        public static UIStoryboardSettingsAsset Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadOrCreateAsset();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Loads the existing asset if possible, otherwise creates a new one.
        /// </summary>
        private static UIStoryboardSettingsAsset LoadOrCreateAsset()
        {
#if UNITY_EDITOR
            // Ensure the folder exists
            if (!AssetDatabase.IsValidFolder(AssetFolderName))
            {
                CreateParentFolders(AssetFolderName);
            }

            // Full path to the .asset file
            string fullPath = Path.Combine(AssetFolderName, AssetFileName);

            // Try to load the existing asset
            var loadedAsset = AssetDatabase.LoadAssetAtPath<UIStoryboardSettingsAsset>(fullPath);
            if (loadedAsset != null)
            {
                return loadedAsset;
            }

            // If null, create a new asset
            var newAsset = ScriptableObject.CreateInstance<UIStoryboardSettingsAsset>();
            AssetDatabase.CreateAsset(newAsset, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return newAsset;
#else
            // In case we are running outside the Editor, return a temporary instance
            return ScriptableObject.CreateInstance<UIStoryboardSettingsAsset>();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Helper method to recursively create folders for the given path.
        /// </summary>
        private static void CreateParentFolders(string path)
        {
            // Example: "Assets/Editor/UIStoryboard" => create "Assets/Editor" if needed, then "Assets/Editor/UIStoryboard".
            var parts = path.Split('/');
            if (parts.Length < 2) return;

            // Start from "Assets" or another root
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
#endif
    }
}