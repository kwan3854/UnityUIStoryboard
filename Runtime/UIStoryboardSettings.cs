#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.kwanjoong.unityuistoryboard
{
    /// <summary>
    /// Static helper class that:
    ///  - Loads/Creates the UIStoryboardSettingsAsset via AssetDatabase
    ///  - Always registers it in Preloaded Assets
    ///  - Creates the project structure (folders + asmdef) on demand
    /// </summary>
    public static class UIStoryboardSettings
    {
#if UNITY_EDITOR
        /// <summary>
        /// Loads the first found UIStoryboardSettingsAsset from the project, or null if none exist.
        /// </summary>
        public static UIStoryboardSettingsAsset LoadAsset()
        {
            return LoadFromAssetDatabase<UIStoryboardSettingsAsset>();
        }

        /// <summary>
        /// Opens a SaveFilePanelInProject to create a new settings asset, then automatically registers it in PreloadedAssets.
        /// If an asset already exists, throws an exception.
        /// </summary>
        public static UIStoryboardSettingsAsset CreateAsset()
        {
            var existing = LoadAsset();
            if (existing != null)
            {
                var path = AssetDatabase.GetAssetPath(existing);
                throw new InvalidOperationException(
                    $"{nameof(UIStoryboardSettingsAsset)} already exists at '{path}'.");
            }

            // Prompt user for the asset save location
            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Save UIStoryboardSettingsAsset",
                nameof(UIStoryboardSettingsAsset),
                "asset",
                "Select where to save the UI Storyboard Settings asset.",
                "Assets"
            );

            if (string.IsNullOrEmpty(assetPath))
                return null; // user canceled

            return CreateAssetAtPath(assetPath);
        }

        /// <summary>
        /// Creates the asset at a given path, then automatically registers it in PreloadedAssets.
        /// </summary>
        private static UIStoryboardSettingsAsset CreateAssetAtPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            var instance = ScriptableObject.CreateInstance<UIStoryboardSettingsAsset>();
            AssetDatabase.CreateAsset(instance, assetPath);
            AssetDatabase.SaveAssets();

            // Always register in PreloadedAssets (no toggle).
            RegisterToPreloadedAssets();

            return instance;
        }

        /// <summary>
        /// Registers the settings asset in Preloaded Assets to ensure it's loaded at runtime.
        /// </summary>
        private static void RegisterToPreloadedAssets()
        {
            var asset = LoadAsset();
            if (asset == null)
                return;

            var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            if (!preloaded.Contains(asset))
            {
                preloaded.Add(asset);
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Creates folder + asmdef structure based on the user's settings (ProjectName, RootPath, etc.).
        /// </summary>
        public static void CreateProjectStructure(UIStoryboardSettingsAsset settings)
        {
            if (settings == null)
            {
                Debug.LogError("[UIStoryboard] Settings asset is null. Cannot create structure.");
                return;
            }

            string rootPath = Path.Combine(settings.ProjectRootPath, settings.ProjectName);
            CreateFolderIfNotExist(rootPath);

            // Example subfolders: OutGame/Runtime/Core, OutGame/Runtime/UI, etc.
            // (Adjust as needed)
            string outGamePath = Path.Combine(rootPath, "OutGame");
            CreateFolderIfNotExist(outGamePath);

            string runtimePath = Path.Combine(outGamePath, "Runtime");
            CreateFolderIfNotExist(runtimePath);

            // Core
            string corePath = Path.Combine(runtimePath, "Core");
            CreateFolderIfNotExist(corePath);
            CreateFolderIfNotExist(Path.Combine(corePath, "Gateway"));
            CreateFolderIfNotExist(Path.Combine(corePath, "LifetimeScope"));
            CreateFolderIfNotExist(Path.Combine(corePath, "Repository"));
            CreateFolderIfNotExist(Path.Combine(corePath, "UseCase"));

            // UI
            string uiPath = Path.Combine(runtimePath, "UI");
            CreateFolderIfNotExist(uiPath);
            CreateFolderIfNotExist(Path.Combine(uiPath, "LifetimeScope"));
            CreateFolderIfNotExist(Path.Combine(uiPath, "Model"));
            CreateFolderIfNotExist(Path.Combine(uiPath, "View"));

            string presentationPath = Path.Combine(uiPath, "Presentation");
            CreateFolderIfNotExist(presentationPath);
            CreateFolderIfNotExist(Path.Combine(presentationPath, "Builder"));
            CreateFolderIfNotExist(Path.Combine(presentationPath, "Presenter"));

            // Addressable folder
            if (!string.IsNullOrEmpty(settings.AddressableRootFolderName))
            {
                string addrPath = Path.Combine(settings.ProjectRootPath, settings.AddressableRootFolderName);
                CreateFolderIfNotExist(addrPath);
            }

            // Create asmdef
            CreateAsmdefFiles(corePath, uiPath);

            AssetDatabase.Refresh();
            Debug.Log("[UIStoryboard] Project structure initialization complete!");
        }

        /// <summary>
        /// Example logic to create .asmdef in each folder. 
        /// References are placeholders; adapt to your real dependencies.
        /// </summary>
        private static void CreateAsmdefFiles(string corePath, string uiPath)
        {
            // Example references
            string[] exampleRefs = { "UniTask", "VContainer" };

            // UI
            CreateAsmdef(Path.Combine(uiPath, "LifetimeScope"), "OutGame.Runtime.UI.LifetimeScope", exampleRefs);
            CreateAsmdef(Path.Combine(uiPath, "Model"),        "OutGame.Runtime.UI.Model",        exampleRefs);
            CreateAsmdef(Path.Combine(uiPath, "Presentation"), "OutGame.Runtime.UI.Presentation", exampleRefs);
            CreateAsmdef(Path.Combine(uiPath, "View"),         "OutGame.Runtime.UI.View",         exampleRefs);

            // Core
            CreateAsmdef(Path.Combine(corePath, "Gateway"),     "OutGame.Runtime.Core.Gateway",     exampleRefs);
            CreateAsmdef(Path.Combine(corePath, "LifetimeScope"), "OutGame.Runtime.Core.LifetimeScope", exampleRefs);
            CreateAsmdef(Path.Combine(corePath, "Repository"),  "OutGame.Runtime.Core.Repository",  exampleRefs);
            CreateAsmdef(Path.Combine(corePath, "UseCase"),     "OutGame.Runtime.Core.UseCase",     exampleRefs);
        }

        private static void CreateAsmdef(string folderPath, string assemblyName, string[] references)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string asmdefPath = Path.Combine(folderPath, assemblyName + ".asmdef");
            if (File.Exists(asmdefPath))
            {
                Debug.Log($"[UIStoryboard] Asmdef already exists: {asmdefPath}");
                return;
            }

            // Minimal .asmdef JSON
            var data = new AsmdefData
            {
                name = assemblyName,
                references = references ?? new string[0],
                autoReferenced = true
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(asmdefPath, json);
            Debug.Log($"[UIStoryboard] Created asmdef: {asmdefPath}");
        }

        private static void CreateFolderIfNotExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[UIStoryboard] Created folder: {path}");
            }
        }

        [Serializable]
        private class AsmdefData
        {
            public string name;
            public string[] references;
            public bool autoReferenced;
        }

        /// <summary>
        /// Generic method to find a single asset of type T in the project.
        /// </summary>
        private static T LoadFromAssetDatabase<T>() where T : Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var loaded = AssetDatabase.LoadAssetAtPath<T>(path);
                if (loaded != null)
                    return loaded;
            }
            return null;
        }
#endif
    }
}