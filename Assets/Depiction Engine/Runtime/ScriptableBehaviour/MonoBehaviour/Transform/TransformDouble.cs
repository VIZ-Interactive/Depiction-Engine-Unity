// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// 64 bit double version of the Transform.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Transform")]
    public class TransformDouble : TransformBase
    {
        //Maximum origin distance Unity supports before printing out 'Object is too large or too far away from the origin.' Errors
        private const float UNITY_MAX_MESH_DISTANCE = 10000000000000000000.0f;

        [SerializeField, LabelOverride("Position"), Tooltip("Position of the transform relative to the parent transform.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowLocalPosition))]
#endif
        private Vector3Double _localPosition;
        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowGeoCoordinate)), Tooltip("Geo Coordinate of the transform relative to the parent "+nameof(GeoAstroObject)+".")]
#endif
        private GeoCoordinate3Double _geoCoordinate;

#if UNITY_EDITOR
        public static string GetLocalEditorEulerAnglesHintName() { return nameof(_localEditorEulerAnglesHint); }
        [SerializeField, LabelOverride("Rotation"), Tooltip("The rotation of the transform relative to the transform rotation of the parent.")]
        private Vector3Double _localEditorEulerAnglesHint;
#endif
        [SerializeField, HideInInspector]
        private QuaternionDouble _localRotation;

        [SerializeField, LabelOverride("Scale"), Tooltip("The scale of the transform relative to the GameObjects parent.")]
        private Vector3Double _localScale;

        private LocalPositionParam _localPositionParam;
        private LocalRotationParam _localRotationParam;
        private LocalScaleParam _localScaleParam;

        public Action<LocalPositionParam, LocalRotationParam, LocalScaleParam, Camera> ObjectCallback;

#if UNITY_EDITOR
        private bool GetShowLocalPosition()
        {
            return !isGeoCoordinateTransform;
        }

        private bool GetShowGeoCoordinate()
        {
            return isGeoCoordinateTransform;
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            RemoveOriginShiftDirty(GetInstanceID());

            _worldToLocalMatrix = default;
            _localToWorldMatrix = default;

            localPositionParam.Recycle();
            localRotationParam.Recycle();
            localScaleParam.Recycle();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            TransformDouble newParent = GetParent() as TransformDouble;

            Vector3Double localPosition = this.localPosition;
            QuaternionDouble localRotation = this.localRotation;
            Vector3Double localScale = this.localScale;

            if (initializingContext == InitializationContext.Editor || initializingContext == InitializationContext.Programmatically || initializingContext == InitializationContext.Reset)
            {
                localPosition = transformLocalPosition;

#if UNITY_EDITOR
                if (renderingManager.wasFirstUpdated && renderingManager.originShifting && (initializingContext == InitializationContext.Reset || initializingContext == InitializationContext.Editor) && transformLocalPosition != Vector3.zero)
                {
                    Editor.SceneCamera sceneCamera = Editor.SceneViewDouble.lastActiveOrMouseOverSceneViewDouble != Disposable.NULL ? Editor.SceneViewDouble.lastActiveOrMouseOverSceneViewDouble.camera : null;
                    localPosition += sceneCamera.GetOrigin();
                }
#endif
     
                localRotation = transformLocalRotation;
                localScale = transformLocalScale;
            }
            else if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                if (parent != newParent)
                {
                    TransformComponents3Double components = new(localPosition, localRotation, localScale);

                    if (parent != Disposable.NULL)
                    {
                        Vector3Double position = GetPosition(parent, components.localPosition);
                        QuaternionDouble rotation = GetRotation(parent, components.localRotation);
                        Vector3Double lossyScale = GetLossyScale(parent, components.localPosition, components.localRotation, components.localScale);
                        Matrix4x4Double localToWorldMatrix = GetLocalToWorldMatrix(parent, components.localPosition, components.localRotation, components.localScale);

                        components = GetRelativeTransformComponents(newParent, position, rotation, lossyScale, localToWorldMatrix);
                    }

                    localPosition = components.localPosition;
                    localRotation = components.localRotation;
                    localScale = components.localScale;
                }
            }

            SetLocalPosition(localPosition, false);
            if (IsGeoCoordinateContext())
                SetGeoCoordinate(GetGeoCoordinateFromLocalPoint(localPosition, FindParentGeoAstroObject(), newParent), false, false);

            SetLocalRotation(localRotation);
            SetLocalScale(localScale);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastLocalPosition = localPosition;
                _lastGeoCoordinate = geoCoordinate;
                _lastLocalRotation = localRotation;
                _lastLocalScale = localScale;
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private Vector3Double _lastLocalPosition;
        private GeoCoordinate3Double _lastGeoCoordinate;
        private QuaternionDouble _lastLocalRotation;
        private Vector3Double _lastLocalScale;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            if (isGeoCoordinateTransform)
                SerializationUtility.PerformUndoRedoPropertyChange((value) => { geoCoordinate = value; }, ref _geoCoordinate, ref _lastGeoCoordinate);
            else
                SerializationUtility.PerformUndoRedoPropertyChange((value) => { localPosition = value; }, ref _localPosition, ref _lastLocalPosition);
            SerializationUtility.PerformUndoRedoPropertyChange((value) => { localRotation = value; }, ref _localRotation, ref _lastLocalRotation);
            SerializationUtility.PerformUndoRedoPropertyChange((value) => { localScale = value; }, ref _localScale, ref _lastLocalScale);
        }
#endif

        /// <summary>
        /// Returns true if the transform is a child.
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public bool IsChildOf(TransformDouble transform)
        {
            if (transform != Disposable.NULL)
            {
                TransformDouble parent = this.parent;
                while (parent != Disposable.NULL)
                {
                    if (parent == transform)
                        return true;
                    parent = parent.parent;
                }
            }

            return false;
        }

        private LocalPositionParam localPositionParam
        {
            get => _localPositionParam ??= new LocalPositionParam();
        }

        private LocalRotationParam localRotationParam
        {
            get => _localRotationParam ??= new LocalRotationParam();
        }

        private LocalScaleParam localScaleParam
        {
            get => _localScaleParam ??= new LocalScaleParam();
        }

        /// <summary>
        /// The parent of the Transform.
        /// </summary>
        [Json(propertyName: nameof(parentJson))]
        public new TransformDouble parent
        {
            get => base.parent as TransformDouble;
            set => SetParent(value);
        }

        public Vector3Double forward { get { return rotation * Vector3Double.forward; } }
        public Vector3Double back { get { return rotation * Vector3Double.back; } }
        public Vector3Double right { get { return rotation * Vector3Double.right; } }
        public Vector3Double left { get { return rotation * Vector3Double.left; } }
        public Vector3Double up { get { return rotation * Vector3Double.up; } }
        public Vector3Double down { get { return rotation * Vector3Double.down; } }

        private Matrix4x4Double _worldToLocalMatrix;
        /// <summary>
        /// Matrix that transforms a point from world space into local space (Read Only).
        /// </summary>
        public Matrix4x4Double worldToLocalMatrix
        {
            get
            {
                if (worldToLocalMatrixDirty)
                {
                    Vector3Double inverseLocalScale = localScale.Invert();
                    QuaternionDouble inverseLocalRotation = QuaternionDouble.Inverse(localRotation);

                    _worldToLocalMatrix = Matrix4x4Double.Scale(inverseLocalScale) * Matrix4x4Double.Rotate(inverseLocalRotation);

                    Vector3Double parentInverseTranslation = Vector3Double.zero;
                    if (parent != Disposable.NULL)
                    {
                        _worldToLocalMatrix *= parent.worldToLocalMatrix;
                        parentInverseTranslation = parent.worldToLocalMatrix.GetColumn(3);
                    }
                    Vector3Double translation = Vector3Double.Scale(inverseLocalRotation * (parentInverseTranslation - localPosition), inverseLocalScale);
                    _worldToLocalMatrix.SetColumn(3, new Vector4Double(translation.x, translation.y, translation.z, 1.0d));

                    ResetComponentDirtyFlag(false, false, false, true);
                }
                return _worldToLocalMatrix;
            }
        }


        private Matrix4x4Double _localToWorldMatrix;
        /// <summary>
        /// Matrix that transforms a point from local space into world space (Read Only).
        /// </summary>
        public Matrix4x4Double localToWorldMatrix
        {
            get
            {
                if (localToWorldMatrixDirty)
                {
                    _localToWorldMatrix = GetLocalToWorldMatrix(parent, localPosition, localRotation, localScale);
                    ResetComponentDirtyFlag(false, false, false, false, true);
                }
                return _localToWorldMatrix;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Matrix4x4Double GetLocalToWorldMatrix(TransformDouble parent, Vector3Double localPosition, QuaternionDouble localRotation, Vector3Double localScale)
        {
            Matrix4x4Double localToWorldMatrix = Matrix4x4Double.TRS(localPosition, localRotation, localScale);

            if (parent != Disposable.NULL)
                localToWorldMatrix = parent.localToWorldMatrix * localToWorldMatrix;

            return localToWorldMatrix;
        }

        /// <summary>
        /// Is the property name <see cref="DepictionEngine.TransformDouble.position"/>, <see cref="DepictionEngine.TransformDouble.rotation"/> or <see cref="DepictionEngine.TransformDouble.lossyScale"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsTransformProperty(string name)
        {
            return name == nameof(position) || name == nameof(rotation) || name == nameof(lossyScale);
        }

        protected override bool positionDirty
        {
            set { SetValue(nameof(position), value, ref _positionDirty); }
        }

        protected override bool rotationDirty
        {
            set { SetValue(nameof(rotation), value, ref _rotationDirty); }
        }

        protected override bool lossyScaleDirty
        {
            set { SetValue(nameof(lossyScale), value, ref _lossyScaleDirty); }
        }

        /// <summary>
        /// The world space position of the Transform.
        /// </summary>
        private Vector3Double _position;
        public Vector3Double position
        {
            get
            {
                if (positionDirty)
                {
                    _position = GetPosition(parent, localPosition);
                    ResetComponentDirtyFlag(true);
                }
                return _position;
            }
            set
            {
                localPosition = parent != Disposable.NULL ? parent.InverseTransformPoint(value) : value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3Double GetPosition(TransformDouble parent, Vector3Double localPosition)
        {
            return parent != Disposable.NULL ? parent.TransformPoint(localPosition) : localPosition;
        }

        /// <summary>
        /// Position of the transform relative to the parent transform, in Json format.
        /// </summary>
        public JSONNode localPositionJson
        {
            get { return !isGeoCoordinateTransform ? JsonUtility.ToJson(localPosition) : null; }
            set
            {
                if (JsonUtility.FromJson(out Vector3Double parsedLocalPosition, value))
                    localPosition = parsedLocalPosition;
            }
        }

        private bool IncludeLocalPosition()
        {
            return !isGeoCoordinateTransform;
        }

        /// <summary>
        /// Position of the transform relative to the parent transform.
        /// </summary>
        [Json(conditionalMethod: nameof(IncludeLocalPosition), propertyName: nameof(localPositionJson))]
        public Vector3Double localPosition
        {
            get { return _localPosition; }
            set
            {
                if (!localPositionParam.isGeoCoordinate)
                    SetComponents(localPositionParam.SetValue(value), UpdateLocalRotationParam(), UpdateLocalScaleParam());
                else
                    geoCoordinate = GetGeoCoordinateFromLocalPoint(value);
            }
        }

        private bool SetLocalPosition(Vector3Double value, bool deriveGeoordinate = true)
        {
            return SetValue(nameof(localPosition), ValidateVector3Double(value), ref _localPosition,
                (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastLocalPosition = newValue;
#endif
                    SetComponentDirtyFlag(true);
                    if (deriveGeoordinate)
                        SetGeoCoordinate(GetGeoCoordinateFromLocalPoint(_localPosition), false);

                    OriginShiftDirty(this);
                });
        }

        /// <summary>
        /// Geo Coordinate of the transform relative to the parent <see cref="DepictionEngine.GeoAstroObject"/>, in Json format.
        /// </summary>
        public JSONNode geoCoordinateJson
        {
            get { return isGeoCoordinateTransform ? JsonUtility.ToJson(geoCoordinate) : null; }
            set
            {
                if (JsonUtility.FromJson(out GeoCoordinate3Double parsedGeoCoordinate, value))
                    geoCoordinate = parsedGeoCoordinate;
            }
        }

        private bool IncludeGeoCoordinate()
        {
            return isGeoCoordinateTransform;
        }

        /// <summary>
        /// Geo Coordinate of the transform relative to the parent <see cref="DepictionEngine.GeoAstroObject"/>.
        /// </summary>
        [Json(conditionalMethod: nameof(IncludeGeoCoordinate), propertyName: nameof(geoCoordinateJson))]
        public GeoCoordinate3Double geoCoordinate
        {
            get { return _geoCoordinate; }
            set
            {
                if (localPositionParam.isGeoCoordinate)
                    SetComponents(localPositionParam.SetValue(value), UpdateLocalRotationParam(), UpdateLocalScaleParam());
                else
                    localPosition = GetLocalPointFromGeoCoordinate(value);
            }
        }

        private bool SetGeoCoordinate(GeoCoordinate3Double value, bool deriveLocalPosition = true, bool forceDeriveLocalPosition = false)
        {
            if (SetValue(nameof(geoCoordinate), value, ref _geoCoordinate) || (forceDeriveLocalPosition && deriveLocalPosition))
            {
#if UNITY_EDITOR
                _lastGeoCoordinate = value;
#endif
                SetComponentDirtyFlag(true);

                if (deriveLocalPosition)
                    SetLocalPosition(GetLocalPointFromGeoCoordinate(_geoCoordinate), false);

                return true;
            }
            return false;
        }

        private QuaternionDouble _rotation;
        /// <summary>
        /// A Quaternion that stores the rotation of the Transform in world space.
        /// </summary>
        public QuaternionDouble rotation
        {
            get
            {
                if (rotationDirty)
                {
                    _rotation = GetRotation(parent, localRotation);
                    ResetComponentDirtyFlag(false, true);
                }
                return _rotation;
            }
            set
            {
                QuaternionDouble localRotation;
                if (parent != Disposable.NULL)
                {
                    Vector3 negativeAxes = GetNegativeAxes(parent);
                    localRotation = parent != Disposable.NULL ? QuaternionDouble.Inverse(parent.rotation) * value : value;
                    localRotation = localRotation.ReflectQuaternionAroundAxes(negativeAxes).FlipQuaternionAroundAxes(negativeAxes);
                }
                else
                    localRotation = value;
                this.localRotation = localRotation;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected QuaternionDouble GetRotation(TransformDouble parent, QuaternionDouble localRotation)
        {
            QuaternionDouble newRotation;

            if (parent != Disposable.NULL)
            {
                Vector3 negativeAxes = GetNegativeAxes(parent);
                newRotation = parent.rotation * localRotation.ReflectQuaternionAroundAxes(negativeAxes).FlipQuaternionAroundAxes(negativeAxes);
            }
            else
                newRotation = localRotation;

            return newRotation;
        }

        /// <summary>
        /// Rotates the transform about axis passing through point in world coordinates by angle degrees.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RotateAround(Vector3Double point, Vector3Double axis, double angle)
        {
            QuaternionDouble rot = QuaternionDouble.AngleAxis(angle, axis);

            position = point + rot * (position - point);
         
            rotation *= QuaternionDouble.Inverse(rotation) * rot * rotation;
        }

#if UNITY_EDITOR
        private Vector3Double localEditorEulerAnglesHint
        {
            set { localRotation = GetLocalRotationFromInspectorEuler(value); }
        }
#endif

        /// <summary>
        /// The rotation as Euler angles in degrees.
        /// </summary>
        public Vector3Double localEulerAngles
        {
            get { return localRotation.eulerAngles; }
            set { localRotation = QuaternionDouble.Euler(value); }
        }

        /// <summary>
        /// The rotation of the transform relative to the transform rotation of the parent, in Json format.
        /// </summary>
        public JSONNode localRotationJson
        {
            get { return JsonUtility.ToJson(localRotation); }
            set
            {
                if (JsonUtility.FromJson(out QuaternionDouble parsedLocalRotation, value))
                    localRotation = parsedLocalRotation;
            }
        }

        /// <summary>
        /// The rotation of the transform relative to the transform rotation of the parent.
        /// </summary>
        [Json(propertyName: nameof(localRotationJson))]
        public QuaternionDouble localRotation
        {
            get { return _localRotation; }
            set { SetComponents(UpdateLocalPositionParam(), localRotationParam.SetValue(value), UpdateLocalScaleParam()); }
        }

        private bool SetLocalRotation(QuaternionDouble value)
        {
            return SetValue(nameof(localRotation), ValidateQuaternionDouble(value.normalized), ref _localRotation, 
                (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastLocalRotation = newValue;
                    _localEditorEulerAnglesHint = GetInspectorEulerFromLocalRotation(newValue);
#endif
                    SetComponentDirtyFlag(false, true);

                    OriginShiftDirty(this);
                });
        }

        /// <summary>
        /// The global scale of the object (Read Only).
        /// </summary>
        private Vector3Double _lossyScale;
        public Vector3Double lossyScale
        {
            get
            {
                if (lossyScaleDirty)
                {
                    _lossyScale = (Matrix4x4Double.Rotate(QuaternionDouble.Inverse(rotation)) * localToWorldMatrix).GetDiagonalComponentsOnly();
                    ResetComponentDirtyFlag(false, false, true);
                }
                return _lossyScale;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3Double GetLossyScale(TransformDouble parent, Vector3Double localPosition, QuaternionDouble localRotation, Vector3Double localScale)
        {
            return (Matrix4x4Double.Rotate(QuaternionDouble.Inverse(GetRotation(parent, localRotation))) * GetLocalToWorldMatrix(parent, localPosition, localRotation, localScale)).GetDiagonalComponentsOnly();
        }

        /// <summary>
        /// The scale of the transform relative to the GameObjects parent, in Json format.
        /// </summary>
        public JSONNode localScaleJson
        {
            get { return JsonUtility.ToJson(localScale); }
            set
            {
                if (JsonUtility.FromJson(out Vector3Double parsedLocalScale, value))
                    localScale = parsedLocalScale;
            }
        }

        /// <summary>
        /// The scale of the transform relative to the GameObjects parent.
        /// </summary>
        [Json(propertyName: nameof(localScaleJson))]
        public Vector3Double localScale
        {
            get { return _localScale; }
            set { SetComponents(UpdateLocalPositionParam(), UpdateLocalRotationParam(), localScaleParam.SetValue(value)); }
        }

        private bool SetLocalScale(Vector3Double value)
        {
            return SetValue(nameof(localScale), value, ref _localScale, 
                (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    _lastLocalScale = newValue;
#endif
                    SetComponentDirtyFlag(false, false, true);

                    OriginShiftDirty(this);
                });
        }

        public LocalPositionParam UpdateLocalPositionParam()
        {
            localPositionParam.SetValue(_localPosition);
            localPositionParam.SetValue(localPositionParam.isGeoCoordinate ? _geoCoordinate : GeoCoordinate3Double.zero);
            localPositionParam.ResetChanged();
            return localPositionParam;
        }

        public LocalRotationParam UpdateLocalRotationParam()
        {
            localRotationParam.SetValue(_localRotation);
            localRotationParam.ResetChanged();
            return localRotationParam;
        }

        public LocalScaleParam UpdateLocalScaleParam()
        {
            localScaleParam.SetValue(_localScale);
            localScaleParam.ResetChanged();
            return localScaleParam;
        }

        public GeoCoordinate3Double GetGeoCoordinate()
        {
            return isGeoCoordinateTransform ? geoCoordinate : GetGeoCoordinateFromLocalPoint(localPosition);
        }

        protected override void IsGeoCoordinateTransformChanged()
        {
            base.IsGeoCoordinateTransformChanged();
           
            localPositionParam.isGeoCoordinate = isGeoCoordinateTransform;

            if (initialized)
            {
                if (isGeoCoordinateTransform)
                    SetGeoCoordinate(parentGeoAstroObject.GetGeoCoordinateFromPoint(position), false, false);
                else
                    SetGeoCoordinate(GeoCoordinate3Double.zero, false, false);
            }

            PropertyAssigned(this, nameof(localPosition), localPosition, localPosition);
            PropertyAssigned(this, nameof(geoCoordinate), geoCoordinate, geoCoordinate);
        }

        protected override void DetectUserChanges()
        {
            base.DetectUserChanges();

            if (DetectDirectTransformLocalPositionManipulation() || DetectDirectTransformLocalRotationManipulation() || DetectDirectTransformLocalScaleManipulation())
            {
#if UNITY_EDITOR
                if (SceneManager.IsUserChangeContext())
                    RegisterCompleteObjectUndo();
#endif
                SetComponents(CaptureLocalPosition(), CaptureLocalRotation(), CaptureLocalScale());

                InitLastTransformFields();
            }
        }

        protected LocalPositionParam CaptureLocalPosition()
        {
            Vector3 localPositionDelta = CaptureLocalPositionDelta();

            if (!Object.Equals(localPositionDelta, Vector3.zero))
            {
                Vector3Double localPosition = _localPosition + localPositionDelta;
                if (localPositionParam.isGeoCoordinate)
                    localPositionParam.SetValue(GetGeoCoordinateFromLocalPoint(localPosition));
                else
                    localPositionParam.SetValue(localPosition);
            }
            else
                UpdateLocalPositionParam();

            return localPositionParam;
        }

        protected LocalRotationParam CaptureLocalRotation()
        {
            Quaternion localRotationDelta = CaptureLocalRotationDelta();
            if (!Object.Equals(localRotationDelta, Quaternion.identity))
                localRotationParam.SetValue(_localRotation * (QuaternionDouble)localRotationDelta);
            else
                UpdateLocalRotationParam();
            return localRotationParam;
        }

        protected LocalScaleParam CaptureLocalScale()
        {
            Vector3 localScaleDelta = CaptureLocalScaleDelta();
            if (!Object.Equals(localScaleDelta, Vector3.zero))
                localScaleParam.SetValue(_localScale + localScaleDelta);
            else
                UpdateLocalScaleParam();
            return localScaleParam;
        }

        public override bool IsDynamicProperty(int key)
        {
            bool isDynamicProperty = base.IsDynamicProperty(key);

            if (!isDynamicProperty && objectBase != Disposable.NULL && objectBase.IsPhysicsObject())
            {
                if (key == GetPropertyKey(nameof(localPosition)) || key == GetPropertyKey(nameof(localRotation)))
                    isDynamicProperty = true;
            }

            return isDynamicProperty;
        }

        public override bool ForceUpdateTransform(out Component changedComponents, bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false, Camera camera = null, bool forceDeriveLocalPosition = false, bool triggerTransformChanged = true)
        {
            if (base.ForceUpdateTransform(out changedComponents, localPositionChanged, localRotationChanged, localScaleChanged, camera, forceDeriveLocalPosition, triggerTransformChanged))
            {
                LocalPositionParam localPositionParam = UpdateLocalPositionParam();
                if (localPositionChanged)
                    localPositionParam.Changed();

                LocalRotationParam localRotationParam = UpdateLocalRotationParam();
                if (localRotationChanged)
                    localRotationParam.Changed();

                LocalScaleParam localScaleParam = UpdateLocalScaleParam();
                if (localScaleChanged)
                    localScaleParam.Changed();

                changedComponents = SetComponents(localPositionParam, localRotationParam, localScaleParam, camera, forceDeriveLocalPosition, triggerTransformChanged);

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Component SetComponents(LocalPositionParam localPosition, LocalRotationParam localRotation, LocalScaleParam localScale, Camera camera = null, bool forceDeriveLocalPosition = false, bool triggerTransformChanged = true)
        {
            ObjectCallback?.Invoke(localPosition, localRotation, localScale, camera);

            Component changedComponents = Component.None;
            Component capturedComponents = Component.None;

            bool isUserChange = SceneManager.IsUserChangeContext();

            if (localPosition.changed && (!localPosition.isGeoCoordinate && SetLocalPosition(localPosition.vector3DoubleValue) || (localPosition.isGeoCoordinate && SetGeoCoordinate(localPosition.geoCoordinateValue, true, forceDeriveLocalPosition))))
            {
                changedComponents |= Component.LocalPosition | Component.Position;
                if (isUserChange)
                    capturedComponents |= Component.LocalPosition | Component.Position;
            }
            if (localRotation.changed && SetLocalRotation(localRotation.value))
            {
                changedComponents |= Component.LocalRotation | Component.Rotation;
                if (isUserChange)
                    capturedComponents |= Component.LocalRotation | Component.Rotation;
            }
            if (localScale.changed && SetLocalScale(localScale.value))
            {
                changedComponents |= Component.LocalScale | Component.LossyScale;
                if (isUserChange)
                    capturedComponents |= Component.LocalScale | Component.LossyScale;
            }

            if (triggerTransformChanged)
                TransformChanged(changedComponents, capturedComponents, this);
 
            return changedComponents;
        }

        private static HashSet<int> _originShiftDirty;
        private static void OriginShiftDirty(TransformDouble transformDouble)
        {
            _originShiftDirty ??= new HashSet<int>();
            _originShiftDirty.Add(transformDouble.GetInstanceID());
        }

        public static void RemoveOriginShiftDirty(int instanceId)
        {
            if (_originShiftDirty != null)
                _originShiftDirty.Remove(instanceId);
        }

        public static Vector3Double AddOrigin(Vector3 point)
        {
            return origin + point;
        }

        public static Vector3 SubtractOrigin(Vector3Double point)
        {
            return point - origin;
        }

        public static Vector3Double AddPointToCameraOrigin(Vector3 point, Camera camera)
        {
            if (RenderingManager.Instance().originShifting && camera != Disposable.NULL)
                return camera.GetOrigin() + point;
            else
                return point;
        }

        public static Vector3Double SubtractPointFromCameraOrigin(Vector3Double point, Camera camera)
        {
            if (RenderingManager.Instance().originShifting && camera != Disposable.NULL)
                return point - camera.GetOrigin();
            else
                return point;
        }

        private static Vector3Double origin
        {
            get { return _origin; }
        }

        public static OriginShiftSnapshot GetOriginShiftSnapshot()
        {
            return new OriginShiftSnapshot(_transformDoubles, _origin, _forceOriginShiftSelected);
        }

        public static void ApplyOriginShifting(OriginShiftSnapshot originShiftSnapshot)
        {
            ApplyOriginShifting(originShiftSnapshot.transforms, originShiftSnapshot.origin, originShiftSnapshot.forceOriginShiftSelected);
        }

        public static void ApplyOriginShifting(Vector3Double origin)
        {
            ApplyOriginShifting(null, origin, false);
        }

        private static List<TransformDouble> _transformDoubles;
        private static Vector3Double _origin;
        private static bool _forceOriginShiftSelected;
        public static void ApplyOriginShifting(List<TransformDouble> transformDoubles, Vector3Double origin, bool forceOriginShiftSelected = false, bool forceDirty = false)
        {
            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager == Disposable.NULL || !renderingManager.originShifting)
            {
                transformDoubles = null;
                origin = Vector3Double.zero;
                forceOriginShiftSelected = false;
                forceDirty = false;
            }

            bool dirty = forceDirty || (_originShiftDirty != null && _originShiftDirty.Count > 0);
            if (dirty || _forceOriginShiftSelected != forceOriginShiftSelected || _origin != origin || !CompareArrays(_transformDoubles, transformDoubles))
            {
                _transformDoubles = transformDoubles;
                _origin = origin;
                _forceOriginShiftSelected = forceOriginShiftSelected;

                if (transformDoubles != null)
                {
                    foreach (TransformDouble transformDouble in transformDoubles)
                    {
                        if (transformDouble != Disposable.NULL)
                            transformDouble.HierarchicalApplyOriginShifting(origin, forceOriginShiftSelected);
                    }
                }
                else
                {
                    SceneManager sceneManager = SceneManager.Instance(false);
                    if (sceneManager != Disposable.NULL)
                        sceneManager.HierarchicalApplyOriginShifting(origin, forceOriginShiftSelected);
                }
            }
        }

        private static bool CompareArrays(List<TransformDouble> transformDoubles, List<TransformDouble> otherTransformDoubles)
        {
            if (transformDoubles != null || otherTransformDoubles != null)
            {
                if (transformDoubles == null || otherTransformDoubles == null)
                    return false;

                if (transformDoubles.Count == otherTransformDoubles.Count)
                {
                    foreach (TransformDouble transformDouble in transformDoubles)
                    {
                        if (otherTransformDoubles.IndexOf(transformDouble) == -1)
                            return false;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool UpdateUnityTransformComponents(ref TransformComponents3 unityTransformComponents, Vector3Double origin, bool forceOriginShiftSelected)
        {
            RemoveOriginShiftDirty(GetInstanceID());

            bool originShifted = false;

            Vector3 localPosition = this.localPosition;
            Quaternion localRotation = this.localRotation;
            Vector3 localScale = this.localScale;

            if (origin != Vector3Double.zero)
            {
                bool requiresOriginShiftingPositioning = objectBase != Disposable.NULL && objectBase.RequiresPositioning();

#if UNITY_EDITOR
                if (!requiresOriginShiftingPositioning && forceOriginShiftSelected && Editor.Selection.GetTransformDoubleSelectionCount() > 0 && Editor.Selection.Contains(this))
                    requiresOriginShiftingPositioning = true;
#endif

                if (requiresOriginShiftingPositioning)
                {
                    Vector3Double localOrigin = ValidateVector3Double(parent != Disposable.NULL ? parent.InverseTransformPoint(origin) : origin);
                    
                    localPosition = ValidateVector3(this.localPosition - localOrigin);

                    originShifted = true;
                }
                else
                    localPosition = Vector3.zero;
            }

            unityTransformComponents.SetComponents(localPosition, localRotation, localScale);

            return originShifted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector3 ValidateVector3(Vector3 value)
        {
            if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z))
                value = Vector3.zero;
            else
            {
                if (value.x == float.PositiveInfinity || value.y == float.PositiveInfinity || value.z == float.PositiveInfinity)
                    value = Vector3.one * UNITY_MAX_MESH_DISTANCE;
                else if (value.x == float.NegativeInfinity || value.y == float.NegativeInfinity || value.z == float.NegativeInfinity)
                    value = Vector3.one * -UNITY_MAX_MESH_DISTANCE;
                else if (value.magnitude > UNITY_MAX_MESH_DISTANCE)
                    value = value.normalized * UNITY_MAX_MESH_DISTANCE;
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double GetLocalPointFromGeoCoordinate(GeoCoordinate3Double geoCoordinate)
        {
            Vector3Double point = GetPointFromGeoCoordinate(geoCoordinate);
            return parent != Disposable.NULL ? parent.InverseTransformPoint(point) : point;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double GetPointFromGeoCoordinate(GeoCoordinate3Double geoCoordinate)
        {
            GeoAstroObject parentGeoAstroObject = GetParentGeoAstroObject();
            return parentGeoAstroObject != Disposable.NULL ? parentGeoAstroObject.GetPointFromGeoCoordinate(geoCoordinate) : Vector3Double.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private GeoCoordinate3Double GetGeoCoordinateFromLocalPoint(Vector3Double localPoint)
        {
            return GetGeoCoordinateFromLocalPoint(localPoint, parentGeoAstroObject, parent);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private GeoCoordinate3Double GetGeoCoordinateFromLocalPoint(Vector3Double localPoint, GeoAstroObject parentGeoAstroObject, TransformDouble parent)
        {
            GeoCoordinate3Double geoCoordinate = GeoCoordinate3Double.zero;

            if (parent != Disposable.NULL && parentGeoAstroObject != Disposable.NULL)
            {
                if (parent != parentGeoAstroObject.transform)
                    localPoint = parentGeoAstroObject.transform.InverseTransformPoint(parent.TransformPoint(localPoint));

                geoCoordinate = parentGeoAstroObject.GetGeoCoordinateFromLocalPoint(localPoint);
            }

            return geoCoordinate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double TransformDirection(Vector3Double direction)
        {
            return rotation * direction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double TransformPoint(Vector3Double point)
        {
            return localToWorldMatrix.MultiplyPoint3x4(point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3Double InverseTransformPoint(Vector3Double point)
        {
            return worldToLocalMatrix.MultiplyPoint3x4(point);
        }

        public override bool SetParent(TransformBase value, bool worldPositionStays = true)
        {
            TransformComponents3Double relativeTransformComponents = null;

            if (worldPositionStays && !IsDisposing())
                relativeTransformComponents = GetRelativeTransformComponents(value as TransformDouble, position, rotation, lossyScale, localToWorldMatrix);

            if (base.SetParent(value, worldPositionStays))
            {
                if (relativeTransformComponents != null)
                {
                    SetLocalPosition(relativeTransformComponents.localPosition);
                    SetLocalRotation(relativeTransformComponents.localRotation);
                    SetLocalScale(relativeTransformComponents.localScale);
                }

                OriginShiftDirty(this);

                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TransformComponents3Double GetRelativeTransformComponents(TransformDouble newParent, Vector3Double position, QuaternionDouble rotation, Vector3Double lossyScale, Matrix4x4Double localToWorldMatrix)
        {
            Vector3Double localPosition = position;
            QuaternionDouble localRotation = rotation;
            Vector3Double localScale = lossyScale;

            if (newParent != Disposable.NULL)
            {
                Matrix4x4Double parentWorldToLocalMatrix = newParent.worldToLocalMatrix;
                Matrix4x4Double parentLocalToWorldMatrix = newParent.localToWorldMatrix;

                Matrix4x4Double targetLocalToWorldMatrix = localToWorldMatrix;
                Vector3Double targetPosition = position;
                QuaternionDouble targetRotation = rotation;

                Vector3Double parentNegativeAxes = GetNegativeAxes(newParent);

                QuaternionDouble parentInvRotation = QuaternionDouble.Inverse(newParent.rotation);
                Matrix4x4Double parentInvRotationMatrix = Matrix4x4Double.Rotate(parentInvRotation);

                QuaternionDouble relativeRotation = parentInvRotation * targetRotation;
                Matrix4x4Double relativeRotationMatrix = Matrix4x4Double.Rotate(relativeRotation);

                //LocalPosition
                Vector3Double newLocalPosition = parentWorldToLocalMatrix.MultiplyPoint3x4(targetPosition);

                //LocalRotation
                QuaternionDouble newLocalRotation = relativeRotation.ReflectQuaternionAroundAxes(parentNegativeAxes).FlipQuaternionAroundAxes(parentNegativeAxes);

                Vector3Double parentZeroScaleAxes = new(parentLocalToWorldMatrix.GetRow(0).magnitude, parentLocalToWorldMatrix.GetRow(1).magnitude, parentLocalToWorldMatrix.GetRow(2).magnitude);

                parentLocalToWorldMatrix = parentLocalToWorldMatrix.Negate3x3Columns(parentNegativeAxes);
                Matrix4x4Double parentLocalToWorldNoZeroMatrix = parentLocalToWorldMatrix.Set3x3Rows(parentZeroScaleAxes, 0.00000001d);

                Matrix4x4Double targetMatrix = parentLocalToWorldMatrix * relativeRotationMatrix;
                targetMatrix = Matrix4x4Double.Rotate(QuaternionDouble.Inverse(targetRotation)) * targetMatrix.ProjectMatrixOntoAxes(targetRotation);
                targetMatrix = Matrix4x4Double.Scale(targetMatrix.GetDiagonalComponentsOnly().Invert());

                Matrix4x4Double parentScaleMatrix = parentLocalToWorldNoZeroMatrix * parentInvRotationMatrix;
                parentScaleMatrix = parentScaleMatrix.ProjectMatrixOntoAxes(QuaternionDouble.identity);
                Matrix4x4Double parentMatrix = parentLocalToWorldNoZeroMatrix * parentInvRotationMatrix;
                parentMatrix = parentMatrix.ProjectMatrixOntoAxes(QuaternionDouble.identity);
                parentMatrix = Matrix4x4Double.Scale(parentMatrix.GetDiagonalComponentsOnly().Invert());

                Matrix4x4Double relativeRotationScaleMatrix = parentMatrix * (parentScaleMatrix * (targetLocalToWorldMatrix.ProjectMatrixOntoAxes(targetRotation) * targetMatrix));
                Vector3Double relativeZeroScaleAxes = GetZeroAxes(parentMatrix * (parentScaleMatrix * (Matrix4x4Double.Rotate(targetRotation) * targetMatrix)));
                relativeRotationScaleMatrix = parentInvRotationMatrix * relativeRotationScaleMatrix.Set3x3Columns(relativeZeroScaleAxes, 0.0d);
                relativeRotationScaleMatrix = relativeRotationScaleMatrix.Negate3x3Rows(parentNegativeAxes);

                //LocalScale
                Vector3Double newLocalScale = new(relativeRotationScaleMatrix.GetColumn(0).magnitude, relativeRotationScaleMatrix.GetColumn(1).magnitude, relativeRotationScaleMatrix.GetColumn(2).magnitude);
                Matrix4x4Double newMat = Matrix4x4Double.TRS(Vector3Double.zero, newLocalRotation, newLocalScale);
                double x = Vector3Double.Dot(newMat.GetColumn(0), relativeRotationScaleMatrix.GetColumn(0));
                double y = Vector3Double.Dot(newMat.GetColumn(1), relativeRotationScaleMatrix.GetColumn(1));
                double z = Vector3Double.Dot(newMat.GetColumn(2), relativeRotationScaleMatrix.GetColumn(2));
                newLocalScale = NegateScaleAxes(newLocalScale, new Vector3Double(x < 0.0d ? -1.0d : 1.0d, y < 0.0d ? -1.0d : 1.0d, z < 0.0d ? -1.0d : 1.0d));

                //TODO: Investigate using these functions to round numbers
                //Mathf.RoundBasedOnMinimumDifference()
                //Mathf.GetNumberOfDecimalsForMinimumDifference()

                localPosition = newLocalPosition;
                localRotation = newLocalRotation;
                localScale = newLocalScale;
            }

            return new TransformComponents3Double(localPosition, localRotation, localScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double GetZeroAxes(Matrix4x4Double relative)
        {
            double x = relative.GetColumn(0).magnitude > 1000000000.0d ? 0.0d : 1.0d;
            double y = relative.GetColumn(1).magnitude > 1000000000.0d ? 0.0d : 1.0d;
            double z = relative.GetColumn(2).magnitude > 1000000000.0d ? 0.0d : 1.0d;
            return new Vector3Double(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double NegateScaleAxes(Vector3Double scale, Vector3Double negativeAxis)
        {
            return new Vector3Double(scale.x * negativeAxis.x, scale.y * negativeAxis.y, scale.z * negativeAxis.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double GetNegativeAxes(TransformDouble transform)
        {
            Vector3Double negativeScale = transform.parent != Disposable.NULL ? GetNegativeAxes(transform.parent) : Vector3Double.one;
            if (transform.localScale.x < 0.0d)
                negativeScale.x = -negativeScale.x;
            if (transform.localScale.y < 0.0d)
                negativeScale.y = -negativeScale.y;
            if (transform.localScale.z < 0.0d)
                negativeScale.z = -negativeScale.z;
            return negativeScale;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3Double ValidateVector3Double(Vector3Double value)
        {
            if (double.IsNaN(value.x) || double.IsNaN(value.y) || double.IsNaN(value.z))
                value = Vector3Double.zero;
            else
            {
                if (value.x == double.PositiveInfinity || value.y == double.PositiveInfinity || value.z == double.PositiveInfinity)
                    value = Vector3Double.one * (double.MaxValue - double.Epsilon);
                if (value.x == double.NegativeInfinity || value.y == double.NegativeInfinity || value.z == double.NegativeInfinity)
                    value = Vector3Double.one * (double.MinValue + double.Epsilon);
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private QuaternionDouble ValidateQuaternionDouble(QuaternionDouble value)
        {
            if (value.x == 0.0d && value.y == 0.0d && value.z == 0.0d && value.w == 0.0d)
                value = QuaternionDouble.identity;
            if (double.IsNaN(value.x) || double.IsNaN(value.y) || double.IsNaN(value.z) || double.IsNaN(value.w))
                value = QuaternionDouble.identity;
            if (value.x == double.PositiveInfinity || value.y == double.PositiveInfinity || value.z == double.PositiveInfinity)
                value = QuaternionDouble.identity;
            if (value.x == double.NegativeInfinity || value.y == double.NegativeInfinity || value.z == double.NegativeInfinity)
                value = QuaternionDouble.identity;
            return value;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                ObjectCallback = null;

                return true;
            }
            return false;
        }

        private class TransformComponents3Double
        {
            private Vector3Double _localPosition;
            private QuaternionDouble _localRotation;
            private Vector3Double _localScale;

            public TransformComponents3Double(Vector3Double localPosition, QuaternionDouble localRotation, Vector3Double localScale)
            {
                _localPosition = localPosition;
                _localRotation = localRotation;
                _localScale = localScale;
            }

            public Vector3Double localPosition { get { return _localPosition; } }
            public QuaternionDouble localRotation { get { return _localRotation; } }
            public Vector3Double localScale { get { return _localScale; } }
        }
    }

    public class OriginShiftSnapshot
    {
        public List<TransformDouble> transforms;
        public Vector3Double origin;
        public bool forceOriginShiftSelected;

        public OriginShiftSnapshot(List<TransformDouble> transforms, Vector3Double origin, bool forceOriginShiftSelected)
        {
            this.transforms = transforms != null && transforms.Count > 0? new List<TransformDouble>(transforms) : null;
            this.origin = origin;
            this.forceOriginShiftSelected = forceOriginShiftSelected;
        }
    }

    public class TransformComponents2Double
    {
        public Vector3Double position;
        public QuaternionDouble rotation;

        public void Reset()
        {
            position = Vector3Double.zero;
            rotation = QuaternionDouble.identity;
        }
    }

    [Serializable]
    public class LocalPositionParam : TransformComponentParam
    {
        [SerializeField]
        private Vector3Double _vector3DoubleValue;
        [SerializeField]
        private GeoCoordinate3Double _geoCoordinateValue;
        [SerializeField]
        private bool _isGeoCoordinate;

        public LocalPositionParam()
        {
            _vector3DoubleValue = Vector3Double.zero;
            _geoCoordinateValue = GeoCoordinate3Double.zero;
        }

        public LocalPositionParam SetValue(Vector3Double value)
        {
            _vector3DoubleValue = value;

            Changed();

            return this;
        }

        public LocalPositionParam SetValue(GeoCoordinate3Double value)
        {
            _geoCoordinateValue = value;

            Changed();

            return this;
        }

        public bool isGeoCoordinate
        {
            get { return _isGeoCoordinate; }
            set
            {
                if (_isGeoCoordinate == value)
                    return;
                _isGeoCoordinate = value;
            }
        }

        public Vector3Double vector3DoubleValue
        {
            get { return _vector3DoubleValue; }
        }

        public GeoCoordinate3Double geoCoordinateValue
        {
            get { return _geoCoordinateValue; }
        }

        public override void Recycle()
        {
            base.Recycle();

            _vector3DoubleValue = Vector3Double.zero;
            _geoCoordinateValue = GeoCoordinate3Double.zero;
        }
    }

    [Serializable]
    public class LocalRotationParam : TransformComponentParam
    {
        [SerializeField]
        private QuaternionDouble _value;

        public LocalRotationParam()
        {
            _value = QuaternionDouble.identity;
        }

        public LocalRotationParam SetValue(QuaternionDouble value)
        {
            _value = value;

            Changed();

            return this;
        }

        public QuaternionDouble value
        {
            get { return _value; }
        }

        public override void Recycle()
        {
            base.Recycle();

            _value = QuaternionDouble.identity;
        }
    }

    [Serializable]
    public class LocalScaleParam : TransformComponentParam
    {
        [SerializeField]
        private Vector3Double _value;

        public LocalScaleParam()
        {
            _value = Vector3Double.one;
        }

        public LocalScaleParam SetValue(Vector3Double value)
        {
            _value = value;

            Changed();

            return this;
        }

        public Vector3Double value
        {
            get { return _value; }
        }

        public override void Recycle()
        {
            base.Recycle();

            _value = Vector3Double.one;
        }
    }

    [Serializable]
    public class ComponentChangedPending
    {
        public bool pending;

        public bool localPositionChanged;
        public bool localRotationChanged;
        public bool localScaleChanged;

        public ComponentChangedPending(bool localPositionChanged = false, bool localRotationChanged = false, bool localScaleChanged = false)
        {
            this.pending = false;
            this.localPositionChanged = localPositionChanged;
            this.localRotationChanged = localRotationChanged;
            this.localScaleChanged = localScaleChanged;
        }

        public void Clear()
        {
            pending = false;
            localPositionChanged = false;
            localRotationChanged = false;
            localScaleChanged = false;
        }
    }
}
