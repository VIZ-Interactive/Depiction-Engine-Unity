// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public interface IDisposable
    {
        bool initialized { get; }

        bool disposedComplete { get; set; }
        Action InitializedEvent { get; set; }
        Action<IDisposable> DisposingEvent { get; set; }
        Action<IDisposable> DisposedEvent { get; set; }
        DisposeManager.DestroyContext destroyingState { get; }

#if UNITY_EDITOR
        bool hasEditorUndoRedo { get; }
#endif

        /// <summary>
        /// Acts as a constructor. Needs to be called before the object can be used. Objects created throught the <see cref="InstanceManager"/> should automatically Initialize the object.
        /// </summary>
        /// <returns>False if the object is already initializing or initialized.</returns>
        /// <remarks>In some edge cases, in the editor, the Initialize may not be called immediately aftet the object is instantiated.</remarks>
        bool Initialize();

        /// <summary>
        /// Resets the fields to their default value so the object can be reused again. Used by the <see cref="PoolManager"/>.
        /// </summary>
        void Recycle();

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
        bool OnDispose();
        void OnDisposedInternal(DisposeManager.DestroyContext destroyState);

        bool Equals(Disposable.Null value);
    }
}
