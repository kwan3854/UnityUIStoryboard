#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// For Directory, Path

namespace com.kwanjoong.unityuistoryboard.Editor
{
    [CustomEditor(typeof(UnityUIStoryboardSettings))]
    public class UnityUIStoryboardSettingsEditor : UnityEditor.Editor
    {
        #region Dependencies
        // External Dependencies
        private const string UnityScreenNavigator = "UnityScreenNavigator";
        private const string ScreenSystem = "ScreenSystem";
        private const string VContainer = "VContainer";
        private const string MessagePipe = "MessagePipe";
        private const string MessagePipeVContainer = "MessagePipe.VContainer";
        private const string UniTask = "UniTask";
        private const string UniTaskLinq = "UniTask.Linq";
        private const string UniTaskTextMeshPro = "UniTask.TextMeshPro";
        
        // Engine Dependencies
        private const string TextMeshPro = "Unity.TextMeshPro";
        
        // Internal Core dependencies
        private const string UseCase = "OutGame.Runtime.Core.UseCase";
        private const string Gateway = "OutGame.Runtime.Core.Gateway";
        private const string LifetimeScopeCore = "OutGame.Runtime.Core.LifetimeScope";
        private const string Repository = "OutGame.Runtime.Core.Repository";
        
        // Internal UI dependencies
        private const string LifetimeScopeUI = "OutGame.Runtime.UI.LifetimeScope";
        private const string Model = "OutGame.Runtime.UI.Model";
        private const string Presentation = "OutGame.Runtime.UI.Presentation";
        private const string View = "OutGame.Runtime.UI.View";
        #endregion

        #region Directories
        private const string CoreFolder = "Core";
        private const string UIFolder = "UI";
        private const string LifetimeScopeFolder = "LifetimeScope";
        private const string ModelFolder = "Model";
        private const string PresentationFolder = "Presentation";
        private const string ViewFolder = "View";
        private const string GatewayFolder = "Gateway";
        private const string RepositoryFolder = "Repository";
        private const string UseCaseFolder = "UseCase";
        private const string BuilderFolder = "Builder";
        private const string PresenterFolder = "Presenter";
        private const string OutGameFolder = "OutGame";
        private const string RuntimeFolder = "Runtime";
        #endregion

        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            UnityUIStoryboardSettings settings = (UnityUIStoryboardSettings)target;

            if (GUILayout.Button("프로젝트 초기 설정 (폴더 & asmdef 생성)"))
            {
                CreateProjectStructure(settings);
            }
        }

        /// <summary>
        /// 프로젝트 구조를 생성하는 메인 함수
        /// </summary>
        private void CreateProjectStructure(UnityUIStoryboardSettings settings)
        {
            // 1) 최상위 폴더 생성 (예: Assets/SampleProject)
            string rootPath = Path.Combine(settings.projectRootPath, settings.projectName);
            CreateFolderIfNotExist(rootPath);

            // (A) OutGame/Runtime/Core
            CreateFolderIfNotExist(Path.Combine(rootPath, OutGameFolder));
            string runtimePath = Path.Combine(rootPath, OutGameFolder, RuntimeFolder);
            CreateFolderIfNotExist(runtimePath);

            // 2) Core 관련 폴더
            string corePath = Path.Combine(runtimePath, CoreFolder);
            CreateFolderIfNotExist(corePath);
            CreateFolderIfNotExist(Path.Combine(corePath, GatewayFolder));
            CreateFolderIfNotExist(Path.Combine(corePath, LifetimeScopeFolder));
            CreateFolderIfNotExist(Path.Combine(corePath, RepositoryFolder));
            CreateFolderIfNotExist(Path.Combine(corePath, UseCaseFolder));

            // 3) UI 관련 폴더
            string uiPath = Path.Combine(runtimePath, UIFolder);
            CreateFolderIfNotExist(uiPath);
            CreateFolderIfNotExist(Path.Combine(uiPath, LifetimeScopeFolder));
            CreateFolderIfNotExist(Path.Combine(uiPath, ModelFolder));
            CreateFolderIfNotExist(Path.Combine(uiPath, ViewFolder));

            string presentationPath = Path.Combine(uiPath, PresentationFolder);
            CreateFolderIfNotExist(presentationPath);
            CreateFolderIfNotExist(Path.Combine(presentationPath, BuilderFolder));
            CreateFolderIfNotExist(Path.Combine(presentationPath, PresenterFolder));

            // 4) asmdef 생성
            CreateAsmdefFiles(settings, corePath, uiPath);

            // 5) Addressable 루트 폴더 (예: Assets/Prefabs)
            if (!string.IsNullOrEmpty(settings.addressableRootFolderName))
            {
                string addressableFolder = Path.Combine(settings.projectRootPath, settings.addressableRootFolderName);
                CreateFolderIfNotExist(addressableFolder);
            }

            AssetDatabase.Refresh();
            Debug.Log("[UIStoryboard] 프로젝트 초기 설정 완료!");
        }

        /// <summary>
        /// 폴더가 없으면 생성
        /// </summary>
        private void CreateFolderIfNotExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[UIStoryboard] Created Folder: {path}");
            }
        }

        /// <summary>
        /// 필요한 asmdef 파일을 생성
        /// </summary>
        private void CreateAsmdefFiles(UnityUIStoryboardSettings settings, string corePath, string uiPath)
        {
            // (1) UI 쪽 asmdef
            // LifetimeScope
            CreateAsmdef(Path.Combine(uiPath, LifetimeScopeFolder), 
                LifetimeScopeUI, 
                new string[]
                {
                    UnityScreenNavigator,
                    ScreenSystem,
                    VContainer,
                    MessagePipe,
                    MessagePipeVContainer,
                    UseCase,
                    View,
                    Presentation,
                }
            );

            // Model
            CreateAsmdef(Path.Combine(uiPath, ModelFolder), 
                Model, 
                new string[]
                {
                    UniTask,
                    UniTaskLinq,
                }
            );

            // Presentation (Builder, Presenter 함께)
            CreateAsmdef(Path.Combine(uiPath, PresentationFolder), 
                Presentation, 
                new string[]
                {
                    UniTask,
                    UniTaskLinq,
                    UnityScreenNavigator,
                    ScreenSystem,
                    VContainer,
                    View,
                    Model,
                    UseCase,
                }
            );

            // View
            CreateAsmdef(Path.Combine(uiPath, ViewFolder), 
                View, 
                new string[]
                {
                    UniTask,
                    UniTaskLinq,
                    UniTaskTextMeshPro,
                    ScreenSystem,
                    TextMeshPro,
                    UnityScreenNavigator,
                    Model,
                }
            );

            // (2) Core 쪽 asmdef
            // Gateway
            CreateAsmdef(Path.Combine(corePath, GatewayFolder),
                Gateway,
                new string[]
                {
                    UniTask,
                    UniTaskLinq,
                }
            );

            // LifetimeScope
            CreateAsmdef(Path.Combine(corePath, LifetimeScopeFolder),
                LifetimeScopeCore,
                new string[]
                {
                    VContainer,
                    MessagePipe,
                    MessagePipeVContainer,
                    UnityScreenNavigator,
                    ScreenSystem,
                    Gateway,
                    Repository,
                    UseCase,
                    Presentation,
                }
            );

            // Repository
            CreateAsmdef(Path.Combine(corePath, RepositoryFolder),
                Repository,
                new string[]
                {
                    UniTask,
                    UniTaskLinq,
                    VContainer,
                    Repository,
                }
            );

            // UseCase
            CreateAsmdef(Path.Combine(corePath, UseCaseFolder),
                UseCase,
                new string[]
                {
                    UniTask,
                    UniTaskLinq,
                    VContainer,
                    Repository,
                }
            );
        }

        /// <summary>
        /// 실제 .asmdef 파일을 작성
        /// </summary>
        private void CreateAsmdef(string folderPath, string assemblyName, string[] references = null)
        {
            // asmdef 파일 경로
            string asmdefPath = Path.Combine(folderPath, assemblyName + ".asmdef");

            if (File.Exists(asmdefPath))
            {
                Debug.Log($"[UIStoryboard] Asmdef already exists: {asmdefPath}");
                return;
            }

            // 간단한 asmdef JSON 템플릿
            // 필요 시 "includePlatforms", "excludePlatforms", "allowUnsafeCode", "overrideReferences" 등도 넣을 수 있음
            AsmdefData asmdefData = new AsmdefData
            {
                name = assemblyName,
                references = (references == null) ? new string[]{} : references,
                autoReferenced = true
            };

            string json = JsonUtility.ToJson(asmdefData, true);
            File.WriteAllText(asmdefPath, json);
            Debug.Log($"[UIStoryboard] Created Asmdef: {asmdefPath}");
        }
        
        [System.Serializable]
        private class AsmdefData
        {
            public string name;
            public string[] references;
            public bool autoReferenced;
        }

    }
}
#endif