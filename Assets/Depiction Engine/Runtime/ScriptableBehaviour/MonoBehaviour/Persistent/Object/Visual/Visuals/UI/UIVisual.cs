// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class UIVisual : Visual
    {
        [SerializeField, HideInInspector]
        private List<int> _cameras;

        public override void Recycle()
        {
            base.Recycle();

            if (_cameras != null)
                _cameras.Clear();
        }

        public bool AddCamera(Camera camera)
        {
            int cameraInstanceID = camera.GetCameraInstanceID();
            if (!cameras.Contains(cameraInstanceID))
            {
                cameras.Add(cameraInstanceID);
                return true;
            }
            return false;
        }

        public List<int> cameras
        {
            get 
            {
                if (_cameras == null)
                    _cameras = new List<int>();
                return _cameras; 
            }
        }
    }
}
