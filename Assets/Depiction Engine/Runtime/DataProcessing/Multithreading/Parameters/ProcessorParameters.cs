// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;

namespace DepictionEngine
{
    public class ProcessorParameters : Disposable
    {
        private CancellationTokenSource _cancellationTokenSource;

        private Processor.ProcessingType _processingType;

        private List<MultithreadSafeScriptableObject> _locked;

        public override bool Initialize()
        {
            if (base.Initialize())
            {
                if (_locked == null)
                    _locked = new List<MultithreadSafeScriptableObject>();

                return true;
            }
            return false;
        }

        public ProcessorParameters Init(CancellationTokenSource cancellationTokenSource, Processor.ProcessingType processingType)
        {
            _cancellationTokenSource = cancellationTokenSource;
            _processingType = processingType;

            return this;
        }

        public Processor.ProcessingType processingType
        {
            get { return _processingType; }
        }

        public CancellationTokenSource cancellationTokenSource
        {
            get { return _cancellationTokenSource; }
        }

        protected void Lock(MultithreadSafeScriptableObject multithreaded)
        {
            _locked.Add(multithreaded);
            multithreaded.locked = true;
        }
        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
            {
                foreach (MultithreadSafeScriptableObject multithreaded in _locked)
                    multithreaded.locked = false;
                _locked.Clear();
                _cancellationTokenSource = null;

                return true;
            }
            return false;
        }
    }
}
