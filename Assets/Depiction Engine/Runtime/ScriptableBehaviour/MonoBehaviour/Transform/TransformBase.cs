// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Position, rotation and scale of an object.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1)]
    public class TransformBase : JsonMonoBehaviour, IHasChildren
    {
        /// <summary>
        /// The different transform components.
        /// </summary>
        [Flags]
        public enum Component : byte
        {
            None = 1 << 0,
            Position = 1 << 1,
            Rotation = 1 << 2,
            LossyScale = 1 << 3,
            LocalPosition = 1 << 4,
            LocalRotation = 1 << 5,
            LocalScale = 1 << 6
        };

        [Serializable]
        private class TransformCacheDictionary : SerializableDictionary<int, TransformComponents3> { };

        private bool _isGeoCoordinateTransform;

        private Object _parentObject;
        private GeoAstroObject _parentGeoAstroObject;

        private Object _objectBase;

        protected bool _positionDirty;
        protected bool _rotationDirty;
        protected bool _lossyScaleDirty;
        private bool _localToWorldMatrixDirty;
        private bool _worldToLocalMatrixDirty;
      
        private TransformComponents3 _unityTransformComponents;

        /// <summary>
        /// Dispatched when a child such as a <see cref="DepictionEngine.TransformDouble"/> or a <see cref="DepictionEngine.Visual"/>, when a <see cref="DepictionEngine.VisualObject"/> is present, is added. 
        /// </summary>
        public Action<TransformBase, PropertyMonoBehaviour> ChildAddedEvent;
        /// <summary>
        /// Dispatched when a child is removed. 
        /// </summary>
        public Action<TransformBase, PropertyMonoBehaviour> ChildRemovedEvent;
        /// <summary>
        /// Dispatched when a property assignment is detected in any of the children.
        /// </summary>
        public Action<TransformBase, string, object, object> ChildPropertyAssignedEvent;
        /// <summary>
        /// Dispatched when some transform component(s) such as localPosition, localRotation or localScale have changed.
        /// </summary>
        public Action<Component, Component> ChangedEvent;

        public override void Recycle()
        {
            base.Recycle();

            transformLocalPosition = default;
            transformLocalRotation = default;
            transformLocalScale = Vector3.one;

            ResetComponentDirtyFlag(true, true, true, true, true);

            _parentGeoAstroObject = default;

            _objectBase = default;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            UpdateIsGeoCoordinateTransform();

            SetComponentDirtyFlag(true, true, true);
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Reset)
            {
                transformLocalPosition = Vector3.zero;
                transformLocalRotation = Quaternion.identity;
                transformLocalScale = Vector3.one;
            }
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
                _lastIndex = index;

                InitLastTransformFields();

                return true;
            }
            return false;
        }

        public void InitLastTransformFields()
        {
            InitLastLocalPositionField();
            InitLastLocalRotationField();
            InitLastLocalScaleField();
        }

        private void InitLastLocalPositionField()
        {
            if (!IsDestroying())
                _lastTransformLocalPosition = transformLocalPosition;
        }

        private void InitLastLocalRotationField()
        {
            if (!IsDestroying())
                _lastTransformLocalRotation = transformLocalRotation;
        }

        private void InitLastLocalScaleField()
        {
            if (!IsDestroying())
                _lastTransformLocalScale = transformLocalScale;
        }

        protected override JSONObject GetInitializationJson(JSONObject initializeJSON)
        {
            initializeJSON = base.GetInitializationJson(initializeJSON);

            if (initializeJSON != null)
            {
                string transformName = nameof(Object.transform);
                if (initializeJSON[transformName] != null)
                    initializeJSON = initializeJSON[transformName].AsObject;
            }

            return initializeJSON;
        }

        /// <summary>
        /// Detect changes that happen as a result of an external influence.
        /// </summary>
        public bool DetectGameObjectChanges()
        {
            if (IsDisposing())
                return false;

            if (_lastIndex != index)
            {
                int newValue = index;
                gameObject.transform.SetSiblingIndex(_lastIndex);
                index = newValue;
            }

            return true;
        }

        protected override bool LateInitialize(InitializationContext initializingContext)
        {
            if (base.LateInitialize(initializingContext))
            {
#if UNITY_EDITOR
                InitializeToTopInInspector();
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private List<UnityEngine.Component> _componentsList;
        public void InitializeToTopInInspector()
        {
            bool isEditorObject = false;

            if (_objectBase != Disposable.NULL)
                isEditorObject = SceneManager.IsEditorNamespace(_objectBase.GetType());
            
            if (!isEditorObject)
            {
                _componentsList ??= new List<UnityEngine.Component>();
                GetComponents(_componentsList);
                if (_componentsList[1] != this)
                    Editor.ComponentUtility.MoveComponentRelativeToComponent(this, _componentsList[0], false);
            }
        }

        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                if (!IsDisposing())
                {
                    RevertUnityLocalPosition();
                    RevertUnityLocalRotation();
                    RevertUnityLocalScale();
                }

                DetectGameObjectChanges();

                return true;
            }
            return false;
        }
#endif

        protected override bool AddInstanceToManager()
        {
           return true;
        }

        protected override bool ParentHasChanged()
        {
            bool parentChanged = base.ParentHasChanged();

            if (!parentChanged)
            {
                Transform parentTransform = parent != Disposable.NULL && parent != sceneManager ? parent.transform : null;
                if (parentTransform != transform.parent)
                    parentChanged = true;
            }

            return parentChanged;
        }

        protected override bool SiblingsHasChanged()
        {
            bool siblingsChanged = base.SiblingsHasChanged();

            if (!siblingsChanged && objectBase == Disposable.NULL && gameObject.GetComponentInitialized<Object>(GetInitializeContext()) != Disposable.NULL)
                siblingsChanged = true;

            return siblingsChanged;
        }

        protected override bool ChildrenHasChanged()
        {
            bool childrenChanged = base.ChildrenHasChanged();

            if (!childrenChanged)
            {
                if (this != null)
                {
                    int childCount = children.Count + (objectBase != Disposable.NULL ? objectBase.GetAdditionalChildCount() : 0);
                    if (transform.childCount != childCount)
                        childrenChanged = true;
                }
            }

            return childrenChanged;
        }

        protected override void UpdateChildren()
        {
            base.UpdateChildren();

            if (objectBase != Disposable.NULL)
                objectBase.UpdateAdditionalChildren();
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                if (_objectBase != Disposable.NULL && _objectBase is AstroObject)
                {
                    AstroObject astroObject = _objectBase as AstroObject;
                    RemoveAstroObjectDelegates(astroObject);
                    if (!IsDisposing())
                        AddAstroObjectDelegates(astroObject);
                }

                if (children != null)
                {
                    foreach (PropertyMonoBehaviour property in children)
                    {
                        if (property is TransformBase)
                        {
                            TransformBase transform = property as TransformBase;
                            RemoveChildDelegates(transform);
                            if (!IsDisposing())
                                AddChildDelegates(transform);
                        }
                    }
                }

                return true;
            }
            return false;
        }

        private void RemoveAstroObjectDelegates(AstroObject astroObject)
        {
            if (astroObject is not null)
                astroObject.PropertyAssignedEvent -= AstroObjectPropertyAssignedHandler;
        }

        private void AddAstroObjectDelegates(AstroObject astroObject)
        {
            if (astroObject != Disposable.NULL)
                astroObject.PropertyAssignedEvent += AstroObjectPropertyAssignedHandler;
        }

        protected override bool RemoveParentDelegates(PropertyMonoBehaviour parent)
        {
            if (base.RemoveParentDelegates(parent))
            {
                parent.PropertyAssignedEvent -= ParentPropertyAssignedHandler;

                return true;
            }
            return false;
        }

        protected override bool AddParentDelegates(PropertyMonoBehaviour parent)
        {
            if (base.AddParentDelegates(parent))
            {
                parent.PropertyAssignedEvent += ParentPropertyAssignedHandler;

                return true;
            }
            return false;
        }

        private void RemoveChildDelegates(TransformBase child)
        {
            child.PropertyAssignedEvent -= ChildPropertyAssignedHandler;
        }

        private void AddChildDelegates(TransformBase child)
        {
            child.PropertyAssignedEvent += ChildPropertyAssignedHandler;
        }

        protected void ParentPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(oldValue, newValue))
                return;

            Originator(() =>
            {
                if (name == nameof(parentGeoAstroObject))
                    UpdateParentGeoAstroObject();

                if (name == nameof(objectBase))
                    UpdateParentObject();
            }, property);
        }

        protected void ChildPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(oldValue, newValue))
                return;

            ChildPropertyAssignedEvent?.Invoke(property as TransformBase, name, newValue, oldValue);
        }

        protected override bool CanBeDisabled()
        {
            return false;
        }

        private bool _invalidParentWarning;
        public override void UpdateParent(PropertyMonoBehaviour originator = null)
        {
            Originator(() => 
            {
                TransformBase parentTransform = GetParent() as TransformBase;

                if (!_invalidParentWarning && parentTransform != Disposable.NULL)
                {
                    Object parentObject = parentTransform.GetComponent<Object>();
                    if (parentObject is VisualObject)
                    {
                        _invalidParentWarning = true;
                        Debug.LogWarning("VisualObject '" + parentTransform.name + "' cannot contain anything other then Visuals, 'DepictionEngine.Object's are not allowed and may not be properly Initialized.");
                    }
                    else if (gameObject.transform.parent != null && parentObject.gameObject.transform != gameObject.transform.parent)
                    {
                        _invalidParentWarning = true;
                        Debug.LogWarning("Missing 'DepictionEngine.Object' component in '" + gameObject.transform.parent.name + "' GameObject. Every GameObject in the hierarchy needs to have a component that implements 'DepictionEngine.Object'.");
                    }
                }

                SetParent(parentTransform, initialized); 
            }, originator);
        }

        protected override PropertyMonoBehaviour GetParent()
        {
            PropertyMonoBehaviour parent = null;

            Type parentType = GetParentType();
            if (parentType != null)
                parent = (PropertyMonoBehaviour)(transform.parent != null ? transform.parent.gameObject.GetComponentInParentInitialized(parentType, true) : null);

            return parent;
        }

        protected override Type GetParentType()
        {
            return typeof(TransformBase);
        }

        protected override Type GetSiblingType()
        {
            return typeof(Object);
        }

        protected override Type GetChildType()
        {
            return _objectBase is VisualObject ? typeof(Visual) : typeof(TransformBase);
        }

        private TransformComponents3 unityTransformComponents
        {
            get { _unityTransformComponents ??= new TransformComponents3(); return _unityTransformComponents; }
        }

#if UNITY_EDITOR
        //Get a euler rotation value which is similar to unity's Transform
        protected Vector3 GetInspectorEulerFromLocalRotation(Quaternion localRotation)
        {
            if (float.IsNaN(localRotation.x))
                return Vector3.up;
            bool localRotationChangeDetected = DetectDirectTransformLocalRotationManipulation();

            Quaternion lastLocalRotation = transform.localRotation;
            transform.localRotation = localRotation;
            Vector3 euler = UnityEditor.TransformUtils.GetInspectorRotation(transform);
            transform.localRotation = lastLocalRotation;

            //Weird behavior in Unity where sometimes the localRotation value is being slightly altered after assignment
            //Fix: Simply update the recorded transform with the new altered value if they where the same before
            if (localRotationChangeDetected && !DetectDirectTransformLocalRotationManipulation())
                InitLastLocalRotationField();

            return euler;
        }

        //Get a localRotation value which is similar to unity's Transform
        protected Quaternion GetLocalRotationFromInspectorEuler(Vector3 euler)
        {
            bool localRotationChangeDetected = DetectDirectTransformLocalRotationManipulation();

            Quaternion localRotation = transform.localRotation;
            UnityEditor.TransformUtils.SetInspectorRotation(transform, euler);
            Quaternion newLocalRotation = transform.localRotation;
            transform.localRotation = localRotation;

            //Weird behavior in Unity where sometimes the localRotation value is being slightly altered after assignment
            //Fix: Simply update the recorded transform with the new altered value if they where the same before
            if (localRotationChangeDetected && !DetectDirectTransformLocalRotationManipulation())
                InitLastLocalRotationField();

            return newLocalRotation;
        }
#endif

        private int _lastIndex;
        /// <summary>
        /// An index number representing the order of the children in relation to their siblings.
        /// </summary>
        [Json]
        public int index
        {
            get => transform.GetSiblingIndex();
            set 
            {
                int oldValue = index;

                if (HasChanged(value, oldValue))
                {
                    transform.SetSiblingIndex(value);
                    PropertyAssigned(this, nameof(index), value, oldValue);
                }

                _lastIndex = value;
            }
        }

        public bool IsGeoCoordinateContext()
        {
            Object parentObject = FindParentObject();
            return parentObject != Disposable.NULL && parentObject is DatasourceRoot && FindParentGeoAstroObject() != Disposable.NULL;
        }

        protected void UpdateIsGeoCoordinateTransform()
        {
            isGeoCoordinateTransform = IsGeoCoordinateContext();
        }

        public bool isGeoCoordinateTransform
        {
            get => _isGeoCoordinateTransform;
            protected set
            {
                SetValue(nameof(isGeoCoordinateTransform), value, ref _isGeoCoordinateTransform, (newValue, oldValue) => 
                {
                    IsGeoCoordinateTransformChanged();
                });
            }
        }

        protected virtual void IsGeoCoordinateTransformChanged()
        {
            
        }

        public int childCount
        {
            get => children != null ? children.Count : 0;
        }

        public void RevertUnityLocalPosition()
        {
            transform.localPosition = _lastTransformLocalPosition;
        }

        private Vector3 _lastTransformLocalPosition;
        protected Vector3 transformLocalPosition
        {
            get => transform.localPosition;
            set
            {
                if (_lastTransformLocalPosition.Equals(value))
                    return;
                transform.localPosition = value;
                InitLastLocalPositionField();
            }
        }

        public void RevertUnityLocalRotation()
        {
            transform.localRotation = _lastTransformLocalRotation;
        }

        private Quaternion _lastTransformLocalRotation;
        protected Quaternion transformLocalRotation
        {
            get => transform.localRotation;
            set
            {
                if (_lastTransformLocalRotation.Equals(value))
                    return;
                transform.localRotation = value;
                InitLastLocalRotationField();
            }
        }

        public void RevertUnityLocalScale()
        {
            transform.localScale = _lastTransformLocalScale;
        }

        private Vector3 _lastTransformLocalScale;
        protected Vector3 transformLocalScale
        {
            get => transform.localScale;
            set
            {
                if (_lastTransformLocalScale.Equals(value))
                    return;
                transform.localScale = value;
                InitLastLocalScaleField();
            }
        }

        protected virtual bool positionDirty
        {
            get => _positionDirty;
            set => _positionDirty = value;
        }

        protected virtual bool rotationDirty
        {
            get => _rotationDirty;
            set => _rotationDirty = value;
        }

        protected virtual bool lossyScaleDirty
        {
            get => _lossyScaleDirty;
            set => _lossyScaleDirty = value;
        }

        protected bool localToWorldMatrixDirty
        {
            get => _localToWorldMatrixDirty;
            private set => _localToWorldMatrixDirty = value;
        }

        protected bool worldToLocalMatrixDirty
        {
            get => _worldToLocalMatrixDirty;
            private set => _worldToLocalMatrixDirty = value;
        }

        protected override bool AddChild(PropertyMonoBehaviour child)
        {
            bool added = false;

            Originator(() =>
            {
                if (child is Object)
                {
                    if (SetObjectBase(child as Object))
                    {
                        added = true;
#if UNITY_EDITOR
                        if (initialized && SceneManager.GetIsUserChangeContext())
                            MarkAsNotPoolable();
#endif
                    }
                }
                else
                {
                    if (base.AddChild(child))
                    {
                        added = true;

                        if (child is TransformBase)
                            AddChildDelegates(child as TransformBase);

                        ChildAddedEvent?.Invoke(this, child);
                    }
                }
            }, child);

            return added;
        }

        protected override bool RemoveChild(PropertyMonoBehaviour child)
        {
            bool removed = false;

            Originator(() =>
            {
                if (child is Object)
                {
                    if (SetObjectBase(null))
                    {
                        removed = true;
#if UNITY_EDITOR
                        if (initialized && SceneManager.GetIsUserChangeContext())
                            MarkAsNotPoolable();
#endif
                    }
                }
                else
                {
                    if (base.RemoveChild(child))
                    {
                        removed = true;

                        if (child is TransformBase)
                            RemoveChildDelegates(child as TransformBase);


                        ChildRemovedEvent?.Invoke(this, child);
                    }
                }
            }, child);

            return removed;
        }

        public Object objectBase
        {
            get => _objectBase;
            protected set => SetObjectBase(value);
        }

        private bool SetObjectBase(Object value)
        {
            return SetValue(nameof(objectBase), value, ref _objectBase, (newValue, oldValue) =>
            {
                if (!HasChanged(newValue, oldValue))
                    return;

                RemoveAstroObjectDelegates(oldValue as AstroObject);
                AddAstroObjectDelegates(newValue as AstroObject);
            });
        }

        public Object parentObject
        {
            get => _parentObject;
            protected set 
            {
                SetValue(nameof(parentObject), value, ref _parentObject, (newValue, oldValue) =>
                {
                    if (!IsDisposing())
                    {
                        if (!UpdateParentGeoAstroObject())
                            UpdateIsGeoCoordinateTransform();
                    }
                });
            }
        }

        public GeoAstroObject parentGeoAstroObject
        {
            get => _parentGeoAstroObject;
            private set => SetParentGeoAstroObject(value);
        }

        private bool SetParentGeoAstroObject(GeoAstroObject value)
        {
            return SetValue(nameof(parentGeoAstroObject), value, ref _parentGeoAstroObject, (newValue, oldValue) =>
            {
                if (!IsDisposing())
                    UpdateIsGeoCoordinateTransform();
            });
        }

        protected void UpdateParentObject()
        {
            parentObject = FindParentObject();
        }

        protected bool UpdateParentGeoAstroObject()
        {
            return SetParentGeoAstroObject(FindParentGeoAstroObject());
        }

        private Object FindParentObject()
        {
            Object parentObject;

            if (initialized)
                parentObject = parent != Disposable.NULL && parent is TransformBase ? (parent as TransformBase).objectBase : null;
            else
            {
                try
                {
                    parentObject = transform.parent != null ? transform.parent.GetComponent<Object>() : null;
                }
                catch(MissingReferenceException e)
                {
                    parentObject = null;
                    Debug.Log(e.Message); 
                }
            }
            return parentObject;
        }

        protected GeoAstroObject FindParentGeoAstroObject()
        {
            GeoAstroObject parentGeoAstroObject;

            if (initialized)
            {
                if (parent != Disposable.NULL && parent is TransformBase)
                {
                    TransformBase parentTransform = parent as TransformBase;
                    parentGeoAstroObject = parentTransform.objectBase is GeoAstroObject ? parentTransform.objectBase as GeoAstroObject : parentTransform.parentGeoAstroObject;
                }
                else
                    parentGeoAstroObject = null;
            }
            else
                parentGeoAstroObject = transform.parent != null ? transform.parent.GetComponentInParent<GeoAstroObject>(true) : null;

            return parentGeoAstroObject;
        }

        public GeoAstroObject GetParentGeoAstroObject(bool includeSibling = true)
        {
            return _parentGeoAstroObject != Disposable.NULL ? _parentGeoAstroObject : includeSibling ? objectBase as GeoAstroObject : null;
        }

        protected void SetComponentDirtyFlag(bool position = false, bool rotation = false, bool lossyScale = false)
        {
            if (position)
                positionDirty = true;
            if (rotation)
                rotationDirty = true;
            if (lossyScale)
                lossyScaleDirty = true;
            worldToLocalMatrixDirty = localToWorldMatrixDirty = true;
        }

        protected void ResetComponentDirtyFlag(bool position = false, bool rotation = false, bool lossyScale = false, bool worldToLocalMatrix = false, bool localToWorldMatrix = false)
        {
            if (position)
                positionDirty = false;
            if (rotation)
                rotationDirty = false;
            if (lossyScale)
                lossyScaleDirty = false;
            if (worldToLocalMatrix)
                worldToLocalMatrixDirty = false;
            if (localToWorldMatrix)
                localToWorldMatrixDirty = false;
        }

        public bool SetParent(Guid id, bool worldPositionStays = true)
        {
            if ((parent == Disposable.NULL && id == null) || (parent != Disposable.NULL && parent.id == id))
                return false;

            return SetParent(instanceManager.GetTransform(id), worldPositionStays);
        }

        public virtual bool SetParent(TransformBase value, bool worldPositionStays = true)
        {
            if (base.SetParent(value))
            {
                if (!IsDisposing())
                {
                    Transform newTransform = value != Disposable.NULL ? value.transform : null;
                    if (transform.parent != newTransform)
                    {
                        bool parentChanged = false;
#if UNITY_EDITOR
                        if (SceneManager.GetIsUserChangeContext())
                        {
                            parentChanged = true;
                            RegisterCompleteObjectUndo();
                            Editor.UndoManager.SetTransformParent(this, value);
                        }
#endif
                        if (!parentChanged)
                            transform.SetParent(newTransform, false);
                    }

                    InitLastTransformFields();

                    UpdateParentObject();

                    return true;
                }
            }

            return false;
        }

        protected Vector3 CaptureLocalPositionDelta()
        {
            return transformLocalPosition - _lastTransformLocalPosition;
        }

        protected Quaternion CaptureLocalRotationDelta()
        {
            return Quaternion.Inverse(_lastTransformLocalRotation) * transformLocalRotation;
        }

        protected Vector3 CaptureLocalScaleDelta()
        {
            return transformLocalScale - _lastTransformLocalScale;
        }

        protected bool DetectDirectTransformLocalPositionManipulation()
        {
            bool manipulationDetected = false;
           
            Vector3 delta = CaptureLocalPositionDelta();
            if (delta != Vector3.zero)
                manipulationDetected = true;

            return manipulationDetected;
        }

        protected bool DetectDirectTransformLocalRotationManipulation()
        {
            bool manipulationDetected = false;

            Quaternion delta = CaptureLocalRotationDelta();
            if (delta != Quaternion.identity)
                manipulationDetected = true;

            return manipulationDetected;
        }

        protected bool DetectDirectTransformLocalScaleManipulation()
        {
            bool manipulationDetected = false;

            Vector3 delta = CaptureLocalScaleDelta();
            if (delta != Vector3.zero)
                manipulationDetected = true;

            return manipulationDetected;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                UpdateTransformHideFlags();

                return true;
            }
            return false;
        }

        public void UpdateTransformHideFlags()
        {
            bool isEditorObject = false;
#if UNITY_EDITOR
            if (_objectBase != Disposable.NULL)
                isEditorObject = SceneManager.IsEditorNamespace(_objectBase.GetType());
#endif
            if (!isEditorObject && transform is not null)
            {
                transform.hideFlags &= ~HideFlags.HideInInspector;
                if (!DisposeManager.IsNullOrDisposing(this))
                    transform.hideFlags |= HideFlags.HideInInspector;
            }
        }

        public override void HierarchicalApplyOriginShifting(Vector3Double origin, bool originShiftSelected)
        {
            TransformComponents3 unityTransformComponentsReference = unityTransformComponents;

            //Apply before Children
            if (UpdateUnityTransformComponents(ref unityTransformComponentsReference, origin, originShiftSelected))
                origin = Vector3Double.zero;

            transformLocalPosition = unityTransformComponentsReference.localPosition;
            transformLocalRotation = unityTransformComponentsReference.localRotation;
            transformLocalScale = unityTransformComponentsReference.localScale;

            base.HierarchicalApplyOriginShifting(origin, originShiftSelected);
        }

        protected virtual bool UpdateUnityTransformComponents(ref TransformComponents3 unityTransformComponents, Vector3Double origin, bool originShiftSelected)
        {
            unityTransformComponents.SetComponents(transformLocalPosition, transformLocalRotation, transformLocalScale);

            return false;
        }

        public virtual bool ForceUpdateTransform(out Component changedComponents, bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false, Camera camera = null, bool forceDeriveLocalPosition = false, bool triggerTransformChanged = true)
        {
            changedComponents = Component.None;
            return initialized && transform != null;
        }

        protected void AstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (!HasChanged(oldValue, newValue))
                return;

            Component changedComponents = Component.None;

            bool isSphericalRatio = name == nameof(GeoAstroObject.sphericalRatio);
            if (isSphericalRatio || name == nameof(GeoAstroObject.size))
                changedComponents |= Component.Position;
            if (isSphericalRatio)
                changedComponents |= Component.Rotation;

            if (changedComponents != Component.None)
            {
                Datasource.StartAllowAutoDisposeOnOutOfSynchProperty();

                IterateOverChildren(
                    (child) =>
                    {
                        if (child is TransformBase transform && transform.initialized)
                            transform.ParentChanged(changedComponents, Component.None, this);
                    });

                Datasource.EndAllowAutoDisposeOnOutOfSynchProperty();
            }
        }

        private Component _changedComponents;
        private Component _capturedComponents;
        private TransformBase _originator;
        public bool UpdatingChildren()
        {
            return _changedComponents != Component.None;
        }

        public bool TransformChanged(Component changedComponents, Component capturedComponents, TransformBase originator)
        {
            if (changedComponents != Component.None)
            {
                if (initialized && ChangedEvent != null)
                    ChangedEvent(changedComponents, capturedComponents);

                _changedComponents = changedComponents;
                _capturedComponents = capturedComponents;
                _originator = originator;
                IterateOverChildren(
                    (child) =>
                    {
                        if (child is TransformBase childTransform)
                            UpdateChild(childTransform);
                    });
                _changedComponents = Component.None;
                _capturedComponents = Component.None;
                _originator = null;

                return true;
            }
            return false;
        }

        public Component UpdateChild(TransformBase childTransform)
        {
            if (childTransform != Disposable.NULL && childTransform.initialized)
                childTransform.ParentChanged(_changedComponents, _capturedComponents, _originator);
            return _changedComponents;
        }

        protected void ParentChanged(Component parentChangedComponents, Component capturedComponents, TransformBase originator)
        {
            Component updateComponents = GetUpdateComponentsFromParentChangedComponents(parentChangedComponents);

            if (updateComponents != Component.None)
            {
                bool updateLocalPosition = updateComponents.HasFlag(Component.Position);
                bool updateLocalRotation = updateComponents.HasFlag(Component.Rotation);
                bool updateLocalScale = updateComponents.HasFlag(Component.LossyScale);
                
                SetComponentDirtyFlag(updateLocalPosition, updateLocalRotation, updateLocalScale);

                if (ForceUpdateTransform(out Component changedComponent, updateLocalPosition, updateLocalRotation, updateLocalScale, null, true, false))
                    updateComponents |= changedComponent;
            }

            TransformChanged(updateComponents, capturedComponents, originator);
        }

        public static Component GetUpdateComponentsFromParentChangedComponents(Component parentChangedComponents)
        {
            Component updateComponents = Component.None;

            if (parentChangedComponents.HasFlag(Component.Position) || parentChangedComponents.HasFlag(Component.Rotation) || parentChangedComponents.HasFlag(Component.LossyScale))
                updateComponents |= Component.Position;
            if (parentChangedComponents.HasFlag(Component.Rotation))
                updateComponents |= Component.Rotation;
            if (parentChangedComponents.HasFlag(Component.LossyScale))
                updateComponents |= Component.LossyScale;

            return updateComponents;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (_objectBase is not null && !TriggerCallback(_objectBase, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        public void IterateOverChildren<T>(Func<T, bool> callback, bool includeDontSave = true) where T : PropertyMonoBehaviour
        {
            for (int i = childCount - 1 ; i >= 0 ; i--)
            {
                PropertyMonoBehaviour propertyMonoBehaviour = children[i];
                if (propertyMonoBehaviour is T  && propertyMonoBehaviour != Disposable.NULL && (includeDontSave || !propertyMonoBehaviour.hideFlags.HasFlag(HideFlags.DontSave)) && !callback(propertyMonoBehaviour as T))
                    return;
            }
        }

        public void IterateOverChildrenObject<T>(Func<T, bool> callback) where T : Object
        {
            IterateOverChildren<TransformDouble>((transform) => 
            {
                if (transform.objectBase != Disposable.NULL && transform.objectBase is T && !callback(transform.objectBase as T))
                    return false;
                return true;
            });
        }

        public void IterateOverChildrenGameObject(GameObject gameObject, Action<GameObject> callback, bool recursive = false)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject child = gameObject.transform.GetChild(i).gameObject;
                callback(child);
                if (recursive)
                    IterateOverChildrenGameObject(child, callback, recursive);
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                try
                {
                    UpdateTransformHideFlags();
                }
                catch (MissingReferenceException)
                {}

                ChildAddedEvent = null;
                ChildRemovedEvent = null;
                ChildPropertyAssignedEvent = null;
                ChangedEvent = null;

                return true;
            }
            return false;
        }

        protected override DisposeContext GetDisposingContext()
        {
            DisposeContext overrideDestroyingContext = base.GetDisposingContext();

#if UNITY_EDITOR
            if (_objectBase != Disposable.NULL && _objectBase is Editor.SceneCamera)
                overrideDestroyingContext = DisposeContext.Programmatically_Destroy;
#endif

            return overrideDestroyingContext;
        }
    }

    [Serializable]
    public class TransformComponents3
    {
        [SerializeField]
        public Vector3 localPosition;
        [SerializeField]
        public Quaternion localRotation;
        [SerializeField]
        public Vector3 localScale;

        public TransformComponents3()
        {
        }

        public TransformComponents3(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.localScale = localScale;
        }

        public TransformComponents3 SetComponents(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            this.localPosition = localPosition;
            this.localRotation = localRotation;
            this.localScale = localScale;
            return this;
        }
    }

    [Serializable]
    public class TransformComponentParam
    {
        [SerializeField]
        private bool _changed;

        public bool changed
        {
            get => _changed;
        }

        public void Changed()
        {
            _changed = true;
        }

        public void ResetChanged()
        {
            _changed = false;
        }

        public virtual void Recycle()
        {
            ResetChanged();
        }
    }
}