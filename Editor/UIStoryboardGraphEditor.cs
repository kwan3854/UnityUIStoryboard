using System;
using XNodeEditor;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    [CustomNodeGraphEditor(typeof(UIStoryboardGraph))]
    public class UIStoryboardGraphEditor : NodeGraphEditor
    {
        public override string GetNodeMenuName(Type type)
        {
            // Only show PageNode and ModalNode in the context menu
            if (type.Name == nameof(PageNode))
                return "Create/PageNode";
            if (type.Name == nameof(ModalNode))
                return "Create/ModalNode";
            return null;
        }
    }
}
