// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class UIBase : VisualObject
    {
        [BeginFoldout("Collider")]
        [SerializeField, Tooltip("When enabled "+nameof(UIVisual)+"'s will have collider component."), EndFoldout]
        private bool _useCollider;

        [BeginFoldout("UI")]
        [SerializeField, Tooltip("When enabled the UI will always face the Camera.")]
        private bool _screenSpace;
        [SerializeField, Tooltip("A multiplier used to alter the size of the UI."), EndFoldout]
        private float _scale;

#if UNITY_EDITOR
        protected override bool GetShowDontSaveVisualsToScene()
        {
            return false;
        }
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => useCollider = value, GetDefaultUseCollider(), initializingContext);
            InitValue(value => screenSpace = value, true, initializingContext);
            InitValue(value => scale = value, 2.0f, initializingContext);
        }

        protected override void VisibleCamerasChanged()
        {
            base.VisibleCamerasChanged();

            meshRendererVisualDirtyFlags.Recreate();
        }

        protected virtual Type GetVisualType()
        {
            return null;
        }

        protected virtual bool GetDefaultUseCollider()
        {
            return true;
        }

#if UNITY_EDITOR
        protected UnityEngine.Object[] GetUIVisualsAdditionalRecordObjects()
        {
            List<UnityEngine.Object> uiVisuals = new List<UnityEngine.Object>();

            transform.IterateOverChildren<UIVisual>((uiVisual) =>
            {
                uiVisuals.Add(uiVisual);

                return true;
            });

            return uiVisuals.ToArray();
        }

        protected override UnityEngine.Object[] GetAlphaAdditionalRecordObjects()
        {
            return GetUIVisualsAdditionalRecordObjects();
        }
#endif

        protected override bool GetDefaultDontSaveVisualsToScene()
        {
            return true;
        }

        /// <summary>
        /// When enabled <see cref="DepictionEngine.UIVisual"/>'s will have collider component.
        /// </summary>
        [Json]
        public bool useCollider
        {
            get { return _useCollider; }
            set { SetValue(nameof(useCollider), value, ref _useCollider); }
        }

        /// <summary>
        /// When enabled the UI will always face the Camera.
        /// </summary>
        [Json]
        public bool screenSpace
        {
            get { return _screenSpace; }
            set { SetValue(nameof(screenSpace), value, ref _screenSpace); }
        }

        /// <summary>
        /// A multiplier used to alter the size of the UI.
        /// </summary>
        [Json]
        public float scale
        {
            get { return _scale; }
            set { SetValue(nameof(scale), value < 0.001f ? 0.001f : value, ref _scale); }
        }

        protected override Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(UIBaseVisualDirtyFlags);
        }

        protected override void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            meshRendererVisualDirtyFlags.colliderType = useCollider ? MeshRendererVisual.ColliderType.Box : MeshRendererVisual.ColliderType.None;

            if (meshRendererVisualDirtyFlags is UIBaseVisualDirtyFlags)
            {
                UIBaseVisualDirtyFlags uiMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as UIBaseVisualDirtyFlags;

                uiMeshRendererVisualDirtyFlags.SetCameraProperties(screenSpace, instanceManager.camerasInstanceIds);
            }
        }

        protected override void UpdateMeshRendererVisuals(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.UpdateMeshRendererVisuals(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags.isDirty && meshRendererVisualDirtyFlags is UIBaseVisualDirtyFlags)
            {
                UIBaseVisualDirtyFlags uiMeshRendererVisualDirtyFlags = meshRendererVisualDirtyFlags as UIBaseVisualDirtyFlags;
                UIVisual uiVisual = null;
                instanceManager.IterateOverInstances<Camera>(
                    (camera) =>
                    {
                        if (!CameraIsMasked(camera))
                        {
                            if (uiVisual == Disposable.NULL || uiMeshRendererVisualDirtyFlags.screenSpace)
                            {
                                uiVisual = CreateUIVisual(typeof(UIVisual), uiMeshRendererVisualDirtyFlags.colliderType != MeshRendererVisual.ColliderType.None);
                                uiVisual.name += uiMeshRendererVisualDirtyFlags.screenSpace ? "(" + camera.gameObject.name + ")" : "(All)";
#if UNITY_EDITOR
                                //We dont want to see the MainCamera colliders in the SceneCamera
                                if (screenSpace && !(camera is Editor.SceneCamera))
                                    Editor.SceneVisibilityManagerExtension.SetGameObjectHiddenNoUndoViaReflection(uiVisual.gameObject, true, true);
#endif
                            }
                            uiVisual.AddCamera(camera);
                        }

                        return true;
                    });
            }
        }

        protected virtual UIVisual CreateUIVisual(Type visualType, bool useCollider)
        {
            return CreateVisual(visualType, visualType.Name) as UIVisual;
        }

        public Vector3 GetScreenPosition()
        {
            Camera camera = Camera.main;
            return camera != Disposable.NULL ? camera.WorldToViewportPoint(transform.position) : Vector3.negativeInfinity;
        }

        public Vector3 GetScreenPosition(Camera camera)
        {
            return camera.WorldToViewportPoint(transform.position);
        }

        protected double GetDistanceScaleForCamera(Camera camera)
        {
            double correctedScale = scale;

            if (screenSpace)
                correctedScale *= camera.GetDistanceScaleForCamera(gameObject.transform.position);

            return correctedScale;
        }

        public Quaternion GetUIVisualLocalRotation(Camera camera)
        {
            return screenSpace ? QuaternionDouble.Inverse(transform.rotation) * camera.transform.rotation : QuaternionDouble.identity;
        }

        protected Vector3Double GetUILocalScale(Camera camera)
        {
            Vector3Double localScale = (Vector3Double.one / transform.lossyScale) * (scale == 0.0f ? double.Epsilon : GetDistanceScaleForCamera(camera));

            if (double.IsNaN(localScale.x) || double.IsInfinity(localScale.x))
                localScale.x = 0.0d;
            if (double.IsNaN(localScale.y) || double.IsInfinity(localScale.y))
                localScale.y = 0.0d;
            if (double.IsNaN(localScale.z) || double.IsInfinity(localScale.z))
                localScale.z = 0.0d;

            return localScale;
        }

        public UIVisual GetVisualForCamera(Camera camera)
        {
            UIVisual matchingUIVisual = null;

            transform.IterateOverChildren<UIVisual>((uiVisual) =>
            {
                if (uiVisual.cameras.Contains(camera.GetInstanceID()))
                {
                    matchingUIVisual = uiVisual;
                    return false;
                }
                return true;
            });

            return matchingUIVisual;
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                transform.IterateOverChildren<UIVisual>((uiVisual) =>
                {
                    if (!uiVisual.IsIgnoredInRender())
                    {
                        Vector3Double localScale = GetUILocalScale(camera);
                        uiVisual.transform.localScale = new Vector3((float)localScale.x, (float)localScale.y * GetPopupT(PopupType.Scale), (float)localScale.z);

                        uiVisual.transform.localRotation = GetUIVisualLocalRotation(camera);
                    }
                    return true;
                });

                return true;
            }
            return false;
        }

        protected override bool GetVisualIgnoreRender(Visual visual, Camera camera, bool afterRendering)
        {
            if (!base.GetVisualIgnoreRender(visual, camera, afterRendering))
            {
                if (visual is UIVisual)
                {
                    UIVisual uiVisual = visual as UIVisual;
                    if (uiVisual.cameras.Contains(camera.GetInstanceID()))
                    {
                        //Leave the layer to 'Ignore Render' for Visuals that are are not in the current window to prevent them from dispatching mouse events and to hide the outline in the Editor
                        Camera currentCamera = cameraManager.GetMouseOverWindowCamera();

#if UNITY_EDITOR
                        if (!Application.isPlaying)
                            currentCamera = Editor.SceneViewDouble.lastActiveSceneViewDouble != Disposable.NULL ? Editor.SceneViewDouble.lastActiveSceneViewDouble.camera : null;
#endif

                        if (!afterRendering || camera == currentCamera)
                            return false;
                    }
                }
                else
                    return false;
            }
            return true;
        }

        protected override void ApplyClosestGeoAstroObjectPropertiesToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, GeoAstroObject closestGeoAstroObject, Star star, Camera camera)
        {
        }

        protected override void ApplyAtmospherePropertiesToMaterial(Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, GeoAstroObject closestGeoAstroObject, Star star, Camera camera)
        {
        }

        protected void ApplyFontsToMaterials(Material material, MaterialPropertyBlock materialPropertyBlock)
        {
            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager != Disposable.NULL)
            {
                foreach (Font font in renderingManager.fonts)
                    SetTextureToMaterial("_" + font.name, font.material.mainTexture, material, materialPropertyBlock);
            }
        }

        protected void UpdateMeshRendererVisualCollider(MeshRendererVisual meshRendererVisual, bool visualsChanged)
        {
            UpdateMeshRendererVisualCollider(meshRendererVisual, MeshRendererVisual.ColliderType.Box, true, visualsChanged);
        }
    }
}
