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

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // (1) 상단 포트
            NodeEditorGUILayout.PortField(target.GetInputPort("pageViewIn"), GUILayout.MinWidth(30));
            NodeEditorGUILayout.PortField(target.GetInputPort("modalViewIn"), GUILayout.MinWidth(30));
            NodeEditorGUILayout.PortField(target.GetOutputPort("pageViewOut"), GUILayout.MinWidth(30));

            EditorGUILayout.Space();

            // 가로 배치 시작
            EditorGUILayout.BeginHorizontal();
            {
                // ---------------------------------------------
                // [왼쪽 썸네일 영역]
                // ---------------------------------------------
                var thumbProp = serializedObject.FindProperty("cachedThumbnail");
                Texture2D cachedTex = thumbProp.objectReferenceValue as Texture2D;

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

                    // Thumbnail 버튼
                    if (GUILayout.Button("Update Thumbnail"))
                    {
                        var node = (PageNode)target;
                        CapturePrefabThumbnail(node);
                    }
                }
                EditorGUILayout.EndVertical();



                // ---------------------------------------------
                // [오른쪽: 프로퍼티/버튼 영역]
                // ---------------------------------------------
                EditorGUILayout.BeginVertical();
                {
                    // ----------------------------
                    // View Prefab (한 줄 + Open)
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
                        // 멀티라인 TextArea, 스크롤 가능
                        memoProp.stringValue = EditorGUILayout.TextArea(
                            memoProp.stringValue,
                            GUILayout.MinHeight(60),    // 최소 높이
                            GUILayout.MaxHeight(230),   // 최대 높이
                            GUILayout.MaxWidth(300),    // 최대 너비
                            GUILayout.ExpandWidth(true),
                            GUILayout.ExpandHeight(false) // 스크롤
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
            Texture2D screenshot = PrefabScreenshotUtility.CapturePrefab(prefab, width, height);
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
        /// 한 줄에 Object 필드 + [Open] 버튼
        /// 사용자가 Prefab이나 다른 Object를 할당할 수 있으며, 오른쪽 버튼으로 ping or open 가능.
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
                    // 열기
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