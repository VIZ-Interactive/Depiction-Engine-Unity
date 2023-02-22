// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using System;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    [Serializable]
    public class Disposable : IDisposable, ISerializationCallbackReceiver
    {
        public static readonly Null NULL = new Null();

        private bool _initializing;
        private bool _initialized;
        private bool _disposing;
        private bool _dispose;
        private bool _disposed;
        private bool _disposedComplete;

        private DisposeManager.DestroyContext _destroyingContext;

        private Action _initializedEvent;
        private Action<IDisposable> _disposeEvent;
        private Action<IDisposable> _disposedEvent;

        public virtual void Recycle()
        {
            _initializing = _initialized = _disposing = _dispose = _disposed = _disposedComplete = false;
            _destroyingContext = DisposeManager.DestroyContext.Unknown;
        }

        public bool Initialize()
        {
            if (!_initializing)
            {
                Initializing();

                InitializeFields();
                InitializeSerializedFields();

                UpdateAllDelegates();

                Initialized();
                if (InitializedEvent != null)
                    InitializedEvent();

                return true;
            }
            return false;
        }

        public virtual void Initializing()
        {
            _initializing = true;
        }

        public virtual void InitializeFields()
        {
            
        }

        public virtual void InitializeSerializedFields()
        {
        }

        public virtual void UpdateAllDelegates()
        {
           
        }

        /// <summary>
        /// Acts as a reliable constructor and will always by called unlike Awake which is sometimes skipped.
        /// </summary>
        protected virtual void Initialized()
        {
            _initialized = true;
        }

        public bool initialized
        {
            get { return _initialized; }
        }

        public bool IsDisposing()
        {
            return _disposing;
        }

        public bool IsDisposed()
        {
            return _disposed;
        }

        public bool disposedComplete
        {
            get { return _disposedComplete; }
            set { _disposedComplete = value; }
        }

        public bool IsDestroying()
        {
            return _disposing && _destroyingContext != DisposeManager.DestroyContext.Unknown;
        }

        public DisposeManager.DestroyContext destroyingContext
        {
            get { return _destroyingContext; }
        }

        public bool hasEditorUndoRedo
        {
            get { return false; }
        }

        public Action InitializedEvent
        {
            get { return _initializedEvent; }
            set { _initializedEvent = value; }
        }

        public Action<IDisposable> DisposeEvent
        {
            get { return _disposeEvent; }
            set { _disposeEvent = value; }
        }

        public Action<IDisposable> DisposedEvent
        {
            get { return _disposedEvent; }
            set { _disposedEvent = value; }
        }

        protected virtual DisposeManager.DestroyContext GetDestroyingContext(DisposeManager.DestroyContext destroyingContext)
        {
            return _destroyingContext != DisposeManager.DestroyContext.Unknown ? _destroyingContext : destroyingContext;
        }

        public static bool IsDisposed(object disposable)
        {
            if (Object.ReferenceEquals(disposable, null))
                return true;

            if (disposable is IDisposable)
                return (disposable as IDisposable).Equals(NULL);
            else
                return disposable == null;
        }

        public virtual void OnBeforeSerialize()
        {
            
        }

        public virtual void OnAfterDeserialize()
        {
            
        }

        public virtual bool OnDisposing()
        {
            if (!_disposing)
            {
                _disposing = true;
                return true;
            }
            return false;
        }

        public virtual bool OnDispose()
        {
            if (!_dispose)
            {
                _dispose = true;

                _destroyingContext = GetDestroyingContext(DisposeManager.destroyingContext);

                if (DisposeEvent != null)
                    DisposeEvent(this);
                DisposeEvent = null;

                return true;
            }
            return false;
        }

        public void OnDisposedInternal(DisposeManager.DestroyContext destroyContext)
        {
            OnDisposed(destroyingContext);
        }

        /// <summary>
        /// This is the last chance to clear or dipose any remaining references. It will be called immediately after the <see cref="DepictionEngine.IDisposable.OnDispose"/> unless a <see cref="DepictionEngine.DisposeManager.DestroyDelay"/> was passed to the <see cref="DepictionEngine.DisposeManager.Dispose"/> call.
        /// </summary>
        /// <param name="destroyContext">The context under which the object is being destroyed.</param>
        /// <returns>False if the object was already disposed otherwise True.</returns>
        protected virtual bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (DisposedEvent != null)
                    DisposedEvent(this);
                DisposedEvent = null;

                UpdateAllDelegates();

                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Disposable lhs, Null rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Disposable lhs, Null rhs)
        {
            return DisposeManager.IsNullOrDisposing(lhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Null value)
        {
            return this == value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object value)
        {
            return base.Equals(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public struct Null
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Null lhs, IDisposable rhs)
            {
                return !(lhs == rhs);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Null lhs, IDisposable rhs)
            {
                return DisposeManager.IsNullOrDisposing(rhs);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(IDisposable value)
            {
                return this == value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object value)
            {
                return value == null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
