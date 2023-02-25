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
        protected override bool InitializeStack(InstanceManager.InitializationContext initializingContext)
        {
            return false;
        }

        protected override void InitializeAdditionalData()
        {
        }

        protected override void UpdateFields()
        {
            base.UpdateFields();

            InitializeSceneCameraController();
        }

        private void InitializeSceneCameraController()
        {
            if (SceneManager.sceneClosing || IsDisposing())
                return;

            //Recreate the SceneCameraTarget if it was destroyed along with the parent GeoAstroObject
            if (sceneCameraController == Disposable.NULL)
                gameObject.AddSafeComponent<SceneCameraController>();
        }

        protected override void InitializeToTopInInspector()
        {
            
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

        public override UnityEngine.Camera unityCamera
        {
            protected set
            {
                if (base.unityCamera == value)
                    return;

                base.unityCamera = value;

                UpdateShowImageEffects();
            }
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
            get { return controller as SceneCameraController; }
        }

        protected override bool SetController(ControllerBase value)
        {
            if (base.SetController(value))
            {
                InitializeSceneCameraController();

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

        public override int GetCameraInstanceId()
        {
            //Problem: SceneCameras are Destroyed and Recreated between compilation.
            //Fix: To Maintain continuity we use the sceneView InstanceID instead which is not Destroyed and Recreated
            return SceneViewDouble.GetSceneViewDouble(this).GetInstanceID();
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

                UniversalRenderPipeline.RenderSingleCamera(context, unityCamera);

                postProcessing = lastPostProcessing;

                renderingManager.EndCameraDistancePassRendering(this, unityCamera);

                if (i == distancePass)
                    unityCamera.clearFlags = CameraClearFlags.Nothing;
            }

            ApplyClipPlanePropertiesToUnityCamera(unityCamera, 0);
        }

        protected override DisposeManager.DestroyContext GetDestroyingContext()
        {
            return DisposeManager.DestroyContext.Programmatically;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
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