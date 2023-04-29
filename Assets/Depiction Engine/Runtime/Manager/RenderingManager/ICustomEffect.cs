// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    /// <summary>
    /// A <see cref="DepictionEngine.RenderingManager"/> compatible shader based custom visual effect that can be applied to specific layers.
    /// </summary>
    public interface ICustomEffect : IPersistent
    {
        int maskedLayers { get; set; }

        int GetCustomEffectComputeBufferDataSize();
        int AddToComputeBufferData(int startIndex, float[] computeBufferData);
    }
}
