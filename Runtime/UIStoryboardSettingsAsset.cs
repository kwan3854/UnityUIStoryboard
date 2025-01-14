using System;
using UnityEngine;

namespace com.kwanjoong.unityuistoryboard
{
    /// <summary>
    /// Main ScriptableObject for UI Storyboard settings (used by both Editor and Runtime).
    /// User sets "ProjectName", "ProjectRootPath", and "AddressableRootFolderName" in Project Settings.
    /// </summary>
    [Serializable]
    public sealed class UIStoryboardSettingsAsset : ScriptableObject
    {
        [SerializeField] private string projectName = "ProjectName";
        [SerializeField] private string projectRootPath = "Assets";
        [SerializeField] private string addressableRootFolderName = "Prefabs";
        
        [SerializeField] private int canvasReferenceWidth = 1080;
        [SerializeField] private int canvasReferenceHeight = 1920;

        /// <summary>
        /// The top-level folder name (e.g. "SampleProject").
        /// </summary>
        public string ProjectName
        {
            get => projectName;
            set => projectName = value;
        }

        /// <summary>
        /// Usually "Assets".
        /// </summary>
        public string ProjectRootPath
        {
            get => projectRootPath;
            set => projectRootPath = value;
        }

        /// <summary>
        /// The folder name for Addressable assets (e.g. "Prefabs").
        /// </summary>
        public string AddressableRootFolderName
        {
            get => addressableRootFolderName;
            set => addressableRootFolderName = value;
        }
        
        /// <summary>
        /// The reference resolution width for uGUI CanvasScaler.
        /// </summary>
        public int CanvasReferenceWidth
        {
            get => canvasReferenceWidth;
            set => canvasReferenceWidth = value;
        }

        /// <summary>
        /// The reference resolution height for uGUI CanvasScaler.
        /// </summary>
        public int CanvasReferenceHeight
        {
            get => canvasReferenceHeight;
            set => canvasReferenceHeight = value;
        }

        // --------------------------------------------------------------------------------
        // Runtime/Editor Singleton Access
        // --------------------------------------------------------------------------------

        private static UIStoryboardSettingsAsset _instance;

        public static UIStoryboardSettingsAsset Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null)
                    _instance = UIStoryboardSettings.LoadAsset();
#else
                // In a built player or at runtime, if not preloaded, fallback to a new in-memory instance.
                if (_instance == null)
                    Debug.LogError("UIStoryboardSettings scriptable object not found in preloaded assets.");
#endif
                return _instance;
            }
        }

        private void OnEnable()
        {
#if !UNITY_EDITOR
            // If this asset is in PreloadedAssets at runtime, ensure the static instance points here.
            _instance = this;
#endif
        }
    }
}