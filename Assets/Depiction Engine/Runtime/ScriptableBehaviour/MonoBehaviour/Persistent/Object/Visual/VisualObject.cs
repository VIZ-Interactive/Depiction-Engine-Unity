// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DepictionEngine
{
    /// <summary>
    /// A base class for any object which needs to have a visual representation in the scene.
    /// </summary>
    [SelectionBase]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/" + nameof(VisualObject))]
    public class VisualObject : Object
    {
        /// <summary>
        /// The different types of popup. <br/><br/>
        /// <b><see cref="Alpha"/>:</b> <br/>
        /// An alpha fade in. <br/><br/>
        /// <b><see cref="Scale"/>:</b> <br/>
        /// An expansion of the y axis.
        /// </summary>
        public enum PopupType
        {
            Alpha,
            Scale
        }

        private static readonly double EARTH_RADIUS = MathPlus.GetRadiusFromCircumference(GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Earth));

        [BeginFoldout("Visual")]
        [SerializeField, Tooltip("When enabled the child 'Visual' will not be saved as part of the Scene."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDontSaveVisualsToScene))]
#endif
        private bool _dontSaveVisualsToScene;

        [BeginFoldout("Opacity")]
        [SerializeField, Range(0.0f, 1.0f), Tooltip("How transparent should the object be."), EndFoldout]
        private float _alpha;

        [BeginFoldout("Popup")]
        [SerializeField, Tooltip("How the object should fade in. Alpha Popup requires an instanced Material to work.")]
        private PopupType _popupType;
        [SerializeField, Tooltip("The duration of the fade in animation."), EndFoldout]
        private float _popupDuration;

        [SerializeField, HideInInspector]
        private VisualObjectVisualDirtyFlags _meshRendererVisualDirtyFlags;

        private List<MeshRenderer> _meshRenderers;

        private static MaterialPropertyBlock _materialPropertyBlock;

        private Tween _popupTween;
        private bool _popup;
        private float _popupT;

#if UNITY_EDITOR
        protected virtual bool GetShowDontSaveVisualsToScene()
        {
            return true;
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            if (_meshRendererVisualDirtyFlags != null)
                _meshRendererVisualDirtyFlags.Recycle();

            if (_materialPropertyBlock != null)
                _materialPropertyBlock.Clear();

            _popup = false;
            _popupT = 0.0f;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastAlpha = alpha;
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

            if (meshRendererVisualDirtyFlags == null)
                meshRendererVisualDirtyFlags = ScriptableObject.CreateInstance(GetMeshRendererVisualDirtyFlagType()) as VisualObjectVisualDirtyFlags;

            //Dont Popup if the object was duplicated or already existed
            popup = initializingState == InstanceManager.InitializationContext.Editor || initializingState == InstanceManager.InitializationContext.Programmatically;
            
            if (!popup)
                DetectCompromisedPopup();
        }

        public override void ExplicitOnEnable()
        {
            base.ExplicitOnEnable();

            DetectCompromisedPopup();
        }

        private void DetectCompromisedPopup()
        {
            if (!(popupT == 0.0f || popupT == 1.0f))
                popup = true;
        }

        protected virtual Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(VisualObjectVisualDirtyFlags);
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => dontSaveVisualsToScene = value, GetDefaultDontSaveVisualsToScene(), initializingState);
            InitValue(value => alpha = value, GetDefaultAlpha(), initializingState);
            InitValue(value => popupType = value, GetDefaultPopupType(), initializingState);
            InitValue(value => popupDuration = value, GetDefaultPopupDuration(), initializingState);
        }

#if UNITY_EDITOR
        private float _lastAlpha;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            Editor.UndoManager.PerformUndoRedoPropertyChange((value) => { alpha = value; }, ref _alpha, ref _lastAlpha);
        }
#endif

        public override void OnMouseUpHit(RaycastHitDouble hit)
        {
            base.OnMouseUpHit(hit);

            _mouseDown = false;
        }

        public override void OnMouseDownHit(RaycastHitDouble hit)
        {
            base.OnMouseDownHit(hit);

            _mouseDown = true;
        }

        public override void OnMouseEnterHit(RaycastHitDouble hit)
        {
            base.OnMouseEnterHit(hit);

            _mouseOver = true;
        }

        public override void OnMouseExitHit(RaycastHitDouble hit)
        {
            base.OnMouseExitHit(hit);

            _mouseDown = false;
            _mouseOver = false;
        }

        private bool _mouseDown;
        public bool GetMouseDown()
        {
            return _mouseDown;
        }

        private bool _mouseOver;
        public bool GetMouseOver()
        {
            return _mouseOver;
        }

        public override bool RequiresPositioning()
        {
            return true;
        }

        protected virtual bool GetDefaultDontSaveVisualsToScene()
        {
            return false;
        }

        protected virtual float GetDefaultAlpha()
        {
            return 1.0f;
        }

        protected virtual PopupType GetDefaultPopupType()
        {
            return PopupType.Alpha;
        }

        protected virtual float GetDefaultPopupDuration()
        {
            return 0.2f;
        }

        private List<MeshRenderer> meshRenderers
        {
            get 
            {
                if (_meshRenderers == null)
                    _meshRenderers = new List<MeshRenderer>();
                return _meshRenderers;
            }
        }

        private MaterialPropertyBlock materialPropertyBlock
        {
            get 
            {
                if (_materialPropertyBlock == null)
                    _materialPropertyBlock = new MaterialPropertyBlock();
                return _materialPropertyBlock; 
            }
        }

        /// <summary>
        /// When enabled the child <see cref="Visual"/> will not be saved as part of the Scene.
        /// </summary>
        [Json]
        public bool dontSaveVisualsToScene
        {
            get { return _dontSaveVisualsToScene; }
            set 
            { 
                SetValue(nameof(dontSaveVisualsToScene), value, ref _dontSaveVisualsToScene, (newValue, oldValue) => 
                {
                    DontSaveVisualsToSceneChanged(newValue, oldValue);
                }); 
            }
        }

        protected virtual void DontSaveVisualsToSceneChanged(bool newValue, bool oldValue)
        {

        }

        /// <summary>
        /// How transparent should the object be.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetAlphaAdditionalRecordObjects))]
#endif
        public float alpha
        {
            get { return _alpha; }
            set { SetAlpha(value); }
        }

        /// <summary>
        /// How the object should fade in. Alpha Popup requires an instanced Material to work.
        /// </summary>
        [Json]
        public PopupType popupType
        {
            get { return _popupType; }
            set
            {
#if UNITY_EDITOR
                //Demo the effect if the change was done in the inspector
                if (IsUserChangeContext())
                    _popup = true;
#endif
                SetValue(nameof(popupType), value, ref _popupType);
            }
        }

        /// <summary>
        /// The duration of the fade in animation.
        /// </summary>
        [Json]
        public float popupDuration
        {
            get { return _popupDuration; }
            set { SetValue(nameof(popupDuration), value, ref _popupDuration); }
        }

#if UNITY_EDITOR
        protected virtual UnityEngine.Object[] GetAlphaAdditionalRecordObjects()
        {
            return null;
        }
#endif

        public Tween popupTween
        {
            get { return _popupTween; }
            set
            {
                if (Object.ReferenceEquals(_popupTween, value))
                    return;

                Dispose(_popupTween);

                _popupTween = value;
            }
        }

        protected virtual bool SetAlpha(float value)
        {
            return SetValue(nameof(alpha), value, ref _alpha, (newValue, oldValue) => 
            {
#if UNITY_EDITOR
                _lastAlpha = newValue;
#endif
            });
        }

        protected bool popup
        {
            get { return _popup; }
            set { _popup = value; }
        }

        protected float GetPopupT(PopupType popupType)
        {
            return popupType == this.popupType && popupTween != Disposable.NULL ? popupT : 1.0f;
        }

        protected float popupT
        {
            get { return _popupT; }
            set { SetPopupT(value); }
        }

        protected virtual bool SetPopupT(float value)
        {
            return SetValue(nameof(popupT), value, ref _popupT);
        }

        protected override Type GetChildType()
        {
            return typeof(Visual);
        }

        protected override void Saving(Scene scene, string path)
        {
            base.Saving(scene, path);

            if (dontSaveVisualsToScene)
            {
                if (meshRendererVisualDirtyFlags != null)
                    meshRendererVisualDirtyFlags.AllDirty();
            }
        }

        protected override void Saved(Scene scene)
        {
            base.Saved(scene);

            if (meshRendererVisualDirtyFlags != null)
                meshRendererVisualDirtyFlags.ResetDirty();
        }

        protected VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags
        {
            get { return _meshRendererVisualDirtyFlags; }
            set { _meshRendererVisualDirtyFlags = value; }
        }

        public bool GetMeshRenderersInitialized()
        {
            return gameObject.transform.childCount > 0;
        }

        public void IterateOverMaterials(Action<Material, MaterialPropertyBlock, MeshRenderer> callback)
        {
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer != null && meshRenderer.sharedMaterial != null)
                {
                    MaterialPropertyBlock materialPropertyBlock = null;

                    //materialPropertyBlock = this.materialPropertyBlock;

                    if (materialPropertyBlock != null)
                        meshRenderer.GetPropertyBlock(materialPropertyBlock);

                    callback(meshRenderer.sharedMaterial, materialPropertyBlock, meshRenderer);

                    if (materialPropertyBlock != null)
                        meshRenderer.SetPropertyBlock(materialPropertyBlock);
                }
            }
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                Star star = null;
                InstanceManager instanceManager = InstanceManager.Instance(false);
                if (renderingManager != Disposable.NULL)
                    star = instanceManager.GetStar();

                GeoAstroObject closestGeoAstroObject = GetClosestGeoAstroObject();

                Vector3Double cameraPosition = camera.transform.position;

                double cameraAtmosphereAltitudeRatio = 0.0d;
                if (closestGeoAstroObject != Disposable.NULL)
                {
                    double atmosphereThickness = closestGeoAstroObject.GetScaledAtmosphereThickness();
                    cameraAtmosphereAltitudeRatio = closestGeoAstroObject.GetAtmosphereAltitudeRatio(atmosphereThickness, cameraPosition);
                }

                IterateOverMaterials((material, materialPropertyBlock, meshRenderer) =>
                {
                    ApplyPropertiesToMaterial(meshRenderer, meshRenderer.sharedMaterial, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);
                });

                return true;
            }
            return false;
        }

        public override bool HierarchicalUpdateEnvironmentAndReflection(Camera camera, ScriptableRenderContext? context)
        {
            if (base.HierarchicalUpdateEnvironmentAndReflection(camera, context))
            {
                GeoAstroObject closestGeoAstroObject = GetClosestGeoAstroObject();

                Vector3Double cameraPosition = camera.transform.position;

                double cameraAtmosphereAltitudeRatio = 0.0d;
                if (closestGeoAstroObject != Disposable.NULL)
                {
                    double atmosphereThickness = closestGeoAstroObject.GetScaledAtmosphereThickness();
                    cameraAtmosphereAltitudeRatio = closestGeoAstroObject.GetAtmosphereAltitudeRatio(atmosphereThickness, cameraPosition);
                }

                RTTCamera rttCamera = null;
                RenderingManager renderingManager = RenderingManager.Instance(false);
                if (renderingManager != Disposable.NULL)
                    rttCamera = renderingManager.rttCamera;

                foreach (MeshRenderer meshRenderer in meshRenderers)
                {
                    if (meshRenderer != null && meshRenderer.sharedMaterial != null)
                    {
                        MaterialPropertyBlock materialPropertyBlock = null;

                        //materialPropertyBlock = this.materialPropertyBlock;

                        if (materialPropertyBlock != null)
                            meshRenderer.GetPropertyBlock(materialPropertyBlock);

                        ApplyReflectionTextureToMaterial(meshRenderer, meshRenderer.sharedMaterial, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, rttCamera, context);
                       
                        if (materialPropertyBlock != null)
                            meshRenderer.SetPropertyBlock(materialPropertyBlock);
                    }
                }

                return true;
            }
            return false;
        }

        public override bool HierarchicalEndCameraRendering(Camera camera)
        {
            if (base.HierarchicalEndCameraRendering(camera))
            {
                ApplyCameraMaskLayerToVisuals(camera, true);

                return true;
            }
            return false;
        }

        protected virtual void UpdateVisualProperties()
        {

        }

        private bool _visualsChanged;
        protected override void PreHierarchicalUpdateBeforeChildrenAndSiblings()
        {
            base.PreHierarchicalUpdateBeforeChildrenAndSiblings();

            UpdateVisualProperties();

            if (AssetLoaded())
            {
                UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

                UpdateMeshRendererVisuals(meshRendererVisualDirtyFlags);

                if (GetMeshRenderersInitialized())
                {
                    if (popup && !IsFullyInitialized())
                        popup = false;

                    if (popup)
                    {
                        popup = false;
                        if (_popupDuration != 0.0f)
                        {
                            switch (_popupType)
                            {
                                case PopupType.Alpha:
                                    popupTween = tweenManager.To(0.0f, alpha, popupDuration, (value) => { popupT = value; }, null, () =>
                                    {
                                        popupTween = null;
                                    });
                                    break;
                                case PopupType.Scale:
                                    popupTween = tweenManager.To(0.0f, 1.0f, popupDuration, (value) => { popupT = value; }, null, () =>
                                    {
                                        popupTween = null;
                                    }, EasingType.ElasticEaseOut);
                                    break;
                            }
                        }
                    }

                    ApplyPropertiesToVisual(_visualsChanged, meshRendererVisualDirtyFlags);
                    _visualsChanged = false;
                }

                meshRendererVisualDirtyFlags.ResetDirty();
            }
        }

        protected virtual bool AssetLoaded()
        {
            return true;
        }

        protected virtual void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {

        }

        protected virtual void UpdateMeshRendererVisuals(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            if (meshRendererVisualDirtyFlags.disposeAllVisuals)
                DisposeAllChildren();
        }

        protected override void TransformChildAddedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            base.TransformChildAddedHandler(transform, child);

            if (child is Visual)
                _visualsChanged = true;
        }

        protected override void TransformChildRemovedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            base.TransformChildRemovedHandler(transform, child);

            if (child is Visual)
                _visualsChanged = true;
        }

        protected void UpdateMeshRendererVisualCollider(MeshRendererVisual meshRendererVisual, MeshRendererVisual.ColliderType colliderType, bool convexCollider, bool visualsChanged, bool colliderTypeChanged = true, bool convexColliderChanged = true)
        {
            bool colliderChanged = false;
            if (visualsChanged || colliderTypeChanged)
            {
                if (meshRendererVisual.SetColliderType(colliderType))
                    colliderChanged = true;
            }

            if (visualsChanged || colliderChanged || convexColliderChanged)
            {
                if (meshRendererVisual.GetCollider() != null && meshRendererVisual.GetCollider() is MeshCollider)
                {
                    MeshCollider meshCollider = meshRendererVisual.GetCollider() as MeshCollider;
                    if (meshCollider.convex != convexCollider)
                        meshCollider.convex = convexCollider;
                }
            }
        }

        protected virtual void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {

        }

        protected virtual void InitializeMaterial(MeshRendererVisual meshRendererVisual, Material material = null)
        {
            meshRendererVisual.sharedMaterial = material;
        }

        protected Material UpdateMaterial(ref Material material, string shaderPath)
        {
            Shader shader = RenderingManager.LoadShader(shaderPath);
            if (material == null || material.shader != shader)
            {
                Dispose(material);
                material = InstantiateMaterial(shader);
            }

            return material;
        }

        protected Material InstantiateMaterial(Shader shader)
        {
            return shader != null ? new Material(shader) : null;
        }

        public int meshRendererCount
        {
            get { return meshRenderers.Count; }
        }

        public virtual float GetCurrentAlpha()
        {
            return alpha * GetPopupT(PopupType.Alpha);
        }

        protected bool GetEnableAlphaClip()
        {
            return RenderingManager.GetEnableAlphaClip();
        }

        protected Visual GetVisual(string name)
        {
            Visual matchingVisual = null;
            transform.IterateOverRootChildren<Visual>((visual) =>
            {
                if (visual.name == name)
                {
                    matchingVisual = visual;
                    return false;
                }
                return true;
            });
            return matchingVisual;
        }

        protected virtual bool GetVisualIgnoreRender(Visual visual, Camera camera, bool afterRendering)
        {
            return !afterRendering && CameraIsMasked(camera);
        }

        protected virtual Vector3Double GetClosestGeoAstroObjectCenterOS(GeoAstroObject closestGeoAstroObject)
        {
            return transform.InverseTransformPoint(closestGeoAstroObject.transform.position);
        }

        public void ApplyCameraMaskLayerToVisuals(Camera camera = null, bool afterRendering = false)
        {
            transform.IterateOverRootChildren<Visual>((visual) =>
            {
                visual.UpdateLayer(GetVisualIgnoreRender(visual, camera, afterRendering));
                return true;
            });
        }

        protected virtual T CreateMeshRendererVisual<T>(string name = null, Transform parent = null, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically) where T : MeshRendererVisual
        {
            return CreateVisual<T>(name, parent, initializingState);
        }

        protected virtual MeshRendererVisual CreateMeshRendererVisual(Type type, string name = null, Transform parent = null, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically)
        {
            return CreateVisual(type, name, parent, initializingState) as MeshRendererVisual;
        }

        protected T CreateVisual<T>(string name = null, Transform parent = null, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically, List<PropertyModifier> propertyModifers = null) where T : Visual
        {
            return (T)CreateVisual(typeof(T), name, parent, initializingState, propertyModifers);
        }

        protected Visual CreateVisual(Type type, string name = null, Transform parent = null, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically, List<PropertyModifier> propertyModifers = null)
        {
            return CreateChild(type, name, parent, initializingState, propertyModifers) as Visual;
        }

        public void AddAllChildMeshRenderers()
        {
            gameObject.GetComponentsInChildren(true, meshRenderers);
        }

        public void AddMeshRenderer(MeshRenderer meshRenderer)
        {
            if (!meshRenderers.Contains(meshRenderer))
                meshRenderers.Add(meshRenderer);
        }

        public void RemoveMeshRenderer(MeshRenderer meshRenderer)
        {
            meshRenderers.Remove(meshRenderer);
            if (meshRenderer != null)
                meshRenderer.SetPropertyBlock(null);
        }

        public void RemoveDisposedMeshRenderers()
        {
            for(int i = meshRenderers.Count - 1; i >= 0; i--)
            {
                MeshRenderer meshRenderer = meshRenderers[i];
                if (meshRenderer == null)
                    meshRenderers.RemoveAt(i);
            }
        }

        protected virtual void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            ApplyAlphaToMaterial(material, materialPropertyBlock, closestGeoAstroObject, camera, GetCurrentAlpha());

            ApplyClosestGeoAstroObjectPropertiesToMaterial(material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, closestGeoAstroObject, star, camera);

            ApplyAtmospherePropertiesToMaterial(material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, closestGeoAstroObject, star, camera);

            if (GetEnableAlphaClip())
                material.EnableKeyword("ENABLE_ALPHA_CLIP");
            else
                material.DisableKeyword("ENABLE_ALPHA_CLIP");
        }

        private RayDouble _shadowRay;
        protected virtual void ApplyClosestGeoAstroObjectPropertiesToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, GeoAstroObject closestGeoAstroObject, Star star, Camera camera)
        {
            Vector3Double closestGeoAstroObjectSurfacePointWS = Vector3Double.zero;
            Vector3Double closestGeoAstroObjectCenterOS = Vector3Double.zero;
            Vector3Double closestGeoAstroObjectCenterWS = Vector3Double.zero;

            Vector3Double shadowPositionWS = new Vector3Double(0.0d, -10000000000000000.0d, 0.0d);
            QuaternionDouble shadowDirectionWS = QuaternionDouble.identity;
            float shadowAttenuationDistance = 100000000000.0f;

            float tileSizeLatitudeFactor = 1.0f;

            float sphericalRatio = 0.0f;

            float radius = 0.0f;

            if (closestGeoAstroObject != Disposable.NULL && (closestGeoAstroObject.IsValidSphericalRatio()) && star != Disposable.NULL)
            {
                if (closestGeoAstroObject.IsSpherical())
                    tileSizeLatitudeFactor = (float)(1.0d / Math.Cos(MathPlus.DEG2RAD * transform.GetGeoCoordinate().latitude));

                sphericalRatio = closestGeoAstroObject.GetSphericalRatio();
                radius = (float)closestGeoAstroObject.GetScaledRadius();

                closestGeoAstroObjectSurfacePointWS = closestGeoAstroObject.GetSurfacePointFromPoint(transform.position);
                closestGeoAstroObjectCenterOS = GetClosestGeoAstroObjectCenterOS(closestGeoAstroObject);
                closestGeoAstroObjectCenterWS = closestGeoAstroObject.transform.position;

                Vector3Double starPosition = star.transform.position;

                Vector3Double upVector = closestGeoAstroObject.GetUpVectorFromPoint(closestGeoAstroObjectSurfacePointWS) * Vector3Double.up;
                Vector3Double forwardVector = (starPosition - closestGeoAstroObjectCenterWS).normalized;

                Vector3Double surfacePoint;

                Vector3Double edgePosition;
                if (closestGeoAstroObject.IsSpherical())
                {
                    surfacePoint = closestGeoAstroObjectSurfacePointWS;
                    edgePosition = QuaternionDouble.LookRotation(forwardVector, upVector) * Vector3Double.up * closestGeoAstroObject.radius;
                }
                else
                {
                    surfacePoint = closestGeoAstroObjectCenterWS;
                    edgePosition = QuaternionDouble.LookRotation(upVector, forwardVector) * Vector3Double.up * closestGeoAstroObject.size / 2.0d;
                }
                edgePosition += closestGeoAstroObjectCenterWS;

                if (_shadowRay == null)
                    _shadowRay = new RayDouble();
                _shadowRay.Init(starPosition, (edgePosition - starPosition).normalized);

                Vector3Double intersection;
                if (Vector3Double.Dot(upVector, forwardVector) <= 0.0d && MathGeometry.LinePlaneIntersection(out intersection, surfacePoint, QuaternionDouble.LookRotation(upVector, (surfacePoint - starPosition).normalized) * QuaternionDouble.Euler(90.0d, 0.0d, 0.0d) * Vector3Double.forward, _shadowRay, false))
                {
                    shadowPositionWS = intersection;
                    shadowDirectionWS = closestGeoAstroObject.GetUpVectorFromPoint(edgePosition);
                }
            }

            SetVectorToMaterial("_ClosestGeoAstroObjectSurfacePointWS", TransformDouble.SubtractOrigin(closestGeoAstroObjectSurfacePointWS), material, materialPropertyBlock);
            SetVectorToMaterial("_ClosestGeoAstroObjectCenterOS", closestGeoAstroObjectCenterOS, material, materialPropertyBlock);
            SetVectorToMaterial("_ClosestGeoAstroObjectCenterWS", TransformDouble.SubtractOrigin(closestGeoAstroObjectCenterWS), material, materialPropertyBlock);

            Vector3Double mainLightPositionWS = star != Disposable.NULL ? star.transform.position : Vector3Double.zero;

            SetVectorToMaterial("_MainLightPositionWS", TransformDouble.SubtractOrigin(mainLightPositionWS), material, materialPropertyBlock);
            SetVectorToMaterial("_ShadowPositionWS", TransformDouble.SubtractOrigin(shadowPositionWS), material, materialPropertyBlock);
            SetVectorToMaterial("_ShadowDirectionQuaternionWS", new Vector4((float)shadowDirectionWS.x, (float)shadowDirectionWS.y, (float)shadowDirectionWS.z, (float)shadowDirectionWS.w), material, materialPropertyBlock);
            SetFloatToMaterial("_ShadowAttenuationDistance", (float)shadowAttenuationDistance, material, materialPropertyBlock);
           
            SetFloatToMaterial("_CameraAtmosphereAltitudeRatio", (float)cameraAtmosphereAltitudeRatio, material, materialPropertyBlock);

            //The zero check prevents a weird Shadow flickering bug when the star is almost perpendicular to the surface
            SetFloatToMaterial("_SphericalRatio", sphericalRatio == 0.0f ? 0.00000000000000001f : sphericalRatio, material, materialPropertyBlock);
  
            SetFloatToMaterial("_Radius", radius, material, materialPropertyBlock);
  
            SetFloatToMaterial("_TileSizeLatitudeFactor", tileSizeLatitudeFactor, material, materialPropertyBlock);
        }

        protected virtual void ApplyAtmospherePropertiesToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, GeoAstroObject closestGeoAstroObject, Star star, Camera camera)
        {
            float atmosphereAlpha = 0.0f;

            if (closestGeoAstroObject != Disposable.NULL)
            {
                closestGeoAstroObject.IterateOverEffects<AtmosphereEffect>((effect) =>
                {
                    atmosphereAlpha = effect.isActiveAndEnabled && closestGeoAstroObject.ContainsInitializedTerraingGridMeshObject() && closestGeoAstroObject.IsSpherical() ? effect.GetAlpha() : 0.0f;

                    //TODO: Atmosphere shader needs to be optimized, most of these variables should be derived in the shader directly
                    if (atmosphereAlpha != 0.0f)
                    {
                        //Lowest land depression on Earth: 413m below sea level
                        //Lowest underwater point on Earth: 10971m below sea level
                        //Problem: If the camera dips below the geoAstroObjectRadius the atmopshere goes all white
                        //Fix: We add an offset to the geoAstroObjectRadius consisting of the lowest point we think the camera will have to go below sea level
                        //In this case we use a variable offset which goes from 0.0f to 10000.0d as we get closer to sea level
                        double geoAstroObjectRadius = closestGeoAstroObject.GetScaledRadius();
                        geoAstroObjectRadius -= cameraAtmosphereAltitudeRatio * (11000.0d * (geoAstroObjectRadius / EARTH_RADIUS));
                        double atmosphereRadius = AtmosphereEffect.ATMOPSHERE_ALTITUDE_FACTOR * geoAstroObjectRadius;

                        SetFloatToMaterial("_OuterRadius", (float)atmosphereRadius, material, materialPropertyBlock);
                        SetFloatToMaterial("_OuterRadius2", (float)atmosphereRadius * (float)atmosphereRadius, material, materialPropertyBlock);
                        SetFloatToMaterial("_InnerRadius", (float)geoAstroObjectRadius, material, materialPropertyBlock);
                        SetFloatToMaterial("_InnerRadius2", (float)geoAstroObjectRadius * (float)geoAstroObjectRadius, material, materialPropertyBlock);

                        Vector3 invWaveLength4 = new Vector3(1.0f / Mathf.Pow(effect.waveLength.r, 4.0f), 1.0f / Mathf.Pow(effect.waveLength.g, 4.0f), 1.0f / Mathf.Pow(effect.waveLength.b, 4.0f));
                        SetVectorToMaterial("_InvWavelength", invWaveLength4, material, materialPropertyBlock);

                        float atmosphereScale = (float)(1.0d / effect.GetAtmosphereAltitude());
                        SetFloatToMaterial("_Scale", atmosphereScale, material, materialPropertyBlock);
                        SetFloatToMaterial("_ScaleOverScaleDepth", atmosphereScale / effect.scaleDepth, material, materialPropertyBlock);
                        SetFloatToMaterial("_ScaleDepth", effect.scaleDepth, material, materialPropertyBlock);

                        float sunBrightness = GetSunBrightness(effect.sunBrightness * (star != Disposable.NULL ? star.intensity : 1.0f), (float)cameraAtmosphereAltitudeRatio);
                        SetFloatToMaterial("_KrESun", effect.rayleighScattering * sunBrightness, material, materialPropertyBlock);
                        SetFloatToMaterial("_KmESun", effect.mieScattering * sunBrightness, material, materialPropertyBlock);
                        SetFloatToMaterial("_Kr4PI", effect.rayleighScattering * 4.0f * Mathf.PI, material, materialPropertyBlock);
                        SetFloatToMaterial("_Km4PI", effect.mieScattering * 4.0f * Mathf.PI, material, materialPropertyBlock);
                        SetFloatToMaterial("_G", effect.miePhaseAsymmetryFactor, material, materialPropertyBlock);
                    }
                    return false;
                });
            }

            SetFloatToMaterial("_AtmosphereAlpha", GetAtmosphereAlpha(atmosphereAlpha, (float)cameraAtmosphereAltitudeRatio), material, materialPropertyBlock);

            if (atmosphereAlpha != 0.0f)
                material.EnableKeyword("ENABLE_ATMOSPHERE");
            else
                material.DisableKeyword("ENABLE_ATMOSPHERE");
        }

        protected virtual void ApplyAlphaToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, GeoAstroObject closestGeoAstroObject, Camera camera, float alpha)
        {
            SetFloatToMaterial("_Alpha", alpha, material, materialPropertyBlock);
        }

        protected void SetColorToMaterial(Color color, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            SetColorToMaterial("_BaseColor", color, material, materialPropertyBlock);
        }

        protected void SetFloatToMaterial(string name, float value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            material.SetFloat(name, value);
        }

        protected void SetIntToMaterial(string name, int value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            material.SetInt(name, value);
        }

        protected void SetColorToMaterial(string name, Color value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            material.SetColor(name, value);
        }

        protected void SetVectorToMaterial(string name, Vector4 value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            material.SetVector(name, value);
        }

        protected void SetVectorToMaterial(string name, Vector3 value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            material.SetVector(name, value);
        }

        protected void SetVectorToMaterial(string name, Vector2 value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            material.SetVector(name, value);
        }

        protected virtual void SetTextureToMaterial(string name, Texture value, Texture2D defaultTexture, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            SetTextureToMaterial(name, value != Disposable.NULL ? value.unityTexture : defaultTexture, material, materialPropertyBlock);
        }

        protected void SetTextureToMaterial(string name, UnityEngine.Texture value, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            if (value != null)
                material.SetTexture(name, value);
        }

        protected virtual float GetSunBrightness(float sunBrightness, float atmosphereAltitudeRatio)
        {
            return sunBrightness;
        }

        protected virtual float GetAtmosphereAlpha(float atmosphereAlpha, float atmosphereAltitudeRatio)
        {
            return atmosphereAlpha;
        }

        public GeoAstroObject GetClosestGeoAstroObject()
        {
            return parentGeoAstroObject != Disposable.NULL ? parentGeoAstroObject : GeoAstroObject.GetClosestGeoAstroObject(transform.position);
        }

        protected virtual void ApplyReflectionTextureToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, RTTCamera rttCamera, ScriptableRenderContext? context)
        {
           
        }

        /// <summary>
        /// Dispose all children visuals.
        /// </summary>
        /// <param name="destroyDelay"></param>
        /// <returns></returns>
        protected bool DisposeAllChildren(DisposeManager.DestroyDelay destroyDelay = DisposeManager.DestroyDelay.None)
        {
            if (initialized && transform != null)
            {
                for (int i = transform.transform.childCount - 1; i >= 0 ; i--)
                {
                    Transform child = transform.transform.GetChild(i);
                    if (child != null)
                        Dispose(child.gameObject, destroyDelay);
                }

                return true;
            }
            return false;
        }

        public override bool OnDisposing()
        {
            if (base.OnDisposing())
            {
                popupTween = null;

                return true;
            }
            return false;
        }

        public override bool OnDispose()
        {
            if (base.OnDispose())
            {
                //We dispose the visuals manualy in case only the Component is disposed and not the entire GameObject and its children
                //The delay is required during a Dispose of the Object since we do not know wether it is only the Component being disposed or the entire GameObject. 
                //If the Entire GameObject is being disposed in the Editor then some Destroy Undo operations will be registered by the Editor automaticaly. By the time the delayed dispose will be performed the children will already be Destroyed and we will not register additional Undo Operations
                DisposeAllChildren(DisposeManager.DestroyDelay.Delayed);

                return true;
            }
            return false;
        }
    }
}
