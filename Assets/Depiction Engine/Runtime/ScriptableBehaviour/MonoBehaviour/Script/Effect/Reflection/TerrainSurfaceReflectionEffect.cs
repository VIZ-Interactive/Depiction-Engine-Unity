// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [DisallowMultipleComponent]
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Effect/" + nameof(TerrainSurfaceReflectionEffect))]
    [RequireComponent(typeof(GeoAstroObject))]
    public class TerrainSurfaceReflectionEffect : ReflectionEffectBase
    {
        [Serializable]
        private class CamerasSmoothElevationDictionary : SerializableDictionary<int, CameraSmoothElevation> { };

        private static Vector3 NON_ZERO_NORMAL = new Vector3(0.0f, 0.0001f, 0.0f);

        [BeginFoldout("Surface")]
        [SerializeField, Tooltip("An offset added to the surface altitude at which the reflection render is made."), EndFoldout]
        private float _surfaceAltitudeRenderOffset;

        private Matrix4x4 _reflectionMat;

        private CamerasSmoothElevationDictionary _camerasSmoothElevation;

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => surfaceAltitudeRenderOffset = value, 0.0f, initializingState);
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                if (_camerasSmoothElevation != null)
                {
                    foreach (CameraSmoothElevation cameraSmoothElevation in _camerasSmoothElevation.Values)
                    {
                        cameraSmoothElevation.UpdateAllDelegates();
                        RemoveCameraSmoothElevationDelegates(cameraSmoothElevation);
                        AddCameraSmoothElevationDelegates(cameraSmoothElevation);
                    }
                }

                return true;
            }
            return false;
        }

        private void RemoveCameraSmoothElevationDelegates(CameraSmoothElevation cameraSmoothElevation)
        {
            if (!Object.ReferenceEquals(cameraSmoothElevation, null))
                cameraSmoothElevation.CameraDisposingEvent -= CameraSmoothElevationCameraDisposingHandler;
        }

        private void AddCameraSmoothElevationDelegates(CameraSmoothElevation cameraSmoothElevation)
        {
            if (!IsDisposing() && cameraSmoothElevation != Disposable.NULL)
                cameraSmoothElevation.CameraDisposingEvent += CameraSmoothElevationCameraDisposingHandler;
        }

        private void CameraSmoothElevationCameraDisposingHandler(CameraSmoothElevation cameraSmoothElevation)
        {
            foreach (int key in _camerasSmoothElevation.Keys)
            {
                if (_camerasSmoothElevation[key] == cameraSmoothElevation)
                {
                    RemoveCameraSmoothElevationDelegates(cameraSmoothElevation);
                    _camerasSmoothElevation.Remove(key);

                    Dispose(cameraSmoothElevation);

                    break;
                }
            }
        }

        /// <summary>
        /// An offset added to the surface altitude at which the reflection render is made.
        /// </summary>
        [Json]
        public float surfaceAltitudeRenderOffset
        {
            get { return _surfaceAltitudeRenderOffset; }
            set { SetValue(nameof(surfaceAltitudeRenderOffset), value, ref _surfaceAltitudeRenderOffset); }
        }

        private bool _lastInvertCulling;
        protected override bool ApplyPropertiesToRTTUnityCamera(UnityEngine.Camera rttUnityCamera, Camera camera, int cullingMask)
        {
            if (base.ApplyPropertiesToRTTUnityCamera(rttUnityCamera, camera, cullingMask))
            {
                bool render = true;

                Vector3Double cameraPosition = camera.transform.position;

                Vector3Double cameraSurfacePosition;
                Vector3Double cameraSurfaceUpVector;

                if (geoAstroObject != Disposable.NULL)
                {
                    GeoCoordinate3Double cameraSmoothProjectedTerrainPoint = geoAstroObject.GetGeoCoordinateFromPoint(cameraPosition);

                    cameraSmoothProjectedTerrainPoint.altitude = GetSmoothAltitude(camera, geoAstroObject);

                    cameraSurfacePosition = geoAstroObject.GetPointFromGeoCoordinate(cameraSmoothProjectedTerrainPoint);
                    cameraSurfaceUpVector = geoAstroObject.GetUpVectorFromGeoCoordinate(cameraSmoothProjectedTerrainPoint) * Vector3Double.up;
                }
                else
                {
                    cameraSurfacePosition = objectBase.transform.position;
                    cameraSurfaceUpVector = objectBase.transform.rotation * Vector3Double.up;
                }

                // find out the reflection plane: position and normal in world space
                Vector3 pos = TransformDouble.SubtractOrigin(cameraSurfacePosition);
                Vector3 normal = !cameraSurfaceUpVector.Equals(Vector3Double.zero) ? cameraSurfaceUpVector : NON_ZERO_NORMAL;

                Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, pos) - surfaceAltitudeRenderOffset);

                if (_reflectionMat == null)
                    _reflectionMat = new Matrix4x4();

                rttUnityCamera.worldToCameraMatrix *= UpdateReflectionMatrix(ref _reflectionMat, reflectionPlane);

                Vector4 clipPlane = CameraSpacePlane(rttUnityCamera, pos, surfaceAltitudeRenderOffset, normal, 1.0f);
                if (clipPlane.w <= -0.0001f)
                {
                    rttUnityCamera.projectionMatrix = !clipPlane.Equals(Vector4.zero) ? rttUnityCamera.CalculateObliqueMatrix(clipPlane) : Matrix4x4.identity;

                    // Set custom culling matrix from the current camera
                    rttUnityCamera.cullingMatrix = rttUnityCamera.projectionMatrix * rttUnityCamera.worldToCameraMatrix;

                    //Set Position and Rotation;
                    rttUnityCamera.transform.position = _reflectionMat.MultiplyPoint(camera.unityCamera.transform.position);
                    Vector3 euler = camera.unityCamera.transform.eulerAngles;
                    euler.x = -euler.x;
                    rttUnityCamera.transform.eulerAngles = euler;

                    _lastInvertCulling = GL.invertCulling;
                    GL.invertCulling = !GL.invertCulling;
                }
                else
                    render = false;

                return render;
            }
            return false;
        }

        protected override void ResetRTTUnityCamera(UnityEngine.Camera unityCamera)
        {
            base.ResetRTTUnityCamera(unityCamera);

            GL.invertCulling = _lastInvertCulling;
        }

        // Given position/normal of the plane, calculates plane in camera space.
        private Vector4 CameraSpacePlane(UnityEngine.Camera cam, Vector3 pos, float clipPlaneOffset, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        private static Matrix4x4 UpdateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1.0f - 2.0f * plane[0] * plane[0]);
            reflectionMat.m01 = (-2.0f * plane[0] * plane[1]);
            reflectionMat.m02 = (-2.0f * plane[0] * plane[2]);
            reflectionMat.m03 = (-2.0f * plane[3] * plane[0]);

            reflectionMat.m10 = (-2.0f * plane[1] * plane[0]);
            reflectionMat.m11 = (1.0f - 2.0f * plane[1] * plane[1]);
            reflectionMat.m12 = (-2.0f * plane[1] * plane[2]);
            reflectionMat.m13 = (-2.0f * plane[3] * plane[1]);

            reflectionMat.m20 = (-2.0f * plane[2] * plane[0]);
            reflectionMat.m21 = (-2.0f * plane[2] * plane[1]);
            reflectionMat.m22 = (1.0f - 2.0f * plane[2] * plane[2]);
            reflectionMat.m23 = (-2.0f * plane[3] * plane[2]);

            reflectionMat.m30 = 0.0f;
            reflectionMat.m31 = 0.0f;
            reflectionMat.m32 = 0.0f;
            reflectionMat.m33 = 1.0f;

            return reflectionMat;
        }

        private double GetSmoothAltitude(Camera camera, GeoAstroObject geoAstroObject)
        {
            //Ugly approximation, SSR is really the way to go when elevation is actived
            CameraSmoothElevation cameraSmoothElevation;

            int instanceId = camera.GetInstanceID();

            if (_camerasSmoothElevation == null)
                _camerasSmoothElevation = new CamerasSmoothElevationDictionary();

            if (!_camerasSmoothElevation.TryGetValue(instanceId, out cameraSmoothElevation) || cameraSmoothElevation.camera == Disposable.NULL)
            {
                cameraSmoothElevation = instanceManager.CreateInstance<CameraSmoothElevation>().Init(camera, geoAstroObject);

                _camerasSmoothElevation[instanceId] = cameraSmoothElevation;
                AddCameraSmoothElevationDelegates(cameraSmoothElevation);
            }

            return cameraSmoothElevation.GetSmoothElevation();
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
                if (_camerasSmoothElevation != null)
                {
                    foreach (CameraSmoothElevation cameraSmoothElevation in _camerasSmoothElevation.Values)
                        Dispose(cameraSmoothElevation);
                    _camerasSmoothElevation.Clear();
                }

                return true;
            }
            return false;
        }

        [Serializable]
        private class CameraSmoothElevation : Disposable
        {
            private Camera _camera;

            private GeoAstroObject _parentGeoAstroObject;

            private double _elevation;
            private List<double> _elevations;

            public Action<CameraSmoothElevation> CameraDisposingEvent;

            public CameraSmoothElevation Init(Camera camera, GeoAstroObject parentGeoAstroObject)
            {
                this.camera = camera;
                this.parentGeoAstroObject = parentGeoAstroObject;

                UpdateElevation();

                return this;
            }

            public override void UpdateAllDelegates()
            {
                base.UpdateAllDelegates();

                RemoveCameraDelegates(camera);
                AddCameraDelegates(camera);

                RemoveParentGeoAstroObjectDelegate(parentGeoAstroObject);
                AddParentGeoAstroObjectDelegate(parentGeoAstroObject);
            }

            private void RemoveCameraDelegates(Camera camera)
            {
                if (!Object.ReferenceEquals(camera, null))
                {
                    camera.DisposingEvent -= CameraDisposingHandler;
                    if (!Object.ReferenceEquals(camera.transform, null))
                        camera.transform.PropertyAssignedEvent -= CameraTransformPropertyAssigned;
                }
            }

            private void AddCameraDelegates(Camera camera)
            {
                if (!IsDisposing() && camera != Disposable.NULL)
                {
                    camera.DisposingEvent += CameraDisposingHandler;
                    camera.transform.PropertyAssignedEvent += CameraTransformPropertyAssigned;
                }
            }

            private void CameraDisposingHandler(IDisposable disposable)
            {
                if (CameraDisposingEvent != null)
                    CameraDisposingEvent(this);
            }

            private void CameraTransformPropertyAssigned(IProperty property, string name, object newValue, object oldValue)
            {
                if (name == nameof(TransformDouble.position) || name == nameof(TransformDouble.parentGeoAstroObject))
                    UpdateElevation();
            }

            private void RemoveParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
            {
                if (!Object.ReferenceEquals(parentGeoAstroObject, null))
                {
                    parentGeoAstroObject.TerrainGridMeshObjectRemovedEvent -= ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;
                    parentGeoAstroObject.TerrainGridMeshObjectAddedEvent -= ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;
                }
            }

            private void AddParentGeoAstroObjectDelegate(GeoAstroObject parentGeoAstroObject)
            {
                if (!IsDisposing() && parentGeoAstroObject != Disposable.NULL)
                {
                    parentGeoAstroObject.TerrainGridMeshObjectRemovedEvent += ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;
                    parentGeoAstroObject.TerrainGridMeshObjectAddedEvent += ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler;
                }
            }

            private void ParentGeoAstroObjectTerrainGridMeshObjectChangedHandler(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
            {
                if (grid2DIndexTerrainGridMeshObject != Disposable.NULL)
                {
                    GeoCoordinate3Double geoCoordinate = GetGeoCoordinate();
                    if (grid2DIndex == (Vector2Int)MathPlus.GetIndexFromGeoCoordinate(geoCoordinate, grid2DDimensions))
                        UpdateElevation(grid2DIndexTerrainGridMeshObject, geoCoordinate);
                }
            }

            public GeoAstroObject parentGeoAstroObject
            {
                get { return _parentGeoAstroObject; }
                private set
                {
                    if (_parentGeoAstroObject == value)
                        return;

                    _parentGeoAstroObject = value;
                }
            }

            public Camera camera
            {
                get { return _camera; }
                private set 
                {
                    if (_camera == value)
                        return;

                    RemoveCameraDelegates(_camera);

                    _camera = value;

                    AddCameraDelegates(_camera);
                }
            }

            private GeoCoordinate3Double GetGeoCoordinate()
            {
                return parentGeoAstroObject.GetGeoCoordinateFromPoint(camera.transform.position);
            }

            private void UpdateElevation()
            {
                GeoCoordinate3Double geoCoordinate = GetGeoCoordinate();
                Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject = parentGeoAstroObject.GetGrid2DIndexTerrainGridMeshObjectFromGeoCoordinate(geoCoordinate);
                if (grid2DIndexTerrainGridMeshObject != Disposable.NULL)
                    UpdateElevation(grid2DIndexTerrainGridMeshObject, geoCoordinate, true);
            }

            private void UpdateElevation(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject, GeoCoordinate3Double geoCoordinate, bool forceAddAltitude = false)
            {
                if (grid2DIndexTerrainGridMeshObject.GetGeoCoordinateElevation(out double elevation, geoCoordinate, camera) && (SetElevation(elevation) || forceAddAltitude))
                    AddElevation(elevation);
            }

            private bool SetElevation(double value)
            {
                if (_elevation == value)
                    return false;

                _elevation = value;

                return true;
            }

            private List<double> elevations
            {
                get
                {
                    if (_elevations == null)
                        _elevations = new List<double>();
                    return _elevations;
                }
            }

            private void AddElevation(double elevation)
            {
                if (elevations.Count > 50)
                    elevations.RemoveAt(0);

                elevations.Add(elevation);
            }

            public double GetSmoothElevation()
            {
                double smoothAltitude = 0.0d;

                if (elevations.Count > 0)
                {
                    foreach (double altitudeSample in elevations)
                        smoothAltitude += altitudeSample;
                    smoothAltitude /= elevations.Count;
                }

                return smoothAltitude;
            }
        }
    }
}
