// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class Grid2DLoaderBase : Index2DLoaderBase
    {
        [BeginFoldout("Grid")]
        [SerializeField, Tooltip("A value (in seconds) by which we will multiply the offset between the center index and the "+nameof(LoadScope)+" index to cause objects farther away from the center to be loaded later then objects nearby. Set to zero to deactivate and load everything at the same time.")]
        protected float _loadDelay;
        [SerializeField, Tooltip("The grid zoom value. Ignored by "+nameof(CameraGrid2DLoader)+ " or "+nameof(FillGrid2DLoader)+"."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowZoom))]
#endif
        protected int _zoom;

#if UNITY_EDITOR
        protected virtual bool GetShowZoom()
        {
            return true;
        }
#endif

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            InitGrids();
        }

        protected virtual void InitGrids()
        {
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => loadDelay = value, GetDefaultLoadInterval(), initializingContext);
            InitValue(value => zoom = value, 0, initializingContext);
        }

        protected override float GetDefaultWaitBetweenLoad()
        {
            return 1.0f;
        }

        protected virtual float GetDefaultLoadInterval()
        {
            return 1.0f;
        }

        /// <summary>
        /// A value (in seconds) by which we will multiply the offset between the center index and the <see cref="DepictionEngine.LoadScope"/> index to cause objects farther away from the center to be loaded later then objects nearby. Set to zero to deactivate and load everything at the same time.
        /// </summary>
        [Json]
        public float loadDelay
        {
            get => _loadDelay;
            set { SetValue(nameof(loadDelay), Mathf.Max(value, 0.0f), ref _loadDelay); }
        }

        /// <summary>
        /// The grid zoom value. Ignored by <see cref="DepictionEngine.CameraGrid2DLoader"/> or <see cref="DepictionEngine.FillGrid2DLoader"/>.
        /// </summary>
        [Json]
        public int zoom
        {
            get => _zoom;
            set { SetValue(nameof(zoom), value, ref _zoom); }
        }

        public override bool IsInList(LoadScope loadScope)
        {
            Index2DLoadScope indexLoadScope = loadScope as Index2DLoadScope;

            IEnumerable<IGrid2D> grids = GetGrids();

            if (grids != null)
            {
                foreach (IGrid2D grid in grids)
                {
                    if (grid != null)
                    {
                        if (grid.IsInGrid(indexLoadScope.scopeIndex, indexLoadScope.scopeDimensions))
                            return true;
                    }
                }
            }

            return false;
        }

        protected virtual IEnumerable<IGrid2D> GetGrids()
        {
            return null;
        }

        protected override void UpdateLoaderFields(bool forceUpdate)
        {
            base.UpdateLoaderFields(forceUpdate);

            IEnumerable<IGrid2D> grids = GetGrids();

            if (grids != null)
            {
                foreach (IGrid2D grid in grids)
                {
                    if (grid != null)
                    {
                        if (UpdateGridsFields(grid, forceUpdate))
                            grid.UpdateGrid();
                    }
                }
            }
        }

        protected virtual bool UpdateGridsFields(IGrid2D grid, bool forceUpdate = false)
        {
            bool changed = forceUpdate || !wasFirstUpdated;

            if (grid is Grid2D) 
            {
                GeoCoordinate2Double geoCenter = GetGeoCoordinateCenter();
                Vector2Int grid2DDimensions = GetGrid2DDimensionsFromZoom(GetZoomForGrid(grid as Grid2D));
                
                Grid2D grid2D = grid as Grid2D;
                if (grid2D.UpdateGridFields(grid2DDimensions, geoCenter))
                    changed = true;

                if (grid2D is GeometricGrid2D && (grid2D as GeometricGrid2D).UpdateGeometricGridFields(grid2DDimensions, geoCenter, parentGeoAstroObject))
                    changed = true;
            }

            return changed;
        }

        protected virtual int GetZoomForGrid(Grid2D grid)
        {
            return _zoom;
        }

        protected override void IterateOverLoadScopeList(Action<Vector2Int, Vector2Int, Vector2Int> callback)
        {
            IEnumerable<IGrid2D> grids = GetGrids();

            if (grids != null)
            {
                foreach (IGrid2D grid in grids)
                {
                    if (grid != null)
                    {
                        grid.IterateOverIndexes((grid, geoCenter, index, dimensions) =>
                            {
                                callback(index, dimensions, MathPlus.GetIndexFromGeoCoordinate(geoCenter, dimensions, false));
                            });
                    }
                }
            }
        }

        protected override float GetLoadInterval(Vector2Int grid2DIndex, Vector2Int grid2DDimensions, Vector2Int centerIndex)
        {
            base.GetLoadInterval(grid2DIndex, grid2DDimensions, centerIndex);

            Vector2Int xyDelta = new(
                Math.Abs(grid2DIndex.x - centerIndex.x),
                Math.Abs(grid2DIndex.y - centerIndex.y));

            if (IsSpherical())
            {
                xyDelta = new(
                    Math.Min(xyDelta.x, Math.Abs(grid2DIndex.x + (grid2DIndex.x < centerIndex.x ? grid2DDimensions.x : -grid2DDimensions.x) - centerIndex.x)),
                    Math.Min(xyDelta.y, Math.Abs(grid2DIndex.y + (grid2DIndex.y < centerIndex.y ? grid2DDimensions.y : -grid2DDimensions.y) - centerIndex.y)));
            }

            return (xyDelta.x + xyDelta.y) * loadDelay + UnityEngine.Random.Range(0, loadDelay);
        }

        protected GeoCoordinate2Double GetGeoCoordinateCenter()
        {
            return transform.GetGeoCoordinate();
        }

        public static T CreateGrid<T>() where T : GridBase
        {
            return ScriptableObject.CreateInstance<T>().Init() as T;
        }
    }
}
