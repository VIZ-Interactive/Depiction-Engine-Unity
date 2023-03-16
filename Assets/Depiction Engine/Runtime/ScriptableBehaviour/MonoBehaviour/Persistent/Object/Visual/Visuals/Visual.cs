// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// A GameObject meant to be used as a child of a <see cref="DepictionEngine.VisualObject"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class Visual : PropertyMonoBehaviour, IRequiresComponents
    {
        private VisualObject _visualObject;

        public override void Recycle()
        {
            base.Recycle();

            gameObject.layer = LayerMask.GetMask("Default");
            gameObject.hideFlags = default;

            transform.localPosition = default;
            transform.localRotation = default;
            transform.localScale = Vector3.one;

            _visualObject = default;
        }

        protected override Type GetParentType()
        {
            return typeof(TransformBase);
        }

        protected override PropertyMonoBehaviour GetParent()
        {
            PropertyMonoBehaviour parent = base.GetParent();

            if (!IsDisposing())
            {
                if (parent == Disposable.NULL || parent.gameObject.transform != transform.parent)
                    parent = transform.parent.GetSafeComponentInParent(typeof(Visual), true, GetInitializeContext()) as Visual;
            }

            return parent;
        }

        protected override bool SetParent(PropertyMonoBehaviour value)
        {
            if (base.SetParent(value))
            {
                visualObject = !IsDisposing() && transform.parent != null ? transform.parent.GetSafeComponentInParent(typeof(VisualObject), true, GetInitializeContext()) as VisualObject : null;
                
                return true;
            }
            return false;
        }

        public VisualObject visualObject
        {
            get { return _visualObject; }
            private set { SetVisualObject(_visualObject, value); }
        }

        protected virtual bool SetVisualObject(VisualObject oldValue, VisualObject newValue)
        {
            if (Object.ReferenceEquals(_visualObject, newValue))
                return false;

            _visualObject = newValue;

            return true;
        } 

        public void UpdateLayer(bool ignoreRender)
        {
            if (ignoreRender)
                IgnoreInRender();
            else
                SetGameObjectLayer(GetLayer());
        }

        public void IgnoreInRender()
        {
            SetGameObjectLayer(LayerUtility.GetLayer(CameraManager.IGNORE_RENDER_LAYER_NAME));
        }

        public bool IsIgnoredInRender()
        {
            return gameObject.layer == LayerUtility.GetLayer(CameraManager.IGNORE_RENDER_LAYER_NAME);
        }

        protected void SetGameObjectLayer(int layer)
        {
            gameObject.SetLayerRecursively(layer);
        }

        protected virtual int GetLayer()
        {
            return _visualObject != Disposable.NULL ? _visualObject.layer : 0;
        }

        public void GetRequiredComponentTypes(ref List<Type> types)
        {
            types.Clear();
            types.Add(typeof(MeshFilter));
            types.Add(typeof(MeshRenderer));
            types.Add(typeof(Collider));
        }
    }
}
