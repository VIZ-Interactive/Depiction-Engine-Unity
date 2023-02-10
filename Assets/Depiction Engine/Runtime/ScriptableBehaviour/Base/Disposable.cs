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

        private DisposeManager.DestroyContext _destroyingState;

        private Action _initializedEvent;
        private Action<IDisposable> _disposingEvent;
        private Action<IDisposable> _disposedEvent;

        public virtual void Recycle()
        {
            _initializing = _initialized = _disposing = _dispose = _disposed = _disposedComplete = false;
            _destroyingState = DisposeManager.DestroyContext.Unknown;
        }

        public virtual bool Initialize()
        {
            if (!_initializing)
            {
                Initializing();

                InitializeFields();
                InitializeSerializedFields();

                UpdateAllDelegates();

                Initialized();

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

        public virtual void Initialized()
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
            return _disposing && _destroyingState != DisposeManager.DestroyContext.Unknown;
        }

        public DisposeManager.DestroyContext destroyingState
        {
            get { return _destroyingState; }
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

        protected virtual DisposeManager.DestroyContext GetDestroyingState(DisposeManager.DestroyContext destroyingState)
        {
            return _destroyingState != DisposeManager.DestroyContext.Unknown ? _destroyingState : destroyingState;
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

                _destroyingState = GetDestroyingState(DisposeManager.destroyingState);

                if (DisposingEvent != null)
                    DisposingEvent(this);
                DisposingEvent = null;

                return true;
            }
            return false;
        }

        public void OnDisposedInternal(DisposeManager.DestroyContext destroyState)
        {
            OnDisposed(destroyingState);
        }

        protected virtual bool OnDisposed(DisposeManager.DestroyContext destroyState)
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
