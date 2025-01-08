using ScreenSystem.Modal;
using ScreenSystem.Page;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;
using XNode;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    /// <summary>
    /// Node that references:
    /// 1) View Prefab (root has PageViewBase + LifetimeScope)
    /// 2) Lifecycle, Model, Builder as MonoScript
    /// 3) Cached Thumbnail (Texture2D) for the prefab's rendered image
    /// </summary>
    public class PageNode : Node
    {
        [Input] public PageViewBase pageViewIn;
        [Input] public ModalViewBase modalViewIn;
        [Output] public PageViewBase pageViewOut;

        // -- Prefab --
        [SerializeField] private GameObject viewPrefab;

        // -- Scripts (MonoScript) --
        [SerializeField] private MonoScript lifecycleScript;
        [SerializeField] private MonoScript modelScript;
        [SerializeField] private MonoScript builderScript;

        // -- Cached thumbnail from "Update Thumbnail" button --
        [SerializeField, HideInInspector] 
        private Texture2D cachedThumbnail;

        [SerializeField] private string memo;

        protected override void Init()
        {
            base.Init();
        }

        public override object GetValue(NodePort port)
        {
            return null;
        }

        // Public getters if needed by NodeEditor
        public GameObject ViewPrefab => viewPrefab;
        public MonoScript LifecycleScript => lifecycleScript;
        public MonoScript ModelScript => modelScript;
        public MonoScript BuilderScript => builderScript;
        public Texture2D CachedThumbnail => cachedThumbnail;

        // Called by editor script to store the newly captured screenshot
        public void SetCachedThumbnail(Texture2D tex)
        {
            cachedThumbnail = tex;
        }
        
        public string Memo {get => memo; set => memo = value;}
    }
}