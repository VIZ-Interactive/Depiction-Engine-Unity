// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    [DefaultExecutionOrder(-2)]
    public class ManagerBase : JsonMonoBehaviour
    {
        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
                _lastGameObjectActiveSelf = gameObjectActiveSelf;

                return true;
            }
            return false;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        public override bool DetectUserGameObjectChanges()
        {
            if (base.DetectUserGameObjectChanges())
            {
                if (_lastGameObjectActiveSelf != gameObjectActiveSelf)
                {
                    bool newValue = gameObjectActiveSelf;
                    gameObject.SetActive(_lastGameObjectActiveSelf);
                    gameObjectActiveSelf = newValue;
                }

                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetManagerComponent<T>(bool createIfMissing = true) where T : ManagerBase
        {
            T manager = null;

            if (!SceneManager.IsSceneBeingDestroyed())
            {
                GameObject go = GameObject.Find(SceneManager.SCENE_MANAGER_NAME);

                if (go == null && createIfMissing)
                {
                    go = new GameObject(SceneManager.SCENE_MANAGER_NAME);
                    go.AddComponentInitialized<ManagersBootstrap>();
                    //Initialize SceneManager
                    go.GetComponentInitialized<SceneManager>();
                }
                
                if (go != null)
                    manager = go.GetComponentInitialized<T>();
            }

            return manager;
        }

        protected override bool CanBeDisabled()
        {
            return false;
        }

        private bool CanGameObjectBeDeactivated()
        {
            return false;
        }

        private bool _lastGameObjectActiveSelf;
        public bool gameObjectActiveSelf
        {
            get { return gameObject.activeSelf; }
            set
            {
                if (!CanGameObjectBeDeactivated())
                    value = true;

                bool oldValue = gameObjectActiveSelf;
                if (HasChanged(value, oldValue))
                {
                    gameObject.SetActive(value);
                    _lastGameObjectActiveSelf = gameObject.activeSelf;
                    PropertyAssignedEvent?.Invoke(this, nameof(gameObjectActiveSelf), value, oldValue);
                }
            }
        }

        protected override bool UpdateHideFlags()
        {
            return true;
        }

#if UNITY_EDITOR
        protected override bool ResetAllowed()
        {
            return false;
        }

        public override bool PasteComponentAllowed()
        {
            return false;
        }
#endif
    }
}
