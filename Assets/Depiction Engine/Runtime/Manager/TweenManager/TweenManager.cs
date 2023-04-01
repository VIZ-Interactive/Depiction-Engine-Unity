// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing <see cref="DepictionEngine.Tween"/>'s.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(TweenManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class TweenManager : ManagerBase
    {
        private List<Tween> _tweens;

        private static TweenManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL)
                _instance = GetManagerComponent<TweenManager>(createIfMissing);
            return _instance;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                foreach (Tween tween in tweens)
                {
                    RemoveTweenDelegates(tween);
                    if (!IsDisposing())
                        AddTweenDelegates(tween);
                }

                return true;
            }
            return false;
        }

        private List<Tween> tweens
        {
            get
            {
                _tweens ??= new List<Tween>();
                return _tweens;
            }
        }

        private void RemoveTweenDelegates(Tween tween)
        {
            if (tween is not null)
                tween.DisposedEvent -= TweenDisposedHandler;
        }

        private void AddTweenDelegates(Tween tween)
        {
            if (tween != Disposable.NULL)
                tween.DisposedEvent += TweenDisposedHandler;
        }

        private void TweenDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            RemoveTween(disposable as Tween);
        }

        private Tween CreateTween(float from, float to, float duration, Action<float> onUpdate, Action onComplete, Action onKill, EasingType easing = EasingType.Linear)
        {
            Tween tween = instanceManager.CreateInstance<Tween>();
            
            if (tween != Disposable.NULL)
                AddTween(tween.Init(from, to, duration, onUpdate, onComplete, onKill, easing));

            return tween;
        }

        private void AddTween(Tween tween)
        {
            if (!tweens.Contains(tween))
            {
                tweens.Add(tween);
                AddTweenDelegates(tween);
            }
        }

        private void RemoveTween(Tween tween)
        {
            int index = tweens.IndexOf(tween);
            if (index != -1)
            {
                tweens.RemoveAt(index);
                RemoveTweenDelegates(tween);
            }
        }

        public Tween DelayedCall(float duration, Action<float> onUpdate = null, Action onComplete = null, Action onKill = null)
        {
            return To(0.0f, 1.0f, duration, onUpdate, onComplete, onKill, EasingType.Linear);
        }

        public Tween To(float from, float to, float duration, Action<float> onUpdate = null, Action onComplete = null, Action onKill = null, EasingType easing = EasingType.Linear)
        {
            Tween tween = CreateTween(from, to, duration, onUpdate, onComplete, onKill, easing);
            
            if (!Object.ReferenceEquals(tween, null))
                tween.Play();
      
            return tween != Disposable.NULL ? tween : null;
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                IterateOverTweens((tween) => { tween.Update(); });

                return true;
            }
            return false;
        }

        private List<Tween> _tweenIterator;
        private void IterateOverTweens(Action<Tween> callback)
        {
            if (tweens != null)
            {
                _tweenIterator ??= new List<Tween>();

                _tweenIterator.AddRange(tweens);

                for (int i = _tweenIterator.Count - 1; i >= 0; i--)
                {
                    Tween tween = _tweenIterator[i];
                    if (tween != Disposable.NULL)
                        callback(_tweenIterator[i]);
                }

                _tweenIterator.Clear();
            }
        }

        public void DisposeAllTweens()
        {
            IterateOverTweens((tween) => { DisposeManager.Dispose(tween); });
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                DisposeAllTweens();

                return true;
            }
            return false;
        }
    }
}
