// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

namespace DepictionEngine
{
    public interface ICustomEffect : IPersistent
    {
        int maskedLayers { get; set; }

        int GetCusomtEffectComputeBufferDataSize();
        int AddToComputeBufferData(int startIndex, float[] computeBufferData);
    }
}
