using UnityEditor.IMGUI.Controls;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    public class PaletteTreeViewItem : TreeViewItem
    {
        public bool IsFolder;
        public bool RenameMode; // Toggle for inline rename

        public string FolderName; // if IsFolder = true
        public string StoryboardName; // if !IsFolder
        public string AssetPath; // if !IsFolder => .asset path

        public string DisplayName => IsFolder ? FolderName : StoryboardName;
    }
}