// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [RequireComponent(typeof(SceneManager))]
    [RequireComponent(typeof(InstanceManager))]
    [RequireComponent(typeof(PoolManager))]
    [RequireComponent(typeof(TweenManager))]
    [RequireComponent(typeof(CameraManager))]
    [RequireComponent(typeof(RenderingManager))]
    [RequireComponent(typeof(InputManager))]
    [RequireComponent(typeof(DatasourceManager))]
    [ExecuteAlways]
    internal class ManagersLock : MonoBehaviour
    {
        public void Awake()
        {
            hideFlags = HideFlags.HideInInspector;
        }
    }
}
