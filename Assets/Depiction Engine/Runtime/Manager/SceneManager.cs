// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design", "IDE1006",
    Justification = "Unity uses camelCase instead of PascalCase for public properties.",
    Scope = "module")]

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing the scene.
    /// </summary>
    [DefaultExecutionOrder(-3)]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(SceneManager))]
    [DisallowMultipleComponent]
    public class SceneManager : ManagerBase, IHasChildren
    {
        /// <summary>
        /// The different steps of an update in order of execution. <br/><br/>
        /// <b><see cref="None"/>:</b> <br/>
        /// Update code is not currently being executed. <br/><br/>
        /// <b><see cref="LateInitialize"/>:</b> <br/>
        /// Objects that were not initialized are automatically initialized. <br/><br/>
        /// <b><see cref="PreHierarchicalUpdate"/>:</b> <br/>
        /// The hierarchy is traversed and values are prepared for the update. <br/><br/>
        /// <b><see cref="HierarchicalUpdate"/>:</b> <br/>
        /// The hierarchy is traversed and the update code is executed. <br/><br/>
        /// <b><see cref="PostHierarchicalUpdate"/>:</b> <br/>
        /// The hierarchy is traversed and code that required updated values is executed. <br/><br/>
        /// <b><see cref="HierarchicalClearDirtyFlags"/>:</b> <br/>
        /// The hierarchy is traversed and dirty flags are cleared. <br/><br/>
        /// <b><see cref="HierarchicalActivate"/>:</b> <br/>
        /// The hierarchy is traversed and gameObjects that have never been active are temporarly activated and deactivated to allow for their Awake to be called. <br/><br/>
        /// <b><see cref="UnityInitialized"/>:</b> <br/>
        /// Marks all newly initialized objects as 'UnityInitialized'. <br/><br/>
        /// <b><see cref="DelayedOnDestroy"/>:</b> <br/>
        /// Objects that were waiting to be destroyed are destroyed. <br/><br/>
        /// <b><see cref="DelayedDispose"/>:</b> <br/>
        /// Objects that were waiting to be disposed are disposed. <br/><br/>
        /// <b><see cref="DelayedDisposeLate"/>:</b> <br/>
        /// Objects that were waiting to be disposed late are disposed.
        /// </summary> 
        public enum UpdateExecutionState
        {
            None,
            LateInitialize,
            PreHierarchicalUpdate,
            HierarchicalUpdate,
            PostHierarchicalUpdate,
            HierarchicalClearDirtyFlags,
            HierarchicalActivate,
            UnityInitialized,
            DelayedOnDestroy,
            DelayedDispose,
            DelayedDisposeLate,
        };

        public const string NAMESPACE = "DepictionEngine";
        public const string SCENE_MANAGER_NAME = "Managers (Required)";

        [BeginFoldout("Debug")]
        [SerializeField, Tooltip("When enabled some hidden properties and objects will be exposed to help with debugging."), EndFoldout]
        public bool _debug;

        [BeginFoldout("Performance")]
        [SerializeField, Tooltip("Should the player be running when the application is in the background?")]
        public bool _runInBackground;
        [SerializeField, Tooltip("When enabled some "+nameof(Processor)+"'s will perform their work on seperate threads."), EndFoldout]
        public bool _enableMultithreading;

#if UNITY_EDITOR
        [BeginFoldout("Asset Bundle")]
        [SerializeField, Tooltip("The directory where the AssetBundle should be built.")]
        private string _buildOutputPath;
        [SerializeField, Tooltip("AssetBundle building options.")]
        private UnityEditor.BuildAssetBundleOptions _buildOptions;
        [SerializeField, Tooltip("Target build platform.")]
        private UnityEditor.BuildTarget _buildTarget;
        [SerializeField, Button(nameof(BuildAssetBundlesBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Build all AssetBundle.")]
        private bool _buildAssetBundles;
        [Space()]
        [SerializeField, Button(nameof(UnloadAllAssetBundlesBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Unloads all currently loaded AssetBundles."), EndFoldout]
        private bool _unloadAllBuildAssetBundles;

        private void BuildAssetBundlesBtn()
        {
            UnityEditor.BuildPipeline.BuildAssetBundles(buildOutputPath, buildOptions, buildTarget);
        }

        private void UnloadAllAssetBundlesBtn()
        {
            UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
        }
#endif

        private TweenManager _tweenManager;
        private PoolManager _poolManager;
        private InputManager _inputManager;
        private CameraManager _cameraManager;
        private RenderingManager _renderingManager;
        private DatasourceManager _datasourceManager;
        private JsonInterface _jsonInterface;
        private InstanceManager _instanceManager;

#if UNITY_EDITOR
        private List<TransformBase> _sceneCameraTransforms;
#endif
        private List<TransformBase> _sceneChildren;

        private bool _updated;

        private static bool _activateAll;

        private static UpdateExecutionState _sceneExecutionState;

        private static bool _sceneClosing;

        public static Action PostLateInitializeEvent;
        public static Action SceneClosingEvent;
        public static Action UnityInitializedEvent;
        public static Action DelayedOnDestroyEvent;

        public static Action BeforeAssemblyReloadEvent;
        public static Action AfterAssemblyReloadEvent;

        private static SceneManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SceneManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL && createIfMissing)
            {
                _instance = GetManagerComponent<SceneManager>();
                if (_instance != Disposable.NULL)
                    _instance.transform.SetAsFirstSibling();
            }
            return _instance;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

#if UNITY_EDITOR            
            UnityEditor.PlayerSettings.allowUnsafeCode = true;

            InitSceneCameraTransforms();

            Editor.AssetManager.InitListeners();

            Editor.SceneViewDouble.InitSceneViewDoubles(ref _sceneViewDoubles);
#endif

            PoolManager.Instance();
            InputManager.Instance();
            DatasourceManager.Instance();
            CameraManager.Instance();
            RenderingManager.Instance();

            Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Ignore Render"), 0);

            UpdateRunInBackground();
        }


#if UNITY_EDITOR
        private void InitSceneCameraTransforms()
        {
            if (_sceneCameraTransforms == null)
                _sceneCameraTransforms = new List<TransformBase>();
        }
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => debug = value, false, initializingState);
            InitValue(value => runInBackground = value, true, initializingState);
            InitValue(value => enableMultithreading = value, true, initializingState);
#if UNITY_EDITOR
            InitValue(value => buildOutputPath = value, "Assets/DepictionEngine/Resources/AssetBundle", initializingState);
            InitValue(value => buildOptions = value, UnityEditor.BuildAssetBundleOptions.None, initializingState);
            InitValue(value => buildTarget = value, UnityEditor.BuildTarget.StandaloneWindows64, initializingState);
#endif
        }

#if UNITY_EDITOR
        protected override void UpdateFields()
        {
            base.UpdateFields();

            Editor.SceneViewDouble.InitSceneViewDoubles(ref _sceneViewDoubles);
        }
#endif

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
                RenderPipelineManager.endCameraRendering -= EndCameraRendering;
                if (!IsDisposing())
                {
                    RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
                    RenderPipelineManager.endCameraRendering += EndCameraRendering;
                }

#if UNITY_EDITOR
                UnityEditor.EditorApplication.pauseStateChanged -= EditorPauseStateChanged;
                UnityEditor.EditorApplication.playModeStateChanged -= PlayModeStateChangedHandler;
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosing -= SceneClosingHandler;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= SceneSaving;
                UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= SceneSaved;
                UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReloadHandler;
                UnityEditor.AssemblyReloadEvents.afterAssemblyReload -= AfterAssemblyReloadHandler;
                UnityEditor.Selection.selectionChanged -= SelectionChangedHandler;
                if (!IsDisposing())
                {
                    UnityEditor.EditorApplication.pauseStateChanged += EditorPauseStateChanged;
                    UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChangedHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += SceneClosingHandler;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += SceneSaving;
                    UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += SceneSaved;
                    UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReloadHandler;
                    UnityEditor.AssemblyReloadEvents.afterAssemblyReload += AfterAssemblyReloadHandler;
                    UnityEditor.Selection.selectionChanged += SelectionChangedHandler;
                }

                Editor.AssetManager.InitListeners();

                Editor.UndoManager.UpdateAllDelegates(IsDisposing());

                UpdateUpdateDelegate(IsDisposing());

                UpdateHarmonyPatches();
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public static Action<UnityEditor.PlayModeStateChange> PlayModeStateChangedEvent;

        private static UnityEditor.PlayModeStateChange _playModeState;
        private void PlayModeStateChangedHandler(UnityEditor.PlayModeStateChange state)
        {
            playModeState = state;

            if (PlayModeStateChangedEvent != null)
                PlayModeStateChangedEvent(state);
        }

        public static UnityEditor.PlayModeStateChange playModeState
        {
            get { return _playModeState; }
            set
            {
                if (_playModeState == value)
                    return;

                _playModeState = value;
            }
        }

        private static bool _saving;
        private void SceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            saving = true;
        }

        private void SceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            saving = false;
        }

        private static bool saving
        {
            get { return _saving; }
            set
            {
                if (_saving == value)
                    return;
                _saving = value;
            }
        }

        private static bool _assembling;
        private void BeforeAssemblyReloadHandler()
        {
            //This is required to prevent UIVisual from not being Destroyed when recompiling scripts repeatedly and not giving enough focus to the Editor window in between recompiles
            //It seems to work despite the fact that the DelayedDispose Queue is empty when this event is dispatched and therefore nothing of relevance happens during the InvokeActions() call
            DisposeManager.InvokeActions();

            UpdateHarmonyPatches(true);

            tweenManager.DisposeAllTweens();
            poolManager.ClearPool();

            if (BeforeAssemblyReloadEvent != null)
                BeforeAssemblyReloadEvent();
        }

        private void AfterAssemblyReloadHandler()
        {
            Resources.UnloadUnusedAssets();

            if (AfterAssemblyReloadEvent != null)
                AfterAssemblyReloadEvent();
        }

        private void SceneClosingHandler(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            if (gameObject.scene == scene)
            {
                UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += SceneClosed;
                _sceneClosing = true;

                if (SceneClosingEvent != null)
                    SceneClosingEvent();
            }
        }

        private void SceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed -= SceneClosed;
            _sceneClosing = false;
        }

        private HarmonyLib.Harmony _harmony;
        private void UpdateHarmonyPatches(bool reloadingAssembly = false)
        {
            string harmonyId = id.ToString();

            if (!reloadingAssembly && !IsDisposing())
            {
                if (Object.ReferenceEquals(_harmony, null))
                {
                    _harmony = new HarmonyLib.Harmony(harmonyId);

                    MethodInfo unityMoveToView = typeof(UnityEditor.SceneView).GetMethod("MoveToView", new Type[] { });
                    MethodInfo preMoveToView = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreMoveToView", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityMoveToView, new HarmonyLib.HarmonyMethod(preMoveToView));

                    MethodInfo unityMoveToViewTarget = typeof(UnityEditor.SceneView).GetMethod("MoveToView", new Type[] { typeof(Transform) });
                    MethodInfo preMoveToViewTarget = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreMoveToViewTarget", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityMoveToViewTarget, new HarmonyLib.HarmonyMethod(preMoveToViewTarget));

                    MethodInfo unityAlignWithView = typeof(UnityEditor.SceneView).GetMethod("AlignWithView", new Type[] { });
                    MethodInfo preAlignWithView = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreAlignWithView", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityAlignWithView, new HarmonyLib.HarmonyMethod(preAlignWithView));

                    MethodInfo unityAlignViewToObject = typeof(UnityEditor.SceneView).GetMethod("AlignViewToObject", new Type[] { typeof(Transform) });
                    MethodInfo preAlignViewToObject = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreAlignViewToObject", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityAlignViewToObject, new HarmonyLib.HarmonyMethod(preAlignViewToObject));

                    MethodInfo unityFrameSelected = typeof(UnityEditor.SceneView).GetMethod("FrameSelected", new Type[] { typeof(bool), typeof(bool) });
                    MethodInfo preFrameSelected = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreFrameSelected", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityFrameSelected, new HarmonyLib.HarmonyMethod(preFrameSelected));

                    MethodInfo unityFrame = typeof(UnityEditor.SceneView).GetMethod("Frame", BindingFlags.Instance | BindingFlags.Public);
                    MethodInfo preFrame = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreFrame", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityFrame, new HarmonyLib.HarmonyMethod(preFrame));

                    MethodInfo unitySetupCamera = typeof(UnityEditor.SceneView).GetMethod("SetupCamera", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo postSetupCamera = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostSetupCamera", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unitySetupCamera, null, new HarmonyLib.HarmonyMethod(postSetupCamera));

                    MethodInfo unityDefaultHandles = typeof(UnityEditor.SceneView).GetMethod("DefaultHandles", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo preDefaultHandles = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreDefaultHandles", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo postDefaultHandles = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostDefaultHandles", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityDefaultHandles, new HarmonyLib.HarmonyMethod(preDefaultHandles), new HarmonyLib.HarmonyMethod(postDefaultHandles));

                    MethodInfo unityHandleSelectionAndOnSceneGUI = typeof(UnityEditor.SceneView).GetMethod("HandleSelectionAndOnSceneGUI", BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    MethodInfo preHandleSelectionAndOnSceneGUI = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreHandleSelectionAndOnSceneGUI", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo postHandleSelectionAndOnSceneGUI = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostHandleSelectionAndOnSceneGUI", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityHandleSelectionAndOnSceneGUI, new HarmonyLib.HarmonyMethod(preHandleSelectionAndOnSceneGUI), new HarmonyLib.HarmonyMethod(postHandleSelectionAndOnSceneGUI));

                    MethodInfo unityGetHandleSize = typeof(UnityEditor.HandleUtility).GetMethod("GetHandleSize", BindingFlags.Static | BindingFlags.Public);
                    MethodInfo postGetHandleSize = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostGetHandleSize", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityGetHandleSize, null, new HarmonyLib.HarmonyMethod(postGetHandleSize));

                    MethodInfo unitySetupArcMaterial = typeof(UnityEditor.Handles).GetMethod("SetupArcMaterial", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo postSetupArcMaterial = typeof(Editor.SceneViewDouble).GetMethod("PatchedPostSetupArcMaterial", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unitySetupArcMaterial, null, new HarmonyLib.HarmonyMethod(postSetupArcMaterial));

                    MethodInfo unityAddCursorRect = typeof(UnityEditor.SceneView).GetMethod("AddCursorRect", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo preAddCursorRect = typeof(Editor.SceneViewDouble).GetMethod("PatchedPreAddCursorRect", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityAddCursorRect, new HarmonyLib.HarmonyMethod(preAddCursorRect));

                    MethodInfo unityHandleMouseUp = Editor.SceneViewMotion.GetUnitySceneViewMotionType().GetMethod("HandleMouseUp", BindingFlags.Static | BindingFlags.NonPublic);
                    MethodInfo preHandleMouseUp = typeof(Editor.SceneViewMotion).GetMethod("PatchedPreHandleMouseUp", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityHandleMouseUp, new HarmonyLib.HarmonyMethod(preHandleMouseUp));

                    MethodInfo unityGetInspectorTitle = typeof(UnityEditor.ObjectNames).GetMethod("GetInspectorTitle", BindingFlags.Static | BindingFlags.Public);
                    MethodInfo postGetInspectorTitle = typeof(Editor.ObjectNames).GetMethod("PatchedPostGetInspectorTitle", BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityGetInspectorTitle, null, new HarmonyLib.HarmonyMethod(postGetInspectorTitle));

                    //Fix for a Unity Bug where a null item is sent to the DoItemGUI method in TreeViewController causing it to spam NullReferenceException.
                    //The bug usually occurs when a GameObject foldout is opened in the SceneHierarchy while new child objects are being added/created quickly by a loader or anything else. 
                    //The spaming, once triggered, will usually stop after the GameObject is deselected 
                    //TODO: File a bug report with Unity to fix the problem in the source code directly
                    MethodInfo unityDoItemGUI = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController").GetMethod("DoItemGUI", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo preDoItemGUI = GetType().GetMethod(nameof(PatchedPreDoItemGUI), BindingFlags.Static | BindingFlags.NonPublic);
                    _harmony.Patch(unityDoItemGUI, new HarmonyLib.HarmonyMethod(preDoItemGUI));
                }
            }
            else
            {
                if (!Object.ReferenceEquals(_harmony, null))
                {
                    _harmony.UnpatchAll(harmonyId);
                    _harmony = null;
                }
            }
        }

        private void SelectionChangedHandler()
        {
            Editor.Selection.SelectionChanged();
        }

        private void EditorPauseStateChanged(UnityEditor.PauseState state)
        {
            UpdateUpdateDelegate();
        }

        private void UpdateUpdateDelegate(bool disposed = false)
        {
            UnityEditor.EditorApplication.update -= Update;
            if (!disposed)
                UnityEditor.EditorApplication.update += Update;
        }

        private static bool PatchedPreDoItemGUI(UnityEditor.IMGUI.Controls.TreeViewItem item, int row, float rowWidth, bool hasFocus)
        {
            return item != null;
        }

        public static bool IsEditorNamespace(Type type)
        {
            return type.Namespace == typeof(Editor.SceneCamera).Namespace;
        }
#endif

        public static bool IsValidActiveStateChange()
        {
            bool isValid = true;

#if UNITY_EDITOR
            isValid = (UnityEditor.EditorApplication.isPlaying && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) || (!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode);
#endif

            return isValid;
        }

        protected override PropertyMonoBehaviour GetParent()
        {
            return null;
        }

        protected override PropertyMonoBehaviour GetRootParent()
        {
            return null;
        }

        protected override Type GetSiblingType()
        {
            return typeof(ManagerBase);
        }

        private List<ManagerBase> _managerList;
        protected override bool SiblingsHasChanged()
        {
            bool siblingChanged = base.SiblingsHasChanged();

            if (!siblingChanged)
            {
                if (_managerList == null)
                    _managerList = new List<ManagerBase>();
                GetComponents(_managerList);
                int unityManagerCount = _managerList.Count;
                foreach (ManagerBase manager in _managerList) 
                {
                    if (manager is SceneManager)
                        unityManagerCount--;
                }

                int managerCount = GetManagerCount();
                if (managerCount != unityManagerCount)
                    siblingChanged = true;
            }

            return siblingChanged;
        }

        protected int GetManagerCount()
        {
            int scriptCount = 0;

            if (_tweenManager != Disposable.NULL)
                scriptCount++;
            if (_poolManager != Disposable.NULL)
                scriptCount++;
            if (_inputManager != Disposable.NULL)
                scriptCount++;
            if (_cameraManager != Disposable.NULL)
                scriptCount++;
            if (_renderingManager != Disposable.NULL)
                scriptCount++;
            if (_datasourceManager != Disposable.NULL)
                scriptCount++;
            if (_jsonInterface != Disposable.NULL)
                scriptCount++;
            if (_instanceManager != Disposable.NULL)
                scriptCount++;

            return scriptCount;
        }

        public int childCount
        {
            get { return sceneChildren.Count; }
        }

        public void IterateOverChildren<T>(Func<T, bool> callback) where T : PropertyMonoBehaviour
        {
            foreach (PropertyMonoBehaviour propertyMonoBehaviour in sceneChildren)
            {
                if (propertyMonoBehaviour is T && propertyMonoBehaviour != Disposable.NULL && !callback(propertyMonoBehaviour as T))
                    return;
            }
        }

        protected override Type GetChildType()
        {
            return typeof(PropertyMonoBehaviour);
        }

        protected override bool ChildrenHasChanged()
        {
            bool childrenChanged = base.ChildrenHasChanged();

            if (!childrenChanged)
            {
                List<GameObject> rootGameObjects = GetRootGameObjects();
                if (rootGameObjects != null)
                {
                    //Subtract one for the SceneManager which is not present in the SceneChildren list
                    int unityVisibleRootGameObjectsCount = rootGameObjects.Count;
                    foreach (GameObject go in rootGameObjects)
                    {
                        if (go.name == SCENE_MANAGER_NAME || IsGameObjectHiddenOrDontSave(go))
                            unityVisibleRootGameObjectsCount--;
                    }

                    int visibleRootGameObjectsCount = childCount;
                    //Subtract the gameObject which are not present in the GetRootGameObjects()
                    IterateOverChildren<TransformBase>((propertyMonoBehaviour) =>
                    {
                        if (IsGameObjectHiddenOrDontSave(propertyMonoBehaviour.gameObject))
                            visibleRootGameObjectsCount--;
                        return true;
                    });

                    if (visibleRootGameObjectsCount != unityVisibleRootGameObjectsCount)
                        childrenChanged = true;
                }
            }

            return childrenChanged;
        }

        private bool IsGameObjectHiddenOrDontSave(GameObject go)
        {
            HideFlags hideFlags = go.hideFlags;
            return hideFlags.HasFlag(HideFlags.HideAndDontSave) || hideFlags.HasFlag(HideFlags.DontSave) || hideFlags.HasFlag(HideFlags.HideInHierarchy);
        }

        protected override void UpdateChildren()
        {
#if UNITY_EDITOR
            //SceneCameras are not included in the GetRootGameObjects()
            foreach (UnityEditor.SceneView sceneView in UnityEditor.SceneView.sceneViews)
                SetGameObjectParent(sceneView.camera.gameObject);
#endif

            List<GameObject> rootGameObjects = GetRootGameObjects();
            if (rootGameObjects != null)
            {
                foreach (GameObject rootGameObject in rootGameObjects)
                    SetGameObjectParent(rootGameObject);
            }
        }

        private void SetGameObjectParent(GameObject go)
        {
            if (go != null && go != gameObject)
            {
                TransformBase childTransform = go.GetComponent<TransformBase>();
                if (childTransform != Disposable.NULL)
                {
                    if (InitializeComponent(childTransform))
                        childTransform.UpdateParent(this);
                }
            }
        }

        private List<GameObject> _rootGameObjects;
        private List<GameObject> GetRootGameObjects()
        {
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            if (scene != null && scene.isLoaded)
            {
                if (_rootGameObjects == null)
                    _rootGameObjects = new List<GameObject>();
                //GetRootGameObjects does not include HideAndDontSave gameObjects
                scene.GetRootGameObjects(_rootGameObjects);

                return _rootGameObjects;
            }
            return null;
        }

#if UNITY_EDITOR
        [SerializeField, HideInInspector]
        private List<Editor.SceneViewDouble> _sceneViewDoubles;
        public List<Editor.SceneViewDouble> sceneViewDoubles
        {
            get { return _sceneViewDoubles; }
        }

        public void SceneViewDoubleDisposed(Editor.SceneViewDouble sceneViewDouble)
        {
            _sceneViewDoubles.Remove(sceneViewDouble);
        }
#endif

        public static bool IsSceneBeingDestroyed()
        {
            return sceneClosing || !IsValidActiveStateChange();
        }

        public static bool sceneClosing
        {
            get { return _sceneClosing; }
        }

        public static UpdateExecutionState sceneExecutionState
        {
            get { return _sceneExecutionState; }
            set { _sceneExecutionState = value; }
        }

        private List<TransformBase> sceneChildren
        {
            get 
            {
                if (_sceneChildren == null)
                    _sceneChildren = new List<TransformBase>();
                return _sceneChildren; 
            }
        }

        /// <summary>
        /// When enabled some hidden properties and objects will be exposed to help with debugging.
        /// </summary>
        [Json]
        public bool debug
        {
            get 
            {
                bool debug = _debug;
#if !UNITY_EDITOR
                debug = false;
#endif
                return debug; 
            }
            set { SetValue(nameof(debug), value, ref _debug); }
        }

        /// <summary>
        /// Should the player be running when the application is in the background?
        /// </summary>
        [Json]
        public bool runInBackground
        {
            get { return _runInBackground; }
            set 
            { 
                SetValue(nameof(runInBackground), value, ref _runInBackground, (newValue, oldValue) =>
                {
                    UpdateRunInBackground();
                }); 
            }
        }

        private void UpdateRunInBackground()
        {
            Application.runInBackground = runInBackground;
        }

        /// <summary>
        /// When enabled some <see cref="Processor"/>'s will perform their work on seperate threads.
        /// </summary>
        [Json]
        public bool enableMultithreading
        {
            get { return _enableMultithreading; }
            set { SetValue(nameof(enableMultithreading), value, ref _enableMultithreading); }
        }

#if UNITY_EDITOR
        public string buildOutputPath
        {
            get { return _buildOutputPath; }
            set { SetValue(nameof(buildOutputPath), value, ref _buildOutputPath); }
        }

        public UnityEditor.BuildAssetBundleOptions buildOptions
        {
            get { return _buildOptions; }
            set { SetValue(nameof(buildOptions), value, ref _buildOptions); }
        }

        public UnityEditor.BuildTarget buildTarget
        {
            get { return _buildTarget; }
            set { SetValue(nameof(buildTarget), value, ref _buildTarget); }
        }

        private bool AddSceneCameraTransform(TransformBase sceneCameraTransform)
        {
            InitSceneCameraTransforms();
            if (!_sceneCameraTransforms.Contains(sceneCameraTransform))
            {
                _sceneCameraTransforms.Add(sceneCameraTransform);
                return true;
            }
            return false;
        }

        private bool RemoveSceneCameraTransform(TransformBase sceneCameraTransform)
        {
            if (_sceneCameraTransforms.Remove(sceneCameraTransform))
                return true;
            return false;
        }
#endif

        protected override bool AddProperty(PropertyMonoBehaviour child)
        {
            if (child is TweenManager)
                _tweenManager = child as TweenManager;
            else if (child is PoolManager)
                _poolManager = child as PoolManager;
            else if (child is InputManager)
                _inputManager = child as InputManager;
            else if (child is DatasourceManager)
                _datasourceManager = child as DatasourceManager;
            else if (child is CameraManager)
                _cameraManager = child as CameraManager;
            else if (child is RenderingManager)
                _renderingManager = child as RenderingManager;
            else if (child is JsonInterface)
                _jsonInterface = child as JsonInterface;
            else if (child is InstanceManager)
                _instanceManager = child as InstanceManager;
            else
            {
                if (base.AddProperty(child))
                {
                    if (child is TransformBase && child.transform.parent == null)
                    {
                        TransformBase transform = child as TransformBase;
#if UNITY_EDITOR
                        if (transform.name == CameraManager.SCENECAMERA_NAME)
                            AddSceneCameraTransform(transform);
                        else
#endif
                            sceneChildren.Add(transform);
                    }
                    return true;
                }
                else
                    return false;
            }
            return true;
        }

        protected override bool RemoveProperty(PropertyMonoBehaviour child)
        {
            if (this != Disposable.NULL)
            {
                if (child is TweenManager)
                    _tweenManager = null;
                else if (child is PoolManager)
                    _poolManager = null;
                else if (child is InputManager)
                    _inputManager = null;
                else if (child is DatasourceManager)
                    _datasourceManager = null;
                else if (child is CameraManager)
                    _cameraManager = null;
                else if (child is RenderingManager)
                    _renderingManager = null;
                else if (child is JsonInterface)
                    _jsonInterface = null;
                else if (child is InstanceManager)
                    _instanceManager = null;
                else
                {
                    if (base.RemoveProperty(child))
                    {
                        if (child is TransformBase)
                        {
                            TransformBase transform = child as TransformBase;
#if UNITY_EDITOR
                            //Use the objectBase name to avoid the "You are trying to access a destroyed object..." error on Destroyed object during Undo/Redo
                            if (transform.objectBase != Disposable.NULL && transform.objectBase.name == CameraManager.SCENECAMERA_NAME)
                                RemoveSceneCameraTransform(transform);
                            else
#endif
                                sceneChildren.Remove(transform);
                        }
                        return true;
                    }
                    else
                        return false;
                }
                return true;
            }
            return false;
        }

        public static void ActivateAll()
        {
            _activateAll = true;
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            _updated = false;
        }

#if UNITY_EDITOR
        private static List<Tuple<UnityEngine.Object[], string>> _queuedRecordObjects;
        public static void RecordObjects(UnityEngine.Object[] targetObjects, string undoGroupName = null)
        {
            if (_queuedRecordObjects == null)
                _queuedRecordObjects = new List<Tuple<UnityEngine.Object[], string>>();

            _queuedRecordObjects.Add(Tuple.Create(targetObjects, undoGroupName));
        }

        private static List<Tuple<UnityEngine.Object, string>> _queuedRecordObject;
        public static void RecordObject(UnityEngine.Object targetObject, string undoGroupName = null)
        {
            if (_queuedRecordObject == null)
                _queuedRecordObject = new List<Tuple<UnityEngine.Object, string>>();

            _queuedRecordObject.Add(Tuple.Create(targetObject, undoGroupName));
        }

        private static List<Tuple<IScriptableBehaviour, PropertyInfo, object>> _queuedPropertyValueChanges;
        public static void QueuePropertyValueChange(IScriptableBehaviour scriptableBehaviour, PropertyInfo propertyInfo, object value)
        {
            if (_queuedPropertyValueChanges == null)
                _queuedPropertyValueChanges = new List<Tuple<IScriptableBehaviour, PropertyInfo, object>>();

            _queuedPropertyValueChanges.Add(Tuple.Create(scriptableBehaviour, propertyInfo, value));
        }
#endif

        public void Update()
        {
            if (this != Disposable.NULL)
            {
                if (!_updated)
                {
                    _updated = true;

                    if (!wasFirstUpdated)
                    {
                        List<GameObject> rootGameObjects = GetRootGameObjects();
                        if (rootGameObjects != null)
                        {
                            foreach (GameObject go in rootGameObjects)
                            {
                                TransformBase childTransform = go.GetComponent<TransformBase>();
                                if (childTransform != Disposable.NULL)
                                    InitializeComponent(childTransform);
                            }
                        }
                    }
#if UNITY_EDITOR
                    Editor.UndoManager.Update();
#endif
                    sceneExecutionState = UpdateExecutionState.LateInitialize;
                    LateInitialize();
                    if (PostLateInitializeEvent != null)
                        PostLateInitializeEvent();
#if UNITY_EDITOR
                    Editor.UndoManager.PostInitialize();

                    if (_queuedRecordObjects != null)
                    {
                        foreach (Tuple<UnityEngine.Object[], string> queuedRecordObjects in _queuedRecordObjects)
                        {
                            UnityEngine.Object[] targetObjects = queuedRecordObjects.Item1;
                            string undoGroupName = queuedRecordObjects.Item2;
                            Editor.UndoManager.SetCurrentGroupName(string.IsNullOrEmpty(undoGroupName) ? "Modified Property in " + (targetObjects.Length == 1 ? targetObjects[0].name : "Multiple Objects") : undoGroupName);
                            Editor.UndoManager.RecordObjects(targetObjects);
                        }

                        _queuedRecordObjects.Clear();
                    }

                    if (_queuedRecordObject != null)
                    {
                        foreach (Tuple<UnityEngine.Object, string> queuedRecordObject in _queuedRecordObject)
                        {
                            UnityEngine.Object targetObject = queuedRecordObject.Item1;
                            string undoGroupName = queuedRecordObject.Item2;
                            Editor.UndoManager.SetCurrentGroupName(string.IsNullOrEmpty(undoGroupName) ? "Modified Property in " + targetObject.name : undoGroupName);
                            Editor.UndoManager.RecordObject(targetObject);
                        }

                        _queuedRecordObject.Clear();
                    }

                    if (_queuedPropertyValueChanges != null)
                    {
                        foreach (Tuple<IScriptableBehaviour, PropertyInfo, object> queuedPropertyValue in _queuedPropertyValueChanges)
                        {
                            IScriptableBehaviour targetObject = queuedPropertyValue.Item1;
                            if (!Disposable.IsDisposed(targetObject))
                            {
                                try 
                                {
                                    targetObject.IsUserChange(() => { queuedPropertyValue.Item2.SetValue(targetObject, queuedPropertyValue.Item3); });
                                }
                                catch(Exception)
                                {

                                }
                            }
                        }

                        _queuedPropertyValueChanges.Clear();
                    }
#endif

                    sceneExecutionState = UpdateExecutionState.PreHierarchicalUpdate;
                    PreHierarchicalUpdate();
                    sceneExecutionState = UpdateExecutionState.HierarchicalUpdate;
                    HierarchicalUpdate();
                    sceneExecutionState = UpdateExecutionState.PostHierarchicalUpdate;
                    PostHierarchicalUpdate();
                    sceneExecutionState = UpdateExecutionState.HierarchicalClearDirtyFlags;
                    HierarchicalClearDirtyFlags();

                    if (_activateAll)
                    {
                        sceneExecutionState = UpdateExecutionState.HierarchicalActivate;
                        HierarchicalActivate();
                        _activateAll = false;
                    }
                }
#if UNITY_EDITOR
                // Ensure continuous Update calls.
                if (!Application.isPlaying || UnityEditor.EditorApplication.isPaused)
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
            }
        }

        public void FixedUpdate()
        {
            Camera physicsCamera = Camera.main;

            if (physicsCamera == Disposable.NULL)
            {
                instanceManager.IterateOverInstances<Camera>(
                    (camera) =>
                    {
#if UNITY_EDITOR
                        if (camera is Editor.SceneCamera)
                            camera = null;
#endif
                        if (camera != Disposable.NULL)
                        {
                            physicsCamera = camera;
                            return false;
                        }

                        return true;
                    });
            }

#if UNITY_EDITOR
            if (physicsCamera == Disposable.NULL)
            {
                instanceManager.IterateOverInstances<Camera>(
                    (camera) =>
                    {
                        if (camera is Editor.SceneCamera && camera != Disposable.NULL)
                        {
                            physicsCamera = camera;
                            return false;
                        }

                        return true;
                    });
            }
#endif

            if (physicsCamera != Disposable.NULL && physicsCamera.transform != Disposable.NULL)
                TransformDouble.ApplyOriginShifting(physicsCamera.GetOrigin());

            HierarchicalFixedUpdate();

            HierarchicalDetectChanges();
        }

#if UNITY_EDITOR
        private static List<UnityEngine.Object> _resetingObjects;
        public static void Reseting(UnityEngine.Object scriptableBehaviour)
        {
            if (_resetingObjects == null)
                _resetingObjects = new List<UnityEngine.Object>();
            _resetingObjects.Add(scriptableBehaviour);
        }

        private static List<(IJson, JSONObject)> _pastingComponentValuesToObjects;
        public static void PastingComponentValues(IJson iJson, JSONObject json)
        {
            if (_pastingComponentValuesToObjects == null)
                _pastingComponentValuesToObjects = new List<(IJson, JSONObject)>();
            _pastingComponentValuesToObjects.Add((iJson, json));
        }
#endif
        public void LateUpdate()
        {
#if UNITY_EDITOR
            if (_resetingObjects != null && _resetingObjects.Count > 0)
            {
                //We assume that all the objects are of the same type
                IScriptableBehaviour firstUnityObject = _resetingObjects[0] as IScriptableBehaviour;
                string groupName = "Reset " + (_resetingObjects.Count == 1 ? firstUnityObject.name : "Object") + " " + firstUnityObject.GetType().Name;

                Editor.UndoManager.SetCurrentGroupName(groupName);
                
                Editor.UndoManager.RecordObjects(_resetingObjects.ToArray());
                
                foreach (IScriptableBehaviour unityObject in _resetingObjects)
                    unityObject.InspectorReset();

                _resetingObjects.Clear();
            }

            if (_pastingComponentValuesToObjects != null && _pastingComponentValuesToObjects.Count > 0)
            {
                //We assume that all the objects are of the same type
                IScriptableBehaviour firstUnityObject = _pastingComponentValuesToObjects[0].Item1;
                string groupName = "Paste " + firstUnityObject.GetType().FullName + " Values";

                Editor.UndoManager.SetCurrentGroupName(groupName);
                
                UnityEngine.Object[] recordObjects = new UnityEngine.Object[_pastingComponentValuesToObjects.Count];
                for (int i = 0; i < recordObjects.Length; i++)
                    recordObjects[i] = _pastingComponentValuesToObjects[i].Item1 as UnityEngine.Object;
                Editor.UndoManager.RecordObjects(recordObjects);
                
                foreach ((IJson, JSONObject) pastingComponentValuesToObject in _pastingComponentValuesToObjects)
                {
                    IJson iJson = pastingComponentValuesToObject.Item1;
                    JSONObject json = pastingComponentValuesToObject.Item2;
                    iJson.IsUserChange(() => 
                    {
                        Debug.Log(json);
                        iJson.SetJson(json);
                    });
                }
                _pastingComponentValuesToObjects.Clear();
            }
#endif

            InvokeAction(ref UnityInitializedEvent, "UnityInitialized", UpdateExecutionState.UnityInitialized);

            InvokeAction(ref DelayedOnDestroyEvent, "DelayedOnDestroy", UpdateExecutionState.DelayedOnDestroy);

            DisposeManager.InvokeActions();

#if UNITY_EDITOR
            //Unsubscribe and Subscribe again to stay at the bottom of the invocation list. 
            //Could be replaced with [DefaultExecutionOrder(-3)] potentialy?
            UpdateUpdateDelegate();
#endif

            _updated = false;
        }

        private void InvokeAction(ref Action action, string actionName, UpdateExecutionState sceneExecutionState, bool clear = true)
        {
            if (action != null)
            {
#if UNITY_EDITOR
                int delegatesCount = action.GetInvocationList().Length;
#endif

                SceneManager.sceneExecutionState = sceneExecutionState;
                action();
                SceneManager.sceneExecutionState = UpdateExecutionState.None;

#if UNITY_EDITOR
                if (delegatesCount != action.GetInvocationList().Length)
                    Debug.LogError(actionName + " delegate list Changed!");
#endif
                if (clear)
                    action = null;
            }
        }

        private int _stackedCameraCount;
        private void BeginCameraRendering(ScriptableRenderContext context, UnityEngine.Camera unityCamera)
        {
            if (IsDisposing())
                return;

            Camera camera = instanceManager.GetCameraFromUnityCamera(unityCamera);

            if (camera != Disposable.NULL)
            {
                _stackedCameraCount = 0;
                if (camera.additionalData != null)
                {
                    foreach (UnityEngine.Camera stackedUnityCamera in camera.additionalData.cameraStack)
                    {
                        if (stackedUnityCamera.isActiveAndEnabled)
                            _stackedCameraCount++;
                    }
                }

                BeginCameraRendering(camera, context);

#if UNITY_EDITOR
                if (camera is Editor.SceneCamera)
                    (camera as Editor.SceneCamera).RenderDistancePass(context);
#endif
            }
            else
            {
                Camera parentCamera = unityCamera.GetComponentInParent<Camera>();
                if (!(parentCamera is RTTCamera) && parentCamera != Disposable.NULL)
                    renderingManager.BeginCameraDistancePassRendering(parentCamera, unityCamera);
            }
        }

        public void BeginCameraRendering(Camera camera, ScriptableRenderContext? context = null)
        {
            HierarchicalDetectChanges();

            //Preemptively apply origin shifting for proper camera relative raycasting
            TransformDouble.ApplyOriginShifting(camera.GetOrigin());

            //Apply Ignore Render layerMask so that viusal objects are property excluded from raycasting
            instanceManager.IterateOverInstances<TransformBase>(
                (transform) =>
                {
                    if (transform.objectBase is VisualObject)
                        (transform.objectBase as VisualObject).ApplyCameraMaskLayerToVisuals(camera);

                    return true;
                });

            //Update Mouse properties
            inputManager.UpdateCameraMouse(camera);

            //Update Camera Controllers
            instanceManager.IterateOverInstances<Camera>(
                (updateCamera) =>
                {
                    if (updateCamera.controller != Disposable.NULL)
                        updateCamera.controller.UpdateControllerTransform(camera);

                    return true;
                });

            //Update AstroObject Controllers
            UpdateAstroObjects(camera);

            //Update the reflection probe Transform
            renderingManager.UpdateReflectionProbeTransform(camera);

            //Apply all the latest Transform changes
            TransformDouble.ApplyOriginShifting(camera.GetOrigin());
         
            //Update Shaders and UnityTransform
            HierarchicalBeginCameraRendering(camera);

            //Update the Star before the Reflection/ReflectionProbe renders are generated
            //Note: The Star LensFlare might require a Raycast so we make sure final Orgin shifting as been performed before we get it ready for render
            Star star = instanceManager.GetStar();
            if (star != Disposable.NULL)
                star.UpdateStar(camera);

            //Generate Reflection/ReflectionProbe renders and assign the resulting textures to the Shaders/RenderSettings
            if (context.HasValue)
            {
                renderingManager.ApplyEnvironmentAndReflectionToRenderSettings(camera);
                HierarchicalUpdateEnvironmentAndReflection(camera, context);
            }
        }

        public void UpdateAstroObjects(Camera camera)
        {
            instanceManager.IterateOverInstances<AstroObject>(
                (astroObject) =>
                {
                    if (astroObject.controller != Disposable.NULL)
                        astroObject.controller.UpdateControllerTransform(camera);

                    return true;
                });
        }

        private void EndCameraRendering(ScriptableRenderContext context, UnityEngine.Camera unityCamera)
        {
            if (IsDisposing())
                return;

            Camera camera = instanceManager.GetCameraFromUnityCamera(unityCamera);

            if (camera == Disposable.NULL)
            {
                Stack stack = unityCamera.GetComponentInParent<Stack>();
                if (stack != null)
                {
                    _stackedCameraCount--;

                    Camera parentCamera = unityCamera.GetComponentInParent<Camera>();
                    if (!(parentCamera is RTTCamera) && parentCamera != Disposable.NULL)
                        renderingManager.EndCameraDistancePassRendering(parentCamera, unityCamera);

                    if (_stackedCameraCount == 0)
                        camera = parentCamera;
                }
            }
            else
            {
                if (camera.additionalData != null && camera.additionalData.cameraStack.Count != 0)
                    camera = null;
            }

            if (camera != Disposable.NULL)
                EndCameraRendering(camera);
        }

        public void EndCameraRendering(Camera camera)
        {
            HierarchicalEndCameraRendering(camera);
        }

        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (!Object.ReferenceEquals(_tweenManager, null) && TriggerCallback(_tweenManager, callback))
                containsDisposed = true;
            if (!Object.ReferenceEquals(_poolManager, null) && TriggerCallback(_poolManager, callback))
                containsDisposed = true;
            if (!Object.ReferenceEquals(_cameraManager, null) && TriggerCallback(_cameraManager, callback))
                containsDisposed = true;
            if (!Object.ReferenceEquals(_inputManager, null) && TriggerCallback(_inputManager, callback))
                containsDisposed = true;
            if (!Object.ReferenceEquals(_instanceManager, null) && TriggerCallback(_instanceManager, callback))
                containsDisposed = true;
            if (!Object.ReferenceEquals(_jsonInterface, null) && TriggerCallback(_jsonInterface, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        protected override bool ApplyAfterChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyAfterChildren(callback);

            if (!Object.ReferenceEquals(_datasourceManager, null) && TriggerCallback(_datasourceManager, callback))
                containsDisposed = true;
            if (!Object.ReferenceEquals(_renderingManager, null) && TriggerCallback(_renderingManager, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
#if UNITY_EDITOR
                Editor.AssetManager.SaveAssets();
#endif
                return true;
            }
            return false;
        }
    }
}
