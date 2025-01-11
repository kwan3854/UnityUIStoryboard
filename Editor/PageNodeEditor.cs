#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using XNodeEditor;
using ScreenSystem.Page;
using VContainer.Unity;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    [CustomNodeEditor(typeof(PageNode))]
    public class PageNodeEditor : NodeEditor
    {
        public override int GetWidth()
        {
            return 700; 
        }
        
        public override Color GetTint()
        {
            return new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // Ports
            NodeEditorGUILayout.PortField(target.GetInputPort("pageViewIn"), GUILayout.MinWidth(30));
            NodeEditorGUILayout.PortField(target.GetInputPort("modalViewIn"), GUILayout.MinWidth(30));
            NodeEditorGUILayout.PortField(target.GetOutputPort("pageViewOut"), GUILayout.MinWidth(30));

            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            {
                // ---------------------------------------------
                // [Left: Thumbnail]
                // ---------------------------------------------
                Texture2D cachedTex = (target as PageNode)?.GetCachedThumbnail();

                float thumbWidth = 320f;
                float thumbHeight = 400f;


                EditorGUILayout.BeginVertical(GUILayout.Width(thumbWidth));
                {
                    Rect thumbRect = GUILayoutUtility.GetRect(thumbWidth, thumbHeight, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawRect(thumbRect, Color.clear);
                    
                    if (cachedTex != null)
                    {
                        EditorGUI.DrawPreviewTexture(
                            thumbRect,
                            cachedTex,
                            null,
                            ScaleMode.ScaleToFit
                        );
                    }

                    // Thumbnail Button
                    if (GUILayout.Button("Update Thumbnail"))
                    {
                        var node = (PageNode)target;
                        CapturePrefabThumbnail(node);
                    }
                }
                EditorGUILayout.EndVertical();



                // ---------------------------------------------
                // [Right: Properties]
                // ---------------------------------------------
                EditorGUILayout.BeginVertical();
                {
                    // ----------------------------
                    // View Prefab
                    // ----------------------------
                    var prefabProp = serializedObject.FindProperty("viewPrefab");
                    DrawObjectFieldWithOpenButton(
                        prefabProp,
                        "View Prefab",
                        () => {
                            // 만약 Prefab을 열기 => 애셋 열기
                            GameObject p = prefabProp.objectReferenceValue as GameObject;
                            if (p != null)
                            {
                                // ping or open in project
                                EditorGUIUtility.PingObject(p);
                                // optionally: AssetDatabase.OpenAsset(p);
                            }
                        }
                    );

                    // Prefab Info (Includes "LifetimeScope" open button)
                    GameObject prefab = prefabProp.objectReferenceValue as GameObject;
                    if (prefab != null)
                    {
                        DrawPageViewAndScopeInfo(prefab);
                    }

                    EditorGUILayout.Space();

                    // ----------------------------
                    // Lifecycle
                    // ----------------------------
                    var lifecycleProp = serializedObject.FindProperty("lifecycleScript");
                    DrawMonoScriptWithOpenButton(
                        lifecycleProp,
                        "Lifecycle",
                        scriptClass => {
                            bool isValid = typeof(LifecyclePageBase).IsAssignableFrom(scriptClass) 
                                           && !scriptClass.IsAbstract;
                            return isValid;
                        }
                    );

                    // ----------------------------
                    // Model
                    // ----------------------------
                    var modelProp = serializedObject.FindProperty("modelScript");
                    DrawMonoScriptWithOpenButton(
                        modelProp,
                        "Model",
                        scriptClass => !scriptClass.IsAbstract
                    );

                    // ----------------------------
                    // Builder + memo
                    // ----------------------------
                    var builderProp = serializedObject.FindProperty("builderScript");
                    DrawMonoScriptWithOpenButton(
                        builderProp,
                        "Builder",
                        scriptClass => {
                            bool isValid = typeof(IPageBuilder).IsAssignableFrom(scriptClass)
                                           && !scriptClass.IsAbstract;
                            return isValid;
                        }
                    );

                    // === Memo for builder ===
                    EditorGUILayout.LabelField("Memo");
                    var memoProp = serializedObject.FindProperty("memo");
                    if (memoProp != null)
                    {
                        memoProp.stringValue = EditorGUILayout.TextArea(
                            memoProp.stringValue,
                            GUILayout.MinHeight(60),
                            GUILayout.MaxHeight(230),
                            GUILayout.MaxWidth(300),
                            GUILayout.ExpandWidth(true),
                            GUILayout.ExpandHeight(false)
                        );
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        #region Capture Thumbnail
        private void CapturePrefabThumbnail(PageNode node)
        {
            var prefab = node.ViewPrefab;
            if (!prefab)
            {
                EditorUtility.DisplayDialog("Capture Thumbnail", "No Prefab assigned!", "OK");
                return;
            }

            var settings = UIStoryboardSettingsAsset.Instance;
            int width = settings.CanvasReferenceWidth;
            int height = settings.CanvasReferenceHeight;
            Texture2D screenshot = PrefabScreenshotUtility.TakeScreenshot(prefab, width, height);
            if (screenshot != null)
            {
                node.SetCachedThumbnail(screenshot);
                AssetDatabase.SaveAssets();
            }
            else
            {
                EditorUtility.DisplayDialog("Capture Thumbnail", "Failed to capture screenshot.", "OK");
            }
        }
        #endregion

        #region Helper UI: DrawObjectFieldWithOpenButton
        /// <summary>
        /// ObjectField + Open Button
        /// </summary>
        private void DrawObjectFieldWithOpenButton(SerializedProperty prop, string label, Action openAction)
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label), GUILayout.MinWidth(100));

                bool hasObject = (prop.objectReferenceValue != null);
                bool oldEnabled = GUI.enabled;
                GUI.enabled = hasObject;

                if (GUILayout.Button("Open", GUILayout.Width(50)))
                {
                    openAction?.Invoke();
                }
                GUI.enabled = oldEnabled;
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Helper UI: DrawMonoScriptWithOpenButton
        private void DrawMonoScriptWithOpenButton(
            SerializedProperty prop,
            string label,
            Func<Type, bool> validator
        )
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label), GUILayout.MinWidth(100));

                MonoScript script = prop.objectReferenceValue as MonoScript;
                bool isValid = false;
                if (script != null)
                {
                    Type scriptClass = script.GetClass();
                    if (scriptClass != null) isValid = validator.Invoke(scriptClass);
                }

                bool oldEnabled = GUI.enabled;
                GUI.enabled = (script != null && isValid);

                if (GUILayout.Button("Open", GUILayout.Width(50)))
                {
                    // Open MonoScript
                    AssetDatabase.OpenAsset(script);
                }

                GUI.enabled = oldEnabled;
            }
            EditorGUILayout.EndHorizontal();

            // Validation Msg
            if (prop.objectReferenceValue != null)
            {
                MonoScript ms = prop.objectReferenceValue as MonoScript;
                if (ms != null)
                {
                    Type cls = ms.GetClass();
                    if (cls == null || !validator.Invoke(cls))
                    {
                        EditorGUILayout.HelpBox(
                            $"Selected script is not a valid {label} or is abstract.",
                            MessageType.Warning
                        );
                    }
                }
            }
        }
        #endregion

        #region Prefab Info (View & LifetimeScope)
        private void DrawPageViewAndScopeInfo(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path))
            {
                EditorGUILayout.HelpBox("Prefab is not an asset? Cannot load contents.", MessageType.Warning);
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null)
            {
                EditorGUILayout.HelpBox("Cannot load prefab contents.", MessageType.Error);
                return;
            }

            var pageView = prefabRoot.GetComponent<PageViewBase>();
            var scope = prefabRoot.GetComponent<LifetimeScope>();

            // PageView
            if (!pageView)
            {
                EditorGUILayout.HelpBox("No PageViewBase found on root GameObject.", MessageType.Warning);
            }
            else
            {
                // 한 줄 + [Open]
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"PageViewBase: {pageView.GetType().Name}");
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        OpenMonoScript(pageView);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // LifetimeScope
            if (!scope)
            {
                EditorGUILayout.HelpBox("No LifetimeScope found on root GameObject.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"LifetimeScope: {scope.GetType().Name}");
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        OpenMonoScript(scope);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        private void OpenMonoScript(Component component)
        {
            if (component == null) return;
            var mono = component as MonoBehaviour;
            if (!mono)
            {
                mono = component.GetComponent<MonoBehaviour>();
            }
            if (mono)
            {
                var script = MonoScript.FromMonoBehaviour(mono);
                if (script != null) AssetDatabase.OpenAsset(script);
            }
            else
            {
                EditorUtility.DisplayDialog("Open Code",
                    "Could not open script (not a MonoBehaviour?).",
                    "OK");
            }
        }
        #endregion
    }
}
#endif