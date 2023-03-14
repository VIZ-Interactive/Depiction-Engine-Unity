// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    public class TargetControllerBase : ControllerBase
    {
        private const double MAX_DISTANCE = double.MaxValue;

        public const double PENETRATION_TRESHOLD = 0.05d;

        public const double DEFAULT_DISTANCE_VALUE = 1000.0d;

        [BeginFoldout("Target")]
        [SerializeField, ComponentReference, Tooltip("The id of the target.")]
        private SerializableGuid _targetId;
        [SerializeField, Tooltip("A min and max clamping values used on the "+nameof(distance)+".")]
        private Vector2Double _minMaxDistance;
        [SerializeField, Tooltip("The distance between the object and the target, in world units.")]
        private double _distance;

        [SerializeField, HideInInspector]
        private QuaternionDouble _rotation;

        [SerializeField, HideInInspector]
        private Object _target;

        private TransformDouble _targetTransform;

        protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {
            base.IterateOverComponentReference(callback);

            if (_targetId != null)
                callback(_targetId, UpdateTarget);
        }

        protected override bool Initialize(InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                UpdateTargetTransform();

                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => targetId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(targetId, target, initializingContext); }, initializingContext);
            InitValue(value => minMaxDistance = value, GetDefaultMinMaxDistance(), initializingContext);
            InitValue(value => distance = value, DEFAULT_DISTANCE_VALUE, initializingContext);
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveTargetDelegate(target);
                if (!IsDisposing())
                    AddTargetDelegate(target);

                RemoveTargetTransformDelegate(targetTransform);
                if (!IsDisposing())
                    AddTargetTranformsDelegate(targetTransform);

                return true;
            }
            return false;
        }

        private void RemoveTargetDelegate(Object target)
        {
            if (target is not null)
                target.PropertyAssignedEvent -= TargetPropertyAssignedHandler;
        }

        private void AddTargetDelegate(Object target)
        {
            if (target != Disposable.NULL)
                target.PropertyAssignedEvent += TargetPropertyAssignedHandler;
        }

        private void TargetPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(Object.transform))
                UpdateTargetTransform();
        }

        private void RemoveTargetTransformDelegate(TransformDouble targetTransform)
        {
            if (targetTransform is not null)
                targetTransform.ChangedEvent -= TargetTransformChanged;
        }

        private void AddTargetTranformsDelegate(TransformDouble targetTransform)
        {
            if (targetTransform != Disposable.NULL)
                targetTransform.ChangedEvent += TargetTransformChanged;
        }

        protected virtual void TargetTransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            bool rotationChanged = changedComponent.HasFlag(TransformBase.Component.Rotation);
            bool positionChanged = changedComponent.HasFlag(TransformBase.Component.Position);

            if (!_forcingTargetTransformUpdate && (rotationChanged || positionChanged))
                ForceUpdateTransform(true, true);
        }

        protected override bool TransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            if (base.TransformChanged(changedComponent, capturedComponent))
            {
                //Prevent user captured changes from taking effect
                bool rotationChanged = capturedComponent.HasFlag(TransformBase.Component.LocalRotation);
                bool positionChanged = capturedComponent.HasFlag(TransformBase.Component.LocalPosition);
                
                if (rotationChanged || positionChanged)
                    ForceUpdateTransformPending(true, true);

                return true;
            }
            return false;
        }

        protected virtual Vector2Double GetDefaultMinMaxDistance()
        {
            return new Vector2Double(PENETRATION_TRESHOLD, MAX_DISTANCE);
        }

        public virtual bool SetTargetPositionRotationDistance(Vector3Double targetPosition, QuaternionDouble rotation, double cameraDistance)
        {
            bool cameraDistanceChanged = SetDistance(cameraDistance);
            bool rotationChanged = SetRotation(rotation);
            bool targetPositionChanged = SetTargetPosition(targetPosition, false);

            //If target transform changes it will automaticaly trigger a forceUpdateTransform so there is no need to call it twice
            if (targetPositionChanged || rotationChanged || cameraDistanceChanged)
                return ForceUpdateTransform(true, true);

            return false;
        }

        protected virtual QuaternionDouble GetRotation()
        {
            return _rotation;
        }

        public QuaternionDouble rotation
        {
            get { return _rotation; }
            protected set
            {
                if (SetRotation(value))
                    ForceUpdateTransform();
            }
        }

        protected bool SetRotation(QuaternionDouble value)
        {
            if (_rotation == value)
                return false;

            _rotation = value;

            return true;
        }

        public virtual Object target
        {
            get { return _target; }
            set { targetId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
        }

        /// <summary>
        /// The id of the target.
        /// </summary>
        [Json]
        public SerializableGuid targetId
        {
            get { return _targetId; }
            set 
            {
                SetValue(nameof(targetId), value, ref _targetId, (newValue, oldValue) =>
                {
                    UpdateTarget();
                });
            }
        }

        private void UpdateTarget()
        {
            SetValue(nameof(target), GetComponentFromId<Object>(targetId), ref _target, (newValue, oldValue) =>
            {
                RemoveTargetDelegate(oldValue);
                if (oldValue is not null)
                    oldValue.targetController = null;

                AddTargetDelegate(newValue);
                if (newValue != Disposable.NULL)
                    newValue.targetController = this;

                UpdateTargetTransform();

                TargetChanged(newValue, oldValue);
            });
        }

        protected virtual void TargetChanged(Object newValue, Object oldValue)
        {

        }

        private void UpdateTargetTransform()
        {
            targetTransform = target != Disposable.NULL ? target.transform : null;
        }

        public GeoAstroObject GetTargetParentGeoAstroObject()
        {
            return targetTransform != Disposable.NULL ? targetTransform.parentGeoAstroObject : null;
        }

        public TransformDouble targetTransform
        {
            get { return _targetTransform; }
            private set 
            { 
                if (SetTargetTransform(value))
                    ForceUpdateTransform();
            }
        }

        protected virtual bool SetTargetTransform(TransformDouble value)
        {
            if (Object.ReferenceEquals(_targetTransform, value))
                return false;

            TransformDouble oldValue = _targetTransform;
            TransformDouble newValue = value;

            _targetTransform = newValue;

            RemoveTargetTransformDelegate(oldValue);
            AddTargetTranformsDelegate(newValue);

            return true;
        }

        public bool SetTargetPosition(Vector3Double value, bool forceUpdate = true)
        {
            if (!forceUpdate)
                _forcingTargetTransformUpdate = true;

            bool targetPositionChanged = false;
            if (targetTransform != Disposable.NULL)
            {
                if (targetTransform.position != value)
                {
                    targetTransform.position = value;
                    targetPositionChanged = true;
                }
            }
            
            _forcingTargetTransformUpdate = false;
           
            return targetPositionChanged;
        }

        /// <summary>
        /// The distance between the object and the target, in world units.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
        public double distance
        {
            get { return _distance; }
            set
            {
                if (SetDistance(value))
                    ForceUpdateTransform();
            }
        }

        protected bool SetDistance(double value)
        {
            return SetValue(nameof(distance), ClampDistance(value), ref _distance);
        }

        protected double ClampDistance(double distance)
        {
            return MathPlus.Clamp(distance, minMaxDistance.x, minMaxDistance.y);
        }

        /// <summary>
        /// A min and max clamping values used on the <see cref="DepictionEngine.TargetControllerBase.distance"/>. 
        /// </summary>
        [Json]
        public Vector2Double minMaxDistance
        {
            get { return GetMinMaxDistance(); }
            set { SetMinMaxDistance(value); }
        }

        protected virtual Vector2Double GetMinMaxDistance()
        {
            return _minMaxDistance;
        }

        protected virtual bool SetMinMaxDistance(Vector2Double value)
        {
            if (value.x < PENETRATION_TRESHOLD)
                value.x = PENETRATION_TRESHOLD;
            return SetValue(nameof(minMaxDistance), value, ref _minMaxDistance);
        }

        public Vector3Double GetCameraPosition(Vector3Double targetPosition, QuaternionDouble rotation, double distance)
        {
            return GetTargetPosition(targetPosition, rotation, -distance);
        }

        public Vector3Double GetTargetPosition()
        {
            return GetTargetPosition(transform.position, transform.rotation, distance);
        }

        public static Vector3Double GetTargetPosition(Vector3Double position, QuaternionDouble rotation, double distance)
        {
            return position + rotation * new Vector3Double(0.0f, 0.0f, distance);
        }

        public void GetGeoAstroObjectSurfaceComponents(out Vector3Double targetPosition, out QuaternionDouble rotation, out double cameraDistance, GeoAstroObject geoAstroObject)
        {
            Camera camera = objectBase as Camera;

            targetPosition = geoAstroObject.GetSurfacePointFromPoint(camera.transform.position);

            double tiltLimit = -2.0d;
            double cameraAltitude = geoAstroObject.GetGeoCoordinateFromPoint(camera.transform.position).altitude;
            if (cameraAltitude >= 0.0d)
            {
                rotation = QuaternionDouble.LookRotation((targetPosition - camera.transform.position).normalized, camera.transform.up) * QuaternionDouble.Euler(tiltLimit, 0.0d, 0.0d);

                cameraDistance = cameraAltitude;
            }
            else
            {
                rotation = geoAstroObject.GetUpVectorFromPoint(targetPosition) * QuaternionDouble.Euler(90.0d + tiltLimit, 0.0d, 0.0d);

                cameraDistance = geoAstroObject.radius;
            }
        }

        public override void UpdateControllerTransform(Camera camera)
        {
            base.UpdateControllerTransform(camera);

            if (camera != Disposable.NULL && camera == objectBase && targetTransform != Disposable.NULL)
            {
                UpdateTargetControllerTransform(camera);

                ValidateTargetControllerTransform(camera);
            }
        }

        protected virtual void UpdateTargetControllerTransform(Camera camera)
        {

        }

        protected virtual void ValidateTargetControllerTransform(Camera camera)
        {

        }

        private bool _forcingTargetTransformUpdate;
        protected override bool TransformControllerCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            if (base.TransformControllerCallback(localPositionParam, localRotationParam, localScaleParam, camera))
            {
                if (targetTransform != Disposable.NULL && targetTransform != transform)
                {
                    if (target.controller != Disposable.NULL)
                    {
                        _forcingTargetTransformUpdate = true;

                        TransformDouble updatingParent = target.transform.parent;
                        TransformDouble updateChild = target.transform;
                        while (updatingParent != Disposable.NULL)
                        {
                            if (updatingParent.UpdatingChildren())
                            {
                                TransformBase.Component parentChangedComponents = updatingParent.UpdateChild(updateChild);

                                TransformBase.Component updatedComponents = TransformDouble.GetUpdateComponentsFromParentChangedComponents(parentChangedComponents);

                                target.ClearForceUpdateTransformPending(updatedComponents.HasFlag(TransformBase.Component.Position), updatedComponents.HasFlag(TransformBase.Component.Rotation), updatedComponents.HasFlag(TransformBase.Component.LossyScale));

                                break;
                            }
                            updateChild = updatingParent;
                            updatingParent = updatingParent.parent;
                        }

                        target.ForceUpdateTransformIfPending();

                        _forcingTargetTransformUpdate = false;
                    }

                    QuaternionDouble rotation = GetRotation();

                    localRotationParam.SetValue(transform.parent != Disposable.NULL ? QuaternionDouble.Inverse(transform.parent.rotation) * rotation : rotation);

                    Vector3Double position = GetCameraPosition(targetTransform.position, rotation, distance);

                    if (localPositionParam.isGeoCoordinate)
                    {
                        if (transform.parentGeoAstroObject.IsValidSphericalRatio())
                            localPositionParam.SetValue(transform.parentGeoAstroObject.GetGeoCoordinateFromPoint(position));
                    }
                    else
                        localPositionParam.SetValue(transform.parent != Disposable.NULL ? transform.parent.InverseTransformPoint(position) : position);
                }
            }

            return false;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                target = null;

                return true;
            }
            return false;
        }
    }
}
