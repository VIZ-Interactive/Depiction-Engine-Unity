// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public class MultithreadSafeScriptableObject : ScriptableObjectDisposable, IMultithreadSafe
    {
        private bool _locked;
        public bool locked
        {
            get { return _locked; }
            set
            {
                if (_locked == value)
                    return;
                _locked = value;
                OnDisposedLocked();
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                OnDisposedLocked();

                return true;
            }
            return false;
        }

        protected virtual bool OnDisposedLocked()
        {
            return !_locked && IsDisposed();
        }
    }
}
