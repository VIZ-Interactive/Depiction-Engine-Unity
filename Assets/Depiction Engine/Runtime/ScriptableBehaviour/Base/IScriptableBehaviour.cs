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
        string inspectorComponentNameOverride { set; get; }
#endif

        InitializationContext GetInitializeContext();

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

        int GetInstanceID();

#if UNITY_EDITOR
        bool AfterAssemblyReload();
#endif

        /// <summary>
        /// This is where you destroy any remaining dependencies.
        /// </summary>
        void OnDestroy();
    }
}
