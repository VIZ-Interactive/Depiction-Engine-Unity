// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using System;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    [Serializable]
    public class Disposable : IDisposable, ISerializationCallbackReceiver
    {
        public static readonly Null NULL = new();

        private bool _initializing;
        private bool _initialized;
        private bool _disposing;
        private bool _disposingContextUpdated;
        private bool _disposed;
        private bool _poolComplete;

        private DisposeContext _disposingContext;

        private Action<IDisposable> _initializedEvent;
        private Action<IDisposable> _disposingEvent;
        private Action<IDisposable, DisposeContext> _disposedEvent;

        public virtual void Recycle()
        {
            _initializing = _initialized = _disposing = _disposingContextUpdated = _disposed = _poolComplete = default;
            _disposingContext = default;
        }

        public bool Initialize()
        {
            if (!IsDisposing() && !_initializing)
            {
                Initializing();

                InitializeFields();
                InitializeSerializedFields();

                UpdateAllDelegates();

                Initialized(InitializationContext.Programmatically);
                InitializedEvent?.Invoke(this);

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

        public virtual void Initialized(InitializationContext initializingContext)
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

        public bool poolComplete
        {
            get { return _poolComplete; }
            set { _poolComplete = value; }
        }

        public bool IsDestroying()
        {
            return _disposing && _disposingContext != DisposeContext.Programmatically_Pool;
        }

        public DisposeContext disposingContext
        {
            get { return _disposingContext; }
        }

        public bool notPoolable
        {
            get { return false; }
        }

        public void MarkAsNotPoolable()
        {
            
        }

        public Action<IDisposable> InitializedEvent
        {
            get { return _initializedEvent; }
            set { _initializedEvent = value; }
        }

        public Action<IDisposable> DisposingEvent
        {
            get { return _disposingEvent; }
            set { _disposingEvent = value; }
        }

        public Action<IDisposable, DisposeContext> DisposedEvent
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

        public bool OnDisposing()
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

        public bool UpdateDisposingContext()
        {
            if (!_disposingContextUpdated)
            {
                _disposingContextUpdated = true;

                _disposingContext = GetDisposingContext();

                return true;
            }
            return false;
        }

        public void OnDisposeInternal(DisposeContext disposeContext)
        {
            UpdateDisposingContext();
            OnDispose(_disposingContext);
        }

        public virtual bool OnDispose(DisposeContext disposeContext)
        {
            if (!_disposed)
            {
                _disposed = true;

                DisposedEvent?.Invoke(this, disposeContext);

                UpdateAllDelegates();

                InitializedEvent = null;
                DisposingEvent = null;
                DisposedEvent = null;

                return true;
            }
            return false;
        }

        protected virtual DisposeContext GetDisposingContext()
        {
            DisposeContext destroyingContext = DisposeManager.disposingContext;

            if (SceneManager.IsSceneBeingDestroyed())
                destroyingContext = DisposeContext.Programmatically_Destroy;

            return destroyingContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Disposable lhs, Null rhs)
        {
            return !(lhs == rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Disposable lhs, Null _) => DisposeManager.IsNullOrDisposing(lhs);

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
            public static bool operator ==(Null _, IDisposable rhs) => DisposeManager.IsNullOrDisposing(rhs);

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
