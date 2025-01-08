using ScreenSystem.Modal;
using ScreenSystem.Page;
using UnityEditor;
using UnityEngine;
using XNode;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    /// <summary>
    /// Node that references:
    /// 1) View Prefab (root has ModalViewBase + LifetimeScope)
    /// 2) Lifecycle (LifecycleModalBase), Model, Builder(IModalBuilder) as MonoScript
    /// 3) Cached Thumbnail (Texture2D) for the prefab's rendered image
    /// </summary>
    public class ModalNode : Node
    {
        // Ports
        [Input(typeConstraint = TypeConstraint.Inherited)]
        public PageViewBase pageViewIn;
        [Input(typeConstraint = TypeConstraint.Inherited)]
        public ModalViewBase modalViewIn;
        [Output(typeConstraint = TypeConstraint.Inherited)]
        public ModalViewBase modalViewOut;

        // -- Prefab --
        [SerializeField]
        private GameObject viewPrefab;

        // -- Scripts (MonoScript) --
        [SerializeField] private MonoScript lifecycleScript; // => LifecycleModalBase
        [SerializeField] private MonoScript modelScript;
        [SerializeField] private MonoScript builderScript;   // => IModalBuilder

        // -- Cached thumbnail
        [SerializeField, HideInInspector]
        private Texture2D cachedThumbnail;

        // Memo field
        [SerializeField] private string memo;

        protected override void Init()
        {
            base.Init();
        }

        public override object GetValue(NodePort port)
        {
            return null;
        }

        // Properties for editor usage
        public GameObject ViewPrefab => viewPrefab;
        public MonoScript LifecycleScript => lifecycleScript;
        public MonoScript ModelScript => modelScript;
        public MonoScript BuilderScript => builderScript;
        public Texture2D CachedThumbnail => cachedThumbnail;
        public string Memo { get => memo; set => memo = value; }

        public void SetCachedThumbnail(Texture2D tex)
        {
            cachedThumbnail = tex;
        }
    }
}