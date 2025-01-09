#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    [Serializable]
    public class StoryboardManagerData : ScriptableObject
    {
        [Serializable]
        public class TreeNodeData
        {
            public int id;
            public bool isFolder;
            public string name;
            public string assetPath;
            public int parentId;
            public List<int> childrenIds = new List<int>();
        }

        public List<TreeNodeData> nodes = new List<TreeNodeData>();
        public int nextId = 1;
    }
}
#endif