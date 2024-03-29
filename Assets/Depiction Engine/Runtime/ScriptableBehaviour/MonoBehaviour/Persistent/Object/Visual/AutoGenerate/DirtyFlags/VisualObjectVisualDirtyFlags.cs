﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

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
        [SerializeField]
        private bool _disposeAllVisuals;
        [SerializeField]
        private bool _processing;

        private bool _colliderTypeDirty;

        private bool _disableMultithreading;

        private Processor _processor;

        public virtual void Recycle()
        {
            ResetAll();

            _processing = default;
            _processor = default;

            _colliderType = MeshRendererVisual.ColliderType.None;

            _cameraInstanceIds = null;
        }

#if UNITY_EDITOR
        public virtual void UndoRedoPerformed()
        {
        }
#endif

        public MeshRendererVisual.ColliderType colliderType
        {
            get => _colliderType;
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
            get => _colliderTypeDirty;
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
            get => _disableMultithreading;
        }

        public bool isDirty
        {
            get => _isDirty;
            protected set
            {
                if (_isDirty == value)
                    return;

                _isDirty = value;
            }
        }

        public bool disposeAllVisuals
        {
            get => _disposeAllVisuals;
        }

        public void Recreate()
        {
            AllDirty();
            DisposeAllVisuals();
        }

        public virtual void AllDirty()
        {
            isDirty = true;
        }

        public void DisableMultithreading()
        {
            _disableMultithreading = true;
        }

        public void DisposeAllVisuals()
        {
            _disposeAllVisuals = true;
        }

        protected void ColliderTypeDirty()
        {
            _colliderTypeDirty = true;
            Recreate();
        }

        public void ResetAll()
        {
            ResetDirty();
            ResetDisposeAllVisuals();
            ResetColliderDirty();
        }

        public virtual void ResetDirty()
        {
            _isDirty = false;
            _disableMultithreading = false;
        }

        public void ResetDisposeAllVisuals()
        {
            _disposeAllVisuals = false;
        }

        public void ResetColliderDirty()
        {
            _colliderTypeDirty = false;
        }

        public void SetProcessing(bool processing, Processor processor = null)
        {
            _processing = processing;
            _processor = processor;
        }

        public bool ProcessingWasCompromised()
        {
            return _processing && _processor == null;
        }
    }
}
