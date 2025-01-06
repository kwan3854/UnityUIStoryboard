using UnityEngine;

namespace com.kwanjoong.unityuistoryboard
{
    [CreateAssetMenu(fileName = "UnityUIStoryboardSettings", menuName = "UIStoryboard/UnityUIStoryboardSettings")]
    public class UnityUIStoryboardSettings : ScriptableObject
    {
        [Header("UIStoryboard Settings")]
        public string projectName = "ProjectName";
        public string projectRootPath = "Assets";
        public string addressableRootFolderName = "Prefabs";
    }
}
