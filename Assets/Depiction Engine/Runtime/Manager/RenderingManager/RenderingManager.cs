// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering.Universal;
using System.Runtime.CompilerServices;
using System.Linq;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing graphics and rendering.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(RenderingManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
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
        private class MeshesDictionary : SerializableDictionary<int, List<Mesh>> { };

        private static readonly string[] FONT_NAMES = { Marker.MARKER_ICON_FONT_NAME };

        public const int DEFAULT_MISSING_CACHE_HASH = -1;

        public const string SHADER_BASE_PATH = "Shader/";
        public const string MATERIAL_BASE_PATH = "Material/";

#if UNITY_EDITOR
        [BeginFoldout("Universal Render Pipeline")]
        [SerializeField, Button(nameof(PatchURPBtn)), ConditionalShow(nameof(IsNotFallbackValues)), EndFoldout]
        private bool _patchUniversalRenderPipeline;
#endif

        [BeginFoldout("Environment")]
        [SerializeField, Tooltip("When enabled, a realtime environment cubemap will be generated for each camera and be made available for Global Illumination (GI) and reflection. Disable 'Recalculate Environment Lighting' in 'Project Settings > Editor' or 'Lighting Window > Workflow' to avoid GI flashing.")]
        private bool _dynamicEnvironment;
        [SerializeField, Tooltip("The interval (in seconds) at which we call the '"+nameof(UpdateEnvironmentCubemap)+"' function to update the environment cubemap."), EndFoldout]
        private float _dynamicEnvironmentUpdateInterval;

        [BeginFoldout("Double Precision")]
        [SerializeField, Tooltip("When enabled the objects will be rendered relative to the camera's position (origin). Required for large Scene."), EndFoldout]
        private bool _originShifting;

#if UNITY_EDITOR
        [BeginFoldout("Mesh Cache")]
        [SerializeField, ConditionalEnable(nameof(GetEnableCacheMeshCount)), Tooltip("The number of mesh that are currently cached.")]
        private int _cachedMeshCount;
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
        private MeshesDictionary _meshesCache;

        private List<Object> _managedReflectionObjects;

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

        private List<Font> _fonts;

        private RTTCamera _rttCamera;

        private Tween _environmentTimer;

        private Texture2D _emptyTexture;

        private QuadMesh _quadMesh;

        public static bool COMPUTE_BUFFER_SUPPORTED;

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
                                PatchEmbeddedURPFiles(packageInfo.resolvedPath);
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
                    PatchEmbeddedURPFiles(_embedRequest.Result.resolvedPath);
                }

                _embedRequest = null;
            }
        }

        private void PatchEmbeddedURPFiles(string embeddedURPPackageResolvedPath)
        {
            PatchResult patchedResult = PatchResult.Failed;

            string path = embeddedURPPackageResolvedPath + "/ShaderLibrary/Lighting.hlsl";

            string lightingContent;
            using (StreamReader reader = new(path))
            {
                lightingContent = reader.ReadToEnd();
                reader.Close();
            }

            if (!string.IsNullOrEmpty(lightingContent))
            {
                using StreamWriter writer = new(path);
                PatchResult lineChangeResult = PatchResult.AlreadyPatched;

                lineChangeResult = AddBeforeLine(ref lightingContent,
                    "half4 UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData)",
                    "float3 _MainLightDirection;" + StringUtility.NewLine + "half _MainLightDirectionEnabled;", lineChangeResult);

                lineChangeResult = AddAfterLine(ref lightingContent,
                    "Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);",
                    "mainLight.direction = _MainLightDirectionEnabled ? _MainLightDirection : mainLight.direction;", lineChangeResult);

                lineChangeResult = AddBeforeLine(ref lightingContent,
                    "half3 LightingPhysicallyBased(BRDFData brdfData, BRDFData brdfDataClearCoat,",
                    "float _MainLightRadiance;" + StringUtility.NewLine + "half _MainLightRadianceActive;" + StringUtility.NewLine + "half _MainLightRadianceEnabled;", lineChangeResult);

                lineChangeResult = AddAfterLine(ref lightingContent,
                    "half3 radiance = lightColor * (lightAttenuation * NdotL);",
                    "radiance *= _MainLightRadianceActive && _MainLightRadianceEnabled ? _MainLightRadiance : 1;", lineChangeResult);

                lineChangeResult = AddBeforeLine(ref lightingContent,
                    "        lightingData.mainLightColor = LightingPhysicallyBased(brdfData, brdfDataClearCoat,",
                    "        _MainLightRadianceActive = true;", lineChangeResult);

                lineChangeResult = AddAfterLine(ref lightingContent,
                    "surfaceData.clearCoatMask, specularHighlightsOff);",
                    "    _MainLightRadianceActive = false;", lineChangeResult);

                writer.Write(lightingContent);
                writer.Close();

                patchedResult = lineChangeResult;
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

        private void GenerateAllMarkersBtn()
        {
            Editor.UndoManager.CreateNewGroup("Generate all Markers");

            foreach (int i in Enum.GetValues(typeof(Marker.Icon)))
            {
                Marker.Icon icon = (Marker.Icon)i;
                Marker marker = instanceManager.CreateInstance<Marker>(json: new JSONObject { [nameof(Object.name)] = icon.ToString() }, initializingContext: InitializationContext.Editor);
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
            if (_instance == Disposable.NULL)
                _instance = GetManagerComponent<RenderingManager>(createIfMissing);
            return _instance;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            UpdateComputeBufferSupported();

#if UNITY_EDITOR
            StartURPPatching();
#endif

            _fonts ??= new List<Font>();

            foreach (string fontName in FONT_NAMES)
            {
                if (GetFont(fontName) == null)
                    _fonts.Add(Resources.Load("Font/" + fontName) as Font);
            }

            InitRTTCamera();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => dynamicEnvironment = value, true, initializingContext);
            InitValue(value => dynamicEnvironmentUpdateInterval = value, 5.0f, initializingContext);
            InitValue(value => originShifting = value, true, initializingContext);
            InitValue(value => labelOutlineColor = value, Color.black, initializingContext);
            InitValue(value => labelOutlineWidth = value, 0.25f, initializingContext);
            InitValue(value => highlightColor = value, new Color(0.0f, 1.0f, 1.0f, 0.75f), initializingContext);
            InitValue(value => dynamicFocusDistance = value, true, initializingContext);
            InitValue(value => minMaxFocusDistance = value, new Vector2(0.0f, 500.0f), initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            UpdateEnvironmentCubemap();
        }

        public override void UpdateDependencies()
        {
            base.UpdateDependencies();

#if UNITY_EDITOR
            UpdateComputeBufferSupported();
#endif

            InitRendererFeatures();

            InitPostProcessEffects();
        }

        private void UpdateComputeBufferSupported()
        {
#if UNITY_WEBGL
            COMPUTE_BUFFER_SUPPORTED = false;
#else
            COMPUTE_BUFFER_SUPPORTED = true;
#endif
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
#if UNITY_EDITOR
                SceneManager.BeforeAssemblyReloadEvent -= BeforeAssemblyReloadHandler;
                if (!IsDisposing())
                    SceneManager.BeforeAssemblyReloadEvent += BeforeAssemblyReloadHandler;
#endif
                foreach (ICustomEffect customEffect in customEffects)
                {
                    RemoveCustomEffectDelegate(customEffect);
                    if (!IsDisposing())
                        AddCustomEffectDelegate(customEffect);
                }

                IterateOverReflectionObjects((reflectionProbeObject) =>
                {
                    RemoveReflectionObjectDelegate(reflectionProbeObject);
                    if (!IsDisposing())
                        AddReflectionObjectDelegate(reflectionProbeObject);
                });

                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void BeforeAssemblyReloadHandler()
        {
            DisposeAllComputeBuffers();
        }

        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                UpdateEnvironmentCubemap();

                return true;
            }
            return false;
        }
#endif

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

        private void RemoveReflectionObjectDelegate(Object reflectionProbeObject)
        {
            reflectionProbeObject.DisposedEvent -= ReflectionObjectDisposedHandler;
        }

        private void AddReflectionObjectDelegate(Object reflectionProbeObject)
        {
            reflectionProbeObject.DisposedEvent += ReflectionObjectDisposedHandler;
        }

        private void ReflectionObjectDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            RemoveReflectionObject(disposable as Object);
        }

        protected override void InstanceRemovedHandler(IProperty property)
        {
            base.InstanceRemovedHandler(property);

            ICustomEffect customEffect = property as ICustomEffect;
            if (customEffect is not null)
                RemoveCustomEffect(customEffect);
        }

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            ICustomEffect customEffect = property as ICustomEffect;
            if (!Disposable.IsDisposed(customEffect))
                AddCustomEffect(customEffect);
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

        private List<Object> managedReflectionObjects
        {
            get { _managedReflectionObjects ??= new (); return _managedReflectionObjects; }
            set => _managedReflectionObjects = value;
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
                    _dynamicSkyboxMaterial = new(Shader.Find("Skybox/Cubemap"))
                    {
                        name = "DynamicSkybox"
                    };
                    _dynamicSkyboxMaterial.SetFloat("_Exposure", 1.0f);
                }
                return _dynamicSkyboxMaterial;
            }
        }

        private SerializableIPersistentList customEffects 
        {
            get 
            {
                _customEffects ??= new SerializableIPersistentList();
                return _customEffects; 
            }
        }

        private SerializableIPersistentList[] layersCustomEffects
        {
            get
            {
                if (_layersCustomEffects == null || _layersCustomEffects.Length != 32)
                    _layersCustomEffects = new SerializableIPersistentList[32];
                return _layersCustomEffects;
            }
        }

        private ComputeBuffer[] layersCustomEffectComputeBuffer
        {
            get
            {
                _layersCustomEffectComputeBuffer ??= new ComputeBuffer[32];
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

        private void RemoveCustomEffectFromLayers(LayerMask _, ICustomEffect customEffect)
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
        /// When enabled, a realtime environment cubemap will be generated for each camera and be made available for Global Illumination (GI) and reflection. Disable 'Recalculate Environment Lighting' in 'Project Settings > Editor' or 'Lighting Window > Workflow' to avoid GI flashing.
        /// </summary>
        [Json]
        public bool dynamicEnvironment
        {
            get => _dynamicEnvironment;
            set => SetValue(nameof(dynamicEnvironment), value, ref _dynamicEnvironment);
        }

        /// <summary>
        /// The interval (in seconds) at which we call the <see cref="DepictionEngine.RenderingManager.UpdateEnvironmentCubemap"/> function to update the environment cubemap.
        /// </summary>
        [Json]
        public float dynamicEnvironmentUpdateInterval
        {
            get => _dynamicEnvironmentUpdateInterval;
            set
            {
                SetValue(nameof(dynamicEnvironmentUpdateInterval), ValidateDynamicEnvironmentUpdateInterval(value), ref _dynamicEnvironmentUpdateInterval, (newValue, oldValue) =>
                {
                    UpdateEnvironmentCubemap();
                });
            }
        }

        private float ValidateDynamicEnvironmentUpdateInterval(float value)
        {
            if (value < 0.01f)
                value = 0.01f;
            return value;
        }

        /// <summary>
        /// When enabled the objects will be rendered relative to the camera's position (origin). Required for large Scene.
        /// </summary>
        [Json]
        public bool originShifting
        {
            get => _originShifting;
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
        /// The outline color of <see cref="DepictionEngine.Label"/>'s.
        /// </summary>
        [Json]
        public Color labelOutlineColor
        {
            get => _labelOutlineColor;
            set => SetValue(nameof(labelOutlineColor), value, ref _labelOutlineColor);
        }

        /// <summary>
        /// The outline width of <see cref="DepictionEngine.Label"/>'s.
        /// </summary>
        [Json]
        public float labelOutlineWidth
        {
            get => _labelOutlineWidth;
            set => SetValue(nameof(labelOutlineWidth), Mathf.Clamp01(value), ref _labelOutlineWidth);
        }

        /// <summary>
        /// The UniversalRendererData to be controlled by the <see cref="DepictionEngine.RenderingManager"/> for features such as Ambient Occlusions.
        /// </summary>
        public UniversalRendererData rendererData
        {
            get => _rendererData;
            set
            {
                SetValue(nameof(rendererData), value, ref _rendererData, (newValue, oldValue) =>
                {
                    InitRendererFeatures();
                });
            }
        }

        /// <summary>
        /// The Volume to be controlled by the <see cref="DepictionEngine.RenderingManager"/> for effects such as Depth Of Field or others.
        /// </summary>
        public Volume postProcessVolume
        {
            get => _postProcessVolume;
            set
            {
                SetValue(nameof(postProcessVolume), value, ref _postProcessVolume, (newValue, oldValue) =>
                {
                    InitPostProcessEffects();
                });
            }
        }

        /// <summary>
        /// A color used by objects who support highlight such as <see cref="DepictionEngine.FeatureGridMeshObjectBase"/> where individual features will be highlighted on mouse over.
        /// </summary>
        [Json]
        public Color highlightColor
        {
            get => _highlightColor;
            set => SetValue(nameof(highlightColor), value, ref _highlightColor);
        }

        /// <summary>
        /// When enabled the main Camera will always try to focus on its target. This requires the main Camera to have a TargetController and the depth of field effect to be enabled.
        /// </summary>
        [Json]
        public bool dynamicFocusDistance
        {
            get => _dynamicFocusDistance;
        set => SetValue(nameof(dynamicFocusDistance), value, ref _dynamicFocusDistance);
        }

        /// <summary>
        /// A min and max clamping values for the <see cref="DepictionEngine.RenderingManager.dynamicFocusDistance"/> calculations. 
        /// </summary>
        [Json]
        public Vector2 minMaxFocusDistance
        {
            get => _minMaxFocusDistance;
        set => SetValue(nameof(minMaxFocusDistance), value, ref _minMaxFocusDistance);
        }

        public RTTCamera rttCamera
        {
            get 
            {
                InitRTTCamera();
                return _rttCamera; 
            }
            set => _rttCamera = value;
        }

        private void InitRTTCamera()
        {
            if (_rttCamera == Disposable.NULL)
            {
                string rttCameraName = nameof(RTTCamera);
                GameObject reflectionCameraGO = GameObject.Find(rttCameraName);
                if (reflectionCameraGO != null)
                    _rttCamera = reflectionCameraGO.GetComponentInitialized<RTTCamera>();
                if (_rttCamera == Disposable.NULL)
                    _rttCamera = instanceManager.CreateInstance<RTTCamera>(null, new JSONObject() { [nameof(Object.name)] = rttCameraName });
            }
        }

        public List<Font> fonts
        {
            get => _fonts;
        }

        private MeshesDictionary meshesCache
        {
            get { _meshesCache ??= new MeshesDictionary(); return _meshesCache; }
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
                    if (postProcessVolume.profile.TryGet(out DepthOfField volumeDepthOfField))
                        depthOfField = volumeDepthOfField;
                }

                if (bloom == null)
                {
                    if (postProcessVolume.profile.TryGet(out Bloom volumeBloom))
                        bloom = volumeBloom;
                }

                if (colorAdjustments == null)
                {
                    if (postProcessVolume.profile.TryGet(out ColorAdjustments volumeColorAdjustments))
                        colorAdjustments = volumeColorAdjustments;
                }

                if (colorCurves == null)
                {
                    if (postProcessVolume.profile.TryGet(out ColorCurves volumeColorCurves))
                        colorCurves = volumeColorCurves;
                }

                if (chromaticAberration == null)
                {
                    if (postProcessVolume.profile.TryGet(out ChromaticAberration volumeChromaticAberration))
                        chromaticAberration = volumeChromaticAberration;
                }

                if (vignette == null)
                {
                    if (postProcessVolume.profile.TryGet(out Vignette volumeVignette))
                        vignette = volumeVignette;
                }

                if (toneMapping == null)
                {
                    if (postProcessVolume.profile.TryGet(out Tonemapping volumeToneMapping))
                        toneMapping = volumeToneMapping;
                }

                if (motionBlur == null)
                {
                    if (postProcessVolume.profile.TryGet(out MotionBlur volumeMotionBlur))
                        motionBlur = volumeMotionBlur;
                }
            }
        }

        private DepthOfField depthOfField
        {
            get => _depthOfField;
            set => _depthOfField = value;
        }

        private Bloom bloom
        {
            get => _bloom;
            set => _bloom = value;
        }

        private ColorAdjustments colorAdjustments
        {
            get => _colorAdjustments;
            set => _colorAdjustments = value;
        }

        private ColorCurves colorCurves
        {
            get => _colorCurves;
            set => _colorCurves = value;
        }

        private ChromaticAberration chromaticAberration
        {
            get => _chromaticAberration;
            set => _chromaticAberration = value;
        }

        private Vignette vignette
        {
            get => _vignette;
            set => _vignette = value;
        }

        private Tonemapping toneMapping
        {
            get => _toneMapping;
            set => _toneMapping = value;
        }

        private MotionBlur motionBlur
        {
            get => _motionBlur;
            set => _motionBlur = value;
        }

        /// <summary>
        /// Realtime Shadows type to be used.
        /// </summary>
        [Json]
        public UnityEngine.ShadowQuality shadows
        {
            get => QualitySettings.shadows;
            set => QualitySettings.shadows = value;
        }

        /// <summary>
        /// Set the AA Filtering option.
        /// </summary>
        [Json]
        public int antiAliasing
        {
            get => QualitySettings.antiAliasing;
            set => QualitySettings.antiAliasing = value;
        }

        /// <summary>
        /// Is fog enabled?
        /// </summary>
        [Json]
        public bool fogActive
        {
            get => RenderSettings.fog;
            set => RenderSettings.fog = value;
        }

        /// <summary>
        /// The color of the fog.
        /// </summary>
        [Json]
        public Color fogColor
        {
            get => RenderSettings.fogColor;
            set => RenderSettings.fogColor = value;
        }

        /// <summary>
        /// Should ambient occlusion be rendered?
        /// </summary>
        [Json]
        public bool ambientOcclusionActive
        {
            get { return ambientOcclusionRendererFeature != null && ambientOcclusionRendererFeature.isActive; }
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
            get { return depthOfField != null && depthOfField.active; }
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
            get { return vignette != null && vignette.active; }
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
            get { return toneMapping != null && toneMapping.active; }
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
            get { return motionBlur != null && motionBlur.active; }
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
            get { return bloom != null && bloom.active; }
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
            get { return colorAdjustments != null && colorAdjustments.active; }
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
            get { return colorCurves != null && colorCurves.active; }
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
            get { return chromaticAberration != null && chromaticAberration.active; }
            set
            {
                if (chromaticAberration != null)
                    chromaticAberration.active = value;
            }
        }

        private static LocalKeyword _outlineOnLocalKeyword;
        public static LocalKeyword outlineOnLocalKeyword
        {
            get
            {
                if (_outlineOnLocalKeyword == default)
                    _outlineOnLocalKeyword = new LocalKeyword(Shader.Find("TextMeshPro/Mobile/Distance Field"), "OUTLINE_ON");
                return _outlineOnLocalKeyword;
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
            get => _environmentTimer;
            set
            {
                if (Object.ReferenceEquals(_environmentTimer, value))
                    return;

                DisposeManager.Dispose(_environmentTimer);

                _environmentTimer = value;
            }
        }

        public List<Mesh> GetMeshesFromCache(int hash)
        {
            if (hash != DEFAULT_MISSING_CACHE_HASH && meshesCache.TryGetValue(hash, out List<Mesh> meshes))
                return meshes;
            return null;
        }

        public bool AddMeshesToCache(int hash, List<Mesh> meshes)
        {
            if (hash != DEFAULT_MISSING_CACHE_HASH && meshes != null && !meshesCache.ContainsKey(hash))
            {
                meshesCache.Add(hash, meshes);
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
                unityObject = (unityObject as MonoBehaviourDisposable).gameObject;
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
            string basePath = "Assets/Depiction Engine/Editor/Texture/UI/";
            Texture2D icon = null;

            Type transformType = typeof(TransformBase);
            Type scriptType = typeof(Script);
            Type visualType = typeof(Visual);
            Type meshType = typeof(Mesh);
            Type managerType = typeof(ManagerBase);
            if (type == transformType || type.IsSubclassOf(transformType))
                icon = UnityEditor.EditorGUIUtility.IconContent("d_Transform Icon").image as Texture2D;
            else if (typeof(IPersistent).IsAssignableFrom(type))
                icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "PersistentIcon.png");
            else if (type == scriptType || type.IsSubclassOf(scriptType))
                icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + "ScriptIcon.png");
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
                    if (ColorUtility.ColorFromString(out Color color, "#dd1265"))
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
            Texture2D headerTexture = new(100, 1)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            color.a = 0.3f;
            for (int i = 0; i < headerTexture.width; i++)
                headerTexture.SetPixel(i, 0, Color.Lerp(Color.clear, color, Easing.CircEaseOut((float)i / headerTexture.width, 0, 1, 1)));
            headerTexture.Apply();
            headerTextures[index] = headerTexture;

            Texture2D headerLineTexture = new(1, 1);
            color.a = 0.5f;
            headerLineTexture.SetPixel(0, 0, color);
            headerLineTexture.Apply();
            headerTextures[index + 1] = headerLineTexture;
        }

        private void GenerateHeaderTexture(Texture2D[] headerTextures, int index, Color color)
        {
            Texture2D headerTexture = new(1, 1)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            color.a = 0.3f;
            headerTexture.SetPixel(0, 0, color);
            headerTexture.Apply();
            headerTextures[index] = headerTexture;
        }
#endif

        /// <summary>
        /// Added <see cref="DepictionEngine.Object"/> will have their ReflectionProbe automatically managed by the <see cref="DepictionEngine.RenderingManager"/>, which means they will be updated at a regular interval.
        /// </summary>
        /// <param name="reflectionObject"></param>
        public void AddReflectionObject(Object reflectionObject)
        {
            if (!managedReflectionObjects.Contains(reflectionObject))
            {
                managedReflectionObjects.Add(reflectionObject);
                AddReflectionObjectDelegate(reflectionObject);

                MarkReflectionObjectDirty(reflectionObject);
            }
        }

        /// <summary>
        /// Remove the <see cref="DepictionEngine.Object"/> from the <see cref="DepictionEngine.RenderingManager"/> managed list.
        /// </summary>
        /// <param name="reflectionObject"></param>
        /// <returns>True if it was removed successfully.</returns>
        public bool RemoveReflectionObject(Object reflectionObject)
        {
            if (managedReflectionObjects.Remove(reflectionObject))
            {
                RemoveReflectionObjectDelegate(reflectionObject);
                return true;
            }
            return false;
        }

        protected void IterateOverReflectionObjects(Action<Object> callback)
        {
            if (callback != null)
            {
                for (int i = managedReflectionObjects.Count - 1; i >= 0; i--)
                {
                    Object reflectionObject = managedReflectionObjects[i];
       
                    if (reflectionObject.IsReflectionObject())
                        callback(reflectionObject);
                }
            }
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
                        if (dynamicFocusDistance && mainCamera != Disposable.NULL && mainCamera.controller is TargetControllerBase mainCameraTargetController)
                            depthOfField.focusDistance.value = Mathf.Clamp((float)mainCameraTargetController.distance, minMaxFocusDistance.x, minMaxFocusDistance.y);
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
            _layersCustomEffectComputeBufferData ??= new float[32][];

            for (int layer = 0; layer <= 31; layer++)
            {
                float[] layerCustomEffectComputeBufferData = _layersCustomEffectComputeBufferData[layer];

                SerializableIPersistentList layerCustomEffects = layersCustomEffects[layer];
                if (layerCustomEffects != null && layerCustomEffects.Count > 0)
                {
                    int size = 0;

                    foreach (ICustomEffect layerCustomEffect in layerCustomEffects)
                    {
                        if (layerCustomEffect.GetCustomEffectComputeBufferDataSize(out int effectSize))
                            size += effectSize;
                    }

                    if (layerCustomEffectComputeBufferData == null || layerCustomEffectComputeBufferData.Length != size)
                        layerCustomEffectComputeBufferData = new float[size];

                    int startIndex = 0;
                    for (int i = 0; i < layerCustomEffects.Count; i++)
                    {
                        //Null check is required for when a CustomEffect MonoBehaviour Component is removed directly in the Editor instead of deleting the whole GameObject
                        if (layerCustomEffects[i] is ICustomEffect layerCustomEffect)
                        {
                            if (layerCustomEffect.AddToComputeBufferData(out int effectSize, startIndex, layerCustomEffectComputeBufferData))
                                startIndex += effectSize;
                        }
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
                if (layerCustomEffectComputeBufferData != null && layerCustomEffectComputeBufferData.Length != 0)
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
                    visualObject.IterateOverManagedMeshRenderer((materialPropertyBlock, meshRenderer) =>
                    {
                        if (meshRenderer.sharedMaterial != null)
                        {
                            if (COMPUTE_BUFFER_SUPPORTED)
                            {
                                ComputeBuffer layerCustomEffectComputeBuffer = layersCustomEffectComputeBuffer[visualObject.layer];
                                meshRenderer.sharedMaterial.SetBuffer("_CustomEffectsBuffer", layerCustomEffectComputeBuffer);
                                meshRenderer.sharedMaterial.SetInteger("_CustomEffectsBufferDimensions", layerCustomEffectComputeBuffer != null ? layerCustomEffectComputeBuffer.count : 0);

                                meshRenderer.sharedMaterial.EnableKeyword("ENABLE_COMPUTE_BUFFER");
                            }
                            else
                                meshRenderer.sharedMaterial.DisableKeyword("ENABLE_COMPUTE_BUFFER");
                        }
                    });

                    return true;
                });
        }

        public void EndCameraDistancePassRendering(Camera _, UnityEngine.Camera _1)
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

                _cachedMeshCount = meshesCache.Count;
#endif
                return true;
            }
            return false;
        }

        public void UpdateEnvironmentCubemap()
        {
            if (initialized)
            {
                IterateOverReflectionObjects((reflectionObject) =>
                {
                    MarkReflectionObjectDirty(reflectionObject);
                });

                TweenManager tweenManager = TweenManager.Instance();
                if (tweenManager != Disposable.NULL)
                {
                    //Validate to make sure 'dynamicEnvironmentUpdateInterval' is not zero. This can happen if the Update happens before the 'dynamicEnvironmentUpdateInterval' value is initialized.
                    environmentTimer = tweenManager.DelayedCall(ValidateDynamicEnvironmentUpdateInterval(dynamicEnvironmentUpdateInterval), null, UpdateEnvironmentCubemap);
                }
            }
        }

        private static HashSet<Object> _reflectionObjectsDirty = new();
        /// <summary>
        /// Queue the <see cref="DepictionEngine.Object"/> reflectionProbe for update.
        /// </summary>
        /// <param name="reflectionObject"></param>
        public static void MarkReflectionObjectDirty(Object reflectionObject)
        {
            if (!_reflectionObjectsDirty.Contains(reflectionObject))
                _reflectionObjectsDirty.Add(reflectionObject);
        }

        private Dictionary<int, List<Object>> _renderReflectionCameraObjects = new();
        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                if (dynamicEnvironment)
                {
                    _renderReflectionCameraObjects.Clear();
                    foreach (Object reflectionProbeObject in _reflectionObjectsDirty)
                    {
                        if (reflectionProbeObject != Disposable.NULL && reflectionProbeObject.ReflectionRequiresRender(out Camera camera))
                        {
                            int cameraInstanceID = camera.GetInstanceID();
                            if (!_renderReflectionCameraObjects.TryGetValue(cameraInstanceID, out List<Object> renderReflectionProbeObjects))
                            {
                                renderReflectionProbeObjects = new();
                                _renderReflectionCameraObjects.Add(cameraInstanceID, renderReflectionProbeObjects);
                            }
                            renderReflectionProbeObjects.Add(reflectionProbeObject);
                        }
                    }
                    _reflectionObjectsDirty.Clear();

                    instanceManager.IterateOverInstances<Camera>(
                    (camera) =>
                    {
                        if (_renderReflectionCameraObjects.TryGetValue(camera.GetInstanceID(), out List<Object> renderReflectionObjects))
                        {
                            sceneManager.BeginCameraRendering(camera);

                            DisableReflectionAndGI(camera.skybox.material, () =>
                            {
                                foreach (Object reflectionObject in renderReflectionObjects)
                                    reflectionObject.UpdateEnvironmentReflection(rttCamera, camera);
                            });

                            foreach (Object reflectionProbeObject in renderReflectionObjects)
                            {
                                reflectionProbeObject.IterateOverReflectionProbes((reflectionProbe) =>
                                {
                                    if (reflectionProbe != null)
                                        reflectionProbe.customBakedTexture = reflectionProbeObject.reflectionCustomBakedTexture;
                                });
                            }

                            camera.transform.RevertUnityLocalPosition();
                            sceneManager.EndCameraRendering(camera);

                            camera.ambientProbe = default;
                        }

                        return true;
                    });
                }

                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginCameraRendering(Camera camera, ScriptableRenderContext? context)
        {
            if (context.HasValue)
            {
                DisableReflectionAndGI(camera.skybox.material, () =>
                {
                    instanceManager.IterateOverInstances<VisualObject>((visualObject) =>
                    {
                        visualObject.UpdateReflectionEffect(rttCamera, camera, context);
                        return true;
                    });
                });

                if (dynamicEnvironment)
                {
                    DefaultReflectionMode defaultReflectionMode = DefaultReflectionMode.Custom;
                    if (RenderSettings.defaultReflectionMode != defaultReflectionMode)
                        RenderSettings.defaultReflectionMode = defaultReflectionMode;

                    AmbientMode ambientMode = AmbientMode.Skybox;
                    if (RenderSettings.ambientMode != ambientMode)
                        RenderSettings.ambientMode = ambientMode;

                    RenderTexture customReflectionTexture = camera.reflectionCustomBakedTexture;

                    if (RenderSettings.customReflectionTexture != customReflectionTexture)
                        RenderSettings.customReflectionTexture = customReflectionTexture;

                    if (dynamicSkyboxMaterial.GetTexture("_Tex") != customReflectionTexture)
                        dynamicSkyboxMaterial.SetTexture("_Tex", customReflectionTexture);

                    Material skyboxMaterial = dynamicSkyboxMaterial;
                    if (RenderSettings.skybox != skyboxMaterial)
                        RenderSettings.skybox = skyboxMaterial;

                    if (camera.ambientProbe == default)
                    {
                        DynamicGI.synchronousMode = true;
                        DynamicGI.UpdateEnvironment();

                        //Assign after the DynamicGI.UpdateEnvironment();
                        camera.ambientProbe = RenderSettings.ambientProbe;
                    }

                    Shader.SetGlobalVector("_GlossyEnvironmentColor", camera.glossyEnvironmentColor);
                    RenderSettings.ambientProbe = camera.ambientProbe;
                }
            }
        }

        private List<(float, UnityEngine.Texture)> _lastReflectionProbesValues = new();
        public void DisableReflectionAndGI(Material skyboxMaterial, Action callback)
        {
            if (callback != null)
            {
                float lastAmbientIntensity = RenderSettings.ambientIntensity;
                RenderSettings.ambientIntensity = 0.0f;
                float lastReflectionIntensity = RenderSettings.reflectionIntensity;
                RenderSettings.reflectionIntensity = 0.0f;
                Material lastSkyboxMaterial = RenderSettings.skybox;
                RenderSettings.skybox = skyboxMaterial;
                UnityEngine.Texture lastCustomReflectionTexture = RenderSettings.customReflectionTexture;
                RenderSettings.customReflectionTexture = null;
                _lastReflectionProbesValues.Clear();
                IterateOverReflectionObjects((reflectionProbeObject) => 
                { 
                    reflectionProbeObject.IterateOverReflectionProbes((reflectionProbe) =>
                    {
                        if (reflectionProbe != null)
                        {
                            _lastReflectionProbesValues.Add((reflectionProbe.intensity, reflectionProbe.customBakedTexture));
                            reflectionProbe.intensity = 0.0f;
                            reflectionProbe.customBakedTexture = null;
                        }
                    });
                });

                callback();

                RenderSettings.ambientIntensity = lastAmbientIntensity;
                RenderSettings.reflectionIntensity = lastReflectionIntensity;
                RenderSettings.skybox = lastSkyboxMaterial;
                RenderSettings.customReflectionTexture = lastCustomReflectionTexture;
                int index = 0;
                IterateOverReflectionObjects((reflectionProbeObject) => 
                {
                    reflectionProbeObject.IterateOverReflectionProbes((reflectionProbe) =>
                    {
                        if (reflectionProbe != null)
                        {
                            (float, UnityEngine.Texture)  lastReflectionProbeValue = _lastReflectionProbesValues[index];
                            reflectionProbe.intensity = lastReflectionProbeValue.Item1;
                            reflectionProbe.customBakedTexture = lastReflectionProbeValue.Item2;
                        }
                    });
                });
            }
        }

        public static Material LoadMaterial(string path)
        {
            return Resources.Load<Material>(path);
        }

        private static Dictionary<string, Shader> _shaderCache;
        public static Shader LoadShader(string path)
        {
            Shader shader;

            _shaderCache ??= new();
            if (!_shaderCache.TryGetValue(path, out shader))
            {
                shader = Resources.Load<Shader>(path);
                _shaderCache.Add(path, shader);
            }

            return shader;
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
            computeBuffer?.Dispose();
        }

        public void DisposeAllCachedMeshes(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (_meshesCache != null)
            {
                foreach (List<Mesh> meshes in _meshesCache.Values)
                {
                    foreach (Mesh mesh in meshes)
                        DisposeManager.Dispose(mesh, disposeContext);
                }
                _meshesCache.Clear();
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                environmentTimer = null;

                DisposeManager.Dispose(_rttCamera);

                DisposeManager.Dispose(_emptyTexture);

                DisposeManager.Dispose(_quadMesh);

                DisposeManager.Dispose(_dynamicSkyboxMaterial);

                DisposeAllComputeBuffers();

#if UNITY_EDITOR
                if (_headerTextures != null)
                {
                    foreach (Texture2D headerTexture in _headerTextures)
                        DisposeManager.Dispose(headerTexture);
                }
#endif
                DisposeAllCachedMeshes(disposeContext);

                return true;
            }
            return false;
        }
    }
}
