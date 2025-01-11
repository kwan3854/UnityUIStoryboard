using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XNodeEditor;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    public class StoryboardTreeView : TreeView
    {
        public Func<List<string>> OnGetAllStoryboardAssets;
        public Action<StoryboardTreeViewItem> OnDoubleClickStoryboard;

        private int _currentId = 1;
        private Dictionary<int, StoryboardTreeViewItem> _items = new Dictionary<int, StoryboardTreeViewItem>();
        private StoryboardTreeViewItem _root;

        public StoryboardTreeView(TreeViewState state) : base(state)
        {
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            rowHeight = EditorGUIUtility.singleLineHeight * 1.2f;

            _root = new StoryboardTreeViewItem
            {
                id = 0,
                depth = -1,
                displayName = "Root",
                IsFolder = true,
                FolderName = "Root",
                children = new List<TreeViewItem>()
            };
        }

        public void LoadFromManagerData(StoryboardManagerData data)
        {
            _items.Clear();
            _currentId = data.nextId;

            // First pass: Create all items
            foreach (var node in data.nodes)
            {
                var item = new StoryboardTreeViewItem
                {
                    id = node.id,
                    IsFolder = node.isFolder,
                    displayName = node.name,
                    depth = 0 // Will be set correctly later
                };

                if (node.isFolder)
                {
                    item.FolderName = node.name;
                    item.children = new List<TreeViewItem>();
                }
                else
                {
                    item.StoryboardName = node.name;
                    item.AssetPath = node.assetPath;
                }

                _items[node.id] = item;
            }

            // Second pass: Set up parent-child relationships
            foreach (var node in data.nodes)
            {
                var item = _items[node.id];
                if (node.parentId == 0)
                {
                    item.parent = _root;
                    if (!_root.children.Contains(item))
                        _root.children.Add(item);
                }
                else if (_items.ContainsKey(node.parentId))
                {
                    var parent = _items[node.parentId];
                    item.parent = parent;
                    if (parent.children == null)
                        parent.children = new List<TreeViewItem>();
                    if (!parent.children.Contains(item))
                        parent.children.Add(item);
                }
            }

            // Update depths
            UpdateDepthsRecursive(_root, -1);

            Reload();
        }

        private void UpdateDepthsRecursive(TreeViewItem item, int depth)
        {
            item.depth = depth;
            if (item.children != null)
            {
                foreach (var child in item.children)
                {
                    UpdateDepthsRecursive(child, depth + 1);
                }
            }
        }

        public void SaveToManagerData(StoryboardManagerData data)
        {
            data.nodes.Clear();
            data.nextId = _currentId;

            foreach (var item in _items.Values)
            {
                var parentItem = item.parent as StoryboardTreeViewItem;
                var childrenIds = new List<int>();

                if (item.children != null)
                {
                    foreach (var child in item.children)
                    {
                        if (child is StoryboardTreeViewItem paletteChild)
                        {
                            childrenIds.Add(paletteChild.id);
                        }
                    }
                }

                var node = new StoryboardManagerData.TreeNodeData
                {
                    id = item.id,
                    isFolder = item.IsFolder,
                    name = item.IsFolder ? item.FolderName : item.StoryboardName,
                    assetPath = item.IsFolder ? "" : item.AssetPath,
                    parentId = parentItem == _root ? 0 : parentItem?.id ?? 0,
                    childrenIds = childrenIds
                };

                data.nodes.Add(node);
            }
        }
        

        protected override TreeViewItem BuildRoot()
        {
            return _root;
        }

        private void RebuildTreeRecursive(StoryboardTreeViewItem parent)
        {
            var childItems = _items.Values.Where(item => item.parent == parent).ToList();

            parent.children = new List<TreeViewItem>();
            foreach (var child in childItems)
            {
                parent.children.Add(child);
                if (child.IsFolder)
                {
                    RebuildTreeRecursive(child);
                }
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as StoryboardTreeViewItem;
            if (item == null) return;

            // inline rename
            if (item.RenameMode)
            {
                float indent = GetContentIndent(item);
                var rowRect = args.rowRect;
                rowRect.xMin += indent;

                if (item.IsFolder)
                {
                    item.FolderName = EditorGUI.TextField(rowRect, item.FolderName);
                    // finalize on Enter
                    if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
                    {
                        FinalizeRename(item);
                    }
                }
                else
                {
                    item.StoryboardName = EditorGUI.TextField(rowRect, item.StoryboardName);
                    // finalize on Enter
                    if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)
                    {
                        FinalizeRename(item);
                    }
                }
            }
            else
            {
                // default label
                base.RowGUI(args);
            }
        }
        
        private void OpenStoryboardWindow(StoryboardTreeViewItem item)
        {
            if (item.IsFolder) return;

            // 스토리보드 에셋 로드
            var storyboardAsset = AssetDatabase.LoadAssetAtPath<UIStoryboardGraph>(item.AssetPath);
            if (storyboardAsset != null)
            {
                // XNodeEditorWindow를 열고 특정 그래프 로드
                var editorWindow = NodeEditorWindow.Open(storyboardAsset);
                editorWindow.titleContent = new GUIContent(item.StoryboardName);
            }
        }
        

        private void FinalizeRename(StoryboardTreeViewItem item)
        {
            if (item.IsFolder)
            {
                // check duplication among siblings
                if (HasFolderNameDuplicate(item))
                {
                    EditorUtility.DisplayDialog("Error",
                        $"Folder '{item.FolderName}' already exists in this parent!",
                        "OK");
                    // revert or do something
                    item.FolderName = "Folder" + item.id;
                }
            }
            else
            {
                // rename .asset
                if (!string.IsNullOrEmpty(item.AssetPath))
                {
                    string oldPath = item.AssetPath;
                    var dir = Path.GetDirectoryName(oldPath);
                    var newName = item.StoryboardName;
                    var newPath = Path.Combine(dir, newName + ".asset");
                    newPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
                    AssetDatabase.RenameAsset(oldPath, Path.GetFileNameWithoutExtension(newPath));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    item.AssetPath = newPath;
                    
                }
            }

            item.RenameMode = false;
            item.displayName = item.IsFolder ? item.FolderName : item.StoryboardName;
            
            if (EditorWindow.GetWindow<UIStoryboardManagerWindow>() is UIStoryboardManagerWindow window)
            {
                window.SaveManagerData();
            }
            
            Reload();
        }

        private bool HasFolderNameDuplicate(StoryboardTreeViewItem folder)
        {
            if (folder.parent == null) return false;
            var siblings = folder.parent.children;
            foreach (var s in siblings)
            {
                if (s is StoryboardTreeViewItem p
                    && p.IsFolder
                    && p != folder
                    && p.FolderName.Equals(folder.FolderName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #region DragAndDrop

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            var dragged = new List<StoryboardTreeViewItem>();
            foreach (var id in args.draggedItemIDs)
            {
                if (_items.ContainsKey(id))
                    dragged.Add(_items[id]);
            }

            if (dragged.Count == 0) return;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("PaletteTreeViewDrag", dragged);
            DragAndDrop.objectReferences = new UnityEngine.Object[0];
            DragAndDrop.StartDrag(dragged.Count > 1 ? "<Multiple>" : dragged[0].DisplayName);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (!args.performDrop)
                return DragAndDropVisualMode.Move;

            var dragged = DragAndDrop.GetGenericData("PaletteTreeViewDrag") as List<StoryboardTreeViewItem>;
            if (dragged == null || dragged.Count == 0)
                return DragAndDropVisualMode.None;

            var parentItem = args.parentItem as StoryboardTreeViewItem;
            var insertIdx = args.insertAtIndex;

            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.BetweenItems:
                    if (parentItem == null)
                        parentItem = rootItem as StoryboardTreeViewItem;

                    foreach (var d in dragged)
                    {
                        if (d.parent != null && d.parent.children != null)
                            d.parent.children.Remove(d);

                        d.parent = parentItem;
                        d.depth = parentItem.depth + 1;
                    }

                    if (parentItem.children == null)
                        parentItem.children = new List<TreeViewItem>();

                    // Handle the case when dropping at the end of the list
                    if (insertIdx < 0 || insertIdx > parentItem.children.Count)
                        insertIdx = parentItem.children.Count;

                    foreach (var d in dragged)
                    {
                        parentItem.children.Insert(insertIdx, d);
                        insertIdx++;
                    }

                    break;

                case DragAndDropPosition.UponItem:
                    if (parentItem != null && parentItem.IsFolder)
                    {
                        foreach (var d in dragged)
                        {
                            if (d.parent != null && d.parent.children != null)
                                d.parent.children.Remove(d);

                            d.parent = parentItem;
                            d.depth = parentItem.depth + 1;

                            if (parentItem.children == null)
                                parentItem.children = new List<TreeViewItem>();
                            parentItem.children.Add(d);
                        }
                    }

                    break;

                case DragAndDropPosition.OutsideItems:
                    var root = rootItem as StoryboardTreeViewItem;
                    foreach (var d in dragged)
                    {
                        if (d.parent != null && d.parent.children != null)
                            d.parent.children.Remove(d);

                        d.parent = root;
                        d.depth = root.depth + 1;

                        if (root.children == null)
                            root.children = new List<TreeViewItem>();
                        root.children.Add(d);
                    }

                    break;
            }

            // Save the updated hierarchy
            if (EditorWindow.GetWindow<UIStoryboardManagerWindow>() is UIStoryboardManagerWindow window)
            {
                window.SaveManagerData();
            }

            Reload();
            return DragAndDropVisualMode.Move;
        }

        #endregion

        #region Click

        protected override void DoubleClickedItem(int id)
        {
            if (_items.TryGetValue(id, out var item))
            {
                if (!item.IsFolder)
                {
                    OnDoubleClickStoryboard?.Invoke(item);
                }
            }
        }

        #endregion

        #region Context

        protected override void ContextClickedItem(int id)
        {
            if (!_items.ContainsKey(id)) return;
            var item = _items[id];
            var menu = new GenericMenu();

            if (!item.RenameMode)
            {
                menu.AddItem(new GUIContent("Rename"), false, () =>
                {
                    item.RenameMode = true;
                    Reload();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Rename"));
            }

            if (!item.IsFolder)
            {
                menu.AddSeparator("");
                AddMoveToFolderMenuItems(menu, item);
            }

            menu.AddSeparator("");
            AddDeleteMenuItem(menu, item);

            menu.ShowAsContext();
        }

        private void AddMoveToFolderMenuItems(GenericMenu menu, StoryboardTreeViewItem item)
        {
            var folders = _items.Values.Where(x => x.IsFolder && x != item).ToList();
            foreach (var folder in folders)
            {
                menu.AddItem(new GUIContent($"Move to/{folder.DisplayName}"), false,
                    () => { MoveItemToFolder(item, folder); });
            }
        }

        private void AddDeleteMenuItem(GenericMenu menu, StoryboardTreeViewItem item)
        {
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (EditorUtility.DisplayDialog("Delete?",
                        $"Really delete '{item.DisplayName}'?",
                        "Yes", "No"))
                {
                    DeleteItem(item);
                }
            });
        }

        private void MoveItemToFolder(StoryboardTreeViewItem item, StoryboardTreeViewItem targetFolder)
        {
            if (item.parent != null && item.parent.children != null)
            {
                item.parent.children.Remove(item);
            }

            item.parent = targetFolder;
            item.depth = targetFolder.depth + 1;

            if (targetFolder.children == null)
            {
                targetFolder.children = new List<TreeViewItem>();
            }

            targetFolder.children.Add(item);

            if (EditorWindow.GetWindow<UIStoryboardManagerWindow>() is UIStoryboardManagerWindow window)
            {
                window.SaveManagerData();
            }

            Reload();
        }

        private void DeleteItem(StoryboardTreeViewItem item)
        {
            if (!item.IsFolder && !string.IsNullOrEmpty(item.AssetPath))
            {
                AssetDatabase.DeleteAsset(item.AssetPath);
                AssetDatabase.SaveAssets();
            }

            if (item.parent != null && item.parent.children != null)
            {
                item.parent.children.Remove(item);
            }

            _items.Remove(item.id);

            Reload();
        }

        #endregion

        #region CRUD

        /// <summary>
        /// Create a folder item at (optional) parent. 
        /// Checks duplication among parent's children.
        /// </summary>
        public StoryboardTreeViewItem CreateFolder(string name, StoryboardTreeViewItem parent = null)
        {
            if (parent == null) parent = _root;

            // 중복 체크
            if (_items.Values.Any(x => x.parent == parent && x.IsFolder &&
                                       x.FolderName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                EditorUtility.DisplayDialog("Error", $"Folder '{name}' already exists!", "OK");
                return null;
            }

            var folder = new StoryboardTreeViewItem
            {
                id = _currentId++,
                IsFolder = true,
                FolderName = name,
                displayName = name,
                parent = parent,
                depth = parent.depth + 1,
                children = new List<TreeViewItem>()
            };

            _items[folder.id] = folder;

            // 부모의 children 리스트 초기화 및 추가
            if (parent.children == null)
            {
                parent.children = new List<TreeViewItem>();
            }

            parent.children.Add(folder);

            Reload();
            return folder;
        }


        /// <summary>
        /// Create a storyboard item at (optional) parent, referencing an assetPath.
        /// </summary>
        public StoryboardTreeViewItem CreateStoryboard(string name, string assetPath, StoryboardTreeViewItem parent = null)
        {
            if (parent == null) parent = _root;

            var sb = new StoryboardTreeViewItem
            {
                id = _currentId++,
                IsFolder = false,
                StoryboardName = name,
                AssetPath = assetPath,
                displayName = name,
                parent = parent,
                depth = parent.depth + 1,
                children = new List<TreeViewItem>()
            };

            _items[sb.id] = sb;

            // 부모의 children 리스트 초기화 및 추가
            if (parent.children == null)
            {
                parent.children = new List<TreeViewItem>();
            }

            parent.children.Add(sb);

            Reload();
            return sb;
        }

        /// <summary>
        /// Clear all items (folders + storyboards).
        /// </summary>
        public void ClearAll()
        {
            _items.Clear();
            rootItem.children?.Clear();
            Reload();
        }


        /// <summary>
        /// Refresh external storyboard assets by calling a user-provided function 
        /// that returns a list of .asset paths. 
        /// Add any not already in the tree.
        /// </summary>
        public void RefreshAllStoryboards(Func<List<string>> getAllAssets)
        {
            if (getAllAssets == null) return;
            var allPaths = getAllAssets();
            int added = 0;
            foreach (var p in allPaths)
            {
                bool found = _items.Values.Any(x => !x.IsFolder && x.AssetPath == p);
                if (!found)
                {
                    var fileName = Path.GetFileNameWithoutExtension(p);
                    CreateStoryboard(fileName, p, rootItem as StoryboardTreeViewItem);
                    added++;
                }
            }

            if (added > 0)
            {
                Debug.Log($"Refreshed. Found {added} new storyboards from external assets.");
            }
            else
            {
                Debug.Log("No new storyboards found outside the manager.");
            }
        }

        #endregion
    }
}