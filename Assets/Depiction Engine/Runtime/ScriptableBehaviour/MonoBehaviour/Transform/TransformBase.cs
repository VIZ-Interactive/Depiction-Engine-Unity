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
            None = 0,
            Position = 1 << 0,
            Rotation = 1 << 1,
            LossyScale = 1 << 2,
            LocalPosition = 1 << 3,
            LocalRotation = 1 << 4,
            LocalScale = 1 << 5
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

        public Action<TransformBase, PropertyMonoBehaviour> ChildAddedEvent;
        public Action<TransformBase, PropertyMonoBehaviour> ChildRemovedEvent;
        public Action<TransformBase, string, object, object> ChildPropertyChangedEvent;
        public Action<Component, Component> ChangedEvent;
 
        public override void Recycle()
        {
            base.Recycle();

            transformLocalPosition = Vector3.zero;
            transformLocalRotation = Quaternion.identity;
            transformLocalScale = Vector3.one;

            ResetComponentDirtyFlag(true, true, true, true, true);

            _parentGeoAstroObject = null;

            _objectBase = null;
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

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

            //Make sure the gameObject is activated at least once so when we Destroy, the OnDestroy will be triggered even during Undo/Redo
            if (!isActiveAndEnabled)
                SceneManager.ActivateAll();

            UpdateIsGeoCoordinateTransform();

            SetComponentDirtyFlag(true, true, true);
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            if (initializingState == InstanceManager.InitializationContext.Reset)
            {
                transformLocalPosition = Vector3.zero;
                transformLocalRotation = Quaternion.identity;
                transformLocalScale = Vector3.one;
            }
        }

        protected override JSONNode GetInitializationJson(JSONNode initializeJSON)
        {
            initializeJSON = base.GetInitializationJson(initializeJSON);

            if (initializeJSON != null)
            {
                string transformName = nameof(Object.transform);
                if (initializeJSON[transformName] != null)
                    initializeJSON = initializeJSON[transformName];
            }

            return initializeJSON;
        }

        protected override void DetectChanges()
        {
            base.DetectChanges();

            if (_lastIndex != index)
            {
                int newValue = index;
                gameObject.transform.SetSiblingIndex(_lastIndex);
                index = newValue;
            }
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
#if UNITY_EDITOR
                InitializeToTopInInspector();
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        public void InitializeToTopInInspector()
        {
            bool objectIsSceneCamera = _objectBase != Disposable.NULL && _objectBase is Editor.SceneCamera;
            if (!objectIsSceneCamera)
            {
                UnityEngine.Component[] components = gameObject.GetComponents<UnityEngine.Component>();
                if (components[1] != this)
                    Editor.ComponentUtility.MoveComponentRelativeToComponent(this, components[0], false);
            }
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

            if (!siblingsChanged && objectBase == Disposable.NULL && GetSafeComponent<Object>(GetInitializeState()) != Disposable.NULL)
                siblingsChanged = true;

            return siblingsChanged;
        }

        protected override bool ChildrenHasChanged()
        {
            bool childrenChanged = base.ChildrenHasChanged();

            int childCount = transform.childCount + (objectBase != Disposable.NULL ? objectBase.GetAdditionalChildCount() : 0);
            if (!childrenChanged && children.Count != childCount)
                childrenChanged = true;
    
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
            if (!Object.ReferenceEquals(astroObject, null))
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

            if (ChildPropertyChangedEvent != null)
                ChildPropertyChangedEvent(property as TransformBase, name, newValue, oldValue);
        }

        public override bool IsDynamicProperty(int key)
        {
            bool isDynamicProperty = base.IsDynamicProperty(key);

            if (!isDynamicProperty && key == GetPropertyKey(nameof(index)))
                isDynamicProperty = true;

            return isDynamicProperty;
        }

        protected override bool CanBeDisabled()
        {
            return false;
        }

#if UNITY_EDITOR
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            //IsNullOrDisposed required
            if (!DisposeManager.IsNullOrDisposing(this) && ParentHasChanged())
                UpdateParent(null, true);
        }
#endif

        public override void UpdateParent(PropertyMonoBehaviour originator = null, bool isEditorUndo = false)
        {
            Originator(() =>
            {
                SetParent(GetParent() as TransformBase, !isEditorUndo && initialized);
            }, originator);
        }

        protected override PropertyMonoBehaviour GetParent()
        {
            PropertyMonoBehaviour parent = null;

            Type parentType = GetParentType();
            if (parentType != null)
                parent = InitializeComponent(transform.parent != null ? transform.parent.GetComponentInParent(parentType, true) : null);

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
            get 
            {
                if (_unityTransformComponents == null)
                    _unityTransformComponents = new TransformComponents3();
                return _unityTransformComponents; 
            }
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

            //Weird behavior in Unity where sometimes the localRotation value is being slightly altered after assignement
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

            //Weird behavior in Unity where sometimes the localRotation value is being slightly altered after assignement
            //Fix: Simply update the recorded transform with the new altered value if they where the same before
            if (localRotationChangeDetected && !DetectDirectTransformLocalRotationManipulation())
                InitLastLocalRotationField();

            return newLocalRotation;
        }
#endif

        private int _lastIndex;
        /// <summary>
        /// An index number representing the order of the childs in relation to their siblings.
        /// </summary>
        [Json]
        public int index
        {
            get { return transform.GetSiblingIndex(); }
            set 
            {
                int oldValue = index;
                if (HasChanged(value, oldValue))
                {
                    transform.SetSiblingIndex(value);
                    _lastIndex = value;
                    PropertyAssigned(this, nameof(index), value, oldValue);
                }
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
            get { return _isGeoCoordinateTransform; }
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
            get { return children != null ? children.Count : 0; }
        }

        private Vector3 _lastTransformLocalPosition;
        public Vector3 transformLocalPosition
        {
            get { return transform.localPosition; }
            set
            {
                if (_lastTransformLocalPosition.Equals(value))
                    return;
                transform.localPosition = value;
                InitLastLocalPositionField();
            }
        }

        private Quaternion _lastTransformLocalRotation;
        protected Quaternion transformLocalRotation
        {
            get { return transform.localRotation; }
            set
            {
                if (_lastTransformLocalRotation.Equals(value))
                    return;
                transform.localRotation = value;
                InitLastLocalRotationField();
            }
        }

        private Vector3 _lastTransformLocalScale;
        protected Vector3 transformLocalScale
        {
            get { return transform.localScale; }
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
            get { return _positionDirty; }
            set { _positionDirty = value; }
        }

        protected virtual bool rotationDirty
        {
            get { return _rotationDirty; }
            set { _rotationDirty = value; }
        }

        protected virtual bool lossyScaleDirty
        {
            get { return _lossyScaleDirty; }
            set { _lossyScaleDirty = value; }
        }

        protected bool localToWorldMatrixDirty
        {
            get { return _localToWorldMatrixDirty; }
            private set { _localToWorldMatrixDirty = value; }
        }

        protected bool worldToLocalMatrixDirty
        {
            get { return _worldToLocalMatrixDirty; }
            private set { _worldToLocalMatrixDirty = value; }
        }

        protected override bool AddProperty(PropertyMonoBehaviour child)
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
                        if (IsUserChangeContext())
                            EditorUndoRedoDetected();
#endif
                    }
                }
                else
                {
                    if (base.AddProperty(child))
                    {
                        added = true;

                        if (child is TransformBase)
                            AddChildDelegates(child as TransformBase);

                        if (ChildAddedEvent != null)
                            ChildAddedEvent(this, child);
                    }
                }
            }, child);

            return added;
        }

        protected override bool RemoveProperty(PropertyMonoBehaviour child)
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
                        if (IsUserChangeContext())
                            EditorUndoRedoDetected();
#endif
                    }
                }
                else
                {
                    if (base.RemoveProperty(child))
                    {
                        removed = true;

                        if (child is TransformBase)
                            RemoveChildDelegates(child as TransformBase);
                   

                        if (ChildRemovedEvent != null)
                            ChildRemovedEvent(this, child);
                    }
                }
            }, child);

            return removed;
        }

        public Object objectBase
        {
            get { return _objectBase; }
            protected set { SetObjectBase(value); }
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
            get { return _parentObject; }
            protected set { SetParentObject(value); }
        }

        protected bool SetParentObject(Object value)
        {
            return SetValue(nameof(parentObject), value, ref _parentObject, (newValue, oldValue) =>
            {
                if (!IsDisposing())
                {
                    if (!UpdateParentGeoAstroObject())
                        UpdateIsGeoCoordinateTransform();
                }
            });
        }

        public GeoAstroObject parentGeoAstroObject
        {
            get { return _parentGeoAstroObject; }
            private set { SetParentGeoAstroObject(value); }
        }

        protected bool SetParentGeoAstroObject(GeoAstroObject value)
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
                parentObject = transform.parent != null ? transform.parent.GetComponent<Object>() : null;

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

        protected override bool IncludeParentJson()
        {
            return true;
        }

        protected override JSONNode parentJson
        {
            get { return parent != Disposable.NULL ? JsonUtility.ToJson(parent.id) : null; }
            set 
            {
                if (JsonUtility.FromJson(out SerializableGuid parsedParentId, value))
                    SetParent(parsedParentId); 
            }
        }

        public bool SetParent(Guid id, bool worldPositionStays = true)
        {
            if ((parent == Disposable.NULL && id == null) || (parent != Disposable.NULL && parent.id == id))
                return false;

            return SetParent(instanceManager.GetTransform(id), worldPositionStays);
        }

        public virtual bool SetParent(TransformBase value, bool worldPositionStays = true)
        {
            bool changed = false;

            if (base.SetParent(value))
            {
                if (!IsDisposing())
                {
                    Transform newTransform = value != Disposable.NULL ? value.transform : null;
                    if (transform.parent != newTransform)
                        transform.SetParent(newTransform, false);

                    InitLastTransformFields();
                }

                changed = true;
            }

            return changed;
        }

#if UNITY_EDITOR
        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                UpdateTransformHideFlags();

                return true;
            }
            return false;
        }
#endif

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

        protected bool DetectDirectTransformManipulation()
        {
            return DetectDirectTransformLocalPositionManipulation() || DetectDirectTransformLocalRotationManipulation() || DetectDirectTransformLocalScaleManipulation();
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

        public void RevertUnityLocalPosition()
        {
            transform.localPosition = _lastTransformLocalPosition;
        }

        public void RevertUnityLocalRotation()
        {
            transform.localRotation = _lastTransformLocalRotation;
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
#if UNITY_EDITOR
            if (!DisposeManager.IsNullOrDisposing(this))
                transform.hideFlags |= HideFlags.HideInInspector;
#endif
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

            if (name == nameof(GeoAstroObject.sphericalRatio) || name == nameof(GeoAstroObject.size))
                changedComponents |= Component.Position;
            if (name == nameof(GeoAstroObject.sphericalRatio))
                changedComponents |= Component.Rotation;

            if (changedComponents != Component.None)
            {
                IterateOverChildrenAndSiblings(
                    (child) =>
                    {
                        if (child is TransformBase)
                        {
                            TransformBase transform = child as TransformBase;
                            if (transform.initialized)
                                transform.ParentChanged(changedComponents, Component.None, this);
                        }
                    });
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
                IterateOverChildrenAndSiblings(
                    (child) =>
                    {
                        if (child is TransformBase)
                            UpdateChild(child as TransformBase);
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

        protected override bool ApplyBeforeChildren(Action<PropertyMonoBehaviour> callback)
        {
            bool containsDisposed = base.ApplyBeforeChildren(callback);

            if (!Object.ReferenceEquals(_objectBase, null) && TriggerCallback(_objectBase, callback))
                containsDisposed = true;

            return containsDisposed;
        }

        public void IterateOverRootChildren<T>(Func<T, bool> callback) where T : PropertyMonoBehaviour
        {
            for (int i = childCount - 1 ; i >= 0 ; i--)
            {
                PropertyMonoBehaviour propertyMonoBehaviour = children[i];
                if (propertyMonoBehaviour is T  && propertyMonoBehaviour != Disposable.NULL && !callback(propertyMonoBehaviour as T))
                    return;
            }
        }

        public void IterateOverChildrenObject<T>(Func<T, bool> callback) where T : Object
        {
            IterateOverRootChildren<TransformDouble>((transform) => 
            {
                if (transform.objectBase != Disposable.NULL && transform.objectBase is T && !callback(transform.objectBase as T))
                    return false;
                return true;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void HierarchicalActivate()
        {
            InhibitEnableDisableAll();
            bool lastActiveSelf = gameObject.activeSelf;
            gameObject.SetActive(true);
            base.HierarchicalActivate();
            gameObject.SetActive(lastActiveSelf);
            UninhibitEnableDisableAll();
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
            {
                ChildAddedEvent = null;
                ChildRemovedEvent = null;
                ChildPropertyChangedEvent = null;
                ChangedEvent = null;

                return true;
            }
            return false;
        }

        protected override DisposeManager.DestroyContext OverrideDestroyingState(DisposeManager.DestroyContext destroyingState)
        {
            DisposeManager.DestroyContext overrideDestroyingState = base.OverrideDestroyingState(destroyingState);

#if UNITY_EDITOR
            if (_objectBase != Disposable.NULL && _objectBase is Editor.SceneCamera)
                overrideDestroyingState = DisposeManager.DestroyContext.Programmatically;
#endif

            return overrideDestroyingState;
        }

        public T GetSafeComponent<T>(InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) where T : UnityEngine.Component { return transform.GetSafeComponent<T>(initializationState); }

        public UnityEngine.Component GetSafeComponent(Type type, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) { return transform.GetSafeComponent(type, initializationState); }

        public T GetSafeComponentInParent<T>(bool includeInactive, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) where T : UnityEngine.Component { return transform.GetSafeComponentInParent<T>(includeInactive, initializationState); }

        public UnityEngine.Component GetSafeComponentInParent(Type type, bool includeInactive, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) { return transform.GetSafeComponentInParent(type, includeInactive, initializationState); }

        public List<T> GetSafeComponents<T>(InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) where T : UnityEngine.Component { return transform.GetSafeComponents<T>(initializationState); }

        public List<UnityEngine.Component> GetSafeComponents(Type type, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) { return transform.GetSafeComponents(type, initializationState); }

        public List<T> GetSafeComponentsInChildren<T>(bool includeSibling = false, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) where T : UnityEngine.Component { return transform.GetSafeComponentsInChildren<T>(includeSibling, initializationState); }

        public List<UnityEngine.Component> GetSafeComponentsInChildren(Type type, bool includeSibling = false, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically) { return transform.GetSafeComponentsInChildren(type, includeSibling, initializationState); }
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
            get { return _changed; }
        }

        public void Changed()
        {
            _changed = true;
        }

        public void ResetChanged()
        {
            _changed = false;
        }

        public virtual void OnDisposed()
        {
            ResetChanged();
        }
    }
}