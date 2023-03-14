// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [RequireComponent(typeof(Object))]
    public class Script : JsonMonoBehaviour
    { 
        private Object _objectBase;
        private GeoAstroObject _parentGeoAstroObject;

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveObjectBaseDelegate(objectBase);
                AddObjectBaseDelegate(objectBase);

                RemoveParentGeoAstroObjectDelegates(parentGeoAstroObject);
                AddParentGeoAstroObjectDelegates(parentGeoAstroObject);

                return true;
            }
            return false;
        }

        protected virtual bool RemoveObjectBaseDelegate(Object objectBase)
        {
            if (objectBase is not null)
            {
                objectBase.InitializedEvent -= ObjectInitializedHandler;
                objectBase.PropertyAssignedEvent -= ObjectBasePropertyAssignedHandler;
                objectBase.TransformPropertyAssignedEvent -= TransformPropertyAssignedHandler;
                objectBase.TransformChangedEvent -= TransformChangedHandler;

                return true;
            }
            return false;
        }

        protected virtual bool AddObjectBaseDelegate(Object objectBase)
        {
            if (!IsDisposing() && objectBase != Disposable.NULL)
            {
                objectBase.InitializedEvent += ObjectInitializedHandler;
                objectBase.PropertyAssignedEvent += ObjectBasePropertyAssignedHandler;
                objectBase.TransformPropertyAssignedEvent += TransformPropertyAssignedHandler;
                objectBase.TransformChangedEvent += TransformChangedHandler;

                return true;
            }
            return false;
        }

        private void ObjectInitializedHandler(IDisposable disposable)
        {
            UpdateParentGeoAstroObject();
        }

        private void ObjectBasePropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(Object.parentGeoAstroObject))
                UpdateParentGeoAstroObject();
        }

        protected virtual bool RemoveParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
        {
            if (parentGeoAstroObject is not null)
            {
                parentGeoAstroObject.PropertyAssignedEvent -= ParentGeoAstroObjectPropertyAssignedHandler;

                return true;
            }
            return false;
        }

        protected virtual bool AddParentGeoAstroObjectDelegates(GeoAstroObject parentGeoAstroObject)
        {
            if (!IsDisposing() && parentGeoAstroObject != Disposable.NULL)
            {
                parentGeoAstroObject.PropertyAssignedEvent += ParentGeoAstroObjectPropertyAssignedHandler;

                return true;
            }
            return false;
        }

        protected virtual void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            
        }

        private void TransformPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            TransformPropertyAssigned(property, name, newValue, oldValue);
        }

        protected virtual bool TransformPropertyAssigned(IProperty property, string name, object newValue, object oldValue)
        {
            if (HasChanged(newValue, oldValue))
            {
                if (name == nameof(parent))
                    TransformParentChanged();

                return true;
            }
            return false;
        }

        protected virtual void TransformParentChanged()
        {

        }

        protected void TransformChangedHandler(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            TransformChanged(changedComponent, capturedComponent);
        }

        protected virtual bool TransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            return initialized;
        }

#if UNITY_EDITOR
        protected UnityEngine.Object[] GetTransformAdditionalRecordObjects()
        {
            return objectBase.GetTransformAdditionalRecordObjects();
        }
#endif

        protected override bool IsFullyInitialized()
        {
            return !base.IsFullyInitialized() || transform.parentGeoAstroObject == Disposable.NULL || transform.parentGeoAstroObject.GetLoadingInitialized();
        }

        public float GetSphericalRatio()
        {
            return parentGeoAstroObject != Disposable.NULL ? parentGeoAstroObject.GetSphericalRatio() : 0.0f;
        }

        protected bool IsSpherical()
        {
            return parentGeoAstroObject != Disposable.NULL && parentGeoAstroObject.IsSpherical();
        }

        protected bool IsFlat()
        {
            return parentGeoAstroObject == Disposable.NULL || parentGeoAstroObject.IsFlat();
        }

        protected override Type GetParentType()
        {
            return typeof(Object);
        }

        protected override bool SetParent(PropertyMonoBehaviour value)
        {
            if (base.SetParent(value))
            {
                objectBase = !IsDisposing() ? value as Object : null;

                return true;
            }
            return false;
        }

        public Object objectBase
        {
            get { return _objectBase; }
            protected set { SetObjectBase(value); }
        }

        protected virtual bool SetObjectBase(Object value)
        {
            if (Object.ReferenceEquals(_objectBase, value))
                return false;

            Object oldValue = _objectBase;
            Object newValue = value;

            _objectBase = newValue;

            RemoveObjectBaseDelegate(oldValue);
            AddObjectBaseDelegate(newValue);

            UpdateParentGeoAstroObject();

            return true;
        }

        private void UpdateParentGeoAstroObject()
        {
            parentGeoAstroObject = objectBase != Disposable.NULL ? objectBase.parentGeoAstroObject : null;
        }

        public GeoAstroObject parentGeoAstroObject
        {
            get { return _parentGeoAstroObject; }
            protected set { SetParentGeoAstroObject(value, _parentGeoAstroObject); }
        }

        protected virtual bool SetParentGeoAstroObject(GeoAstroObject newValue, GeoAstroObject oldValue)
        {
            if (Object.ReferenceEquals(_parentGeoAstroObject, newValue))
                return false;

            _parentGeoAstroObject = newValue;

            RemoveParentGeoAstroObjectDelegates(oldValue);
            AddParentGeoAstroObjectDelegates(newValue);

            return true;
        }

        public new TransformDouble transform
        {
            get { return objectBase is not null ? objectBase.transform : null; }
        }
    }
}
