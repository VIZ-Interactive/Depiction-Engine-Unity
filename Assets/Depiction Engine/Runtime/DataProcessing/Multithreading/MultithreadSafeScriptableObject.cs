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
                DisposingLocked();
                OnDisposedLocked();
            }
        }

        public override bool OnDisposing(DisposeManager.DisposeContext disposeContext)
        {
            if (base.OnDisposing(disposeContext))
            {
                DisposingLocked();
                return true;
            }
            return false;
        }

        protected virtual bool DisposingLocked()
        {
            return !_locked && IsDisposing();
        }

        protected override bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled = false)
        {
            if (base.OnDisposed(disposeContext, pooled))
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
