// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class Grid : ScriptableObject, IGrid
    {
        private int _cascade;

        private bool _wasFirstUpdated;

        private bool _enabled;

        public virtual Grid Init()
        {
            _enabled = true;
            return this;
        }

        public int cascade
        {
            get { return _cascade; }
            set
            {
                if (_cascade == value)
                    return;
                _cascade = value;
            }
        }

        public bool enabled
        {
            get { return _enabled; }
        }

        public bool SetEnabled(bool value)
        {
            if (_enabled == value)
                return false;
            _enabled = value;
            return true;
        }

        public bool wasFirstUpdated
        {
            get { return _wasFirstUpdated; }
        }

        protected virtual bool UpdateDerivedProperties()
        {
            return true;
        }

        public virtual bool UpdateGrid()
        {
            Clear();
            _wasFirstUpdated = true;
            return _enabled;
        }

        public virtual void Clear()
        {
            
        }
    }
}
