// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class Tween : ScriptableObjectDisposable
    {
        private float _from;
        private float _to;
        private float _progress;
        private float _duration;
        private EasingType _easing;

        private float _currentLerpTime;
        private bool _playing;

        private Action<float> _onUpdateCallback;
        private Action _onCompleteCallback;
        private Action _onKillCallback;

        public override void Recycle()
        {
            base.Recycle();

            _progress = default;

            _currentLerpTime = default;
            _playing = default;
        }

        public Tween Init(float from, float to, float duration, Action<float> onUpdate, Action onComplete, Action onKill, EasingType easing = EasingType.Linear)
        {
            _to = to;
            _from = from;
            _duration = duration;
            _easing = easing;

            _onUpdateCallback = onUpdate;
            _onCompleteCallback = onComplete;
            _onKillCallback = onKill;

            return this;
        }

        public float from
        {
            get { return _from; }
        }

        public float to
        {
            get { return _to; }
        }

        public float progress
        {
            get { return _progress; }
            private set
            {
                if (_progress == value)
                    return;

                _progress = value;

                _onUpdateCallback?.Invoke(currentValue);

                if (IsDisposing())
                    return;

                if (_progress == 1.0f)
                    Completed();
            }
        }


        public float currentValue
        {
            get { return _from + (_to - _from) * _progress; }
        }

        public float duration
        {
            get { return _duration; }
        }

        public bool playing
        {
            get { return _playing; }
        }

        public Tween Play()
        {
            _playing = true;
            UpdateProgress();
            return this;
        }

        public Tween Pause()
        {
            _playing = false;
            return this;
        }

        public void Update()
        {
            if (IsDisposing())
                return;

            if (_playing)
            {
                _currentLerpTime += Time.deltaTime;
                UpdateProgress();
            }
        }

        public void UpdateProgress()
        {
            progress = _duration == 0.0f ? 1.0f : Easing.Ease(_easing, Mathf.Clamp01(_currentLerpTime / _duration), 0.0f, 1.0f, 1.0f);
        }

        private void Completed()
        {
            _playing = false;

            _onCompleteCallback?.Invoke();

            DisposeManager.Dispose(this);
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                _onKillCallback?.Invoke();

                return true;
            }
            return false;
        }
    }
}
