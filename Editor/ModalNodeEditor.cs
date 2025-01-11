#if UNITY_EDITOR
using System;
using ScreenSystem.Modal;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;
using XNodeEditor;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    [CustomNodeEditor(typeof(ModalNode))]
    public class ModalNodeEditor : NodeEditor
    {
        public override int GetWidth()
        {
            return 700;
        }

        public override void OnBodyGUI()
        {
            serializedObject.Update();

            // Ports
            NodeEditorGUILayout.PortField(target.GetInputPort("pageViewIn"), GUILayout.MinWidth(30));
            NodeEditorGUILayout.PortField(target.GetInputPort("modalViewIn"), GUILayout.MinWidth(30));
            NodeEditorGUILayout.PortField(target.GetOutputPort("modalViewOut"), GUILayout.MinWidth(30));

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            {
                // [Left: Thumbnail]
                Texture2D cachedTex = (target as ModalNode)?.GetCachedThumbnail();

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

                    if (GUILayout.Button("Update Thumbnail"))
                    {
                        var node = (ModalNode)target;
                        CapturePrefabThumbnail(node);
                    }
                }
                EditorGUILayout.EndVertical();

                // [Right: Properties]
                EditorGUILayout.BeginVertical();
                {
                    // View Prefab
                    var prefabProp = serializedObject.FindProperty("viewPrefab");
                    DrawObjectFieldWithOpenButton(
                        prefabProp,
                        "View Prefab",
                        () =>
                        {
                            GameObject p = prefabProp.objectReferenceValue as GameObject;
                            if (p != null)
                            {
                                EditorGUIUtility.PingObject(p);
                                // or AssetDatabase.OpenAsset(p);
                            }
                        }
                    );

                    // If prefab != null => check info
                    GameObject prefab = prefabProp.objectReferenceValue as GameObject;
                    if (prefab != null)
                    {
                        DrawModalViewAndScopeInfo(prefab);
                    }

                    EditorGUILayout.Space();

                    // Lifecycle => LifecycleModalBase
                    var lifecycleProp = serializedObject.FindProperty("lifecycleScript");
                    DrawMonoScriptWithOpenButton(
                        lifecycleProp,
                        "Lifecycle",
                        scriptClass => {
                            bool isValid = typeof(LifecycleModalBase).IsAssignableFrom(scriptClass)
                                           && !scriptClass.IsAbstract;
                            return isValid;
                        }
                    );

                    // Model => any class (non-abstract)
                    var modelProp = serializedObject.FindProperty("modelScript");
                    DrawMonoScriptWithOpenButton(
                        modelProp,
                        "Model",
                        scriptClass => !scriptClass.IsAbstract
                    );

                    // Builder => IModalBuilder
                    var builderProp = serializedObject.FindProperty("builderScript");
                    DrawMonoScriptWithOpenButton(
                        builderProp,
                        "Builder",
                        scriptClass => {
                            bool isValid = typeof(IModalBuilder).IsAssignableFrom(scriptClass)
                                           && !scriptClass.IsAbstract;
                            return isValid;
                        }
                    );

                    // Memo
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

        // Capture
        private void CapturePrefabThumbnail(ModalNode node)
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

        // DrawObjectFieldWithOpenButton / DrawMonoScriptWithOpenButton => same logic as PageNodeEditor
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
                    AssetDatabase.OpenAsset(script);
                }

                GUI.enabled = oldEnabled;
            }
            EditorGUILayout.EndHorizontal();

            // Validation msg
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

        private void DrawModalViewAndScopeInfo(GameObject prefab)
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

            // ModalViewBase instead of PageViewBase
            var modalView = prefabRoot.GetComponent<ModalViewBase>();
            var scope = prefabRoot.GetComponent<LifetimeScope>();

            // ModalView
            if (!modalView)
            {
                EditorGUILayout.HelpBox("No ModalViewBase found on root GameObject.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"ModalViewBase: {modalView.GetType().Name}");
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        OpenMonoScript(modalView);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // LifetimeScope same
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
            if (!mono) mono = component.GetComponent<MonoBehaviour>();
            if (mono)
            {
                var script = MonoScript.FromMonoBehaviour(mono);
                if (script != null) AssetDatabase.OpenAsset(script);
            }
            else
            {
                EditorUtility.DisplayDialog("Open Code", "Could not open script (not a MonoBehaviour?).", "OK");
            }
        }
    }
}
#endif