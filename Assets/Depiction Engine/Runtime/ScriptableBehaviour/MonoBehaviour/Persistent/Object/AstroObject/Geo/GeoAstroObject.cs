// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Astro/" + nameof(GeoAstroObject))]
    public class GeoAstroObject : AstroObject
    {
        [Serializable]
        private class Grid2DIndexTerrainGridMeshObjectDictionary : SerializableDictionary<Vector2Int, Grid2DIndexTerrainGridMeshObjects> { };

        public const double DEFAULT_SIZE = 40000000.0d;

        [BeginFoldout("Geo")]
        [SerializeField, Tooltip("The size (radius in spherical mode or width in flat mode), in world units."), EndFoldout]
        private double _size;

        [BeginFoldout("Spherical")]
        [SerializeField, Tooltip("The amount of time required for the '"+nameof(GeoAstroObject)+"' to transition from spherical to flat.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetEnableSpherical))]
#endif
        private float _sphericalDuration;
        [SerializeField, Tooltip("Display as a sphere (true) or flat (false)?"), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetEnableSpherical))]
#endif
        private bool _spherical;

        [BeginFoldout("Rendering")]
        [SerializeField, Tooltip("Whether the reflection probe should be activated or not."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetEnableReflectionProbe))]
#endif
        private bool _reflectionProbe;

        [SerializeField, HideInInspector]
        private float _sphericalRatio;

        private Tween _sphericalRatioTween;

        private double _radius;

        private Grid2DIndexTerrainGridMeshObjectDictionary[] _grid2DIndexTerrainGridMeshObjects;

        public Action<Grid2DIndexTerrainGridMeshObjects, Vector2Int, Vector2Int> TerrainGridMeshObjectAddedEvent;
        public Action<Grid2DIndexTerrainGridMeshObjects, Vector2Int, Vector2Int> TerrainGridMeshObjectRemovedEvent;

#if UNITY_EDITOR
        protected virtual bool GetEnableSpherical()
        {
            return true;
        }

        protected virtual bool GetEnableReflectionProbe()
        {
            return true;
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            _sphericalRatio = 0.0f;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastSize = size;
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeFields(initializingState);

            InitGrid2DIndexTerrainGridMeshObjects();

            UpdateRadius();
        }

        private void InitGrid2DIndexTerrainGridMeshObjects()
        {
            if (_grid2DIndexTerrainGridMeshObjects == null)
                _grid2DIndexTerrainGridMeshObjects = new Grid2DIndexTerrainGridMeshObjectDictionary[31];
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => size = value, DEFAULT_SIZE, initializingState);
            InitValue(value => sphericalDuration = value, 2.0f, initializingState);
            InitValue(value => spherical = value, true, initializingState);
            InitValue(value => reflectionProbe = value, GetDefaultReflectionProbe(), initializingState);
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
                InitReflectionProbeObject();

                return true;
            }

            return false;
        }

        protected virtual void InitReflectionProbeObject()
        {
            string reflectionProbeName = GetReflectionProbeName();

            ReflectionProbe reflectionProbeObject;

            Transform reflectionProbeTransform = gameObject.transform.Find(reflectionProbeName);

            InstanceManager.InitializationContext initializationState = GetInitializeState();
            if (reflectionProbeTransform != null)
                reflectionProbeObject = reflectionProbeTransform.GetSafeComponent<ReflectionProbe>(initializationState);
            else
            {
                reflectionProbeObject = CreateChild<ReflectionProbe>(reflectionProbeName, null, initializationState);
                reflectionProbeObject.reflectionProbeType = ReflectionProbeMode.Custom;
                reflectionProbeObject.importance = 0;
            }

            if (reflectionProbeObject != Disposable.NULL)
                reflectionProbeObject.isHiddenInHierarchy = true;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                InstanceManager instanceManager = InstanceManager.Instance(false);
                if (instanceManager != Disposable.NULL)
                {
                    instanceManager.IterateOverInstances<TerrainGridMeshObject>(
                        (terrainGridMeshObject) =>
                        {
                            RemoveTerrainGridMeshObjectDelegates(terrainGridMeshObject);
                            if (!IsDisposing())
                                AddTerrainGridMeshObjectDelegates(terrainGridMeshObject);

                            return true;
                        });
                }

                if (_grid2DIndexTerrainGridMeshObjects != null)
                {
                    foreach (Grid2DIndexTerrainGridMeshObjectDictionary grid2DIndexTerrainGridMeshObjectDictionary in _grid2DIndexTerrainGridMeshObjects)
                    {
                        if (grid2DIndexTerrainGridMeshObjectDictionary != null)
                        {
                            foreach (Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObjects in grid2DIndexTerrainGridMeshObjectDictionary.Values)
                            {
                                grid2DIndexTerrainGridMeshObjects.UpdateAllDelegates();
                                RemoveChildTerrainGridMeshObjectDelegates(grid2DIndexTerrainGridMeshObjects);
                                if (!IsDisposing())
                                    AddChildTerrainGridMeshObjectDelegates(grid2DIndexTerrainGridMeshObjects);
                            }
                        }
                    }
                }

                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private double _lastSize;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            Editor.UndoManager.PerformUndoRedoPropertyChange((value) => { size = value; }, ref _size, ref _lastSize);
        }
#endif

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            if (property is TerrainGridMeshObject)
            {
                TerrainGridMeshObject terrainGridMeshObject = (TerrainGridMeshObject)property;
                if (Object.ReferenceEquals(terrainGridMeshObject.parentGeoAstroObject, this))
                    AddTerrainGridMeshObject(terrainGridMeshObject);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveTerrainGridMeshObjectDelegates(TerrainGridMeshObject terrainGridMeshObject)
        {
            terrainGridMeshObject.PropertyAssignedEvent -= TerrainGridMeshObjectPropertyAssignedHandler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddTerrainGridMeshObjectDelegates(TerrainGridMeshObject terrainGridMeshObject)
        {
            terrainGridMeshObject.PropertyAssignedEvent += TerrainGridMeshObjectPropertyAssignedHandler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TerrainGridMeshObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            TerrainGridMeshObject terrainGridMeshObject = property as TerrainGridMeshObject;

            if (name == nameof(parentGeoAstroObject))
            {
                if (Object.ReferenceEquals(newValue, this))
                    AddTerrainGridMeshObject(terrainGridMeshObject);
                else
                    RemoveTerrainGridMeshObject(terrainGridMeshObject);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveChildTerrainGridMeshObjectDelegates(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject)
        {
            grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectPropertyAssignedEvent -= ChildTerrainGridMeshObjectPropertyAssigned;
            grid2DIndexTerrainGridMeshObject.DisposingEvent -= Grid2DIndexTerrainGridMeshObjectDisposing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddChildTerrainGridMeshObjectDelegates(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject)
        {
            grid2DIndexTerrainGridMeshObject.TerrainGridMeshObjectPropertyAssignedEvent += ChildTerrainGridMeshObjectPropertyAssigned;
            grid2DIndexTerrainGridMeshObject.DisposingEvent += Grid2DIndexTerrainGridMeshObjectDisposing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grid2DIndexTerrainGridMeshObjectDisposing(IDisposable disposable)
        {
            Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject = disposable as Grid2DIndexTerrainGridMeshObjects;

            int zoom = MathPlus.GetZoomFromGrid2DDimensions(grid2DIndexTerrainGridMeshObject.grid2DDimensions);

            Grid2DIndexTerrainGridMeshObjectDictionary grid2DIndexTerrainGridMeshObjects = _grid2DIndexTerrainGridMeshObjects[zoom];
            if (grid2DIndexTerrainGridMeshObjects == null)
            {
                grid2DIndexTerrainGridMeshObjects = new Grid2DIndexTerrainGridMeshObjectDictionary();
                _grid2DIndexTerrainGridMeshObjects[zoom] = grid2DIndexTerrainGridMeshObjects;
            }

            if (grid2DIndexTerrainGridMeshObjects.Remove(grid2DIndexTerrainGridMeshObject.grid2DIndex))
            {
                RemoveChildTerrainGridMeshObjectDelegates(grid2DIndexTerrainGridMeshObject);

                if (TerrainGridMeshObjectRemovedEvent != null)
                    TerrainGridMeshObjectRemovedEvent(grid2DIndexTerrainGridMeshObject, grid2DIndexTerrainGridMeshObject.grid2DDimensions, grid2DIndexTerrainGridMeshObject.grid2DIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChildTerrainGridMeshObjectPropertyAssigned(Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObjects, string name, object newValue, object oldValue)
        {
            if (name == nameof(Grid2DMeshObjectBase.grid2DDimensions) || name == nameof(Grid2DMeshObjectBase.grid2DIndex))
            {
                Vector2Int oldGrid2DDimensions;
                Vector2Int oldGrid2DIndex;
                Vector2Int newGrid2DDimensions;
                Vector2Int newGrid2DIndex;

                if (name == nameof(Grid2DMeshObjectBase.grid2DDimensions))
                {
                    oldGrid2DIndex = grid2DIndexTerrainGridMeshObjects.grid2DIndex;
                    oldGrid2DDimensions = (Vector2Int)oldValue;
                    newGrid2DIndex = grid2DIndexTerrainGridMeshObjects.grid2DIndex;
                    newGrid2DDimensions = (Vector2Int)newValue;

                    _grid2DIndexTerrainGridMeshObjects[MathPlus.GetZoomFromGrid2DDimensions((Vector2Int)oldValue)].Remove(newGrid2DIndex);
                    _grid2DIndexTerrainGridMeshObjects[MathPlus.GetZoomFromGrid2DDimensions((Vector2Int)newValue)].Add(newGrid2DIndex, grid2DIndexTerrainGridMeshObjects);
                }
                else
                {
                    oldGrid2DIndex = (Vector2Int)oldValue;
                    oldGrid2DDimensions = grid2DIndexTerrainGridMeshObjects.grid2DDimensions;
                    newGrid2DIndex = (Vector2Int)newValue;
                    newGrid2DDimensions = grid2DIndexTerrainGridMeshObjects.grid2DDimensions;

                    int zoom = MathPlus.GetZoomFromGrid2DDimensions(newGrid2DDimensions);
                    _grid2DIndexTerrainGridMeshObjects[zoom].Remove((Vector2Int)oldValue);
                    _grid2DIndexTerrainGridMeshObjects[zoom][(Vector2Int)newValue] = grid2DIndexTerrainGridMeshObjects;
                }

                if (TerrainGridMeshObjectRemovedEvent != null)
                    TerrainGridMeshObjectRemovedEvent(grid2DIndexTerrainGridMeshObjects, oldGrid2DDimensions, oldGrid2DIndex);
                if (TerrainGridMeshObjectAddedEvent != null)
                    TerrainGridMeshObjectAddedEvent(grid2DIndexTerrainGridMeshObjects, newGrid2DDimensions, newGrid2DIndex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AddTerrainGridMeshObject(TerrainGridMeshObject terrainGridMeshObject)
        {
            InitGrid2DIndexTerrainGridMeshObjects();

            Vector2Int grid2DDimensions = terrainGridMeshObject.grid2DDimensions;
            Vector2Int grid2DIndex = terrainGridMeshObject.grid2DIndex;

            int zoom = MathPlus.GetZoomFromGrid2DDimensions(grid2DDimensions);

            Grid2DIndexTerrainGridMeshObjectDictionary grid2DIndexTerrainGridMeshObjects = _grid2DIndexTerrainGridMeshObjects[zoom];
            if (grid2DIndexTerrainGridMeshObjects == null)
            {
                grid2DIndexTerrainGridMeshObjects = new Grid2DIndexTerrainGridMeshObjectDictionary();
                _grid2DIndexTerrainGridMeshObjects[zoom] = grid2DIndexTerrainGridMeshObjects;
            }

            Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject;
            Vector2Int key = grid2DIndex;
            if (!grid2DIndexTerrainGridMeshObjects.TryGetValue(key, out grid2DIndexTerrainGridMeshObject))
            {
                grid2DIndexTerrainGridMeshObjects[key] = grid2DIndexTerrainGridMeshObject = instanceManager.CreateInstance<Grid2DIndexTerrainGridMeshObjects>();
                grid2DIndexTerrainGridMeshObject.Init(grid2DDimensions, grid2DIndex);

                AddChildTerrainGridMeshObjectDelegates(grid2DIndexTerrainGridMeshObject);

                if (TerrainGridMeshObjectAddedEvent != null)
                    TerrainGridMeshObjectAddedEvent(grid2DIndexTerrainGridMeshObject, grid2DDimensions, grid2DIndex);
            }

            if (!grid2DIndexTerrainGridMeshObject.Contains(terrainGridMeshObject))
            {
                grid2DIndexTerrainGridMeshObject.Add(terrainGridMeshObject);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RemoveTerrainGridMeshObject(TerrainGridMeshObject terrainGridMeshObject)
        {
            if (_grid2DIndexTerrainGridMeshObjects != null)
            {
                Vector2Int grid2DDimensions = terrainGridMeshObject.grid2DDimensions;
                Vector2Int grid2DIndex = terrainGridMeshObject.grid2DIndex;

                int zoom = MathPlus.GetZoomFromGrid2DDimensions(grid2DDimensions);

                Grid2DIndexTerrainGridMeshObjectDictionary grid2DIndexTerrainGridMeshObjects = _grid2DIndexTerrainGridMeshObjects[zoom];
                if (grid2DIndexTerrainGridMeshObjects != null)
                {
                    Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject;
                    Vector2Int key = grid2DIndex;
                    if (grid2DIndexTerrainGridMeshObjects.TryGetValue(key, out grid2DIndexTerrainGridMeshObject))
                    {
                        if (grid2DIndexTerrainGridMeshObject.Remove(terrainGridMeshObject))
                            return true;
                    }
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Grid2DIndexTerrainGridMeshObjects GetGrid2DIndexTerrainGridMeshObject(int zoom, Vector2Int grid2DIndex)
        {
            Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject = null;

            if (_grid2DIndexTerrainGridMeshObjects.Length > zoom)
            {
                Grid2DIndexTerrainGridMeshObjectDictionary terrainGridMeshObjects = _grid2DIndexTerrainGridMeshObjects[zoom];
                if (terrainGridMeshObjects != null && terrainGridMeshObjects.Count > 0)
                    terrainGridMeshObjects.TryGetValue(grid2DIndex, out grid2DIndexTerrainGridMeshObject);
            }

            return grid2DIndexTerrainGridMeshObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetGeoCoordinateElevation(out double elevation, GeoCoordinate3Double geoCoordinate, Camera camera = null)
        {
            Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject = GetGrid2DIndexTerrainGridMeshObjectFromGeoCoordinate(geoCoordinate, camera);

            if (grid2DIndexTerrainGridMeshObject != Disposable.NULL && grid2DIndexTerrainGridMeshObject.GetGeoCoordinateElevation(out elevation, geoCoordinate, camera))
                return true;

            elevation = 0.0f;
            return false;
        }

        public Grid2DIndexTerrainGridMeshObjects GetGrid2DIndexTerrainGridMeshObjectFromGeoCoordinate(GeoCoordinate3Double geoCoordinate, Camera camera = null)
        {
            Grid2DIndexTerrainGridMeshObjects bestGrid2DIndexTerrainGridMeshObject = null;

            if (IsValidSphericalRatio())
            {
                for (int zoom = _grid2DIndexTerrainGridMeshObjects.Length - 1; zoom >= 0; zoom--)
                {
                    Grid2DIndexTerrainGridMeshObjectDictionary grid2DIndexTerrainGridMeshObjectsDictionary = _grid2DIndexTerrainGridMeshObjects[zoom];
                    if (grid2DIndexTerrainGridMeshObjectsDictionary != null && grid2DIndexTerrainGridMeshObjectsDictionary.Count > 0)
                    {
                        //Make the assumption that all the TerrainGridMeshObjects have the same grid2DDimensions
                        Grid2DIndexTerrainGridMeshObjects firstGrid2DIndexTerrainGridMeshObject = grid2DIndexTerrainGridMeshObjectsDictionary.First().Value;

                        Vector2Double grid2DIndex = firstGrid2DIndexTerrainGridMeshObject.GetGrid2DIndexFromGeoCoordinate(geoCoordinate);
                        Vector2Int grid2DIndexInt = new Vector2Int((int)Math.Truncate(grid2DIndex.x), (int)Math.Truncate(grid2DIndex.y));

                        if (grid2DIndexTerrainGridMeshObjectsDictionary.TryGetValue(grid2DIndexInt, out Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject))
                        {
                            grid2DIndexTerrainGridMeshObject.IterateOverTerrainGridMeshObject((terrainGridMeshObject) =>
                            {
                                if (bestGrid2DIndexTerrainGridMeshObject == null || grid2DIndexTerrainGridMeshObject > bestGrid2DIndexTerrainGridMeshObject)
                                {
                                    bestGrid2DIndexTerrainGridMeshObject = grid2DIndexTerrainGridMeshObject;
                                    return true;
                                }
                                return true;
                            }, camera);
                        }
                    }
                }
            }

            return bestGrid2DIndexTerrainGridMeshObject;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsInitializedTerraingGridMeshObject()
        {
            foreach (Grid2DIndexTerrainGridMeshObjectDictionary grid2DIndexTerrainGridMeshObjectsDictionary  in  _grid2DIndexTerrainGridMeshObjects)
            {
                if (grid2DIndexTerrainGridMeshObjectsDictionary != null && grid2DIndexTerrainGridMeshObjectsDictionary.Count > 0)
                {
                    foreach (Grid2DIndexTerrainGridMeshObjects grid2DIndexTerrainGridMeshObject in grid2DIndexTerrainGridMeshObjectsDictionary.Values)
                    {
                        if (grid2DIndexTerrainGridMeshObject.ContainsInitializedTerrainGridMeshObject())
                            return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool GetDefaultReflectionProbe()
        {
            return true;
        }

        public double GetScaledRadius()
        {
            return radius * GetScale();
        }

        private void UpdateRadius()
        {
            radius = MathPlus.GetRadiusFromCircumference(size);
        }

        public double radius
        {
            get { return _radius; }
            private set { SetValue(nameof(radius), value, ref _radius); }
        }

        public double GetScaledSize()
        {
            return size * GetScale();
        }

        /// <summary>
        /// The size (radius in spherical mode or width in flat mode), in world units.
        /// </summary>
        [Json]
        public double size
        {
            get { return _size; }
            set 
            { 
                SetValue(nameof(size), Math.Max(value, 0.01d), ref _size, (newValue, oldValue) => 
                {
#if UNITY_EDITOR
                    _lastSize = newValue;
#endif
                    UpdateRadius();
                }); 
            }
        }

        /// <summary>
        /// The amount of time required for the <see cref="GeoAstroObject"/> to transition from spherical to flat.
        /// </summary>
        [Json]
        public float sphericalDuration
        {
            get { return _sphericalDuration; }
            set { SetValue(nameof(sphericalDuration), value, ref _sphericalDuration); }
        }

        /// <summary>
        /// Display as a sphere (true) or flat (false)?
        /// </summary>
        [Json]
        public bool spherical
        {
            get { return _spherical; }
            set { SetSpherical(value); }
        }

        public void SetSpherical(bool value, bool animate = true)
        {
            SetValue(nameof(spherical), value, ref _spherical, (newValue, oldValue) =>
            {
                sphericalRatioTween = tweenManager.To(sphericalRatio, spherical ? 1.0f : 0.0f, initialized && animate ? sphericalDuration : 0.0f, (value) => { sphericalRatio = value; }, () => { sphericalRatioTween = null; });
            });
        }

        /// <summary>
        /// Whether the reflection probe should be activated or not.
        /// </summary>
        [Json]
        public bool reflectionProbe
        {
            get { return _reflectionProbe; }
            set { SetValue(nameof(reflectionProbe), value, ref _reflectionProbe); }
        }

        public float sphericalRatio
        {
            get { return _sphericalRatio; }
            private set { SetValue(nameof(sphericalRatio), value, ref _sphericalRatio); }
        }

        private Tween sphericalRatioTween
        {
            get { return _sphericalRatioTween; }
            set
            {
                if (Object.ReferenceEquals(_sphericalRatioTween, value))
                    return;

                Dispose(_sphericalRatioTween);

                _sphericalRatioTween = value;
            }
        }

        public virtual float GetSphericalRatio()
        {
            return sphericalRatio;
        }

        public bool IsValidSphericalRatio()
        {
            return IsSpherical() || IsFlat();
        }

        public bool IsSpherical()
        {
            return GetSphericalRatio() == 1.0f;
        }

        public bool IsFlat()
        {
            return GetSphericalRatio() == 0.0f;
        }

        public double GetScaledAtmosphereThickness()
        {
            return GetAtmosphereThickness() * GetScale();
        }

        public double GetScale()
        {
            return (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3.0d;
        }

        public double GetCircumferenceAtLatitude(double latitude)
        {
            return MathPlus.GetCircumferenceAtLatitude(latitude, size);
        }

        public GeoCoordinate3Double GetGeoCoordinateFromPoint(Vector3Double point)
        {
            if (transform != Disposable.NULL)
                return GetGeoCoordinateFromLocalPoint(transform.InverseTransformPoint(point));
            return GeoCoordinate3Double.zero;
        }

        public GeoCoordinate3Double GetGeoCoordinateFromLocalPoint(Vector3Double localPoint)
        {
            return MathPlus.GetGeoCoordinateFromLocalPoint(localPoint, sphericalRatio, radius, size);
        }

        public Vector3Double GetPointFromGeoCoordinate(GeoCoordinate3Double coord)
        {
            return transform.TransformPoint(GetLocalPointFromGeoCoordinate(coord));
        }

        public Vector3Double GetLocalPointFromGeoCoordinate(GeoCoordinate3Double coord)
        {
            return MathPlus.GetLocalPointFromGeoCoordinate(coord, sphericalRatio, radius, size);
        }

        public Vector3Double GetSurfacePointFromPoint(Vector3Double point, bool clamp = true)
        {
            return transform.TransformPoint(GetSurfaceLocalPointFromPoint(point, clamp));
        }

        public Vector3Double GetSurfaceLocalPointFromPoint(Vector3Double point, bool clamp = true)
        {
            return MathPlus.GetLocalSurfaceFromLocalPoint(transform.InverseTransformPoint(point), sphericalRatio, radius, size, clamp);
        }

        public QuaternionDouble GetUpVectorFromPoint(Vector3Double point)
        {
            return GetUpVectorFromGeoCoordinate(GetGeoCoordinateFromPoint(point));
        }

        public QuaternionDouble GetUpVectorFromGeoCoordinate(GeoCoordinate2Double coord)
        {
            return transform.rotation * MathPlus.GetUpVectorFromGeoCoordinate(coord, sphericalRatio);
        }

        public override Vector3 GetGravitationalForce(Object objectBase)
        {
            GeoCoordinate3Double geoCoordinate = GetGeoCoordinateFromPoint(objectBase.transform.position);
            double distance = radius + geoCoordinate.altitude * GetScale();
            Vector3Double direction = GetUpVectorFromGeoCoordinate(geoCoordinate) * Vector3Double.down;

            return GetGravitationalForce(objectBase, distance, direction);
        }

        public override void SetPlanetPreset(PlanetType astroObjectType)
        {
            base.SetPlanetPreset(astroObjectType);

            double presetSize = GetAstroObjectSize(astroObjectType);
            if (presetSize != 0.0d)
                size = presetSize;
        }

        public static double GetAstroObjectSize(PlanetType astroObject)
        {
            double size = 0.0d;
            switch (astroObject)
            {
                case PlanetType.Mercury:
                    size = 15324689.0d;
                    break;
                case PlanetType.Venus:
                    size = 38025837.0d;
                    break;
                case PlanetType.Earth:
                    size = 40075016.685578d;
                    break;
                case PlanetType.Mars:
                    size = 21343980.0d;
                    break;
                case PlanetType.Jupiter:
                    size = 449197484.0d;
                    break;
                case PlanetType.Saturn:
                    size = 378675012.0d;
                    break;
                case PlanetType.Uranus:
                    size = 160591933.0d;
                    break;
                case PlanetType.Neptune:
                    size = 155609367.0d;
                    break;
                case PlanetType.Pluto:
                    size = 7445575.0d;
                    break;
                case PlanetType.Moon:
                    size = 10921000.0d;
                    break;
                case PlanetType.Sun:
                    size = 4379000000.0d;
                    break;
            }
            return size;
        }

        public double GetAtmosphereThickness()
        {
            return AtmosphereEffect.ATMOPSHERE_ALTITUDE_FACTOR * radius - radius;
        }

        public double GetAtmosphereAltitudeRatio(double atmospherethickness, Vector3Double position)
        {
            return 1.0d - MathPlus.Clamp01(Math.Abs(GetGeoCoordinateFromPoint(position).altitude / atmospherethickness));
        }

        public static GeoAstroObject GetClosestGeoAstroObject(Vector3Double point)
        {
            GeoAstroObject closestGeoAstroObject = null;

            double closestDistance = double.MaxValue;
            InstanceManager instanceManager = InstanceManager.Instance();
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<AstroObject>(
                    (astroObject) =>
                    {
                        GeoAstroObject geoAstroObject = astroObject as GeoAstroObject;
                        if (geoAstroObject != Disposable.NULL && astroObject.isActiveAndEnabled)
                        {
                            double distance = geoAstroObject.GetGeoCoordinateFromPoint(point).altitude;
                            if (distance <= closestDistance)
                            {
                                closestDistance = distance;
                                closestGeoAstroObject = geoAstroObject;
                            }
                        }

                        return true;
                    });
            }

            return closestGeoAstroObject;
        }

        public RayDouble GetGeoCoordinateRayDouble(GeoCoordinate3Double geoCoordinate)
        {
            return GetGeoCoordinateRay(geoCoordinate, GetDefaultRaycastMaxDistance());
        }

        public RayDouble GetGeoCoordinateRay(GeoCoordinate3Double geoCoordinate, double maxDistance)
        {
            QuaternionDouble upVector = GetUpVectorFromGeoCoordinate(geoCoordinate);
            return new RayDouble(GetPointFromGeoCoordinate(new GeoCoordinate3Double(geoCoordinate.latitude, geoCoordinate.longitude, 0.0d)) + upVector * new Vector3Double(0.0d, maxDistance, 0.0d), upVector * Vector3Double.down);
        }

        public double GetDefaultRaycastMaxDistance()
        {
            return radius * 0.01d;
        }

        private string GetReflectionProbeName()
        {
            return typeof(GeoAstroObject).Name + "" + typeof(ReflectionProbe).Name;
        }

        public void UpdateReflectionProbeTransform(Camera camera)
        {
            transform.IterateOverChildrenObject<ReflectionProbe>((reflectionProbe) =>
            {
                if (reflectionProbe.name == GetReflectionProbeName())
                {
                    GeoCoordinate3Double geoCoordinate = GetGeoCoordinateFromPoint(camera.transform.position);

                    double maxAltitude = GetScaledAtmosphereThickness();
                    if (geoCoordinate.altitude > maxAltitude)
                        geoCoordinate.altitude = maxAltitude;

                    reflectionProbe.transform.position = GetPointFromGeoCoordinate(geoCoordinate);
                }
                return true;
            });
        }

        public override bool HierarchicalUpdateEnvironmentAndReflection(Camera camera, ScriptableRenderContext? context)
        {
            if (base.HierarchicalUpdateEnvironmentAndReflection(camera, context))
            {
                transform.IterateOverChildrenObject<ReflectionProbe>((reflectionProbe) =>
                {
                    if (reflectionProbe.name == GetReflectionProbeName())
                    {
                        reflectionProbe.intensity = IsValidSphericalRatio() && this.reflectionProbe ? (float)GetAtmosphereAltitudeRatio(GetScaledAtmosphereThickness(), camera.transform.position) : 0.0f;

                        if (reflectionProbe.boxSize.x != size)
                            reflectionProbe.boxSize = Vector3.one * (float)size;
                        reflectionProbe.boxOffset = transform.position - reflectionProbe.transform.position;

                        reflectionProbe.reflectionProbe.customBakedTexture = context.HasValue && this.reflectionProbe ? camera.GetEnvironmentCubeMap() : null;
                    }
                    return true;
                });

                return true;
            }
            return false;
        }

        private int _updateCount;
        public override void HierarchicalClearDirtyFlags()
        {
            base.HierarchicalClearDirtyFlags();

            if (!GetLoadingInitialized())
                _updateCount++;
        }

        public bool GetLoadingInitialized()
        {
            return _updateCount > 2;
        }

        public override bool OnDisposing()
        {
            if (base.OnDisposing())
            {
                sphericalRatioTween = null;

                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class Grid2DIndexTerrainGridMeshObjects : Disposable
    {
        private Vector2Int _grid2DDimensions;
        private Vector2Int _grid2DIndex;

        private List<TerrainGridMeshObject> _terrainGridMeshObjects;

        public Action<Grid2DIndexTerrainGridMeshObjects, string, object, object> TerrainGridMeshObjectPropertyAssignedEvent;

        public Action<Object, PropertyMonoBehaviour> TerrainGridMeshObjectChildAddedEvent;
        public Action<Object, PropertyMonoBehaviour> TerrainGridMeshObjectChildRemovedEvent;

        public Action<TerrainGridMeshObject> TerrainGridMeshObjectAddedEvent;
        public Action<TerrainGridMeshObject> TerrainGridMeshObjectRemovedEvent;

        public void Init(Vector2Int grid2DDimensions, Vector2Int grid2DIndex)
        {
            _grid2DDimensions = grid2DDimensions;
            _grid2DIndex = grid2DIndex;
        }

        public override void InitializeFields()
        {
            base.InitializeFields();

            if (_terrainGridMeshObjects == null)
                _terrainGridMeshObjects = new List<TerrainGridMeshObject>();
        }

        public override void UpdateAllDelegates()
        {
            base.UpdateAllDelegates();

            foreach (TerrainGridMeshObject terrainGridMeshObject in _terrainGridMeshObjects)
            {
                RemoveTerrainGridMeshObject(terrainGridMeshObject);
                if (!IsDisposing())
                    AddTerrainGridMeshObject(terrainGridMeshObject);
            }
        }

        public Vector2Int grid2DDimensions
        {
            get { return _grid2DDimensions; }
        }
        public Vector2Int grid2DIndex
        {
            get { return _grid2DIndex; }
        }

        public int Count
        {
            get { return _terrainGridMeshObjects.Count; }
        }

        public bool Contains(TerrainGridMeshObject terrainGridMeshObject)
        {
            return _terrainGridMeshObjects.Contains(terrainGridMeshObject);
        }

        public void Add(TerrainGridMeshObject terrainGridMeshObject)
        {
            if (!_terrainGridMeshObjects.Contains(terrainGridMeshObject))
            {
                _terrainGridMeshObjects.Add(terrainGridMeshObject);

                AddTerrainGridMeshObject(terrainGridMeshObject);

                if (TerrainGridMeshObjectAddedEvent != null)
                    TerrainGridMeshObjectAddedEvent(terrainGridMeshObject);
            }
        }

        public bool Remove(TerrainGridMeshObject terrainGridMeshObject)
        {
            if (_terrainGridMeshObjects.Remove(terrainGridMeshObject))
            {
                RemoveTerrainGridMeshObject(terrainGridMeshObject);

                if (TerrainGridMeshObjectRemovedEvent != null)
                    TerrainGridMeshObjectRemovedEvent(terrainGridMeshObject);

                if (_terrainGridMeshObjects.Count == 0)
                    DisposeManager.Dispose(this);

                return true;
            }

            return false;
        }

        public bool ContainsInitializedTerrainGridMeshObject()
        {
            foreach(TerrainGridMeshObject terrainGridMeshObject in _terrainGridMeshObjects)
            {
                if (terrainGridMeshObject.GetMeshRenderersInitialized())
                    return true;
            }

            return false;
        }

        private void AddTerrainGridMeshObject(TerrainGridMeshObject terrainGridMeshObject)
        {
            terrainGridMeshObject.PropertyAssignedEvent += TerrainGridMeshObjectPropertyAssignedHandler;
            terrainGridMeshObject.ChildAddedEvent += TerrainGridMeshObjectChildAddedHandler;
            terrainGridMeshObject.ChildRemovedEvent += TerrainGridMeshObjectChildRemovedHandler;
            terrainGridMeshObject.DisposingEvent += TerrainGridMeshObjectDisposingHandler;
        }

        private void RemoveTerrainGridMeshObject(TerrainGridMeshObject terrainGridMeshObject)
        {
            terrainGridMeshObject.PropertyAssignedEvent -= TerrainGridMeshObjectPropertyAssignedHandler;
            terrainGridMeshObject.ChildAddedEvent -= TerrainGridMeshObjectChildAddedHandler;
            terrainGridMeshObject.ChildRemovedEvent -= TerrainGridMeshObjectChildRemovedHandler;
            terrainGridMeshObject.DisposingEvent -= TerrainGridMeshObjectDisposingHandler;
        }

        protected void TerrainGridMeshObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (TerrainGridMeshObjectPropertyAssignedEvent != null)
                TerrainGridMeshObjectPropertyAssignedEvent(this, name, newValue, oldValue);
        }

        protected void TerrainGridMeshObjectChildAddedHandler(Object objectBase, PropertyMonoBehaviour child)
        {
            if (TerrainGridMeshObjectChildAddedEvent != null)
                TerrainGridMeshObjectChildAddedEvent(objectBase, child);
        }

        protected void TerrainGridMeshObjectChildRemovedHandler(Object objectBase, PropertyMonoBehaviour child)
        {
            if (TerrainGridMeshObjectChildRemovedEvent != null)
                TerrainGridMeshObjectChildRemovedEvent(objectBase, child);
        }

        protected void TerrainGridMeshObjectDisposingHandler(IDisposable disposable)
        {
            Remove(disposable as TerrainGridMeshObject);
        }

        public float GetHighestAlpha(Camera camera)
        {
            float alpha = 0.0f;

            IterateOverTerrainGridMeshObject((terrainGridMeshObject) => 
            {
                alpha = Mathf.Max(alpha, terrainGridMeshObject.GetMeshRenderersInitialized() ? terrainGridMeshObject.GetCurrentAlpha() : 0.0f);
                return true;
            }, camera);
           
            return alpha;
        }

        public bool GetGeoCoordinateElevation(out double elevation, GeoCoordinate3Double geoCoordinate, Camera camera = null, bool raycast = false)
        {
            TerrainGridMeshObject terrainGridMeshObject = GetHighestSubdivisionTerrainGridMeshObject(camera);

            if (terrainGridMeshObject != Disposable.NULL)
                return terrainGridMeshObject.GetGeoCoordinateElevation(out elevation, geoCoordinate, raycast);

            elevation = 0.0f;
            return false;
        }

        public TerrainGridMeshObject GetHighestSubdivisionTerrainGridMeshObject(Camera camera = null)
        {
            TerrainGridMeshObject highestSubdivisionTerrainGridMeshObject = null;

            IterateOverTerrainGridMeshObject((terrainGridMeshObject) =>
            {
                if (terrainGridMeshObject.GetMeshRenderersInitialized() && (highestSubdivisionTerrainGridMeshObject == Disposable.NULL || highestSubdivisionTerrainGridMeshObject.subdivision < terrainGridMeshObject.subdivision))
                    highestSubdivisionTerrainGridMeshObject = terrainGridMeshObject;
                return true;
            }, camera);

            return highestSubdivisionTerrainGridMeshObject;
        }

        public Vector2Double GetGrid2DIndexFromGeoCoordinate(GeoCoordinate3Double geoCoordinate)
        {
            return MathPlus.GetGrid2DIndexFromGeoCoordinate(geoCoordinate, grid2DDimensions);
        }

        public void IterateOverTerrainGridMeshObject(Func<TerrainGridMeshObject, bool> callback, Camera camera = null)
        {
            foreach (TerrainGridMeshObject terrainGridMeshObject in _terrainGridMeshObjects)
            {
                if (terrainGridMeshObject != Disposable.NULL && !terrainGridMeshObject.CameraIsMasked(camera) && !callback(terrainGridMeshObject))
                    return;
            }
        }

        public static bool operator <(Grid2DIndexTerrainGridMeshObjects a, Grid2DIndexTerrainGridMeshObjects b)
        {
            return a.grid2DDimensions.x < b.grid2DDimensions.x;
        }

        public static bool operator >(Grid2DIndexTerrainGridMeshObjects a, Grid2DIndexTerrainGridMeshObjects b)
        {
            return a.grid2DDimensions.x > b.grid2DDimensions.x;
        }

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyContext)
        {
            if (base.OnDisposed(destroyContext))
            {
                _grid2DDimensions = Vector2Int.zero;
                _grid2DIndex = Vector2Int.zero;

                if (_terrainGridMeshObjects != null)
                    _terrainGridMeshObjects.Clear();

                return true;
            }
            return false;
        }
    }
}