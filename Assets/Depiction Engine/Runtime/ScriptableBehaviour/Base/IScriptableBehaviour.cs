// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// An abstract interface for both MonoBehaviour and ScriptableObject.
    /// </summary>
    public interface IScriptableBehaviour : IDisposable
    {
#if UNITY_EDITOR
        /// <summary>
        /// Resets all serialized fields to their default value.
        /// </summary>
        void InspectorReset();
        string inspectorComponentNameOverride { set; get; }
#endif

        InstanceManager.InitializationContext GetInitializeContext(InstanceManager.InitializationContext defaultState = InstanceManager.InitializationContext.Programmatically);

        string name { set; get; }

        /// <summary>
        /// Was this component created by a <see cref="DepictionEngine.FallbackValues"/>.
        /// </summary>
        bool isFallbackValues { get; }

        /// <summary>
        /// Which <see cref="DepictionEngine.IScriptableBehaviour"/> triggered the current code execution if any at all.
        /// </summary>
        IScriptableBehaviour originator { get; }

        /// <summary>
        /// Provides the code executed in this callback information about the <see cref="DepictionEngine.IScriptableBehaviour"/> that triggered it by keeping a reference available under <see cref="DepictionEngine.IScriptableBehaviour.originator"/>.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="originator"></param>
        void Originator(Action callback, IScriptableBehaviour originator);

        void ExplicitAwake();
        int GetInstanceID();
        void ExplicitOnEnable();
        void ExplicitOnDisable();

        /// <summary>
        /// Indicates whether the current code execution was triggered as a result of a user action such as altering properties through the editor inspector or moving object using the manipulator.
        /// </summary>
        /// <returns>True if it is the result of a user action, otherwise False.</returns>
        bool IsUserChangeContext();

        /// <summary>
        /// Makes available to the code executed in the callback, the user context under which it was triggered. If it was triggered by a user action, such as altering properties through the editor inspector or moving object using the manipulator, the value passed to isUserChange should be true. User context inside this callback can always be accessed by calling <see cref="DepictionEngine.IScriptableBehaviour.IsUserChangeContext"/>.
        /// </summary>
        /// <param name="callback">The code to execute.</param>
        /// <param name="isUserChange">Whether the current code execution was triggered by a user action.</param>
        void IsUserChange(Action callback, bool isUserChange = true);

        DisposeManager.DestroyContext GetDestroyContext();

        /// <summary>
        /// This is where you destroy any remaining dependencies.
        /// </summary>
        void OnDestroy();
    }
}
