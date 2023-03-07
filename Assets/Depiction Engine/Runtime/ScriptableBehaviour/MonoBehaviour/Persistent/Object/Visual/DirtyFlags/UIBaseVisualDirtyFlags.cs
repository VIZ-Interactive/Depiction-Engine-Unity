// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class UIBaseVisualDirtyFlags : VisualObjectVisualDirtyFlags
    {
        [SerializeField]
        private bool _screenSpace;

        public override void Recycle()
        {
            base.Recycle();

            _screenSpace = false;
        }

        public bool screenSpace
        {
            get { return _screenSpace; }
        }

        public bool SetCameraProperties(bool screenSpace, List<int> cameraInstanceIds)
        {
            bool recreate = false;

            if (_screenSpace != screenSpace)
            {
                _screenSpace = screenSpace;
                recreate = true;
            }

            if (SetCameraInstanceIds(cameraInstanceIds))
                recreate = true;

            if (recreate)
            {
                Recreate();

                return true;
            }
            return false;
        }

    }
}
