// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;

namespace DepictionEngine
{
    public interface IProcessing
    {
        Action ProcessingCompletedEvent { get; }
        Processor.ProcessingState processingState { get; }
    }
}
