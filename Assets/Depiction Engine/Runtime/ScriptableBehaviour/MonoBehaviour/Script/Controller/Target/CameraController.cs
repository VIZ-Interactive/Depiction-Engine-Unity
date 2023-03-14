// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Controller/" + nameof(CameraController))]
    [RequireComponent(typeof(Camera))]
    public class CameraController : TargetController
    {
        private const float MIN_FAR_CLIPPING_DISTANCE = 0.0f;
        private const float MAX_FAR_CLIPPING_DISTANCE = int.MaxValue;

        private const float MIN_TILT_LIMIT = 0.0f;
        private const float MAX_TILT_LIMIT = 10.0f;

        [BeginFoldout("Camera")]
        [SerializeField, Tooltip("When enabled the zoom value (derived from distance) will automatically be rounded to the closest round number.")]
        private bool _snapDistanceToZoom;
        [SerializeField, Tooltip("When enabled the target motion will progressively slow down over a period of time depending on velocity.")]
        private bool _useInertia;
        [SerializeField, Tooltip("When enabled the near and far clipping plane of the "+nameof(Camera)+" will be change automaticaly based on calculations involving the distance between the camera and the target.")]
        private bool _dynamicClippingPlanes;
        [SerializeField, Tooltip("A factor by which the distance between the "+nameof(Camera)+ " and target distance is multiplied for the "+nameof(dynamicClippingPlanes)+" calculations.")]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableDynamicClippingFields))]
#endif
        private float _clippingDistanceMultiplier;
        [SerializeField, MinMaxRange(MIN_FAR_CLIPPING_DISTANCE, MAX_FAR_CLIPPING_DISTANCE), Tooltip("A min and max clamping values for the "+nameof(dynamicClippingPlanes)+" calculations.")]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableDynamicClippingFields))]
#endif
        private Vector2 _minMaxFarClippingDistance;
        [SerializeField, MinMaxRange(MIN_TILT_LIMIT, MAX_TILT_LIMIT), Tooltip("A min and max range of normalized atmosphere values, between 0 to 10 (ground to atmosphere edge * 10), inside of which tilt is allowed. Tilt will become progressively restrained (pointing downwards at 90 degrees) as you near the max value.")]
        private Vector2 _tiltLimit;
        [SerializeField, Tooltip("A factor by which scroll inputs are multiplied to accelerate or slow down zooming."), EndFoldout]
        private float _scrollWheelMultiplier;

        [BeginFoldout("Move")]
        [SerializeField, Tooltip("How long should the move animation last, in seconds.")]
        private float _duration;
        [SerializeField, Tooltip("The distance from the target that the "+nameof(Camera)+" should reach by the end of the animation.")]
        private double _toDistance;
        [SerializeField, Tooltip("The Geo Coordinate that the target should reach by the end of the animation.")]
        private GeoCoordinate3Double _toGeoCoordinate;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(MoveToBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Start moving the camera and target to the specified parameters."), EndFoldout]
        private bool _moveTo;
#endif

        private CollisionSnapShot _collisionSnapshot;
        private Tween _inertiaTween;
        private Vector3 _inertia;

        private Tween _distanceTween;

#if UNITY_EDITOR
        protected bool GetEnableDynamicClippingFields()
        {
            return dynamicClippingPlanes;
        }

        private void MoveToBtn()
        {
            MoveTo(_toGeoCoordinate, _duration, _toDistance);
        }
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => snapDistanceToZoom = value, false, initializingContext);
            InitValue(value => useInertia = value, true, initializingContext);
            InitValue(value => dynamicClippingPlanes = value, true, initializingContext);
            InitValue(value => clippingDistanceMultiplier = value, 5.0f, initializingContext);
            InitValue(value => minMaxFarClippingDistance = value, new Vector2(2000.0f, 15000.0f), initializingContext);
            InitValue(value => tiltLimit = value, new Vector2(0.0f, 1.5f), initializingContext);
            InitValue(value => scrollWheelMultiplier = value, 1.0f, initializingContext);
            InitValue(value => duration = value, 5.0f, initializingContext);
            InitValue(value => toDistance = value, DEFAULT_DISTANCE_VALUE, initializingContext);
            InitValue(value => toGeoCoordinate = value, GeoCoordinate3Double.zero, initializingContext);
        }

        protected override bool GetDefaultPreventMeshPenetration()
        {
            return true;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                InputManager.OnMouseClickedEvent -= InputManagerOnMouseClicked;
                if (!IsDisposing())
                    InputManager.OnMouseClickedEvent += InputManagerOnMouseClicked;

                return true;
            }
            return false;
        }

        private void InputManagerOnMouseClicked(RaycastHitDouble hit)
        {
            MeshRendererVisual meshRendererVisual = hit?.meshRendererVisual;

            if (meshRendererVisual != Disposable.NULL && meshRendererVisual.visualObject.parentGeoAstroObject != Disposable.NULL && target != Disposable.NULL)
            {
                GeoAstroObject meshRendererVisualParentGeoAstroObject = meshRendererVisual.visualObject.parentGeoAstroObject;
                if (target.parentGeoAstroObject != meshRendererVisualParentGeoAstroObject)
                {
                    GetGeoAstroObjectSurfaceComponents(out Vector3Double targetPosition, out QuaternionDouble rotation, out double cameraDistance, meshRendererVisualParentGeoAstroObject);

                    target.SetParent(meshRendererVisualParentGeoAstroObject.transform);

                    SetTargetPositionRotationDistance(targetPosition, rotation, cameraDistance);
                    
                    forwardVector = new Vector3Double(90.0d, 0.0d, 0.0d);
                }
            }
        }

        protected override QuaternionDouble GetRotation()
        {
            return targetTransform.rotation * QuaternionDouble.Euler(Math.Max(forwardVector.x, GetMinForwardVectorX()), forwardVector.y, forwardVector.z);
        }

        protected override double GetMinForwardVectorX()
        {
            float t = 0.0f;

            if (target != Disposable.NULL && target.parentGeoAstroObject != Disposable.NULL)
            {
                double normalizedCameraDistance = base.distance / target.parentGeoAstroObject.radius;
                if (normalizedCameraDistance > tiltLimit.x && normalizedCameraDistance < tiltLimit.y)
                    t = (float)((normalizedCameraDistance - tiltLimit.x) / (tiltLimit.y - tiltLimit.x));
                else
                    t = (float)(normalizedCameraDistance <= tiltLimit.x ? 0.0d : 1.0d);
            }

            return MathPlus.Lerp(0.0d, 90.0d, Easing.CircEaseOut(t, 0.0f, 1.0f, 1.0f));
        }

        protected override Vector2Double GetDefaultMinMaxDistance()
        {
            return new Vector2Double(100.0d, 10000000.0d);
        }

        /// <summary>
        /// When enabled the zoom value (derived from distance) will automatically be rounded to the closest round number.
        /// </summary>
        [Json]
        public bool snapDistanceToZoom
        {
            get { return _snapDistanceToZoom; }
            set { SetValue(nameof(snapDistanceToZoom), value, ref _snapDistanceToZoom); }
        }

        /// <summary>
        /// When enabled the target motion will progressively slow down over a period of time depending on velocity. 
        /// </summary>
        [Json]
        public bool useInertia
        {
            get { return _useInertia; }
            set { SetValue(nameof(useInertia), value, ref _useInertia); }
        }

        /// <summary>
        /// When enabled the near and far clipping plane of the <see cref="DepictionEngine.Camera"/> will be change automaticaly based on calculations involving the distance between the camera and the target. 
        /// </summary>
        [Json]
        public bool dynamicClippingPlanes
        {
            get { return _dynamicClippingPlanes; }
            set { SetValue(nameof(dynamicClippingPlanes), value, ref _dynamicClippingPlanes); }
        }

        /// <summary>
        /// A factor by which the distance between the <see cref="DepictionEngine.Camera"/> and target distance is multiplied for the <see cref="DepictionEngine.CameraController.dynamicClippingPlanes"/> calculations. 
        /// </summary>
        [Json]
        public float clippingDistanceMultiplier
        {
            get { return _clippingDistanceMultiplier; }
            set { SetValue(nameof(clippingDistanceMultiplier), value, ref _clippingDistanceMultiplier); }
        }

        /// <summary>
        /// A min and max clamping values for the <see cref="DepictionEngine.CameraController.dynamicClippingPlanes"/> calculations. 
        /// </summary>
        [Json]
        public Vector2 minMaxFarClippingDistance
        {
            get { return _minMaxFarClippingDistance; }
            set 
            {
                value.x = Mathf.Clamp(value.x, MIN_FAR_CLIPPING_DISTANCE, MAX_FAR_CLIPPING_DISTANCE);
                value.y = Mathf.Clamp(value.y, MIN_FAR_CLIPPING_DISTANCE, MAX_FAR_CLIPPING_DISTANCE);
                SetValue(nameof(minMaxFarClippingDistance), value, ref _minMaxFarClippingDistance); 
            }
        }

        /// <summary>
        /// A min and max range of normalized atmosphere values, between 0 to 10 (ground to atmosphere edge * 10), inside of which tilt is allowed. Tilt will become progressively restrained (pointing downwards at 90 degrees) as you near the max value. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
        public Vector2 tiltLimit
        {
            get { return _tiltLimit; }
            set 
            {
                value.x = Mathf.Clamp(value.x, MIN_TILT_LIMIT, MAX_TILT_LIMIT);
                value.y = Mathf.Clamp(value.y, MIN_TILT_LIMIT, MAX_TILT_LIMIT);
                SetValue(nameof(tiltLimit), value, ref _tiltLimit, (newValue, oldValue) => 
                {
                    ForceUpdateTransform(false, true);
                }); 
            }
        }

        /// <summary>
        /// A factor by which scroll inputs are multiplied to accelerate or slow down zooming.
        /// </summary>
        [Json]
        public float scrollWheelMultiplier
        {
            get { return _scrollWheelMultiplier; }
            set { SetValue(nameof(scrollWheelMultiplier), value, ref _scrollWheelMultiplier); }
        }

        /// <summary>
        /// How long should the move animation last, in seconds.
        /// </summary>
        [Json]
        public float duration
        {
            get { return _duration; }
            set { SetValue(nameof(duration), value, ref _duration); }
        }

        /// <summary>
        /// The distance from the target that the <see cref="DepictionEngine.Camera"/> should reach by the end of the animation.
        /// </summary>
        [Json]
        public double toDistance
        {
            get { return _toDistance; }
            set { SetValue(nameof(toDistance), value, ref _toDistance); }
        }

        /// <summary>
        /// The Geo Coordinate that the target should reach by the end of the animation.
        /// </summary>
        [Json]
        public GeoCoordinate3Double toGeoCoordinate
        {
            get { return _toGeoCoordinate; }
            set { SetValue(nameof(toGeoCoordinate), value, ref _toGeoCoordinate); }
        }

        private float GetDefaultAnimationDuration()
        {
            return 0.3f;
        }

        private Tween distanceTween
        {
            get { return _distanceTween; }
            set
            {
                if (Object.ReferenceEquals(_distanceTween, value))
                    return;

                DisposeManager.Dispose(_distanceTween);

                _distanceTween = value;
            }
        }

        private Tween inertiaTween
        {
            get { return _inertiaTween; }
            set
            {
                if (Object.ReferenceEquals(_inertiaTween, value))
                    return;

                DisposeManager.Dispose(_inertiaTween);

                _inertiaTween = value;
            }
        }

        private Vector3 inertia
        {
            get { return _inertia; }
            set { _inertia = value; }
        }

        protected override bool SetMinMaxDistance(Vector2Double value)
        {
            if (value.y > CameraManager.MAX_CAMERA_DISTANCE)
                value.y = CameraManager.MAX_CAMERA_DISTANCE;
            return base.SetMinMaxDistance(value);
        }

        private void ClearInertia()
        {
            inertiaTween = null;
            _inertia = Vector3.zero;
        }

        /// <summary>
        /// Start moving the camera and target to the specified parameters.
        /// </summary>
        /// <param name="geoCoordinate"></param>
        /// <param name="duration"></param>
        /// <param name="toDistance"></param>
        public void MoveTo(GeoCoordinate3Double geoCoordinate, float duration = 3.0f, double toDistance = 200.0d)
        {
            if (targetTransform != Disposable.NULL)
            { 
                GeoAstroObject targetParentGeoAstroObject = targetTransform.parentGeoAstroObject;
                if (targetParentGeoAstroObject != Disposable.NULL)
                    MoveTo(targetParentGeoAstroObject.GetPointFromGeoCoordinate(geoCoordinate), toDistance, duration);
            }
        }

        /// <summary>
        /// Start moving the camera and target to the specified parameters.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="toDistance"></param>
        public void MoveTo(Vector3Double position, double toDistance = 200.0d)
        {
            if (targetTransform != Disposable.NULL)
            {
                Vector3Double moveToCameraPosition = CameraController.GetTargetPosition(position, transform.rotation, -toDistance);
                float duration = (float)(Math.Log(Vector3Double.Distance(transform.position, moveToCameraPosition)) / 2.0d);
                
                MoveTo(position, toDistance, duration);
            }
        }

        /// <summary>
        /// Start moving the camera and target to the specified parameters.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="toDistance"></param>
        /// <param name="duration"></param>
        public void MoveTo(Vector3Double position, double toDistance = 200.0d, float duration = 3.0f)
        {
            if (targetTransform != Disposable.NULL)
            {
                Camera camera = objectBase as Camera;
                bool isGeoCoordinateTransform = targetTransform.isGeoCoordinateTransform;

                if (target.animator != Disposable.NULL && target.animator is TransformAnimator)
                {
                    EasingType easing = EasingType.QuintEaseOut;
                    TransformAnimator transformAnimator = target.animator as TransformAnimator;
                    if (isGeoCoordinateTransform)
                        transformAnimator.SetGeoCoordinate(targetTransform.parentGeoAstroObject.GetGeoCoordinateFromPoint(position), duration, easing);
                    else
                        transformAnimator.SetPosition(position, duration, easing);
                }
                else
                    SetTargetPosition(position);

                if (camera.animator != Disposable.NULL && camera.animator is TargetControllerAnimator)
                {
                    TargetControllerAnimator targetControllerAnimator = camera.animator as TargetControllerAnimator;
                    if (isGeoCoordinateTransform)
                    {
                        double fromDistance = base.distance;

                        AnimationCurve animationCurve = new();
                        animationCurve.AddKey(new Keyframe(0.0f, (float)fromDistance));
                        animationCurve.AddKey(new Keyframe(0.5f, (float)Math.Max(Vector3Double.Distance(targetTransform.position, position) * 0.5d, fromDistance + (toDistance - fromDistance) * 0.5d)));
                        animationCurve.AddKey(new Keyframe(1.0f, (float)toDistance));

                        for (int i = 0; i < animationCurve.keys.Length; ++i)
                            animationCurve.SmoothTangents(i, 0.0f);

                        distanceTween = tweenManager.To(0.0f, 1.0f, duration, (value) => 
                        {
                            base.distance = animationCurve.Evaluate(value);
                        }, null, () => { distanceTween = null; }, EasingType.QuintEaseOut);
                    }
                    else
                        targetControllerAnimator.SetDistance(toDistance, duration);
                }
                else
                    base.distance = toDistance;
            }
        }

        private int GetZoomFromDistance(double distance)
        {
            int zoom = -1;

            if (targetTransform != Disposable.NULL)
            {
                GeoAstroObject targetParentGeoAstroObject = targetTransform.parentGeoAstroObject;

                if (targetParentGeoAstroObject != Disposable.NULL)
                {
                    double targetParentGeoAstroObjectScaledSize = targetParentGeoAstroObject.GetScaledSize();
                    zoom = Mathf.RoundToInt(Mathf.Clamp(MathPlus.GetZoomFromDistance(distance, 512, targetParentGeoAstroObjectScaledSize), 0.0f, Index2DLoaderBase.MAX_ZOOM));
                }
            }

            return zoom;
        }

        private double GetDistanceFromZoom(int zoom)
        {
            double distance = -1.0d;

            if (targetTransform != Disposable.NULL)
            {
                GeoAstroObject targetParentGeoAstroObject = targetTransform.parentGeoAstroObject;

                if (targetParentGeoAstroObject != Disposable.NULL)
                {
                    double targetParentGeoAstroObjectScaledSize = targetParentGeoAstroObject.GetScaledSize();
                    distance = MathPlus.GetDistanceFromZoom(zoom, 512, targetParentGeoAstroObjectScaledSize);
                }
            }

            return distance;
        }

        protected override void UpdateTargetControllerTransform(Camera camera)
        {
            base.UpdateTargetControllerTransform(camera);
          
            if (isActiveAndEnabled && Application.isPlaying)
            {
                TransformAnimator targetAnimator = target.animator as TransformAnimator;
                            
                TargetControllerAnimator objectAnimator = objectBase.animator as TargetControllerAnimator;
                GeoAstroObject targetParentGeoAstroObject = targetTransform.parentGeoAstroObject;

                Vector2 screenPoint = inputManager.GetScreenCenter();
                int fingerMouseCount = inputManager.GetFingerMouseCount();
                           
                float animationDuration = GetDefaultAnimationDuration();

                double raycastMaxDistance = distance;

                if (targetParentGeoAstroObject != Disposable.NULL)
                    raycastMaxDistance += targetParentGeoAstroObject.radius;

                //Rotation
                Vector3 forwardVectorDelta = inputManager.GetForwardVectorDelta();
                if (forwardVectorDelta != Vector3.zero)
                {
                    StopAllAnimation();

                    forwardVector += forwardVectorDelta;
                    ClearInertia();
                    if (_collisionSnapshot != null)
                        _collisionSnapshot.Reset();
                }

                //Distance
                double newCameraDistance = distance;
                double clampedCameraDistance = MathPlus.Clamp(distance, 0.1d, 1000000000000.0d);
                double inputDistanceDelta = inputManager.GetDistanceDelta() * scrollWheelMultiplier * 4.0d * clampedCameraDistance;
                if (inputDistanceDelta != 0.0d || PropertyDirty(nameof(snapDistanceToZoom)))
                {
                    StopAllAnimation();

                    if (snapDistanceToZoom && fingerMouseCount <= 1 && targetParentGeoAstroObject != Disposable.NULL)
                        newCameraDistance = GetDistanceFromZoom(GetZoomFromDistance(distance) + (inputDistanceDelta > 0.0d ? -1 : 1));
                    else
                        newCameraDistance += Math.Clamp(inputDistanceDelta, -distance / 1.5d, distance * 15.0d);

                    newCameraDistance = ClampDistance(newCameraDistance);

                    if (objectAnimator != Disposable.NULL)
                        objectAnimator.SetDistance(newCameraDistance, animationDuration);
                    else
                        SetDistance(newCameraDistance);

                    if (_collisionSnapshot != null)
                        _collisionSnapshot.Reset();
                }
                        
                //Target Position
                double distanceDelta = newCameraDistance - distance;
                bool leftMouseDown;

#if UNITY_STANDALONE || UNITY_WEBGL || UNITY_EDITOR
                leftMouseDown = fingerMouseCount == 1 && !Input.GetMouseButtonUp(0) && Input.GetMouseButton(0);
#else
                leftMouseDown = fingerMouseCount == 1;
#endif

                if (distanceDelta != 0.0d || (leftMouseDown && !inputManager.IsCtrlDown() && forwardVectorDelta == Vector3.zero))
                {
                    Vector3Double? newPosition = null;

                    bool animate = false;
                    if (distanceDelta != 0.0d)
                    {
                        RaycastHitDouble[] hits = PhysicsDouble.TerrainFiltered(PhysicsDouble.CameraRaycastAll(camera, screenPoint, (float)raycastMaxDistance, 1 << LayerMask.NameToLayer(typeof(TerrainGridMeshObject).Name) | 1 << LayerMask.NameToLayer(typeof(TerrainGridMeshObject).Name + InstanceManager.GLOBAL_LAYER)), camera);
                        if (hits.Length > 0)
                        {
                            RaycastHitDouble hit = hits[0];
                            if (hit.transform.parentGeoAstroObject == targetTransform.parentGeoAstroObject)
                            {
                                animate = true;
                                Vector3Double relativePosition = hits[0].point - targetTransform.position;
                                newPosition = targetTransform.position + (relativePosition.normalized * (relativePosition.magnitude * (-distanceDelta / distance)));
                            }
                        }

                        ClearInertia();
                    }
                    else
                    {
                        StopAllAnimation();

                        _collisionSnapshot ??= new CollisionSnapShot();
                        if (!_collisionSnapshot.ready)
                        {
                            RaycastHitDouble[] hits = PhysicsDouble.TerrainFiltered(PhysicsDouble.CameraRaycastAll(camera, screenPoint, (float)raycastMaxDistance), camera);
                            if (hits.Length > 0)
                            {
                                RaycastHitDouble hit = hits[0];
                                if (targetParentGeoAstroObject != Disposable.NULL && hit.transform.parentGeoAstroObject == targetParentGeoAstroObject && !ClosestHitIsUI(camera.transform.position, hits))
                                {
                                    GeoCoordinate3Double hitGeoCoordinate = targetParentGeoAstroObject.GetGeoCoordinateFromPoint(hit.point);
                                    GeoCoordinate3Double cameraGeoCoordinate = targetParentGeoAstroObject.GetGeoCoordinateFromPoint(camera.transform.position);
                                    if (hitGeoCoordinate.altitude < cameraGeoCoordinate.altitude - 10.0d)
                                        _collisionSnapshot.TakeSnapshot(camera, targetParentGeoAstroObject.transform, targetParentGeoAstroObject.transform.InverseTransformPoint(targetTransform.position), targetParentGeoAstroObject.transform.InverseTransformPoint(hit.point), !targetParentGeoAstroObject.IsFlat());
                                }
                            }
                        }

                        if (_collisionSnapshot.ready && _collisionSnapshot.GetNewLocalPosition(out Vector3Double newCollisionPosition, screenPoint))
                        {
                            newPosition = targetParentGeoAstroObject.transform.TransformPoint(newCollisionPosition);
                            SetInertia(newPosition.Value - targetTransform.position);
                        }
                        else
                            AddInertia(targetParentGeoAstroObject);
                    }

                    if (newPosition.HasValue)
                    {
                        if (targetTransform.isGeoCoordinateTransform)
                        {
                            GeoCoordinate3Double geoCoordinate = targetParentGeoAstroObject.GetGeoCoordinateFromPoint(newPosition.Value);
                            if (animate && targetAnimator != Disposable.NULL)
                                targetAnimator.SetGeoCoordinate(geoCoordinate, animationDuration);
                            else
                                targetTransform.geoCoordinate = geoCoordinate;
                        }
                        else
                        {
                            Vector3Double newLocalPosition = targetTransform.parent.InverseTransformPoint(newPosition.Value);
                            if (animate && targetAnimator != Disposable.NULL)
                                targetAnimator.SetLocalPosition(newLocalPosition, animationDuration);
                            else
                                targetTransform.localPosition = newLocalPosition;
                        }
                    }
                }
                else
                {
                    AddInertia(targetParentGeoAstroObject);
                    if (_collisionSnapshot != null)
                        _collisionSnapshot.Reset();
                }
            }

            if (dynamicClippingPlanes)
            {
                camera.farClipPlane = Mathf.Clamp((float)(Vector3Double.Distance(transform.position, targetTransform.position) * clippingDistanceMultiplier), minMaxFarClippingDistance.x, minMaxFarClippingDistance.y);
                camera.nearClipPlane = camera.farClipPlane / 3333.333f;
            }
        }

        public void StopAllAnimation()
        {
            distanceTween = null;
            TargetControllerAnimator objectAnimator = objectBase.animator as TargetControllerAnimator;
            if (objectAnimator != Disposable.NULL)
                objectAnimator.StopAllAnimations();
            TargetControllerAnimator cameraAnimator = objectBase.animator as TargetControllerAnimator;
            if (cameraAnimator != Disposable.NULL)
                cameraAnimator.StopAllAnimations();
            TransformAnimator targetAnimator = target.animator as TransformAnimator; 
            if (targetAnimator != Disposable.NULL)
                targetAnimator.StopAllAnimations();
        }

        private void SetInertia(Vector3 newInertia)
        {
            if (!useInertia)
                newInertia = Vector3.zero;

            if (inertia != Vector3.zero)
            {
                float maxInertia = (float)distance;
                float newInertiaMagnitude = Mathf.Min(newInertia.magnitude, Mathf.Min(inertia.magnitude + maxInertia / 100.0f, maxInertia));

                newInertiaMagnitude *= Mathf.Clamp01(Vector3.Dot(inertia.normalized, newInertia.normalized));

                newInertia = newInertia.normalized * newInertiaMagnitude;
            }

            inertiaTween = tweenManager.To(0.0f, 1.0f, 1.5f, (value) => { inertia = newInertia * (1.0f - value); }, null, () => { inertiaTween = null; }, EasingType.CircEaseOut);
        }

        private void AddInertia(GeoAstroObject targetParentGeoAstroObject)
        {
            if (inertia.magnitude > 0.0f)
            {
                Vector3Double newPosition = targetTransform.position + inertia;
                if (targetTransform.isGeoCoordinateTransform)
                    targetTransform.geoCoordinate = targetParentGeoAstroObject.GetGeoCoordinateFromPoint(newPosition);
                else
                    targetTransform.localPosition = targetTransform.parent.InverseTransformPoint(newPosition);
            }
        }

        private bool ClosestHitIsUI(Vector3Double point, RaycastHitDouble[] hits)
        {
            RaycastHitDouble closestHit = PhysicsDouble.GetClosestHit(point, hits);
            return closestHit != null && closestHit.transform != Disposable.NULL && closestHit.transform.objectBase != Disposable.NULL && closestHit.transform.objectBase is UIBase;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                inertiaTween = null;
                distanceTween = null;

                return true;
            }
            return false;
        }

        private class CollisionSnapShot
        {
            private static readonly Vector3Double MIRROR_VECTOR = new(-1.0d, 1.0d, -1.0d);

            private bool _ready;

            private Vector3Double _targetLocalPosition;

            private Vector3[] _frustumCorners;
            private Vector3Double _cameraPosition;
            private QuaternionDouble _cameraRotation;
            private float _cameraPixelWidth;
            private float _cameraPixelHeight;
            private bool _cameraOrthographic;
            private float _cameraOrthographicSize;
            private float _cameraAspect;

            private Vector3Double _parentGeoAstroObjectPosition;
            private QuaternionDouble _parentGeoAstroObjectRotation;
            private Vector3Double _collisionLocalPosition;
            private QuaternionDouble _targetUpVector;
            private QuaternionDouble _collisionUpVector;
            private QuaternionDouble _targetAlignedCollisionUpVector;

            private bool _isSpherical;

            public void TakeSnapshot(Camera camera, TransformDouble parentGeoAstroObjectTransform, Vector3Double targetLocalPosition, Vector3Double collisionLocalPosition, bool isSpherical)
            {
                _ready = true;

                GeoAstroObject geoAstroObject = parentGeoAstroObjectTransform.objectBase as GeoAstroObject;

                _parentGeoAstroObjectPosition = parentGeoAstroObjectTransform.position;
                _parentGeoAstroObjectRotation = parentGeoAstroObjectTransform.rotation;
                _collisionLocalPosition = collisionLocalPosition;
                _targetLocalPosition = targetLocalPosition;

                _targetUpVector = MathPlus.GetUpVectorFromGeoCoordinate(geoAstroObject.GetGeoCoordinateFromLocalPoint(_targetLocalPosition), geoAstroObject.GetSphericalRatio());
                _collisionUpVector = MathPlus.GetUpVectorFromGeoCoordinate(geoAstroObject.GetGeoCoordinateFromLocalPoint(_collisionLocalPosition), geoAstroObject.GetSphericalRatio());
                _targetAlignedCollisionUpVector = QuaternionDouble.Inverse(QuaternionDouble.LookRotation(_targetUpVector * Vector3Double.forward, _collisionUpVector * Vector3Double.up));

                _frustumCorners = new Vector3[4];
                camera.CalculateFrustumCorners(_frustumCorners);

                _cameraPosition = camera.transform.position;
                _cameraRotation = camera.transform.rotation;
                _cameraPixelWidth = camera.pixelWidth;
                _cameraPixelHeight = camera.pixelHeight;
                _cameraOrthographic = camera.orthographic;
                _cameraOrthographicSize = camera.orthographicSize;
                _cameraAspect = camera.aspect;

                _isSpherical = isSpherical;
            }

            public bool ready
            {
                get { return _ready; }
            }

            public bool GetNewLocalPosition(out Vector3Double newLocalPosition, Vector2 screenPoint)
            {
                QuaternionDouble parentGeoAstroObjectInverseRotation = QuaternionDouble.Inverse(_parentGeoAstroObjectRotation);

                RayDouble ray = Camera.ScreenPointToRay(screenPoint, _frustumCorners, _cameraPosition, _cameraRotation, _cameraPixelWidth, _cameraPixelHeight, _cameraOrthographic, _cameraOrthographicSize, _cameraAspect);

                ray.origin = parentGeoAstroObjectInverseRotation * (ray.origin - _parentGeoAstroObjectPosition);

                ray.direction = parentGeoAstroObjectInverseRotation * ray.direction;

                Vector3Double localPositionDelta = Vector3Double.zero;

                Vector3Double newCollisionLocalPosition;
                if (_isSpherical)
                {
                    if (MathGeometry.LineSphereIntersection(out newCollisionLocalPosition, _collisionLocalPosition.magnitude, ray))
                        localPositionDelta = newCollisionLocalPosition - _collisionLocalPosition;
                }
                else
                {
                    if (MathGeometry.LinePlaneIntersection(out newCollisionLocalPosition, new Vector3Double(0.0d, _collisionLocalPosition.y, 0.0d), Vector3Double.up, ray, false))
                        localPositionDelta = newCollisionLocalPosition - _collisionLocalPosition;
                }

                newLocalPosition = _targetLocalPosition + (_targetUpVector * (_targetAlignedCollisionUpVector * localPositionDelta * MIRROR_VECTOR));

                _ready = localPositionDelta != Vector3Double.zero;

                return _ready;
            }

            public void Reset()
            {
                _ready = false;
            }
        }
    }
}
