#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.kwanjoong.unityuistoryboard.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XNodeEditor;


namespace com.kwanjoong.unityuistoryboard.Editor
{
    public class UIStoryboardManagerWindow : EditorWindow
    {
        private TreeViewState _treeViewState;
        private StoryboardTreeView _treeView;
        private StoryboardManagerData _managerData;

        private string _newFolderName = "NewFolder";
        private string _newStoryboardName = "NewStoryboard";
        private const string ManagerAssetPath = "Assets/UIStoryboard/StoryboardManagerData.asset";

        [MenuItem("Window/UI Storyboard/Storyboard Manager")]
        public static void Open()
        {
            var wnd = GetWindow<UIStoryboardManagerWindow>();
            wnd.titleContent = new GUIContent("UI Storyboard Manager");
            wnd.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateManagerData();
            InitializeTreeView();
        }

        private void LoadOrCreateManagerData()
        {
            _managerData = AssetDatabase.LoadAssetAtPath<StoryboardManagerData>(ManagerAssetPath);
        }

        private void InitializeTreeView()
        {
            if (_treeViewState == null)
                _treeViewState = new TreeViewState();

            _treeView = new StoryboardTreeView(_treeViewState)
            {
                OnGetAllStoryboardAssets = FindAllStoryboardAssets,
                OnDoubleClickStoryboard = (item) =>
                {
                    // Open the storyboard asset in new window
                    var asset = AssetDatabase.LoadAssetAtPath<UIStoryboardGraph>(item.AssetPath);
                    if (asset != null)
                    {
                        var window = NodeEditorWindow.Open(asset);
                        window.titleContent = new GUIContent(item.StoryboardName);
                    }
                }
            };

            if (_managerData != null)
            {
                _treeView.LoadFromManagerData(_managerData);
            }

            _treeView.Reload();
        }

        private void OnGUI()
        {
            if (_managerData == null)
            {
                EditorGUILayout.HelpBox("Manager Data not found. Create it first.", MessageType.Warning);
                if (GUILayout.Button("Create Manager Data"))
                {
                    CreateManagerData();
                    InitializeTreeView();
                }

                return;
            }

            DrawToolbar();
            DrawTreeView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal("box");
            {
                _newFolderName = EditorGUILayout.TextField("Folder", _newFolderName);
                if (GUILayout.Button("Create Folder"))
                {
                    var item = _treeView.CreateFolder(_newFolderName);
                    if (item != null)
                        SaveManagerData();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            {
                _newStoryboardName = EditorGUILayout.TextField("Storyboard", _newStoryboardName);
                if (GUILayout.Button("Create Storyboard"))
                {
                    string basePath = "Assets/UIStoryboard/" + _newStoryboardName + ".asset";
                    var uniquePath = AssetDatabase.GenerateUniqueAssetPath(basePath);

                    var nodeGraph = ScriptableObject.CreateInstance<UIStoryboardGraph>();
                    AssetDatabase.CreateAsset(nodeGraph, uniquePath);
                    AssetDatabase.SaveAssets();

                    var item = _treeView.CreateStoryboard(_newStoryboardName, uniquePath);
                    if (item != null)
                        SaveManagerData();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh External Storyboards"))
            {
                _treeView.RefreshAllStoryboards(_treeView.OnGetAllStoryboardAssets);
                SaveManagerData();
            }
        }

        private void DrawTreeView()
        {
            var rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            _treeView.OnGUI(rect);
        }

        private void CreateManagerData()
        {
            var directory = Path.GetDirectoryName(ManagerAssetPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _managerData = ScriptableObject.CreateInstance<StoryboardManagerData>();
            AssetDatabase.CreateAsset(_managerData, ManagerAssetPath);
            AssetDatabase.SaveAssets();
        }

        public void SaveManagerData()
        {
            if (_managerData != null && _treeView != null)
            {
                _treeView.SaveToManagerData(_managerData);
                EditorUtility.SetDirty(_managerData);
                AssetDatabase.SaveAssets();
            }
        }

        private List<string> FindAllStoryboardAssets()
        {
            var guids = AssetDatabase.FindAssets("t:UIStoryboardGraph");
            return guids.Select(AssetDatabase.GUIDToAssetPath).ToList();
        }
    }
}
#endif