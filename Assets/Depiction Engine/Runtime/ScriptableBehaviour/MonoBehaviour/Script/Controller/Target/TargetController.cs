// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Controller/" + nameof(TargetController))]
    public class TargetController : TargetControllerBase
    {
        public static readonly Vector3Double DEFAULT_FORWARD_VECTOR_VALUE = new Vector3Double(25.0d, 0.0d, 0.0d);

        private const float MIN_FORWARD_VECTOR_X = -90.0f;
        private const float MAX_FORWARD_VECTOR_X = 90.0f;

        [SerializeField, Tooltip("When enabled the "+nameof(TargetController)+" will always position the object in a way so that it does not penetrate a mesh."), EndFoldout]
        private bool _preventMeshPenetration;

        [BeginFoldout("Forward Vector")]
        [SerializeField, Tooltip("A vector pointing from the target towards the object, in degrees."), EndFoldout]
        private Vector3Double _forwardVector;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => preventMeshPenetration = value, GetDefaultPreventMeshPenetration(), initializingContext);
            InitValue(value => forwardVector = value, DEFAULT_FORWARD_VECTOR_VALUE, initializingContext);
        }

        protected virtual bool GetDefaultPreventMeshPenetration()
        {
            return false;
        }

        protected virtual double GetMinForwardVectorX()
        {
            return MIN_FORWARD_VECTOR_X;
        }

        /// <summary>
        /// When enabled the <see cref="DepictionEngine.TargetController"/> will always position the object in a way so that it does not penetrate a mesh. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
        public bool preventMeshPenetration
        {
            get { return _preventMeshPenetration; }
            set
            {
                SetValue(nameof(preventMeshPenetration), value, ref _preventMeshPenetration, (newValue, oldValue) =>
                {
                    ForceUpdateTransform();
                });
            }
        }

        /// <summary>
        /// A vector pointing from the target towards the object, in degrees.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTransformAdditionalRecordObjects))]
#endif
        public Vector3Double forwardVector
        {
            get { return _forwardVector; }
            set
            {
                if (SetForwardVector(value))
                    ForceUpdateTransform(true, true);
            }
        }

        private bool SetForwardVector(Vector3Double value)
        {
            value.x = MathPlus.ClampAngle180(value.x);
            value.x = MathPlus.Clamp(value.x, GetMinForwardVectorX(), MAX_FORWARD_VECTOR_X);
            return SetValue(nameof(forwardVector), value, ref _forwardVector, (newValue, oldValue) =>
            {
                DeriveRotationFromForwardVector();
            });
        }

        protected override bool SetTargetTransform(TransformDouble value)
        {
            if (base.SetTargetTransform(value))
            {
                DeriveRotationFromForwardVector();

                return true;
            }
            return false;
        }

        protected override void ValidateTargetControllerTransform(Camera camera)
        {
            base.ValidateTargetControllerTransform(camera);

            if (preventMeshPenetration)
            {
                if (targetTransform.parentGeoAstroObject != Disposable.NULL)
                {
                    GeoCoordinate3Double geoCoordinate = targetTransform.parentGeoAstroObject.GetGeoCoordinateFromPoint(transform.position);

                    double altitude = geoCoordinate.altitude + PENETRATION_TRESHOLD;

                    geoCoordinate.altitude = targetTransform.parentGeoAstroObject.radius / 10.0d;

                    RayDouble ray = new RayDouble(targetTransform.parentGeoAstroObject.GetPointFromGeoCoordinate(geoCoordinate), targetTransform.parentGeoAstroObject.GetUpVectorFromGeoCoordinate(geoCoordinate) * Vector3Double.down);
                    RaycastHitDouble[] terrainFilteredHit = PhysicsDouble.TerrainFiltered(PhysicsDouble.RaycastAll(ray, (float)(geoCoordinate.altitude * 20.0d)));
                    if (terrainFilteredHit.Length > 0)
                    {
                        RaycastHitDouble hit = PhysicsDouble.GetClosestHit(ray.origin, terrainFilteredHit);
                        GeoCoordinate3Double collisionGeoCoordinate = targetTransform.parentGeoAstroObject.GetGeoCoordinateFromPoint(hit.point);
                        if (altitude < collisionGeoCoordinate.altitude)
                        {
                            Vector3Double targetLocalCollision = QuaternionDouble.Inverse(targetTransform.rotation) * (hit.point - targetTransform.position);
                            targetLocalCollision.y += PENETRATION_TRESHOLD;

                            Vector3Double newForwardVector = forwardVector;
                            newForwardVector.x = Quaternion.LookRotation(-targetLocalCollision.normalized).eulerAngles.x;

                            bool rotationChanged = SetForwardVector(newForwardVector);
                            bool cameraDistanceChanged = SetDistance(targetLocalCollision.magnitude);

                            if (rotationChanged || cameraDistanceChanged)
                                ForceUpdateTransform(true, true);
                        }
                    }
                }
            }
        }

        protected override void TargetTransformChanged(TransformBase.Component changedComponent, TransformBase.Component capturedComponent)
        {
            if (changedComponent.HasFlag(TransformBase.Component.Rotation))
                DeriveRotationFromForwardVector();
            base.TargetTransformChanged(changedComponent, capturedComponent);
        }

        private bool DeriveRotationFromForwardVector()
        {
            if (targetTransform != Disposable.NULL)
                return SetRotation(targetTransform.rotation * QuaternionDouble.Euler(_forwardVector));
            return false;
        }
    }
}
