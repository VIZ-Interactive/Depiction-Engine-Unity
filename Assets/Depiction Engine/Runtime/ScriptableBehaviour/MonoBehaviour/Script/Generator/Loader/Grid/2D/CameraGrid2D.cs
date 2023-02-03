// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class CameraGrid2D : ScriptableObject, IGrid2D
    {
        private Camera _camera;

        private List<Grid2D> _grids;

        private GeoCoordinate2Double _geoCenter;

        private bool _wasFirstUpdated;

        public CameraGrid2D Init(Camera camera)
        {
            _camera = camera;

            return this;
        }

        public bool wasFirstUpdated { get { return _wasFirstUpdated; } }

        public GeoCoordinate2Double geoCenter { get { return _geoCenter; } }
        public Camera camera { get { return _camera; } }

        public Grid2D IsInGrid(Vector2Int grid2DIndex, Vector2Int grid2DDimensions)
        {
            if (_grids != null)
            {
                foreach (Grid2D grid in _grids)
                {
                    if (grid.IsInGrid(grid2DIndex, grid2DDimensions))
                        return grid;
                }
            }

            return null;
        }

        public virtual void IterateOverIndexes(Action<IGrid2D, GeoCoordinate2Double, Vector2Int, Vector2Int> callback)
        {
            if (_grids != null)
            {
                foreach (Grid2D grid in _grids)
                    grid.IterateOverIndexes(callback);
            }
        }
    
        public bool UpdateCameraGridProperties(bool enabled, GeoAstroObject parentGeoAstroObject, Vector3Double center, Camera camera, Vector2Int cascades, Vector2Int minMaxZoom, float sizeMultiplier, float xyTilesRatio)
        {
            bool changed = false;

            if (enabled)
            {
                Vector3Double localCenter = parentGeoAstroObject.transform.InverseTransformPoint(center);
                
                _geoCenter = parentGeoAstroObject.GetGeoCoordinateFromLocalPoint(localCenter);

                double parentGeoAstroObjectRadius = parentGeoAstroObject.radius;

                double distanceFromCenter = Vector3Double.Distance(localCenter, parentGeoAstroObject.transform.InverseTransformPoint(camera.transform.position));

                int maxZoom = minMaxZoom.y;
                int minZoom = minMaxZoom.x;
                int zoomCount = maxZoom - minZoom + 1;

                bool updateGrids = _grids == null || _grids.Count != zoomCount || minZoom != MathPlus.GetZoomFromGrid2DDimensions(_grids[0].grid2DDimensions) || maxZoom != MathPlus.GetZoomFromGrid2DDimensions(_grids[_grids.Count - 1].grid2DDimensions);

                if (updateGrids)
                {
                    _grids = new List<Grid2D>(zoomCount);
                    for (int i = 0; i < zoomCount; i++)
                        _grids.Add(CreateGrid2D<CircleGrid>());
               
                    changed = true;
                }

                int bestZoom = -1;
                int cascade = 0;
                for (int i = _grids.Count - 1 ; i >= 0 ; i--)
                {
                    int zoom = minZoom + i;
                    Vector2Int gridDimensions = MathPlus.GetGrid2DDimensionsFromZoom(zoom, xyTilesRatio);

                    double gridRadius = 0.0d;

                    int minCascadeZoom = bestZoom - cascades.y;

                    if (zoom >= minCascadeZoom)
                    {
                        if (zoom == 0)
                            gridRadius = double.PositiveInfinity;
                        else
                        {
                            Vector2Double intersection1;
                            Vector2Double intersection2;

                            double cameraRadius = GetZoomRadius(zoom, parentGeoAstroObjectRadius, sizeMultiplier);

                            if (parentGeoAstroObject.IsSpherical())
                            {
                                MathGeometry.GetCircleToCircleIntersections(out intersection1, out intersection2, parentGeoAstroObjectRadius, new Vector2Double(0.0d, parentGeoAstroObjectRadius + distanceFromCenter), cameraRadius);

                                //If the camera radius is completely inside the parentGeoAstroObject dont load anything
                                if (intersection1 == Vector2Double.negativeInfinity && intersection2 == Vector2Double.positiveInfinity && cameraRadius < parentGeoAstroObjectRadius)
                                    intersection1 = intersection2 = Vector2Double.zero;
                            }
                            else
                                MathGeometry.LineCircleIntersections(out intersection1, out intersection2, cameraRadius, new Vector2Double(-100000000.0d, -distanceFromCenter), new Vector2Double(100000000.0d, -distanceFromCenter));

                            gridRadius = Vector3Double.Distance(intersection1, intersection2) / 2.0d;
                        }
                    }

                    CircleGrid grid = _grids[i] as CircleGrid;
                    if (gridRadius != 0.0d)
                    {
                        if (bestZoom == -1)
                            bestZoom = zoom;

                        if (cascade < cascades.x)
                            gridRadius = 0.0f;

                        grid.cascade = cascade;
                        cascade++;
                    }

                    if (grid.SetEnabled(gridRadius != 0.0d))
                        changed = true;
                    if (grid.UpdateCircleGridProperties(gridDimensions, _geoCenter, parentGeoAstroObject, gridRadius))
                        changed = true;
                }
            }
            else
            {
                if (_grids != null)
                {
                    foreach (Grid2D grid in _grids)
                    {
                        if (grid.SetEnabled(false))
                            changed = true;
                    }
                }
            }

            return changed;
        }

        private T CreateGrid2D<T>() where T : Grid2D
        {
            T grid2D = Grid2DLoaderBase.CreateGrid<T>();
            grid2D.Init(this);
            return grid2D;
        }

        private double GetZoomRadius(int zoom, double parentGeoAstroObjectRadius, double sizeMultiplier)
        {
            //The number 5.0 in this equation is an arbitrary number that seems to give a nice default range
            return parentGeoAstroObjectRadius * (1.0d / Math.Pow(2.0d, zoom)) * sizeMultiplier * 5.0d;
        }

        public bool UpdateGrid()
        {
            _wasFirstUpdated = true;
            
            bool updated = false;

            if (_grids != null)
            {
                foreach (Grid2D grid in _grids)
                {
                    if (grid.UpdateGrid())
                        updated = true;
                }
            }

            return updated;
        }
    }
}
