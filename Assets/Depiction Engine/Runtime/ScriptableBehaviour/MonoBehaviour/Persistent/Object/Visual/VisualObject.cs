// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    /// <summary>
    /// An origin shifted container for MeshRenderers.
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

#if UNITY_EDITOR
        [BeginFoldout("Mesh Renderer")]
        [SerializeField, Button(nameof(UpdateMeshRendererBtn)), Tooltip("Automatically add all the child MeshRenderer(s) so the VisualObject can manage them. When managed, the MeshRenderer(s) material(s) fields will be automatically updated by the VisualObject."), EndFoldout]
        [ConditionalShow(nameof(GetShowUpdateMeshRenderer))]
        private bool _updateMeshRenderer;
#endif

        [BeginFoldout("Material")]
        [SerializeField, Tooltip("When enabled, allow the child Material(s) to have their field values overridden.")]
        private bool _overrideMaterialFields;
        [SerializeField, Tooltip("A color value that will override the '_BaseColor' field of child Material(s).")]
        private Color _color;
        [SerializeField, Tooltip("A color value that will override the '_SpecColor' field of child Materials. Specular determines the appearance of reflections on the surface.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowMaterialProperties))]
#endif
        private Color _specular;
        [SerializeField, Range(0.0f, 1.0f), Tooltip("A value that will override the '_Smoothness' field of child Materials. Smoothness Controls the spread of highlights and reflections on the surface."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowMaterialProperties))]
#endif
        private float _smoothness;

        [SerializeField, HideInInspector]
        private List<MeshRenderer> _managedMeshRenderers;

        private static MaterialPropertyBlock _materialPropertyBlock;

#if UNITY_EDITOR
        protected virtual bool GetShowMaterialProperties()
        {
            return true;
        }

        protected virtual bool GetShowUpdateMeshRenderer()
        {
            return true;
        }

        private void UpdateMeshRendererBtn()
        {
            UpdateChildMeshRenderer(out int added, out int removed);

            UnityEditor.EditorUtility.DisplayDialog("Updated", "Found a total of " + managedMeshRenderers.Count + " MeshRenderer(s).                Added: " + added + "                                                Removed: " + removed, "OK");
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            _managedMeshRenderers?.Clear();
            _materialPropertyBlock?.Clear();
        }

        protected void RecycleMaterial(Material material)
        {
            if (material != null)
                OverrideMaterialFields(material, null, Color.white, new Color(0.5943396f, 0.5257822f, 0.4125133f), 0.5f);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastColor = color;
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                if (managedMeshRenderers.Count != 0)
                    UpdateChildMeshRenderer(out int added, out int removed);
            }

            if (initializingContext == InitializationContext.Existing)
                RemoveNullManagedMeshRenderers();

            InitValue(value => overrideMaterialFields = value, GetDefaultOverrideMaterialFields(), initializingContext);
            InitValue(value => color = value, GetDefaultColor(), initializingContext);
            InitValue(value => specular = value, new Color(0.5943396f, 0.5257822f, 0.4125133f), initializingContext);
            InitValue(value => smoothness = value, GetDefaultSmoothness(), initializingContext);
        }

        protected virtual bool GetDefaultOverrideMaterialFields()
        {
            return false;
        }

        protected virtual string GetDefaultShaderPath()
        {
            return RenderingManager.SHADER_BASE_PATH;
        }

        protected virtual Color GetDefaultColor()
        {
            return new Color(1.0f, 1.0f, 1.0f, 1.0f);
        }

        protected virtual float GetDefaultSmoothness()
        {
            return 1.0f;
        }

        private void RemoveNullManagedMeshRenderers()
        {
            if (_managedMeshRenderers != null)
            {
#if UNITY_EDITOR
                SerializationUtility.RecoverLostReferencedObjectsInCollection(_managedMeshRenderers);
#endif
                for (int i = _managedMeshRenderers.Count - 1; i >= 0; i--)
                {
                    if (_managedMeshRenderers[i] == null)
                        _managedMeshRenderers.RemoveAt(i);
                }
            }
        }

#if UNITY_EDITOR
        private Color _lastColor;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                RemoveNullManagedMeshRenderers();

                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { color = value; }, ref _color, ref _lastColor);

                return true;
            }
            return false;
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

        protected List<MeshRenderer> managedMeshRenderers
        {
            get { _managedMeshRenderers ??= new List<MeshRenderer>(); return _managedMeshRenderers; }
            set => _managedMeshRenderers = value;
        }

        private MaterialPropertyBlock materialPropertyBlock
        {
            get { _materialPropertyBlock ??= new MaterialPropertyBlock(); return _materialPropertyBlock; }
        }

        protected virtual void DontSaveVisualsToSceneChanged(bool newValue, bool oldValue)
        {

        }

        /// <summary>
        /// When enabled, allow the child Material(s) to have their field values overridden.
        /// </summary>
        [Json]
        public bool overrideMaterialFields
        {
            get => _overrideMaterialFields;
            set => SetValue(nameof(overrideMaterialFields), value, ref _overrideMaterialFields);
        }

#if UNITY_EDITOR
        protected virtual UnityEngine.Object[] GetColorAdditionalRecordObjects()
        {
            return null;
        }
#endif

        /// <summary>
        /// A color value that will override the '_BaseColor' field of child Materials.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetColorAdditionalRecordObjects))]
#endif
        public Color color
        {
            get => _color;
            set => SetValue(nameof(color), value, ref _color, (newValue, oldValue) => { ColorChanged(newValue, oldValue); });
        }

        protected virtual void ColorChanged(Color newValue, Color oldValue)
        {
#if UNITY_EDITOR
            _lastColor = newValue;
#endif
        }

        /// <summary>
        /// A color value that will override the '_SpecColor' field of child Materials. Specular determines the appearance of reflections on the surface.
        /// </summary>
        [Json]
        public Color specular
        {
            get => _specular;
            set => SetValue(nameof(specular), value, ref _specular);
        }

        /// <summary>
        /// A value that will override the '_Smoothness' field of child Materials. Smoothness Controls the spread of highlights and reflections on the surface.
        /// </summary>
        [Json]
        public float smoothness
        {
            get => _smoothness;
            set => SetValue(nameof(smoothness), Mathf.Clamp01(value), ref _smoothness);
        }

        /// <summary>
        /// Automatically add all the child <see cref="UnityEngine.MeshRenderer"/>(s) so the <see cref="DepictionEngine.VisualObject"/> can manage them. When managed, the <see cref="UnityEngine.MeshRenderer"/>(s) <see cref="UnityEngine.Material"/>(s) fields will be automatically updated by the <see cref="DepictionEngine.VisualObject"/>.
        /// </summary>
        /// <param name="added">The number of added <see cref="UnityEngine.MeshRenderer"/>(s)</param>
        /// <param name="removed">The number of removed <see cref="UnityEngine.MeshRenderer"/>(s)</param>
        public void UpdateChildMeshRenderer(out int added, out int removed)
        {
            removed = 0;
            added = 0;

            List<MeshRenderer> newManagedMeshRenderers = new();
            gameObject.GetComponentsInChildren(true, newManagedMeshRenderers);
            for (int i = managedMeshRenderers.Count - 1; i >= 0; i--)
            {
                MeshRenderer meshRenderer = managedMeshRenderers[i];
                if (!newManagedMeshRenderers.Contains(meshRenderer) && RemoveMeshRenderer(meshRenderer))
                    removed++;
            }

            foreach (MeshRenderer meshRenderer in newManagedMeshRenderers)
            {
                if (!managedMeshRenderers.Contains(meshRenderer))
                {
                    AddMeshRenderer(meshRenderer);
                    added++;
                }
            }
        }

        /// <summary>
        /// Add the <see cref="UnityEngine.MeshRenderer"/> so the <see cref="DepictionEngine.VisualObject"/> can manage it. When managed the <see cref="UnityEngine.MeshRenderer"/> <see cref="UnityEngine.Material"/>(s) fields will be automatically updated by the <see cref="DepictionEngine.VisualObject"/>.
        /// </summary>
        /// <param name="meshRenderer"></param>
        public void AddMeshRenderer(MeshRenderer meshRenderer)
        {
            if (!managedMeshRenderers.Contains(meshRenderer))
                managedMeshRenderers.Add(meshRenderer);
        }

        /// <summary>
        /// Remove the <see cref="UnityEngine.MeshRenderer"/> from the managed list.
        /// </summary>
        /// <param name="meshRenderer"></param>
        /// <returns>True if successfully removed.</returns>
        public bool RemoveMeshRenderer(MeshRenderer meshRenderer)
        {
            bool removed = managedMeshRenderers.Remove(meshRenderer);

            try
            {
                if (meshRenderer != null)
                    meshRenderer.SetPropertyBlock(null);
            } catch (MissingReferenceException)
            { }

            return removed;
        }

        public void IterateOverManagedMeshRenderer(Action<MaterialPropertyBlock, MeshRenderer> callback)
        {
            foreach (MeshRenderer meshRenderer in managedMeshRenderers)
            {
                if (meshRenderer != null)
                {
                    MaterialPropertyBlock materialPropertyBlock = null;

                    //materialPropertyBlock = this.materialPropertyBlock;

                    //if (materialPropertyBlock != null)
                    //    meshRenderer.GetPropertyBlock(materialPropertyBlock);

                    callback(materialPropertyBlock, meshRenderer);

                    //if (materialPropertyBlock != null)
                    //    meshRenderer.SetPropertyBlock(materialPropertyBlock);
                }
            }
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                if (gameObject.activeInHierarchy)
                {
                    Star star = null;

                    InstanceManager instanceManager = InstanceManager.Instance(false);
                    if (instanceManager != null)
                        star = instanceManager.GetStar();

                    GeoAstroObject closestGeoAstroObject = GetClosestGeoAstroObject();

                    Vector3Double cameraPosition = camera.transform.position;

                    double cameraAtmosphereAltitudeRatio = 0.0d;
                    if (closestGeoAstroObject != Disposable.NULL)
                    {
                        double atmosphereThickness = closestGeoAstroObject.GetScaledAtmosphereThickness();
                        cameraAtmosphereAltitudeRatio = closestGeoAstroObject.GetAtmosphereAltitudeRatio(atmosphereThickness, cameraPosition);
                    }

                    IterateOverManagedMeshRenderer((materialPropertyBlock, meshRenderer) =>
                    {
                        if (meshRenderer != null && meshRenderer.gameObject.activeInHierarchy)
                        {
                            InitializeMaterial(meshRenderer, meshRenderer.sharedMaterial);

                            if (meshRenderer.sharedMaterial != null)
                                ApplyPropertiesToMaterial(meshRenderer, meshRenderer.sharedMaterial, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);
                        }
                    });
                }

                return true;
            }
            return false;
        }

        public void UpdateReflectionEffect(RTTCamera rttCamera, Camera camera, ScriptableRenderContext? context)
        {
            if (gameObject.activeInHierarchy && managedMeshRenderers.Count > 0)
            {
                GeoAstroObject closestGeoAstroObject = GetClosestGeoAstroObject();

                Vector3Double cameraPosition = camera.transform.position;

                double cameraAtmosphereAltitudeRatio = 0.0d;
                if (closestGeoAstroObject != Disposable.NULL)
                {
                    double atmosphereThickness = closestGeoAstroObject.GetScaledAtmosphereThickness();
                    cameraAtmosphereAltitudeRatio = closestGeoAstroObject.GetAtmosphereAltitudeRatio(atmosphereThickness, cameraPosition);
                }

                IterateOverManagedMeshRenderer((materialPropertyBlock, meshRenderer) =>
                {
                    if (meshRenderer.gameObject.activeInHierarchy)
                    {
                        foreach (Material material in meshRenderer.sharedMaterials)
                            ApplyReflectionTextureToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, rttCamera, context);
                    }
                });
            }
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

        public int managedMeshRendererCount
        {
            get => managedMeshRenderers.Count;
        }

        public virtual float GetCurrentAlpha()
        {
            return color.a;
        }

        protected bool GetEnableAlphaClip()
        {
            return RenderingManager.GetEnableAlphaClip();
        }

        protected virtual bool GetVisualIgnoreRender(Visual visual, Camera camera, bool afterRendering)
        {
            return !afterRendering && CameraIsMasked(camera);
        }

        protected Vector3Double GetClosestGeoAstroObjectCenterOS(GeoAstroObject closestGeoAstroObject)
        {
            return transform.InverseTransformPoint(closestGeoAstroObject.transform.position) / GetMeshRendererVisualLocalScale();
        }

        protected virtual Vector3 GetMeshRendererVisualLocalScale()
        {
            return Vector3.one;
        }

        public void ApplyCameraMaskLayerToVisuals(Camera camera = null, bool afterRendering = false)
        {
            transform.IterateOverChildren<Visual>((visual) =>
            {
                visual.UpdateLayer(GetVisualIgnoreRender(visual, camera, afterRendering));
                return true;
            });
        }

        protected MeshRendererVisual CreateMeshRendererVisual(Type type, string name = null, Transform parent = null)
        {
            return typeof(MeshRendererVisual).IsAssignableFrom(type) ? CreateVisual(type, name, parent) as MeshRendererVisual : null;
        }

        protected Visual CreateVisual(Type type, string name = null, Transform parent = null, List<PropertyModifier> propertyModifiers = null)
        {
            return typeof(Visual).IsAssignableFrom(type) ? CreateChild(type, name, parent, InitializationContext.Programmatically, propertyModifiers) as Visual : null;
        }

        protected Visual GetVisual(string name)
        {
            Visual matchingVisual = null;
            transform.IterateOverChildren<Visual>((visual) =>
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

        protected Visual GetVisual(int index)
        {
            Visual visual = null;
            if (index < transform.children.Count)
                visual = transform.children[index] as Visual;
            return visual;
        }

        protected override bool DisposeAllChildren(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.DisposeAllChildren(disposeContext))
            {
                managedMeshRenderers.Clear();
                return true;
            }
            return false;
        }

        protected virtual void InitializeMaterial(MeshRenderer meshRendererVisual, Material material = null)
        {
            meshRendererVisual.sharedMaterial = material;
        }

        protected Material UpdateMaterial(ref Material material, string shaderPath)
        {
            Shader shader = RenderingManager.LoadShader(shaderPath);
            if (material == null || material.shader != shader)
            {
                DisposeManager.Dispose(material);
                material = InstantiateMaterial(shader);
            }
            return material;
        }

        protected Material InstantiateMaterial(Shader shader)
        {
            return shader != null ? new Material(shader) : null;
        }

        protected virtual void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            if (material != null)
            {
                //Alpha
                float alpha = GetCurrentAlpha();
                ApplyAlphaToMaterial(material, materialPropertyBlock, closestGeoAstroObject, camera, alpha);

                if (GetEnableAlphaClip())
                    material.EnableKeyword("ENABLE_ALPHA_CLIP");
                else
                    material.DisableKeyword("ENABLE_ALPHA_CLIP");

                //General
                if (overrideMaterialFields)
                    OverrideMaterialFields(material, materialPropertyBlock, GetColor(meshRenderer, alpha), specular, smoothness);

                //Closest GeoAstroObject
                ApplyClosestGeoAstroObjectPropertiesToMaterial(material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, closestGeoAstroObject, star, camera);

                //Atmosphere
                ApplyAtmospherePropertiesToMaterial(material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, closestGeoAstroObject, star, camera);
            }
        }

        protected void OverrideMaterialFields(Material material, MaterialPropertyBlock materialPropertyBlock, Color color, Color specular, float smoothness)
        {
            SetColorToMaterial("_BaseColor", color, material, materialPropertyBlock);
            SetColorToMaterial("_SpecColor", specular, material, materialPropertyBlock);
            SetFloatToMaterial("_Smoothness", smoothness, material, materialPropertyBlock);
        }

        protected virtual void ApplyAlphaToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, GeoAstroObject closestGeoAstroObject, Camera camera, float alpha)
        {
            
        }

        protected virtual Color GetColor(MeshRenderer meshRenderer, float alpha)
        {
            Color currentColor = color;
            
            currentColor.a = alpha;

            return currentColor;
        }

        protected virtual double GetAltitude(bool addOffset = true)
        {
            return 0.0d;
        }

        private RayDouble _shadowRay;
        protected virtual void ApplyClosestGeoAstroObjectPropertiesToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, GeoAstroObject closestGeoAstroObject, Star star, Camera camera)
        {
            Vector3 meshRendererVisualLocalScale = GetMeshRendererVisualLocalScale();

            Vector3Double closestGeoAstroObjectSurfacePointWS = Vector3Double.zero;
            Vector3Double closestGeoAstroObjectCenterOS = Vector3Double.zero;
            Vector3Double closestGeoAstroObjectCenterWS = Vector3Double.zero;

            Vector3Double shadowPositionWS = new(0.0d, -10000000000000000.0d, 0.0d);
            QuaternionDouble shadowDirectionWS = QuaternionDouble.identity;
            float shadowAttenuationDistance = 100000000000.0f;

            float tileSizeLatitudeFactor = 1.0f;

            float radius = 0.0f;

            bool closestGeoAstroObjectIsNotNull = closestGeoAstroObject != Disposable.NULL;

            if (closestGeoAstroObjectIsNotNull && closestGeoAstroObject.IsValidSphericalRatio())
            {
                if (closestGeoAstroObject.IsSpherical())
                    tileSizeLatitudeFactor = (float)(1.0d / Math.Cos(MathPlus.DEG2RAD * transform.GetGeoCoordinate().latitude));

                radius = (float)closestGeoAstroObject.GetScaledRadius();

                closestGeoAstroObjectSurfacePointWS = closestGeoAstroObject.GetSurfacePointFromPoint(transform.position);
                closestGeoAstroObjectCenterOS = GetClosestGeoAstroObjectCenterOS(closestGeoAstroObject);

                closestGeoAstroObjectCenterWS = closestGeoAstroObject.transform.position;

                if (star != Disposable.NULL)
                {
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

                    _shadowRay ??= new RayDouble();
                    _shadowRay.Init(starPosition, (edgePosition - starPosition).normalized);

                    if (Vector3Double.Dot(upVector, forwardVector) <= 0.0d && MathGeometry.LinePlaneIntersection(out Vector3Double intersection, surfacePoint, QuaternionDouble.LookRotation(upVector, (surfacePoint - starPosition).normalized) * QuaternionDouble.Euler(90.0d, 0.0d, 0.0d) * Vector3Double.forward, _shadowRay, false))
                    {
                        shadowPositionWS = intersection;
                        shadowDirectionWS = closestGeoAstroObject.GetUpVectorFromPoint(edgePosition);
                    }
                }
            }

            SetVectorToMaterial("_ClosestGeoAstroObjectCameraSurfaceWS", TransformDouble.SubtractOrigin(closestGeoAstroObject != Disposable.NULL ? closestGeoAstroObject.GetSurfacePointFromPoint(camera.transform.position) : camera.transform.position), material, materialPropertyBlock);
            SetVectorToMaterial("_ClosestGeoAstroObjectSurfacePointWS", TransformDouble.SubtractOrigin(closestGeoAstroObjectSurfacePointWS), material, materialPropertyBlock);
            SetVectorToMaterial("_ClosestGeoAstroObjectCenterOS", closestGeoAstroObjectCenterOS, material, materialPropertyBlock);
            SetVectorToMaterial("_ClosestGeoAstroObjectCenterWS", TransformDouble.SubtractOrigin(closestGeoAstroObjectCenterWS), material, materialPropertyBlock);

            Vector3Double mainLightPositionWS = star != Disposable.NULL ? star.transform.position : Vector3Double.zero;

            SetVectorToMaterial("_MainLightPositionWS", TransformDouble.SubtractOrigin(mainLightPositionWS), material, materialPropertyBlock);
            SetVectorToMaterial("_ShadowPositionWS", TransformDouble.SubtractOrigin(shadowPositionWS), material, materialPropertyBlock);
            SetVectorToMaterial("_ShadowDirectionQuaternionWS", new Vector4((float)shadowDirectionWS.x, (float)shadowDirectionWS.y, (float)shadowDirectionWS.z, (float)shadowDirectionWS.w), material, materialPropertyBlock);
            SetFloatToMaterial("_ShadowAttenuationDistance", (float)shadowAttenuationDistance, material, materialPropertyBlock);
           
            SetFloatToMaterial("_CameraAtmosphereAltitudeRatio", (float)cameraAtmosphereAltitudeRatio, material, materialPropertyBlock);

            //sphericalRatio == 0.0f ? 0.00000000000000001f : sphericalRatio
            SetFloatToMaterial("_SphericalRatio", closestGeoAstroObjectIsNotNull ? closestGeoAstroObject.GetSphericalRatio() : 0.0f, material, materialPropertyBlock);
  
            SetFloatToMaterial("_RadiusWS", radius, material, materialPropertyBlock);
            SetFloatToMaterial("_RadiusOS", radius / meshRendererVisualLocalScale.y, material, materialPropertyBlock);

            SetFloatToMaterial("_AltitudeOS", (float)GetAltitude(false) / meshRendererVisualLocalScale.y, material, materialPropertyBlock);

            SetFloatToMaterial("_TileSizeLatitudeFactor", tileSizeLatitudeFactor, material, materialPropertyBlock);
        }

        protected virtual void ApplyAtmospherePropertiesToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, GeoAstroObject closestGeoAstroObject, Star star, Camera camera)
        {
            float atmosphereAlpha = 0.0f;

            if (closestGeoAstroObject != Disposable.NULL)
            {
                closestGeoAstroObject.IterateOverEffects<AtmosphereEffect>((effect) =>
                {
                    atmosphereAlpha = effect.isActiveAndEnabled && closestGeoAstroObject.ContainsInitializedTerrainGridMeshObject() && closestGeoAstroObject.IsSpherical() ? effect.GetAlpha() : 0.0f;

                    //TODO: Atmosphere shader needs to be optimized, most of these variables should be derived in the shader directly
                    if (atmosphereAlpha != 0.0f)
                    {
                        //Lowest land depression on Earth: 413m below sea level
                        //Lowest underwater point on Earth: 10971m below sea level
                        //Problem: If the camera dips below the geoAstroObjectRadius the atmosphere goes all white
                        //Fix: We add an offset to the geoAstroObjectRadius consisting of the lowest point we think the camera will have to go below sea level
                        //In this case we use a variable offset which goes from 0.0f to 10000.0d as we get closer to sea level
                        double geoAstroObjectRadius = closestGeoAstroObject.GetScaledRadius();
                        geoAstroObjectRadius -= cameraAtmosphereAltitudeRatio * (11000.0f * (geoAstroObjectRadius / EARTH_RADIUS));
                        
                        float outerRadius = (float)(AtmosphereEffect.ATMOPSHERE_ALTITUDE_FACTOR * geoAstroObjectRadius);
                        SetFloatToMaterial("_OuterRadius", outerRadius, material, materialPropertyBlock);
                        SetFloatToMaterial("_OuterRadius2", outerRadius * outerRadius, material, materialPropertyBlock);

                        float innerRadius = (float)geoAstroObjectRadius;
                        SetFloatToMaterial("_InnerRadius", innerRadius, material, materialPropertyBlock);
                        SetFloatToMaterial("_InnerRadius2", innerRadius * innerRadius, material, materialPropertyBlock);

                        Vector3 invWaveLength4 = new(1.0f / Mathf.Pow(effect.waveLength.r, 4.0f), 1.0f / Mathf.Pow(effect.waveLength.g, 4.0f), 1.0f / Mathf.Pow(effect.waveLength.b, 4.0f));
                        SetVectorToMaterial("_InvWavelength", invWaveLength4, material, materialPropertyBlock);

                        float scale = (float)(1.0d / effect.GetAtmosphereAltitude());
                        SetFloatToMaterial("_Scale", scale, material, materialPropertyBlock);

                        SetFloatToMaterial("_ScaleDepth", effect.scaleDepth, material, materialPropertyBlock);

                        float sunBrightness = GetSunBrightness(effect.sunBrightness * (star != Disposable.NULL ? star.intensity : 1.0f), (float)cameraAtmosphereAltitudeRatio);
                        SetFloatToMaterial("_KrESun", effect.rayleighScattering * sunBrightness, material, materialPropertyBlock);
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

        protected void SetMatrixToMaterial(string name, Matrix4x4 value, Material material, MaterialPropertyBlock _)
        {
            material.SetMatrix(name, value);
        }

        protected void SetFloatToMaterial(string name, float value, Material material, MaterialPropertyBlock _)
        {
            material.SetFloat(name, value);
        }

        protected void SetIntToMaterial(string name, int value, Material material, MaterialPropertyBlock _)
        {
            material.SetInt(name, value);
        }

        protected void SetColorToMaterial(string name, Color value, Material material, MaterialPropertyBlock _)
        {
            material.SetColor(name, value);
        }

        protected void SetVectorToMaterial(string name, Vector4 value, Material material, MaterialPropertyBlock _)
        {
            material.SetVector(name, value);
        }

        protected void SetVectorToMaterial(string name, Vector3 value, Material material, MaterialPropertyBlock _)
        {
            material.SetVector(name, value);
        }

        protected void SetVectorToMaterial(string name, Vector2 value, Material material, MaterialPropertyBlock _)
        {
            material.SetVector(name, value);
        }

        protected virtual void SetTextureToMaterial(string name, Texture value, Texture2D defaultTexture, Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            SetTextureToMaterial(name, value != Disposable.NULL ? value.unityTexture : defaultTexture, material, materialPropertyBlock);
        }

        protected void SetTextureToMaterial(string name, UnityEngine.Texture value, Material material, MaterialPropertyBlock _)
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

        protected override void GetReflectionProbes(ref List<ReflectionProbe> managedReflectionProbes)
        {
            base.GetReflectionProbes(ref managedReflectionProbes);

            ReflectionProbe[] reflectionProbes = gameObject.GetComponentsInChildren<ReflectionProbe>(true);
            foreach (ReflectionProbe reflectionProbe in reflectionProbes)
            {
                if (!managedReflectionProbes.Contains(reflectionProbe))
                    managedReflectionProbes.Add(reflectionProbe);
            }
        }
    }
}
