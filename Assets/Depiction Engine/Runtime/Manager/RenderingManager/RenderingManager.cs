﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing graphics and rendering.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(RenderingManager))]
    [RequireComponent(typeof(SceneManager))]
    public class RenderingManager : ManagerBase
    {
        /// <summary>
        /// A list of custom fragment shader effects that the CustomEffect ShaderGraph Node supports. <br/><br/>
        /// <b><see cref="RectangleVolumeMask"/>:</b> <br/>
        /// A rectangular volumetric mask that hides any geometry that happens to be inside.
        /// </summary> 
        public enum CustomEffectType
        {
            RectangleVolumeMask
        }

        [Serializable]
        private class MeshesDictionary : SerializableDictionary<int, Mesh> { };

        private static readonly string[] FONT_NAMES = { Marker.MARKER_ICON_FONT_NAME };

        public const string SHADER_BASE_PATH = "Shader/";
        public const string MATERIAL_BASE_PATH = "Material/";

#if UNITY_EDITOR
        [BeginFoldout("Universal Render Pipeline")]
        [SerializeField, Button(nameof(PatchURPBtn)), ConditionalShow(nameof(IsNotFallbackValues)), EndFoldout]
        private bool _patchUniversalRenderPipeline;
#endif

        [BeginFoldout("Environment")]
        [SerializeField, Tooltip("The interval (in seconds) at which we call the '"+nameof(UpdateEnvironment)+"' function. Set to zero to deactivate."), EndFoldout]
        private float _environmentUpdateInterval;

        [BeginFoldout("Dynamic Skybox")]
        [SerializeField, Tooltip("When enabled the skybox will be updated automatically."), EndFoldout]
        private bool _dynamicSkybox;

        [BeginFoldout("Double Precision")]
        [SerializeField, Tooltip("When enabled the objects will be rendered relative to the camera's position (origin). Required for large Scene."), EndFoldout]
        private bool _originShifting;

#if UNITY_EDITOR
        [BeginFoldout("Mesh Cache")]
        [SerializeField, ConditionalEnable(nameof(GetEnableCacheMeshCount)), Tooltip("The number of mesh that are currently cached.")]
        private int _cachedMeshCount;
        [SerializeField, Button(nameof(ClearMeshCacheBtn)), ConditionalShow(nameof(IsNotFallbackValues)), EndFoldout]
        private bool _clearMeshCache;
#endif

        [BeginFoldout("UI")]
        [SerializeField, Tooltip("The outline color of "+nameof(Label)+"'s.")]
        private Color _labelOutlineColor;
        [SerializeField, Range(0.0f, 1.0f), Tooltip("The outline width of "+nameof(Label)+"'s.")]
        private float _labelOutlineWidth;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(GenerateAllMarkersBtn)), ConditionalShow(nameof(IsNotFallbackValues)), EndFoldout]
        private bool _generateAllMarkers;
#endif

        [BeginFoldout("Post Processing")]
        [SerializeField, Tooltip("The UniversalRendererData to be controlled by the '"+nameof(RenderingManager)+"' for features such as Ambient Occlusions.")]
        private UniversalRendererData _rendererData;
        [SerializeField, Tooltip("The Volume to be controlled by the '"+nameof(RenderingManager)+"' for effects such as Depth Of Field or others."), EndFoldout]
        private Volume _postProcessVolume;

        [BeginFoldout("Effect")]
        [SerializeField, Tooltip("A color used by objects who support highlight such as '"+nameof(FeatureGridMeshObjectBase)+"' where individual features will be highlighted on mouse over."), EndFoldout]
        private Color _highlightColor;

        [BeginFoldout("Depth Of Field")]
        [SerializeField, Tooltip("When enabled the main Camera will always try to focus on its target. This requires the main Camera to have a TargetController and the depth of field effect to be enabled.")]
        private bool _dynamicFocusDistance;
        [SerializeField, Tooltip("A min and max clamping values for the '"+nameof(dynamicFocusDistance)+"' calculations. "), EndFoldout]
        private Vector2 _minMaxFocusDistance;

        [SerializeField, HideInInspector]
        private Material _dynamicSkyboxMaterial;

        private ScriptableRendererFeature _ambientOcclusionRendererFeature;
        private Bloom _bloom;
        private ColorAdjustments _colorAdjustments;
        private ColorCurves _colorCurves;
        private ChromaticAberration _chromaticAberration;
        private DepthOfField _depthOfField;
        private Vignette _vignette;
        private Tonemapping _toneMapping;
        private MotionBlur _motionBlur;

        private SerializableIPersistentList _customEffects;
        private SerializableIPersistentList[] _layersCustomEffects;
        private ComputeBuffer[] _layersCustomEffectComputeBuffer;

        private MeshesDictionary _meshCache;

        private List<Font> _fonts;

        private RTTCamera _rttCamera;

        private Tween _environmentTimer;

        private Texture2D _emptyTexture;

        private QuadMesh _quadMesh;

#if UNITY_EDITOR
        private enum PatchResult
        {
            Success, 
            AlreadyPatched, 
            Failed
        };

        private bool GetEnableCacheMeshCount()
        {
            return false;
        }

        private bool _displayPatchedResultInDialog;
        private void PatchURPBtn()
        {
            _displayPatchedResultInDialog = true;
            StartURPPatching();
        }

        private UnityEditor.PackageManager.Requests.ListRequest _listRequest;
        private void StartURPPatching()
        {
            if (_listRequest == null && _embedRequest == null)
                _listRequest = UnityEditor.PackageManager.Client.List();
        }

        UnityEditor.PackageManager.Requests.EmbedRequest _embedRequest;
        private void EmbedURPPackage()
        {
            if (_listRequest != null && _listRequest.IsCompleted)
            {
                if (_listRequest.Error == null && _listRequest.Result != null)
                {
                    foreach (UnityEditor.PackageManager.PackageInfo packageInfo in _listRequest.Result)
                    {
                        if (packageInfo.resolvedPath.Contains("com.unity.render-pipelines.universal"))
                        {
                            if (packageInfo.resolvedPath.Contains("Packages\\"))
                                PatchEmbededURPFiles(packageInfo.resolvedPath);
                            else
                                _embedRequest = UnityEditor.PackageManager.Client.Embed(packageInfo.name);
                            break;
                        }
                    }
                }
                _listRequest = null;
            }

            if (_embedRequest != null && _embedRequest.IsCompleted)
            {
                if (_embedRequest.Error == null && _embedRequest.Result != null)
                {
                    UnityEditor.AssetDatabase.Refresh();
                    PatchEmbededURPFiles(_embedRequest.Result.resolvedPath);
                }

                _embedRequest = null;
            }
        }

        private void PatchEmbededURPFiles(string embededURPPackageResolvedPath)
        {
            PatchResult patchedResult = PatchResult.Failed;

            string path = embededURPPackageResolvedPath + "/ShaderLibrary/Lighting.hlsl";

            string lightingContent;
            using (StreamReader reader = new StreamReader(path))
            {
                lightingContent = reader.ReadToEnd();
                reader.Close();
            }

            if (!string.IsNullOrEmpty(lightingContent))
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    PatchResult lineChangeResult = PatchResult.AlreadyPatched;

                    lineChangeResult = AddBeforeLine(ref lightingContent,
                        "half4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData)",
                        "float3 _MainLightDirection;" + StringUtility.NewLine + "half _MainLightDirectionEnabled;", lineChangeResult);

                    lineChangeResult = AddAfterLine(ref lightingContent,
                        "Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);",
                        "mainLight.direction = _MainLightDirectionEnabled ? _MainLightDirection : mainLight.direction;", lineChangeResult);

                    lineChangeResult = AddBeforeLine(ref lightingContent,
                        "half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,",
                        "float _MainLightRadiance;" + StringUtility.NewLine + "half _MainLightRadianceEnabled;", lineChangeResult);

                    lineChangeResult = AddAfterLine(ref lightingContent,
                        "half3 radiance = lightColor * (lightAttenuation * NdotL);",
                        "radiance *= _MainLightRadianceEnabled ? _MainLightRadiance : 1;", lineChangeResult);

                    writer.Write(lightingContent);
                    writer.Close();

                    patchedResult = lineChangeResult;
                }
            }

            if (_displayPatchedResultInDialog)
            {
                switch (patchedResult)
                {
                    case PatchResult.Failed:
                        UnityEditor.EditorUtility.DisplayDialog("Fail", "Universal Render Pipeline patching Failed", "Ok");
                        break;
                    case PatchResult.Success:
                        UnityEditor.EditorUtility.DisplayDialog("Success", "Universal Render Pipeline patched Successfully", "Ok");
                        break;
                    case PatchResult.AlreadyPatched:
                        UnityEditor.EditorUtility.DisplayDialog("Already patched", "Universal Render Pipeline is Already patched", "Ok");
                        break;
                    default:
                        break;
                }

                _displayPatchedResultInDialog = false;
            }
        }

        private PatchResult AddAfterLine(ref string file, string line, string append, PatchResult lineChangeResult)
        {
            if (lineChangeResult != PatchResult.Failed)
            {
                int radianceLineIndex = file.IndexOf(line);
                if (radianceLineIndex != -1)
                {
                    if (file.IndexOf(append) == -1)
                    {
                        file = file.Insert(radianceLineIndex + line.Length, StringUtility.NewLine + StringUtility.NewLine + "    " + append);
                        return PatchResult.Success;
                    }

                    return lineChangeResult == PatchResult.AlreadyPatched ? PatchResult.AlreadyPatched : PatchResult.Success;
                }
            }

            return PatchResult.Failed;
        }

        private PatchResult AddBeforeLine(ref string file, string line, string append, PatchResult lineChangeResult)
        {
            if (lineChangeResult != PatchResult.Failed)
            {
                int radianceLineIndex = file.IndexOf(line);
                if (radianceLineIndex != -1)
                {
                    if (file.IndexOf(append) == -1)
                    {
                        file = file.Insert(radianceLineIndex, append + StringUtility.NewLine);
                        return PatchResult.Success;
                    }

                    return lineChangeResult == PatchResult.AlreadyPatched ? PatchResult.AlreadyPatched : PatchResult.Success;
                }
            }

            return PatchResult.Failed;
        }

        private void ClearMeshCacheBtn()
        {
            DisposeAllCachedMesh();
        }

        private void GenerateAllMarkersBtn()
        {
            foreach (int i in Enum.GetValues(typeof(Marker.Icon)))
            {
                Marker.Icon icon = (Marker.Icon)i;
                Marker marker = instanceManager.CreateInstance<Marker>(json: icon.ToString(), initializingState: InstanceManager.InitializationContext.Editor);
                marker.icon = icon;
                marker.color = UnityEngine.Random.ColorHSV(0.0f, 1.0f, 1.0f, 1.0f, 0.5f, 1.0f);
                marker.transform.position = new Vector3Double(i * 25.0f, 0.0f, 0.0f);
            }
        }
#endif

        private static RenderingManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        public static RenderingManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL && createIfMissing)
                _instance = GetManagerComponent<RenderingManager>();
            return _instance;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

#if UNITY_EDITOR
            StartURPPatching();
#endif

            if (_fonts == null)
                _fonts = new List<Font>();

            foreach (string fontName in FONT_NAMES)
            {
                if (GetFont(fontName) == null)
                    _fonts.Add(Resources.Load("Font/" + fontName) as Font);
            }
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => dynamicSkybox = value, true, initializingState);
            InitValue(value => originShifting = value, true, initializingState);
            InitValue(value => environmentUpdateInterval = value, 0.2f, initializingState);
            InitValue(value => highlightColor = value, new Color(0.0f, 1.0f, 1.0f, 0.75f), initializingState);
            InitValue(value => labelOutlineColor = value, Color.black, initializingState);
            InitValue(value => labelOutlineWidth = value, 0.25f, initializingState);
            InitValue(value => dynamicFocusDistance = value, true, initializingState);
            InitValue(value => minMaxFocusDistance = value, new Vector2(0.0f, 500.0f), initializingState);
        }

        protected override void Initialized(InstanceManager.InitializationContext initializingState)
        {
            base.Initialized(initializingState);

            UpdateEnvironment();
        }

        protected override void UpdateFields()
        {
            base.UpdateFields();

            InitRendererFeatures();

            InitPostProcessEffects();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
#if UNITY_EDITOR
                SceneManager.PlayModeStateChangedEvent -= PlayModeStateChangedHandler;
                if (!IsDisposing())
                    SceneManager.PlayModeStateChangedEvent += PlayModeStateChangedHandler;
                SceneManager.BeforeAssemblyReloadEvent -= BeforeAssemblyReloadHandler;
                if (!IsDisposing())
                    SceneManager.BeforeAssemblyReloadEvent += BeforeAssemblyReloadHandler;
#endif

                foreach (Mesh mesh in meshCache.Values)
                {
                    RemoveMeshDelegate(mesh);
                    if (!IsDisposing())
                        AddMeshDelegate(mesh);
                }

                foreach (ICustomEffect customEffect in customEffects)
                {
                    RemoveCustomEffectDelegate(customEffect);
                    if (!IsDisposing())
                        AddCustomEffectDelegate(customEffect);
                }

                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void PlayModeStateChangedHandler(UnityEditor.PlayModeStateChange state)
        {
            UpdateEnvironment();
        }

        private void BeforeAssemblyReloadHandler()
        {
            DisposeAllComputeBuffers();
        }
#endif

        private void RemoveMeshDelegate(Mesh mesh)
        {
            mesh.DisposingEvent -= MeshDisposingHandler;
        }

        private void AddMeshDelegate(Mesh mesh)
        {
            mesh.DisposingEvent += MeshDisposingHandler;
        }

        private void MeshDisposingHandler(IDisposable disposable)
        {

        }

        private void RemoveCustomEffectDelegate(ICustomEffect customEffect)
        {
            customEffect.PropertyAssignedEvent -= CustomEffectPropertyAssignedHandler;
        }

        private void AddCustomEffectDelegate(ICustomEffect customEffect)
        {
            customEffect.PropertyAssignedEvent += CustomEffectPropertyAssignedHandler;
        }

        private void CustomEffectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(ICustomEffect.maskedLayers))
            {
                RemoveCustomEffectFromLayers((int)oldValue, property as ICustomEffect);
                AddCustomEffectToLayers((int)newValue, property as ICustomEffect);
            }
        } 

        protected override void InstanceRemovedHandler(IProperty property)
        {
            base.InstanceRemovedHandler(property);

            ICustomEffect customEffect = property as ICustomEffect;
            if (!Object.ReferenceEquals(customEffect, null))
                RemoveCustomEffect(customEffect);
        }

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            ICustomEffect customEffect = property as ICustomEffect;
            if (!Disposable.IsDisposed(customEffect))
                AddCustomEffect(customEffect);
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            UpdateEnvironment();
        }

        public static bool GetEnableAlphaClip()
        {
            bool enableAlphaClipping = true;

#if UNITY_ANDROID
            //Dither forces the creation of 16 shader variants which can be too much for larger shaders such as the Terrain shader. When trying to run the app on android with Dither enabled warning messages will be thrown and the shader may not work correctly, we therefore disable it.
            enableAlphaClipping = false;
#endif

            return enableAlphaClipping;
        }

        public Texture2D emptyTexture
        {
            get
            {
                if (_emptyTexture == null)
                {
                    _emptyTexture = new Texture2D(1, 1);
                    _emptyTexture.SetPixel(0, 0, Color.clear);
                    _emptyTexture.Apply();
                }
                return _emptyTexture;
            }
        }

        public QuadMesh quadMesh
        {
            get
            {
                if (_quadMesh == Disposable.NULL)
                    _quadMesh = Mesh.CreateMesh<QuadMesh>();
                return _quadMesh;
            }
        }

        public Material dynamicSkyboxMaterial
        {
            get
            {
                if (_dynamicSkyboxMaterial == null)
                {
                    _dynamicSkyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));
                    _dynamicSkyboxMaterial.name = "DynamicSkybox";
                    _dynamicSkyboxMaterial.SetFloat("_Exposure", 1.0f);
                }
                return _dynamicSkyboxMaterial;
            }
        }

        private SerializableIPersistentList customEffects 
        {
            get 
            {
                if (_customEffects == null)
                    _customEffects = new SerializableIPersistentList();
                return _customEffects; 
            }
        }

        private SerializableIPersistentList[] layersCustomEffects
        {
            get
            {
                if (_layersCustomEffects == null)
                    _layersCustomEffects = new SerializableIPersistentList[32];
                return _layersCustomEffects;
            }
        }

        private ComputeBuffer[] layersCustomEffectComputeBuffer
        {
            get
            {
                if (_layersCustomEffectComputeBuffer == null)
                    _layersCustomEffectComputeBuffer = new ComputeBuffer[32];
                return _layersCustomEffectComputeBuffer;
            }
        }

        private void RemoveCustomEffect(ICustomEffect customEffect)
        {
            if (customEffects.Remove(customEffect))
            {
                RemoveCustomEffectDelegate(customEffect);

                RemoveCustomEffectFromLayers(customEffect.maskedLayers, customEffect);
            }
        }

        private void RemoveCustomEffectFromLayers(LayerMask layers, ICustomEffect customEffect)
        {
            for (int layer = 0; layer <= 31; layer++)
            {
                SerializableIPersistentList layerCustomEffects = layersCustomEffects[layer];
                if (layerCustomEffects != null && layerCustomEffects.Remove(customEffect) && layerCustomEffects.Count == 0)
                    layersCustomEffects[layer] = null;
            }
        }

        private void AddCustomEffect(ICustomEffect customEffect)
        {
            if (!customEffects.Contains(customEffect))
            {
                customEffects.Add(customEffect);

                AddCustomEffectDelegate(customEffect);

                AddCustomEffectToLayers(customEffect.maskedLayers, customEffect);
            }
        }

        private void AddCustomEffectToLayers(LayerMask layers, ICustomEffect customEffect)
        {
            for (int layer = 0; layer <= 31; layer++)
            {
                if (!layers.Includes(layer))
                {
                    SerializableIPersistentList layerCustomEffects = layersCustomEffects[layer];
                    if (layerCustomEffects == null)
                    {
                        layerCustomEffects = new SerializableIPersistentList();
                        layersCustomEffects[layer] = layerCustomEffects;
                    }

                    if (!layerCustomEffects.Contains(customEffect))
                        layerCustomEffects.Add(customEffect);
                }
            }
        }

        /// <summary>
        /// The interval (in seconds) at which we call the <see cref="UpdateEnvironment"/> function. Set to zero to deactivate.
        /// </summary>
        [Json]
        public float environmentUpdateInterval
        {
            get { return _environmentUpdateInterval; }
            set
            {
                if (value < 0.01f)
                    value = 0.01f;
                SetValue(nameof(environmentUpdateInterval), value, ref _environmentUpdateInterval, (newValue, oldValue) =>
                {
                    UpdateEnvironment();
                });
            }
        }

        /// <summary>
        /// When enabled the skybox will be updated automatically. 
        /// </summary>
        [Json]
        public bool dynamicSkybox
        {
            get { return _dynamicSkybox; }
            set { SetValue(nameof(dynamicSkybox), value, ref _dynamicSkybox); }
        }

        /// <summary>
        /// When enabled the objects will be rendered relative to the camera's position (origin). Required for large Scene.
        /// </summary>
        [Json]
        public bool originShifting
        {
            get { return _originShifting; }
            set
            {
                SetValue(nameof(originShifting), value, ref _originShifting, (newValue, oldValue) =>
                {
                    if (!newValue)
                        TransformDouble.ApplyOriginShifting(Vector3Double.zero);
                });
            }
        }

        /// <summary>
        /// The outline color of <see cref="Label"/>'s.
        /// </summary>
        [Json]
        public Color labelOutlineColor
        {
            get { return _labelOutlineColor; }
            set { SetValue(nameof(labelOutlineColor), value, ref _labelOutlineColor); }
        }

        /// <summary>
        /// The outline width of <see cref="Label"/>'s.
        /// </summary>
        [Json]
        public float labelOutlineWidth
        {
            get { return _labelOutlineWidth; }
            set { SetValue(nameof(labelOutlineWidth), Mathf.Clamp01(value), ref _labelOutlineWidth); }
        }

        /// <summary>
        /// The UniversalRendererData to be controlled by the <see cref="RenderingManager"/> for features such as Ambient Occlusions.
        /// </summary>
        public UniversalRendererData rendererData
        {
            get { return _rendererData; }
            set
            {
                SetValue(nameof(rendererData), value, ref _rendererData, (newvalue, oldValue) =>
                {
                    InitRendererFeatures();
                });
            }
        }

        /// <summary>
        /// The Volume to be controlled by the <see cref="RenderingManager"/> for effects such as Depth Of Field or others.
        /// </summary>
        public Volume postProcessVolume
        {
            get { return _postProcessVolume; }
            set
            {
                SetValue(nameof(postProcessVolume), value, ref _postProcessVolume, (newvalue, oldValue) =>
                {
                    InitPostProcessEffects();
                });
            }
        }

        /// <summary>
        /// A color used by objects who support highlight such as <see cref="FeatureGridMeshObjectBase"/> where individual features will be highlighted on mouse over.
        /// </summary>
        [Json]
        public Color highlightColor
        {
            get { return _highlightColor; }
            set { SetValue(nameof(highlightColor), value, ref _highlightColor); }
        }

        /// <summary>
        /// When enabled the main Camera will always try to focus on its target. This requires the main Camera to have a TargetController and the depth of field effect to be enabled.
        /// </summary>
        [Json]
        public bool dynamicFocusDistance
        {
            get { return _dynamicFocusDistance; }
            set { SetValue(nameof(dynamicFocusDistance), value, ref _dynamicFocusDistance); }
        }

        /// <summary>
        /// A min and max clamping values for the <see cref="dynamicFocusDistance"/> calculations. 
        /// </summary>
        [Json]
        public Vector2 minMaxFocusDistance
        {
            get { return _minMaxFocusDistance; }
            set { SetValue(nameof(minMaxFocusDistance), value, ref _minMaxFocusDistance); }
        }

        public RTTCamera rttCamera
        {
            get 
            {
                if (_rttCamera == Disposable.NULL)
                {
                    string rttCameraName = nameof(RTTCamera);
                    GameObject reflectionCameraGO = GameObject.Find(rttCameraName);
                    if (reflectionCameraGO != null)
                        _rttCamera = reflectionCameraGO.GetSafeComponent<RTTCamera>();
                    if (_rttCamera == Disposable.NULL)
                        _rttCamera = instanceManager.CreateInstance<RTTCamera>(null, rttCameraName);
                }
                return _rttCamera; 
            }
            set { _rttCamera = value; }
        }

        public List<Font> fonts
        {
            get { return _fonts; }
        }

        private MeshesDictionary meshCache
        {
            get
            {
                if (_meshCache == null)
                    _meshCache = new MeshesDictionary();
                return _meshCache;
            }
        }

        private void InitRendererFeatures()
        {
            if (rendererData != null)
            {
                if (ambientOcclusionRendererFeature == null)
                {
                    foreach (ScriptableRendererFeature scriptableRendererFeature in rendererData.rendererFeatures)
                    {
                        if (scriptableRendererFeature.name == "ScreenSpaceAmbientOcclusion")
                            ambientOcclusionRendererFeature = scriptableRendererFeature;
                    }
                }
            }
        }

        private ScriptableRendererFeature ambientOcclusionRendererFeature
        {
            get { return _ambientOcclusionRendererFeature; }
            set
            {
                if (_ambientOcclusionRendererFeature == value)
                    return;
                _ambientOcclusionRendererFeature = value;
            }
        }

        private void InitPostProcessEffects()
        {
            if (postProcessVolume != null)
            {
                if (depthOfField == null)
                {
                    DepthOfField volumeDepthOfField;
                    if (postProcessVolume.profile.TryGet(out volumeDepthOfField))
                        depthOfField = volumeDepthOfField;
                }

                if (bloom == null)
                {
                    Bloom volumeBloom;
                    if (postProcessVolume.profile.TryGet(out volumeBloom))
                        bloom = volumeBloom;
                }

                if (colorAdjustments == null)
                {
                    ColorAdjustments volumeColorAdjustments;
                    if (postProcessVolume.profile.TryGet(out volumeColorAdjustments))
                        colorAdjustments = volumeColorAdjustments;
                }

                if (colorCurves == null)
                {
                    ColorCurves volumeColorCurves;
                    if (postProcessVolume.profile.TryGet(out volumeColorCurves))
                        colorCurves = volumeColorCurves;
                }

                if (chromaticAberration == null)
                {
                    ChromaticAberration volumeChromaticAberration;
                    if (postProcessVolume.profile.TryGet(out volumeChromaticAberration))
                        chromaticAberration = volumeChromaticAberration;
                }

                if (vignette == null)
                {
                    Vignette volumeVignette;
                    if (postProcessVolume.profile.TryGet(out volumeVignette))
                        vignette = volumeVignette;
                }

                if (toneMapping == null)
                {
                    Tonemapping volumeToneMapping;
                    if (postProcessVolume.profile.TryGet(out volumeToneMapping))
                        toneMapping = volumeToneMapping;
                }

                if (motionBlur == null)
                {
                    MotionBlur volumeMotionBlur;
                    if (postProcessVolume.profile.TryGet(out volumeMotionBlur))
                        motionBlur = volumeMotionBlur;
                }
            }
        }

        private DepthOfField depthOfField
        {
            get { return _depthOfField; }
            set
            {
                if (_depthOfField == value)
                    return;
                _depthOfField = value;
            }
        }

        private Bloom bloom
        {
            get { return _bloom; }
            set
            {
                if (_bloom == value)
                    return;
                _bloom = value;
            }
        }

        private ColorAdjustments colorAdjustments
        {
            get { return _colorAdjustments; }
            set
            {
                if (_colorAdjustments == value)
                    return;
                _colorAdjustments = value;
            }
        }

        private ColorCurves colorCurves
        {
            get { return _colorCurves; }
            set
            {
                if (_colorCurves == value)
                    return;
                _colorCurves = value;
            }
        }

        private ChromaticAberration chromaticAberration
        {
            get { return _chromaticAberration; }
            set
            {
                if (_chromaticAberration == value)
                    return;
                _chromaticAberration = value;
            }
        }

        private Vignette vignette
        {
            get { return _vignette; }
            set
            {
                if (_vignette == value)
                    return;
                _vignette = value;
            }
        }

        private Tonemapping toneMapping
        {
            get { return _toneMapping; }
            set
            {
                if (_toneMapping == value)
                    return;
                _toneMapping = value;
            }
        }

        private MotionBlur motionBlur
        {
            get { return _motionBlur; }
            set
            {
                if (_motionBlur == value)
                    return;
                _motionBlur = value;
            }
        }

        /// <summary>
        /// Real-time Shadows type to be used.
        /// </summary>
        [Json]
        public UnityEngine.ShadowQuality shadows
        {
            get { return QualitySettings.shadows; }
            set { QualitySettings.shadows = value; }
        }

        /// <summary>
        /// Set the AA Filtering option.
        /// </summary>
        [Json]
        public int antiAliasing
        {
            get { return QualitySettings.antiAliasing; }
            set { QualitySettings.antiAliasing = value; }
        }

        /// <summary>
        /// Is fog enabled?
        /// </summary>
        [Json]
        public bool fogActive
        {
            get { return RenderSettings.fog; }
            set { RenderSettings.fog = value; }
        }

        /// <summary>
        /// The color of the fog.
        /// </summary>
        [Json]
        public Color fogColor
        {
            get { return RenderSettings.fogColor; }
            set { RenderSettings.fogColor = value; }
        }

        /// <summary>
        /// Should ambient occlusion be rendered?
        /// </summary>
        [Json]
        public bool ambientOcclusionActive
        {
            get { return ambientOcclusionRendererFeature != null ? ambientOcclusionRendererFeature.isActive : false; }
            set
            {
                if (ambientOcclusionRendererFeature != null)
                    ambientOcclusionRendererFeature.SetActive(value);
            }
        }

        /// <summary>
        /// Should the depth if field effect be rendered.
        /// </summary>
        [Json]
        public bool depthOfFieldActive
        {
            get { return depthOfField != null ? depthOfField.active : false; }
            set
            {
                if (depthOfField != null)
                    depthOfField.active = value;
            }
        }

        /// <summary>
        /// The distance at which the camera will be in focus.
        /// </summary>
        [Json]
        public float depthOfFieldFocusDistance
        {
            get { return depthOfField != null ? depthOfField.focusDistance.value : 0.0f; }
            set
            {
                if (depthOfField != null)
                    depthOfField.focusDistance.value = value;
            }
        }

        /// <summary>
        /// Should the vignette effect be rendered?
        /// </summary>
        [Json]
        public bool vignetteActive
        {
            get { return vignette != null ? vignette.active : false; }
            set
            {
                if (vignette != null)
                    vignette.active = value;
            }
        }

        /// <summary>
        /// Should the tone mapping effect be rendered?
        /// </summary>
        [Json]
        public bool toneMappingActive
        {
            get { return toneMapping != null ? toneMapping.active : false; }
            set
            {
                if (toneMapping != null)
                    toneMapping.active = value;
            }
        }

        /// <summary>
        /// Should the motion blur effect be rendered?
        /// </summary>
        [Json]
        public bool motionBlurActive
        {
            get { return motionBlur != null ? motionBlur.active : false; }
            set
            {
                if (motionBlur != null)
                    motionBlur.active = value;
            }
        }

        /// <summary>
        /// Should the bloom effect be rendered?
        /// </summary>
        [Json]
        public bool bloomActive
        {
            get { return bloom != null ? bloom.active : false; }
            set
            {
                if (bloom != null)
                    bloom.active = bloomActive;
            }
        }

        /// <summary>
        /// Should the color adjustments effect be rendered?
        /// </summary>
        [Json]
        public bool colorAdjustmentsActive
        {
            get { return colorAdjustments != null ? colorAdjustments.active : false; }
            set
            {
                if (colorAdjustments != null)
                    colorAdjustments.active = value;
            }
        }

        /// <summary>
        /// Should the color curves effect be rendered?
        /// </summary>
        [Json]
        public bool colorCurvesActive
        {
            get { return colorCurves != null ? colorCurves.active : false; }
            set
            {
                if (colorCurves != null)
                    colorCurves.active = value;
            }
        }

        /// <summary>
        /// Should the chromatic aberration effect be rendered?
        /// </summary>
        [Json]
        public bool chromaticAberrationActive
        {
            get { return chromaticAberration != null ? chromaticAberration.active : false; }
            set
            {
                if (chromaticAberration != null)
                    chromaticAberration.active = value;
            }
        }

        private Font GetFont(string fontName)
        {
            foreach (Font font in fonts)
            {
                if (font.name == fontName)
                    return font;
            }

            return null;
        }

        public bool GetCharacterInfoFromFontName(char ch, out CharacterInfo characterInfo, string fontName)
        {
            characterInfo = new CharacterInfo();

            Font font = GetFont(fontName);
            return font != null && font.GetCharacterInfo(ch, out characterInfo);
        }

        private Tween environmentTimer
        {
            get { return _environmentTimer; }
            set
            {
                if (Object.ReferenceEquals(_environmentTimer, value))
                    return;
                
                Dispose(_environmentTimer);

                _environmentTimer = value;
            }
        }

        public Mesh GetMeshFromCache(int hash)
        {
            meshCache.TryGetValue(hash, out Mesh mesh);
            return mesh;
        }

        public bool AddMeshToCache(int hash, Mesh mesh)
        {
            if (!meshCache.ContainsKey(hash))
            {
                AddMeshDelegate(mesh);
                meshCache.Add(hash, mesh);
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public static void UpdateIcon(UnityEngine.Object unityObject)
        {
            Texture2D icon = GetDefaultIcon(unityObject.GetType());
            if (!Disposable.IsDisposed(unityObject) && UnityEditor.EditorGUIUtility.GetIconForObject(unityObject) != icon)
                UnityEditor.EditorGUIUtility.SetIconForObject(unityObject, icon);

            if (unityObject is Object || unityObject is Visual || unityObject is ManagerBase)
            {
                unityObject = (unityObject as MonoBehaviourBase).gameObject;
                RenderingManager renderingManager = RenderingManager.Instance(false);
                if (renderingManager != Disposable.NULL)
                {
                    icon = renderingManager.emptyTexture;
                    if (UnityEditor.EditorGUIUtility.GetIconForObject(unityObject) != icon)
                        UnityEditor.EditorGUIUtility.SetIconForObject(unityObject, icon);
                }
            }
        }

        public static Texture2D GetDefaultIcon(Type type)
        {
            Texture2D icon = null;

            Type transformType = typeof(TransformBase);
            Type scriptType = typeof(Script);
            Type visualType = typeof(Visual);
            Type meshType = typeof(Mesh);
            Type managerType = typeof(ManagerBase);
            if (type == transformType || type.IsSubclassOf(transformType))
                icon = UnityEditor.EditorGUIUtility.IconContent("d_Transform Icon").image as Texture2D;
            else if (typeof(IPersistent).IsAssignableFrom(type))
                icon = Resources.Load("Editor/UI/PersistentIcon") as Texture2D;
            else if (type == scriptType || type.IsSubclassOf(scriptType))
                icon = Resources.Load("Editor/UI/ScriptIcon") as Texture2D;
            else if (type == visualType || type.IsSubclassOf(visualType) || type == meshType || type.IsSubclassOf(meshType))
                icon = UnityEditor.EditorGUIUtility.IconContent("MeshRenderer Icon").image as Texture2D;
            else if (type == managerType || type.IsSubclassOf(managerType))
                icon = UnityEditor.EditorGUIUtility.IconContent("GameManager Icon").image as Texture2D;

            return icon;
        }

        private Texture2D[] _headerTextures;
        public Texture2D[] headerTextures
        {
            get
            {
                if (_headerTextures == null || _headerTextures.Length == 0 || _headerTextures[0] == null)
                {
                    _headerTextures = new Texture2D[11];

                    //Transform
                    Color color;
                    if (ColorUtility.ColorFromString(out color, "#dd1265"))
                        GenerateGradientHeaderTextures(_headerTextures, 0, color);
                    //Persistent
                    if (ColorUtility.ColorFromString(out color, "#fdb60d"))
                        GenerateGradientHeaderTextures(_headerTextures, 2, color);
                    //Script
                    if (ColorUtility.ColorFromString(out color, "#6b57ff"))
                        GenerateGradientHeaderTextures(_headerTextures, 4, color);
                    //Visual
                    if (ColorUtility.ColorFromString(out color, "#087cfa"))
                        GenerateGradientHeaderTextures(_headerTextures, 6, color);
                    //Manager
                    if (ColorUtility.ColorFromString(out color, "#131313"))
                        GenerateGradientHeaderTextures(_headerTextures, 8, color);
                    //Property Group Header
                    GenerateHeaderTexture(_headerTextures, 10, Color.black);

                }
                return _headerTextures;
            }
        }

        private void GenerateGradientHeaderTextures(Texture2D[] headerTextures, int index, Color color)
        {
            Texture2D headerTexture = new Texture2D(100, 1);
            headerTexture.wrapMode = TextureWrapMode.Clamp;
            color.a = 0.3f;
            for (int i = 0; i < headerTexture.width; i++)
                headerTexture.SetPixel(i, 0, Color.Lerp(Color.clear, color, Easing.CircEaseOut((float)i / headerTexture.width, 0, 1, 1)));
            headerTexture.Apply();
            headerTextures[index] = headerTexture;

            Texture2D headerLineTexture = new Texture2D(1, 1);
            color.a = 0.5f;
            headerLineTexture.SetPixel(0, 0, color);
            headerLineTexture.Apply();
            headerTextures[index + 1] = headerLineTexture;
        }

        private void GenerateHeaderTexture(Texture2D[] headerTextures, int index, Color color)
        {
            Texture2D headerTexture = new Texture2D(1, 1);
            headerTexture.wrapMode = TextureWrapMode.Clamp;
            color.a = 0.3f;
            headerTexture.SetPixel(0, 0, color);
            headerTexture.Apply();
            headerTextures[index] = headerTexture;
        }
#endif

        private bool _environmentDirty;
        public void UpdateEnvironment()
        {
            if (initialized)
            {
                _environmentDirty = true;

                if (environmentUpdateInterval != 0.0f)
                {
                    TweenManager tweenManager = TweenManager.Instance();
                    if (tweenManager != Disposable.NULL)
                        environmentTimer = tweenManager.DelayedCall(environmentUpdateInterval, null, UpdateEnvironment);
                }
            }
        }

        public void ApplyEnvironmentAndReflectionToRenderSettings(Camera camera)
        { 
            if (dynamicSkybox)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = camera.GetEnvironmentCubeMap();

                RenderSettings.skybox = dynamicSkyboxMaterial;
                RenderSettings.skybox.SetTexture("_Tex", RenderSettings.customReflectionTexture);
           
                DynamicGI.UpdateEnvironment();
            }
        }

        public void UpdateReflectionProbeTransform(Camera camera)
        {
            instanceManager.IterateOverInstances<AstroObject>(
                (astroObject) =>
                {
                    GeoAstroObject geoAstroObject = astroObject as GeoAstroObject;
                    if (geoAstroObject != Disposable.NULL)
                        geoAstroObject.UpdateReflectionProbeTransform(camera);

                    return true;
                });
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                if (postProcessVolume != null)
                {
                    if (depthOfField != null)
                    {
                        Camera mainCamera = Camera.main;
                        if (dynamicFocusDistance && mainCamera != Disposable.NULL && mainCamera.controller is TargetControllerBase)
                            depthOfField.focusDistance.value = Mathf.Clamp((float)(mainCamera.controller as TargetControllerBase).distance, minMaxFocusDistance.x, minMaxFocusDistance.y);
                    }
                }

                return true;
            }
            return false;
        }

        private float _fogDensity;
        private float _fogStartDistance;
        private float _fogEndDistance;
        private float[][] _layersCustomEffectComputeBufferData;
        public void BeginCameraDistancePassRendering(Camera camera, UnityEngine.Camera unityCamera)
        {
            //Fog
            _fogDensity = RenderSettings.fogDensity;
            _fogStartDistance = RenderSettings.fogStartDistance;
            _fogEndDistance = RenderSettings.fogEndDistance;

            if (RenderSettings.fog)
            {
                float nearClipPlaneOffset = unityCamera.nearClipPlane - camera.nearClipPlane;
                switch (RenderSettings.fogMode)
                {
                    case FogMode.Linear:
                        RenderSettings.fogStartDistance -= nearClipPlaneOffset;
                        RenderSettings.fogEndDistance -= nearClipPlaneOffset;
                        break;
                }
            }

            //Custom Effect
            if (_layersCustomEffectComputeBufferData == null)
                _layersCustomEffectComputeBufferData = new float[32][];

            for (int layer = 0; layer <= 31; layer++)
            {
                float[] layerCustomEffectComputeBufferData = _layersCustomEffectComputeBufferData[layer];

                SerializableIPersistentList layerCustomEffects = layersCustomEffects[layer];
                if (layerCustomEffects != null && layerCustomEffects.Count > 0)
                {
                    int size = 0;

                    foreach (ICustomEffect layerCustomEffect in layerCustomEffects)
                        size += layerCustomEffect.GetCusomtEffectComputeBufferDataSize();

                    if (layerCustomEffectComputeBufferData == null || layerCustomEffectComputeBufferData.Length != size)
                        layerCustomEffectComputeBufferData = new float[size];

                    int startIndex = 0;
                    for (int i = 0; i < layerCustomEffects.Count; i++)
                    {
                        ICustomEffect layerCustomEffect = layerCustomEffects[i] as ICustomEffect;
                        //Null check is required for when a CustomEffect MonoBehaviour Component is removed directly in the Editor instead of deleting the whole GameObject
                        if (layerCustomEffect != null)
                            startIndex += layerCustomEffect.AddToComputeBufferData(startIndex, layerCustomEffectComputeBufferData);
                    }
                }
                else
                {
                    if (layerCustomEffects != null)
                        layersCustomEffects[layer] = null;
                    layerCustomEffectComputeBufferData = null;
                }

                _layersCustomEffectComputeBufferData[layer] = layerCustomEffectComputeBufferData;

                ComputeBuffer layerCustomEffectComputeBuffer = layersCustomEffectComputeBuffer[layer];
                if (layerCustomEffectComputeBufferData != null)
                {
                    if (layerCustomEffectComputeBuffer == null || layerCustomEffectComputeBuffer.count != layerCustomEffectComputeBufferData.Length)
                    {
                        if (layerCustomEffectComputeBuffer != null)
                            DisposeComputeBuffer(layerCustomEffectComputeBuffer);
                        layerCustomEffectComputeBuffer = new ComputeBuffer(layerCustomEffectComputeBufferData.Length, sizeof(float));
                        layersCustomEffectComputeBuffer[layer] = layerCustomEffectComputeBuffer;
                    }

                    layerCustomEffectComputeBuffer.SetData(layerCustomEffectComputeBufferData);
                }
                else
                {
                    if (layerCustomEffectComputeBuffer != null)
                    {
                        DisposeComputeBuffer(layerCustomEffectComputeBuffer);
                        layersCustomEffectComputeBuffer[layer] = null;
                    }
                }
            }

            instanceManager.IterateOverInstances<VisualObject>(
                (visualObject) =>
                {
                    visualObject.IterateOverMaterials((material, materialPropertyBlock, meshRenderer) =>
                    {
                        material.SetBuffer("_CustomEffectsBuffer", layersCustomEffectComputeBuffer[visualObject.layer]);
                    });

                    return true;
                });
        }

        public void EndCameraDistancePassRendering(Camera camera, UnityEngine.Camera unityCamera)
        {
            RenderSettings.fogDensity = _fogDensity;
            RenderSettings.fogStartDistance = _fogStartDistance;
            RenderSettings.fogEndDistance = _fogEndDistance;
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
#if UNITY_EDITOR
                EmbedURPPackage();

                _cachedMeshCount = meshCache.Count;
#endif

                instanceManager.IterateOverInstances<Camera>(
                    (camera) => 
                    {
                        if (_environmentDirty || camera.environmentCubemap == null)
                            UpdateCameraEnvironmentCubemap(camera);

                        return true;
                    });

                _environmentDirty = false;

                return true;
            }
            return false;
        }
      
        private void UpdateCameraEnvironmentCubemap(Camera camera)
        {
            float lastAmbientIntensity = RenderSettings.ambientIntensity;
            RenderSettings.ambientIntensity = 0.0f;
            float lastReflectionIntensity = RenderSettings.reflectionIntensity;
            RenderSettings.reflectionIntensity = 0.0f;
            
            try
            {
                sceneManager.BeginCameraRendering(camera);

                rttCamera.RenderToCubemap(camera, camera.GetEnvironmentCubeMap(), ApplyPropertiesToUnityCamera);

                sceneManager.EndCameraRendering(camera);

            }catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            RenderSettings.ambientIntensity = lastAmbientIntensity;
            RenderSettings.reflectionIntensity = lastReflectionIntensity;
        }

        private void ApplyPropertiesToUnityCamera(UnityEngine.Camera unityCamera, Camera copyFromCamera)
        {
            unityCamera.cullingMask = 1 << LayerUtility.GetLayer(typeof(TerrainGridMeshObject).Name) | 1 << LayerUtility.GetLayer(typeof(AtmosphereGridMeshObject).Name);

            float far = Camera.GetFarClipPlane(1, copyFromCamera.farClipPlane);

            if (far > 155662040916.9f)
                far = 155662040916.9f;

            Camera.ApplyClipPlanePropertiesToUnityCamera(unityCamera, 0, copyFromCamera.nearClipPlane, far);
        }

        public static Material LoadMaterial(string path)
        {
            return Resources.Load<Material>(path);
        }

        public static Shader LoadShader(string path)
        {
            return Resources.Load<Shader>(path);
        }

#if UNITY_EDITOR
        private void DisposeAllHeaderTextures()
        {
            if (_headerTextures != null)
            {
                foreach (Texture2D headerTexture in _headerTextures)
                    Dispose(headerTexture);
            }
        }
#endif

        public void DisposeAllCachedMesh()
        {
            if (_meshCache != null)
            {
                foreach (Mesh mesh in _meshCache.Values)
                    Dispose(mesh);
                _meshCache.Clear();
            }
        }

        private void DisposeAllComputeBuffers()
        {
            if (_layersCustomEffectComputeBuffer != null)
            {
                foreach (ComputeBuffer computeBuffer in _layersCustomEffectComputeBuffer)
                    DisposeComputeBuffer(computeBuffer);

                _layersCustomEffectComputeBuffer = null;
            }
        }

        private void DisposeComputeBuffer(ComputeBuffer computeBuffer)
        {
            if (computeBuffer != null)
                computeBuffer.Dispose();
        }

        public override bool OnDisposing()
        {
            if (base.OnDisposing())
            {
                environmentTimer = null;

                return true;
            }
            return false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Dispose(_rttCamera);

            Dispose(_emptyTexture);

            Dispose(_quadMesh);

            Dispose(_dynamicSkyboxMaterial);

            DisposeAllCachedMesh();

#if UNITY_EDITOR
            DisposeAllHeaderTextures();
#endif
            DisposeAllComputeBuffers();
        }
    }
}