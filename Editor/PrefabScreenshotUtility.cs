#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;   // for Canvas, CanvasScaler
using System.Collections.Generic;

namespace com.kwanjoong.unityuistoryboard.Editor
{
    public static class PrefabScreenshotUtility
    {
        /// <summary>
        /// Uses PreviewRenderUtility to instantiate a uGUI prefab in ScreenSpaceCamera mode.
        /// Automatically adjusts an orthographic camera so the entire UI is visible.
        /// Returns the rendered Texture2D.
        /// </summary>
        public static Texture2D CapturePrefab(GameObject prefab, int width, int height)
        {
            if (prefab == null)
            {
                Debug.LogError("[CapturePrefabWithPreviewRenderUtility] No prefab provided.");
                return null;
            }

            // 1) Create PreviewRenderUtility & configure camera
            var preview = new PreviewRenderUtility();
            preview.camera.backgroundColor = Color.gray;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.cameraType = CameraType.Game;
            preview.camera.farClipPlane = 1000f;
            preview.camera.nearClipPlane = 0.1f;
            // We will switch to orthographic below, after we find the bounding box
            preview.camera.orthographic = false;  
            preview.camera.transform.position = new Vector3(0, 0, -10f);
            preview.camera.transform.LookAt(Vector3.zero);

            // 2) BeginStaticPreview with a rectangle (this defines the render area)
            var previewRect = new Rect(0, 0, width, height);
            preview.BeginStaticPreview(previewRect);

            // 3) Instantiate prefab via PreviewRenderUtility
            var instance = preview.InstantiatePrefabInScene(prefab);
            if (!instance)
            {
                Debug.LogError("[CapturePrefabWithPreviewRenderUtility] Failed to instantiate prefab in preview scene.");
                preview.EndStaticPreview();
                preview.Cleanup();
                return null;
            }

            // 4) Setup Canvas
            var canvas = instance.GetComponent<Canvas>();
            if (canvas == null) canvas = instance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;

            canvas.worldCamera = preview.camera;

            var scaler = instance.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = instance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(width, height);

            // Force positioning
            instance.transform.position = Vector3.zero;

            // 5) Force UI layout
            Canvas.ForceUpdateCanvases();

            // 6) Calculate bounding box of all UI elements -> set Orthographic camera so entire UI fits
            Bounds bounds = CalculateUIBounds(instance);
            // Switch to orthographic
            preview.camera.orthographic = true;

            // bounding box extents
            float xSize = bounds.size.x;
            float ySize = bounds.size.y;
            float halfMax = Mathf.Max(xSize, ySize) * 0.5f;
            // Add small padding factor so UI isn't right at the edge
            float paddingFactor = 1.1f;
            halfMax *= paddingFactor;

            preview.camera.orthographicSize = halfMax;

            // Position camera to center on bounding box
            // boundingBox center is 'bounds.center'
            // We'll place the camera at (center.x, center.y, someZ) so the UI is centered
            // (Make sure we offset by z so we don't clip through the UI)
            Vector3 center = bounds.center;
            Vector3 camPos = new Vector3(center.x, center.y, -10f);
            preview.camera.transform.position = camPos;
            preview.camera.transform.LookAt(center);

            // Force update again
            Canvas.ForceUpdateCanvases();

            // 7) Render
            preview.Render();

            // 8) EndStaticPreview to get the final texture
            var texture = preview.EndStaticPreview();

            // 9) Cleanup
            preview.camera.targetTexture = null;
            preview.Cleanup();

            return texture;
        }

        /// <summary>
        /// Calculate the bounding box of all RectTransforms in the prefab instance.
        /// We'll gather their world corners and encapsulate them into a Bounds.
        /// </summary>
        private static Bounds CalculateUIBounds(GameObject root)
        {
            var rectTransforms = root.GetComponentsInChildren<RectTransform>(true);

            bool first = true;
            Bounds bounds = new Bounds();

            foreach (var rt in rectTransforms)
            {
                var corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                // Encapsulate all 4 corners
                foreach (var c in corners)
                {
                    if (first)
                    {
                        bounds = new Bounds(c, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(c);
                    }
                }
            }
            return bounds;
        }
    }
}
#endif