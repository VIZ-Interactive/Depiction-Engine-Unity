// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using System;
using System.Runtime.CompilerServices;
using static UnityEngine.Rendering.DebugUI;

namespace DepictionEngine
{
    [Serializable]
    public class Disposable : IDisposable, ISerializationCallbackReceiver
    {
        public static readonly Null NULL = new();

        private bool _initializing;
        private bool _initialized;
        private bool _disposing;
        private bool _destroyingContextUpdated;
        private bool _disposed;
        private bool _disposedComplete;

        private DisposeManager.DisposeContext _destroyingContext;

        private Action _initializedEvent;
        private Action<IDisposable> _disposingEvent;
        private Action<IDisposable> _disposedEvent;

        public virtual void Recycle()
        {
            _initializing = _initialized = _disposing = _destroyingContextUpdated = _disposed = _disposedComplete = false;
            _destroyingContext = DisposeManager.DisposeContext.Unknown;
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
                InitializedEvent?.Invoke();

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
            return _disposing && _destroyingContext != DisposeManager.DisposeContext.Unknown;
        }

        public DisposeManager.DisposeContext destroyingContext
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

        public Action<IDisposable> DisposingEvent
        {
            get { return _disposingEvent; }
            set { _disposingEvent = value; }
        }

        public Action<IDisposable> DisposedEvent
        {
            get { return _disposedEvent; }
            set { _disposedEvent = value; }
        }

        public static bool IsDisposed(object disposable)
        {
            if (disposable is null)
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

        public virtual bool OnDisposing(DisposeManager.DisposeContext disposeContext)
        {
            if (!_disposing)
            {
                _disposing = true;

                DisposingEvent?.Invoke(this);
                DisposingEvent = null;

                return true;
            }
            return false;
        }

        public virtual bool UpdateDestroyingContext()
        {
            if (!_destroyingContextUpdated)
            {
                _destroyingContextUpdated = true;

                _destroyingContext = GetDestroyingContext();

                return true;
            }
            return false;
        }

        public void OnDisposedInternal(DisposeManager.DisposeContext disposeContext, bool pooled = false)
        {
            OnDisposed(destroyingContext, pooled);
        }

        /// <summary>
        /// This is the last chance to clear or dipose any remaining references. It will be called immediately after the <see cref="DepictionEngine.IDisposable.UpdateDestroyingContext"/> unless a <see cref="DepictionEngine.DisposeManager.DisposeDelay"/> was passed to the <see cref="DepictionEngine.DisposeManager.Dispose"/> call.
        /// </summary>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        /// <returns>False if the object was already disposed otherwise True.</returns>
        protected virtual bool OnDisposed(DisposeManager.DisposeContext disposeContext, bool pooled = false)
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposedEvent?.Invoke(this);
                DisposedEvent = null;

                InitializedEvent = null;
                DisposingEvent = null;
                DisposedEvent = null;

                UpdateAllDelegates();

                return true;
            }
            return false;
        }

        protected virtual DisposeManager.DisposeContext GetDestroyingContext()
        {
            DisposeManager.DisposeContext destroyingContext = DisposeManager.disposingContext;

            if (SceneManager.IsSceneBeingDestroyed())
                destroyingContext = DisposeManager.DisposeContext.Programmatically;

            return destroyingContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Disposable lhs, Null rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Disposable lhs, Null rhs) => DisposeManager.IsNullOrDisposing(lhs);

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
            public static bool operator ==(Null lhs, IDisposable rhs) => DisposeManager.IsNullOrDisposing(rhs);

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
