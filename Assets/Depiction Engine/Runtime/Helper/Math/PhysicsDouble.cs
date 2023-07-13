// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;
using System;

namespace DepictionEngine
{
    /// <summary>
    /// Global physics properties and helper methods.
    /// </summary>
    public class PhysicsDouble
    {
        private static readonly int EVERY_LAYER_BUT_IGNORE_RAYCAST_AND_RENDER = ~(LayerMask.GetMask("Ignore Raycast") | LayerMask.GetMask("Ignore Render"));

        public static RaycastHitDouble[] CameraRaycastAll(Camera camera, Vector2 screenPosition, float maxDistance)
        {
            return CameraRaycastAll(camera, screenPosition, maxDistance, EVERY_LAYER_BUT_IGNORE_RAYCAST_AND_RENDER);
        }

        public static RaycastHitDouble[] CameraRaycastAll(Camera camera, Vector2 screenPosition, float maxDistance, int layerMask)
        {
            return RaycastAll(camera.ScreenPointToRay(screenPosition), maxDistance, layerMask);
        }

        public static RaycastHitDouble[] RaycastAll(RayDouble ray, float maxDistance)
        {
            return RaycastAll(ray, maxDistance, EVERY_LAYER_BUT_IGNORE_RAYCAST_AND_RENDER);
        }

        public static RaycastHitDouble[] RaycastAll(RayDouble ray, float maxDistance, int layerMask)
        {
            return GetHitsDouble(Physics.RaycastAll(ray, maxDistance, layerMask));
        }

        public static bool CameraRaycast(Camera camera, Vector2 screenPosition, out RaycastHitDouble hit, float maxDistance)
        {
            return CameraRaycast(camera, screenPosition, out hit, maxDistance, EVERY_LAYER_BUT_IGNORE_RAYCAST_AND_RENDER);
        }

        public static bool CameraRaycast(Camera camera, Vector2 screenPosition, out RaycastHitDouble hit, float maxDistance, int layerMask)
        {
            return Raycast(camera.ScreenPointToRay(screenPosition), out hit, maxDistance, layerMask);
        }

        public static bool Raycast(RayDouble ray, out RaycastHitDouble hit, float maxDistance)
        {
            return Raycast(ray, out hit, maxDistance, EVERY_LAYER_BUT_IGNORE_RAYCAST_AND_RENDER);
        }

        public static bool Raycast(RayDouble ray, out RaycastHitDouble hit, float maxDistance, int layerMask)
        {
            hit = null;

            if (Physics.Raycast(ray, out RaycastHit hitSingle, maxDistance, layerMask))
            {
                hit = hitSingle;
                return true;
            }

            return false;
        }

        private static RaycastHitDouble[] GetHitsDouble(RaycastHit[] hits)
        {
            RaycastHitDouble[] hitsDouble = new RaycastHitDouble[hits.Length];

            for (int i = 0; i < hits.Length; i++)
                hitsDouble[i] = hits[i];

            return hitsDouble;
        }

        public static RaycastHitDouble GetClosestHit(Camera camera, RaycastHitDouble[] hits, bool prioritizeUI = true)
        {
            RaycastHitDouble closestHit = null;

            bool compareUIOnly = false;

            if (prioritizeUI)
            {
                foreach (RaycastHitDouble hit in hits)
                {
                    if (hit.meshRendererVisual is UIMeshRendererVisual)
                    {
                        compareUIOnly = true;
                        break;
                    }
                }
            }

            double closestDistance = double.PositiveInfinity;
            foreach (RaycastHitDouble hit in hits)
            {
                if (hit.meshRendererVisual is UIMeshRendererVisual)
                {
                    UIVisual uiVisual = (hit.meshRendererVisual as UIMeshRendererVisual).GetComponentInParent<UIVisual>();
                    if (uiVisual.cameras.Contains(camera.GetInstanceID()))
                        IsClosestHit(ref closestDistance, ref closestHit, hit, camera.transform.position);
                }
                else if (!compareUIOnly)
                    IsClosestHit(ref closestDistance, ref closestHit, hit, camera.transform.position);
            }

            return closestHit;
        }

        public static RaycastHitDouble GetClosestHit(Vector3Double point, RaycastHitDouble[] hits)
        {
            RaycastHitDouble closestHit = null;

            double closestDistance = double.PositiveInfinity;
            foreach (RaycastHitDouble hit in hits)
                IsClosestHit(ref closestDistance, ref closestHit, hit, point);

            return closestHit;
        }

        private static void IsClosestHit(ref double closestDistance, ref RaycastHitDouble closestHit, RaycastHitDouble hit, Vector3Double point)
        {
            double distance = Vector3Double.Distance(point, hit.point);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestHit = hit;
            }
        }

        private static Dictionary<Guid, RaycastHitDouble> _bestGeoAstroObjectTerrainHit;
        public static RaycastHitDouble[] TerrainFiltered(RaycastHitDouble[] hits, Camera camera = null)
        {
            if (hits != null)
            {
                List<RaycastHitDouble> filteredHits = new List<RaycastHitDouble>();

                if (_bestGeoAstroObjectTerrainHit == null)
                    _bestGeoAstroObjectTerrainHit = new Dictionary<Guid, RaycastHitDouble>();

                foreach (RaycastHitDouble hit in hits)
                {
                    if (hit.transform != Disposable.NULL && hit.transform != Disposable.NULL && hit.transform.objectBase != Disposable.NULL && !hit.transform.objectBase.CameraIsMasked(camera))
                    {
                        if (hit.transform.objectBase is Grid2DMeshObjectBase)
                        {
                            Grid2DMeshObjectBase grid2DMeshObject = hit.transform.objectBase as Grid2DMeshObjectBase;

                            if (grid2DMeshObject.IsValidSphericalRatio())
                            {
                                if (grid2DMeshObject is TerrainGridMeshObject)
                                {
                                    Guid gridParentGeoAstroObjectGuid = grid2DMeshObject.parentGeoAstroObject.id;
                                    RaycastHitDouble bestTerrainHit;
                                    if (!_bestGeoAstroObjectTerrainHit.TryGetValue(gridParentGeoAstroObjectGuid, out bestTerrainHit) || grid2DMeshObject > (bestTerrainHit.transform.objectBase as Grid2DMeshObjectBase))
                                        _bestGeoAstroObjectTerrainHit[gridParentGeoAstroObjectGuid] = hit;
                                }
                                else
                                    filteredHits.Add(hit);
                            }
                        }
                        else
                            filteredHits.Add(hit);
                    }
                }

                foreach (RaycastHitDouble bestGeoAstroObjectTerrainHit in _bestGeoAstroObjectTerrainHit.Values)
                    filteredHits.Add(bestGeoAstroObjectTerrainHit);

                _bestGeoAstroObjectTerrainHit.Clear();

                return filteredHits.ToArray();
            }
            return null;
        }
    }

    /// <summary>
    /// 64 bit representation of rays.
    /// </summary>
    public class RayDouble
    {
        public Vector3Double origin;
        public Vector3Double direction;

        public RayDouble()
        {
        }

        public RayDouble(Ray ray)
        {
            origin = TransformDouble.AddOrigin(ray.origin);
            direction = ray.direction;
        }

        public RayDouble(Vector3Double origin, Vector3Double direction)
        {
            Init(origin, direction);
        }

        public void Init(Vector3Double origin, Vector3Double direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public override string ToString()
        {
            return origin + ", " + direction;
        }

        public Vector3Double GetPoint(double enter)
        {
            return origin + direction.normalized * enter;
        }

        public static implicit operator Ray(RayDouble ray)
        {
            return new Ray(TransformDouble.SubtractOrigin(ray.origin), ray.direction);
        }
    }

    public class RaycastHitDouble
    {
        public Vector3Double point;
        public TransformBase transform;
        public MeshRendererVisual meshRendererVisual;
        public int triangleIndex;

        public RaycastHitDouble(Vector3Double point, Transform transform, int triangleIndex)
        {
            this.point = point;
            this.meshRendererVisual = transform.gameObject.GetComponentInitialized<MeshRendererVisual>();
            if (meshRendererVisual != Disposable.NULL && meshRendererVisual.visualObject != Disposable.NULL && meshRendererVisual.visualObject.transform != Disposable.NULL)
                this.transform = meshRendererVisual.visualObject.transform;
            else
                this.transform = null;
            this.triangleIndex = triangleIndex;
        }

        public static implicit operator RaycastHitDouble(RaycastHit hit)
        {
            return new RaycastHitDouble(TransformDouble.AddOrigin(hit.point), hit.transform, hit.triangleIndex);
        }
    }
}
