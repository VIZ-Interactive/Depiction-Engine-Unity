// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class SceneCameraController : TargetControllerBase
    {
        public bool wasFirstUpdatedBySceneViewDouble;

        public override void UpdateDependencies()
        {
            base.UpdateDependencies();

            InitTarget();
        }

        protected bool InitTarget()
        {
            if (!IsDisposing() && base.target == Disposable.NULL)
            {
                SceneView sceneView = SceneViewDouble.GetSceneView(objectBase as Camera);
                if (sceneView != null)
                {
                    string targetName = GetTargetName(sceneView);

                    GameObject sceneCameraTargetGO = GameObject.Find(targetName);
                    if (sceneCameraTargetGO == null)
                        sceneCameraTargetGO = new GameObject(targetName);

                    SceneCameraTarget sceneCameraTarget = sceneCameraTargetGO.GetComponentInitialized<SceneCameraTarget>();

                    if (sceneCameraTarget == Disposable.NULL)
                        sceneCameraTarget = sceneCameraTargetGO.AddComponentInitialized<SceneCameraTarget>();

                    if (sceneCameraTarget != Disposable.NULL)
                        sceneCameraTarget.isHiddenInHierarchy = true;

                    base.target = sceneCameraTarget;
                }

                return true;
            }
            return false;
        }

        private string GetTargetName(SceneView sceneView)
        {
            return typeof(SceneCameraTarget).Name + " (SceneViewId: " + sceneView.GetInstanceID() + ")";
        }

        public override Object target
        {
            get 
            { 
                InitTarget();
                return base.target;
            }
        }

        protected override void TargetChanged(Object newValue, Object oldValue)
        {
            base.TargetChanged(newValue, oldValue);
           
            if (oldValue != Disposable.NULL)
                DisposeManager.Destroy(oldValue.gameObject);
        }

        protected override bool SetMinMaxDistance(Vector2Double value)
        {
            return base.SetMinMaxDistance(cameraManager.minMaxCameraDistance);
        }

        protected override bool AddInstanceToManager()
        {
            return false;
        }

        protected override Vector2Double GetMinMaxDistance()
        {
            return cameraManager.minMaxCameraDistance;
        }

        public override bool SetTargetPositionRotationDistance(Vector3Double targetPosition, QuaternionDouble rotation, double cameraDistance)
        {
            if (base.SetTargetPositionRotationDistance(targetPosition, rotation, cameraDistance))
            {
                wasFirstUpdatedBySceneViewDouble = true;

                return true;
            }
            return false;
        }
    }
}
#endif