// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class VisualObjectVisualDirtyFlags : ScriptableObject
    {
        [SerializeField]
        private MeshRendererVisual.ColliderType _colliderType;
        [SerializeField]
        private int[] _cameraInstanceIds;
        [SerializeField]
        private bool _isDirty;

        private bool _colliderTypeDirty;

        private bool _disableMultithreading;

        private bool _disposeAllVisuals;

        public virtual void Recycle()
        {
            ResetDirty();

            _colliderType = MeshRendererVisual.ColliderType.None;

            _cameraInstanceIds = null;
        }

        public MeshRendererVisual.ColliderType colliderType
        {
            get { return _colliderType; }
            set
            {
                if (_colliderType == value)
                    return;

                _colliderType = value;

                ColliderTypeDirty();
            }
        }

        public bool colliderTypeDirty
        {
            get { return _colliderTypeDirty; }
        }

        protected void ColliderTypeDirty()
        {
            _colliderTypeDirty = true;
            Recreate();
        }

        public bool SetCameraInstanceIds(List<int> cameraInstanceIds)
        {
            bool changed = false;

            int cameraInstanceIdsCount = _cameraInstanceIds != null ? _cameraInstanceIds.Length : 0;
            int newCameraInstanceIdsCount = cameraInstanceIds != null ? cameraInstanceIds.Count : 0;
            if (cameraInstanceIdsCount != newCameraInstanceIdsCount)
                changed = true;
            else if (_cameraInstanceIds != null && cameraInstanceIds != null)
            { 
                for(int i = 0; i < _cameraInstanceIds.Length; i++)
                {
                    if (_cameraInstanceIds[i] != cameraInstanceIds[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                int[] instanceIds = new int[cameraInstanceIds.Count];

                for (int i = 0; i < cameraInstanceIds.Count; i++)
                    instanceIds[i] = cameraInstanceIds[i];

                _cameraInstanceIds = instanceIds;
                return true;
            }

            return false;
        }

        public bool disableMultithreading
        {
            get { return _disableMultithreading; }
        }

        public void DisableMultithreading()
        {
            _disableMultithreading = true;
        }

        public void Recreate()
        {
            DisposeAllVisuals();
            isDirty = true;
        }

        public bool isDirty
        {
            get { return _isDirty; }
            protected set
            {
                if (_isDirty == value)
                    return;

                _isDirty = value;
            }
        }

        public virtual void AllDirty()
        {
            isDirty = true;
        }

        public bool disposeAllVisuals
        {
            get { return _disposeAllVisuals; }
        }

        protected void DisposeAllVisuals()
        {
            _disposeAllVisuals = true;
        }

        public virtual void ResetDirty()
        {
            _colliderTypeDirty = false;

            _isDirty = false;
            _disposeAllVisuals = false;
            _disableMultithreading = false;
        }
    }
}
