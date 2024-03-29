﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Supports Dispose and Initialization. 
    /// </summary>
    public interface IDisposable
    {
        /// <summary>
        /// Dispatched after the object as been initialized.
        /// </summary>
        Action<IDisposable> InitializedEvent { get; set; }

        /// <summary>
        /// Dispatched during the <see cref="DepictionEngine.MonoBehaviourDisposable.OnDispose"/>, <see cref="DepictionEngine.ScriptableObjectDisposable.OnDispose"/> or <see cref="DepictionEngine.Disposable.OnDispose"/>.
        /// </summary>
        Action<IDisposable, DisposeContext> DisposedEvent { get; set; }

        /// <summary>
        /// Acts as a reliable constructor and will always by called unlike Awake which is sometimes skipped during Undo / Redo operations.
        /// </summary>
        /// <param name="initializingContext"></param>
        void Initialized(InitializationContext initializingContext);

#if UNITY_EDITOR
        /// <summary>
        /// Can the object be found on the Editor undo or redo stack.
        /// </summary>
        bool notPoolable { get; }

        /// <summary>
        /// Marking the object as not poolable ensures that the object will always be destroyed when disposed. If the object is a Component the entire GameObject will be destroyed when disposed.
        /// </summary>
        void MarkAsNotPoolable();
#endif

        /// <summary>
        /// Has the object been initialized.
        /// </summary>
        bool initialized { get; }

        /// <summary>
        /// Is the object done disposing.
        /// </summary>
        bool poolComplete { get; set; }

        /// <summary>
        /// The <see cref="DepictionEngine.DisposeContext"/> under which the object was destroyed.
        /// </summary>
        DisposeContext disposingContext { get; }

        /// <summary>
        /// Resets the fields to their default value so the object can be reused again. It will be called by the <see cref="DepictionEngine.PoolManager"/> if the object is being recycled from the pool.
        /// </summary>
        void Recycle();

        /// <summary>
        /// Needs to be called before the object can be used. Objects created through the <see cref="DepictionEngine.InstanceManager"/> should automatically Initialize the object.
        /// </summary>
        /// <returns>False if the object is already initializing or initialized.</returns>
        /// <remarks>In some edge cases, in the editor, the Initialize may not be called immediately after the object is instantiated.</remarks>
        bool Initialize();

        /// <summary>
        /// Is the object disposing?.
        /// </summary>
        /// <returns>True if the object is being disposed / destroyed or as already been disposed / destroyed.</returns>
        bool IsDisposing();

        /// <summary>
        /// Has the object been disposed?
        /// </summary>
        /// <returns>True if the object as already been disposed / destroyed.</returns>
        bool IsDisposed();

        bool OnDisposing();

        /// <summary>
        /// This is where you dispose any remaining dependencies.
        /// </summary>
        /// <param name="disposeContext">The context under which the object is being destroyed.</param>
        /// <returns>False if the object was already disposed otherwise True.</returns>
        bool OnDispose(DisposeContext disposeContext);
        void OnDisposeInternal(DisposeContext disposeContext);

        /// <summary>
        /// This is where you clear or dispose any remaining references. It will be called automatically by <see cref="DepictionEngine.DisposeManager"/> immediately after <see cref="DepictionEngine.IDisposable.OnDisposing"/> unless the object was Destroyed as a result of an Editor action.
        /// </summary>
        /// <param name="forceUpdate"></param>
        /// <returns>True if disposing, False if the object is already disposing or was disposed.</returns>
        bool UpdateDisposingContext(bool forceUpdate = false);

        bool Equals(Disposable.Null value);
    }
}
