// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    /// <summary>
    /// Implements mesh access for <see cref="DepictionEngine.AssetBase"/> who act as wrapper for mesh instance(s).
    /// </summary>
    public interface IUnityMeshAsset
    {
        void IterateOverUnityMesh(Action<UnityEngine.Mesh> callback);
    }
}
