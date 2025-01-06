#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    /// <summary>
    /// A custom Project Settings page for "UI Storyboard".
    /// If no asset exists: "Create Settings Asset" only.
    /// If exists: Show fields (Project Name, Root Path, Addressable Folder), plus "Initialize Project Structure" button.
    /// PreloadedAssets registration is ALWAYS done automatically, no toggle shown.
    /// Remove Settings functionality is not provided (user cannot delete).
    /// </summary>
    public class UIStoryboardSettingsProvider : SettingsProvider
    {
        private SerializedObject _serializedObject;
        private UIStoryboardSettingsAsset _settingsAsset;

        /// <summary>
        /// Needed constructor for SettingsProvider.
        /// </summary>
        private UIStoryboardSettingsProvider(string path, SettingsScope scope)
            : base(path, scope)
        {
        }

        /// <summary>
        /// Registers "Project/UI Storyboard" in Project Settings.
        /// </summary>
        [SettingsProvider]
        public static SettingsProvider CreateUIStoryboardSettingsProvider()
        {
            return new UIStoryboardSettingsProvider("Project/UI Storyboard", SettingsScope.Project)
            {
                label = "UI Storyboard"
            };
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            RefreshSettings();
        }

        public override void OnGUI(string searchContext)
        {
            if (_settingsAsset == null)
            {
                // Asset doesn't exist -> show create button
                EditorGUILayout.HelpBox("No UIStoryboardSettingsAsset found in the project.", MessageType.Info);

                if (GUILayout.Button("Create Settings Asset"))
                {
                    var created = UIStoryboardSettings.CreateAsset();
                    if (created != null)
                    {
                        RefreshSettings();
                    }
                }
            }
            else
            {
                // Asset exists -> show fields + "Initialize" button
                if (_serializedObject == null)
                    _serializedObject = new SerializedObject(_settingsAsset);

                _serializedObject.Update();

                EditorGUILayout.LabelField("UI Storyboard Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(_serializedObject.FindProperty("projectName"), 
                    new GUIContent("Project Name"));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty("projectRootPath"), 
                    new GUIContent("Project Root Path"));
                EditorGUILayout.PropertyField(_serializedObject.FindProperty("addressableRootFolderName"), 
                    new GUIContent("Addressable Root Folder"));

                EditorGUILayout.Space();
                if (GUILayout.Button("Initialize Project Structure"))
                {
                    UIStoryboardSettings.CreateProjectStructure(_settingsAsset);
                }

                _serializedObject.ApplyModifiedProperties();
            }
        }

        private void RefreshSettings()
        {
            _settingsAsset = UIStoryboardSettings.LoadAsset();
            _serializedObject = _settingsAsset ? new SerializedObject(_settingsAsset) : null;
        }
    }
}
#endif