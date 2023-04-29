// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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
    public class ManagersBootstrap : MonoBehaviour
    {
        public void Awake()
        {
            hideFlags = HideFlags.HideInInspector;
        }
    }
}
