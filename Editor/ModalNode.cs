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

        // // -- Cached thumbnail
        // [SerializeField, HideInInspector]
        // private Texture2D cachedThumbnail;
        [SerializeField, HideInInspector]
        private byte[] thumbnailData;

        // Memo field
        [SerializeField] private string memo;
        
        private Texture2D _cachedThumbnail;

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
        public string Memo { get => memo; set => memo = value; }

        // Called by editor script to store the newly captured screenshot
        public void SetCachedThumbnail(Texture2D tex)
        {
            if (tex == null)
            {
                return;
            }
            
            thumbnailData = tex.EncodeToPNG();
            _cachedThumbnail = tex;
        }
        
        public Texture2D GetCachedThumbnail()
        {
            if (thumbnailData == null)
            {
                return null;
            }
            
            if (_cachedThumbnail != null)
            {
                return _cachedThumbnail;
            }

            var tex = new Texture2D(2, 2);
            tex.LoadImage(thumbnailData);
            _cachedThumbnail = tex;
            return tex;
        }
    }
}