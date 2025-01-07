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

        #region Folder Names
        private const string CoreFolder = "Core";
        private const string UIFolder = "UI";
        private const string LifetimeScopeFolder = "LifetimeScope";
        private const string ModelFolder = "Model";
        private const string PresentationFolder = "Presentation";
        private const string ViewFolder = "View";
        private const string GatewayFolder = "Gateway";
        private const string RepositoryFolder = "Repository";
        private const string UseCaseFolder = "UseCase";
        private const string OutGameFolder = "OutGame";
        private const string RuntimeFolder = "Runtime";
        private const string BuilderFolder = "Builder";
        private const string PresenterFolder = "Presenter";
        #endregion

        #region Reference Names
        private const string UniTaskRef = "UniTask";
        private const string UniTaskLinqRef = "UniTask.Linq";
        private const string UniTaskTextMeshProRef = "UniTask.TextMeshPro";
        private const string VContainerRef = "VContainer";
        private const string MessagePipeRef = "MessagePipe";
        private const string MessagePipeVContainerRef = "MessagePipe.VContainer";
        private const string UnityScreenNavigatorRef = "UnityScreenNavigator";
        private const string ScreenSystemRef = "ScreenSystem";
        private const string TextMeshProRef = "Unity.TextMeshPro";
        
        private const string PresentationRef = "OutGame.Runtime.UI.Presentation";
        private const string UILifetimeScopeRef = "OutGame.Runtime.UI.LifetimeScope";
        private const string ViewRef = "OutGame.Runtime.UI.View";
        private const string ModelRef = "OutGame.Runtime.UI.Model";
        
        private const string UseCaseRef = "OutGame.Runtime.Core.UseCase";
        private const string GatewayRef = "OutGame.Runtime.Core.Gateway";
        private const string RepositoryRef = "OutGame.Runtime.Core.Repository";
        private const string CoreLifetimeScopeRef = "OutGame.Runtime.Core.LifetimeScope";
        #endregion

        #region Core References
        private static readonly string[] CoreGatewayRefs = { UniTaskRef, UniTaskLinqRef, VContainerRef };
        private static readonly string[] CoreLifetimeScopeRefs = { VContainerRef, MessagePipeRef, MessagePipeVContainerRef,
            UnityScreenNavigatorRef, ScreenSystemRef, PresentationRef, UseCaseRef, GatewayRef, RepositoryRef };
        private static readonly string[] RepositoryRefs = { VContainerRef, UniTaskRef, UniTaskLinqRef, GatewayRef };
        private static readonly string[] UseCaseRefs = { VContainerRef, UniTaskRef, UniTaskLinqRef, RepositoryRef };
        #endregion

        #region UI References
        private static readonly string[] UILifetimeScopeRefs =
        {
            UnityScreenNavigatorRef, ScreenSystemRef, VContainerRef, MessagePipeRef, MessagePipeVContainerRef, ViewRef,
            PresentationRef, UseCaseRef
        };
        private static readonly string[] ModelRefs = { UniTaskRef, UniTaskLinqRef };
        private static readonly string[] PresentationRefs =
        {
            UniTaskRef, UniTaskLinqRef, UnityScreenNavigatorRef, ScreenSystemRef, VContainerRef, ViewRef, ModelRef,
            UseCaseRef
        };
        private static readonly string[] ViewRefs =
        {
            UniTaskRef, UniTaskLinqRef, UniTaskTextMeshProRef, ScreenSystemRef, TextMeshProRef, UnityScreenNavigatorRef,
            ModelRef
        };
        #endregion

        
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
            string outGamePath = Path.Combine(rootPath, OutGameFolder);
            CreateFolderIfNotExist(outGamePath);

            string runtimePath = Path.Combine(outGamePath, RuntimeFolder);
            CreateFolderIfNotExist(runtimePath);

            // Core
            string corePath = Path.Combine(runtimePath, CoreFolder);
            CreateFolderIfNotExist(corePath);
            CreateFolderIfNotExist(Path.Combine(corePath, GatewayFolder));
            CreateFolderIfNotExist(Path.Combine(corePath, LifetimeScopeFolder));
            CreateFolderIfNotExist(Path.Combine(corePath, RepositoryFolder));
            CreateFolderIfNotExist(Path.Combine(corePath, UseCaseFolder));

            // UI
            string uiPath = Path.Combine(runtimePath, UIFolder);
            CreateFolderIfNotExist(uiPath);
            CreateFolderIfNotExist(Path.Combine(uiPath, LifetimeScopeFolder));
            CreateFolderIfNotExist(Path.Combine(uiPath, ModelFolder));
            CreateFolderIfNotExist(Path.Combine(uiPath, PresentationFolder));

            string presentationPath = Path.Combine(uiPath, PresentationFolder);
            CreateFolderIfNotExist(presentationPath);
            CreateFolderIfNotExist(Path.Combine(presentationPath, BuilderFolder));
            CreateFolderIfNotExist(Path.Combine(presentationPath, PresenterFolder));

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
            // UI
            CreateAsmdef(Path.Combine(uiPath, LifetimeScopeFolder), UILifetimeScopeRef, UILifetimeScopeRefs);
            CreateAsmdef(Path.Combine(uiPath, ModelFolder), ModelRef, ModelRefs);
            CreateAsmdef(Path.Combine(uiPath, PresentationFolder), PresentationRef, PresentationRefs);
            CreateAsmdef(Path.Combine(uiPath, ViewFolder), ViewRef, ViewRefs);

            // Core
            CreateAsmdef(Path.Combine(corePath, GatewayFolder), GatewayRef, CoreGatewayRefs);
            CreateAsmdef(Path.Combine(corePath, LifetimeScopeFolder), CoreLifetimeScopeRef, CoreLifetimeScopeRefs);
            CreateAsmdef(Path.Combine(corePath, RepositoryFolder), RepositoryRef, RepositoryRefs);
            CreateAsmdef(Path.Combine(corePath, UseCaseFolder), UseCaseRef, UseCaseRefs);
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