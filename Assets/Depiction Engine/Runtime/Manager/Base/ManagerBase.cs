﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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

        protected override void DetectChanges()
        {
            base.DetectChanges();

            if (_lastGameObjectActiveSelf != gameObjectActiveSelf)
            {
                bool newValue = gameObjectActiveSelf;
                gameObject.SetActive(_lastGameObjectActiveSelf);
                gameObjectActiveSelf = newValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetManagerComponent<T>(string gameOjectName = SceneManager.SCENE_MANAGER_NAME) where T : ManagerBase
        {
            T manager = null;

            if (!SceneManager.IsSceneBeingDestroyed())
            {
                GameObject go = GameObject.Find(gameOjectName);
                if (go is null)
                {
                    go = new GameObject(gameOjectName);
                    go.AddComponent<ManagersBootstrap>();
                }
            
                manager = go.GetSafeComponent<T>();
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
