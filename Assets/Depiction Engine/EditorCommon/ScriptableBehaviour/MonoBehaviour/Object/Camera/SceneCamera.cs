// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DepictionEngine.Editor
{
    public class SceneCamera : Camera
    {
        protected override bool InitializeStack(InitializationContext initializingContext)
        {
            return false;
        }

        protected override void InitializeAdditionalData()
        {
        }

        public override void UpdateDependencies()
        {
            base.UpdateDependencies();

            InitSceneCameraController();
        }

        private void InitSceneCameraController()
        {
            if (SceneManager.sceneClosing || IsDisposing())
                return;

            //Recreate the SceneCameraTarget if it was destroyed along with the parent GeoAstroObject
            if (sceneCameraController == Disposable.NULL)
                AddComponent<SceneCameraController>();
        }

        protected override void InitializeToTopInInspector()
        {
            
        }

        protected override void CreateAndInitializeDependencies(InitializationContext initializingContext)
        {
            base.CreateAndInitializeDependencies(initializingContext);

            UpdateShowImageEffects();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                SceneManager.SceneClosingEvent -= SceneClosing;
                if (!IsDisposing())
                    SceneManager.SceneClosingEvent += SceneClosing;

                return true;
            }
            return false;
        }

        public override UniversalAdditionalCameraData GetUniversalAdditionalCameraData()
        {
            return null;
        }

        public override int GetMainStackCount()
        {
            return GetDefaultDistancePass();
        }

        private void SceneClosing()
        {
            ControllerBase controller = this.controller;
            SceneCamera sceneCamera = this;
            TransformDouble transform = this.transform;

            DisposeManager.Destroy(controller);
            DisposeManager.Destroy(sceneCamera);
            DisposeManager.Destroy(transform);
        }

        public override int GetDefaultDistancePass()
        {
            return cameraManager.distancePass;
        }

        public SceneCameraController sceneCameraController
        {
            get => controller as SceneCameraController;
        }

        protected override bool SetController(ControllerBase value)
        {
            if (base.SetController(value))
            {
                InitSceneCameraController();

                return true;
            }
            return false;
        }

        //Used for debugging when UpdateHideFlags() is commented out
        protected override bool GetDefaultIsHiddenInHierarchy()
        {
            return false;
        }

        protected override bool UpdateHideFlags()
        {
            return false;
        }

        private int _instanceId;
        public override int GetCameraInstanceID()
        {
            if (_instanceId == 0)
            {
                //Problem: SceneCameras are Destroyed and Recreated between compilation.
                //Fix: To Maintain continuity we use the sceneView InstanceID instead which is not Destroyed and Recreated
                SceneViewDouble sceneViewDouble = SceneViewDouble.GetSceneViewDouble(this);
                if (sceneViewDouble != Disposable.NULL)
                    _instanceId = sceneViewDouble.GetInstanceID();
            }
            return _instanceId;
        }

        protected override void PostProcessingChanged()
        {
            base.PostProcessingChanged();

            UpdateShowImageEffects();
        }

        private void UpdateShowImageEffects()
        {
            SceneView sceneView = SceneViewDouble.GetSceneView(this);
            if (sceneView != null)
            {
                SceneView.SceneViewState sceneViewState = sceneView.sceneViewState;
                sceneViewState.showImageEffects = postProcessing;
                sceneView.sceneViewState = sceneViewState;
            }
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                skybox.enabled = true;

                return true;
            }
            return false;
        }

        protected override void ApplyCullingMaskPropertyToUnityCamera(UnityEngine.Camera unityCamera)
        {
            base.ApplyCullingMaskPropertyToUnityCamera(unityCamera);

            RemoveIgnoreRenderFromUnityCameraCullingMask(unityCamera);
        }

        public void RenderDistancePass(ScriptableRenderContext context)
        {
            int distancePass = GetDefaultDistancePass();
            for (int i = distancePass; i > 0; i--)
            {
                ApplyClipPlanePropertiesToUnityCamera(unityCamera, i);

                renderingManager.BeginCameraDistancePassRendering(this, unityCamera);

                bool lastPostProcessing = postProcessing;
                postProcessing = false;

#pragma warning disable CS0618 // Type or member is obsolete
                UniversalRenderPipeline.RenderSingleCamera(context, unityCamera);
#pragma warning restore CS0618 // Type or member is obsolete

                postProcessing = lastPostProcessing;

                renderingManager.EndCameraDistancePassRendering(this, unityCamera);

                if (i == distancePass)
                    unityCamera.clearFlags = CameraClearFlags.Nothing;
            }

            ApplyClipPlanePropertiesToUnityCamera(unityCamera, 0);
        }

        protected override DisposeContext GetDisposingContext()
        {
            return DisposeContext.Programmatically_Destroy;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (skybox != null)
                    skybox.enabled = false;

                return true;
            }
            return false;
        }
    }
}
#endif