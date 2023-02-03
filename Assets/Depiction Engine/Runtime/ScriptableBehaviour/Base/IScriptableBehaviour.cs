// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public interface IScriptableBehaviour : IDisposable
    {
#if UNITY_EDITOR
        void InspectorReset();
        string inspectorComponentNameOverride { set; get; }
#endif

        InstanceManager.InitializationContext GetInitializeState(InstanceManager.InitializationContext defaultState = InstanceManager.InitializationContext.Programmatically);

        string name { set; get; }
        bool isFallbackValues { get; }

        IScriptableBehaviour originator { get; }
        void Originator(Action callback, IScriptableBehaviour originator);

        void ExplicitAwake();
        int GetInstanceID();
        void ExplicitOnEnable();
        void ExplicitOnDisable();
        void OnDestroy();

        bool IsUserChangeContext();

        /// <summary>
        /// When isUserChange is true, any changes made to properties, with an <see cref="JsonAttribute"/>, within the callback will be marked as 'Out of Synch' with their datasource provided they are associated with one.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="isUserChange"></param>
        void IsUserChange(Action callback, bool isUserChange = true);

        DisposeManager.DestroyContext GetDestroyState();
    }
}
