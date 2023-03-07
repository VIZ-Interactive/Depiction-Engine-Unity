// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Supports Dispose and Initialization. 
    /// </summary>
    public interface IDisposable
    {
#if UNITY_EDITOR
        /// <summary>
        /// Can the object be found on the Editor undo or redo stack.
        /// </summary>
        bool hasEditorUndoRedo { get; }
#endif

        /// <summary>
        /// Has the object been initialized.
        /// </summary>
        bool initialized { get; }

        /// <summary>
        /// Is the object done disposing.
        /// </summary>
        bool disposedComplete { get; set; }

        /// <summary>
        /// The <see cref="DepictionEngine.DisposeManager.DisposeContext"/> under which the object was destroyed.
        /// </summary>
        DisposeManager.DisposeContext destroyingContext { get; }

        /// <summary>
        /// Dispatched after the object as been initialized.
        /// </summary>
        Action InitializedEvent { get; set; }
        /// <summary>
        /// Dispatched during the <see cref="DepictionEngine.IDisposable.UpdateDestroyingContext"/>.
        /// </summary>
        Action<IDisposable> DisposingEvent { get; set; }
        /// <summary>
        /// Dispatched during the <see cref="DepictionEngine.MonoBehaviourDisposable.OnDisposed"/>, <see cref="DepictionEngine.ScriptableObjectDisposable.OnDisposed"/> or <see cref="DepictionEngine.Disposable.OnDisposed"/>.
        /// </summary>
        Action<IDisposable> DisposedEvent { get; set; }

        /// <summary>
        /// Resets the fields to their default value so the object can be reused again. It will be called by the <see cref="DepictionEngine.PoolManager"/> if the object is being recycled from the pool.
        /// </summary>
        void Recycle();

        /// <summary>
        /// Needs to be called before the object can be used. Objects created throught the <see cref="DepictionEngine.InstanceManager"/> should automatically Initialize the object.
        /// </summary>
        /// <returns>False if the object is already initializing or initialized.</returns>
        /// <remarks>In some edge cases, in the editor, the Initialize may not be called immediately aftet the object is instantiated.</remarks>
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

        /// <summary>
        /// This is where you clear or dipose any references. Should be called automatically by the <see cref="DepictionEngine.DisposeManager"/> immediately after <see cref="DepictionEngine.DisposeManager.Dispose"/> or <see cref="DepictionEngine.DisposeManager.Destroy"/> is called. The <see cref="IDisposable.destroyingContext"/> is not initialized at this point.
        /// </summary>
        /// <returns>True if disposing, False if the object is already disposing or was disposed.</returns>
        bool OnDisposing(DisposeManager.DisposeContext disposeContext);

        /// <summary>
        /// This is where you clear or dipose any remaining references. It will be called automatically by <see cref="DepictionEngine.DisposeManager"/> immediately after <see cref="DepictionEngine.IDisposable.OnDisposing"/> unless the object was Destroyed as a result of an Editor action.
        /// </summary>
        /// <returns>True if disposing, False if the object is already disposing or was disposed.</returns>
        bool UpdateDestroyingContext();

        void OnDisposedInternal(DisposeManager.DisposeContext disposeContext, bool pooled = false);

        bool Equals(Disposable.Null value);
    }
}
