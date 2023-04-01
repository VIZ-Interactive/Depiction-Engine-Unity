// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public class Index2DLoadScope : LoadScope
    {
        /// <summary>
        /// Different types of URL parameters. <br/><br/>
		/// <b><see cref="ZoomXY"/>:</b> <br/>
        /// Tile index defined by Zoom{0} and X{1}, Y{2}. <br/><br/>
		/// <b><see cref="LatLon"/>:</b> <br/>
        /// GeoCoordinate defined by Latitude{0} and Longitude{1}. <br/><br/>
		/// <b><see cref="LatLonAlt"/>:</b> <br/>
        /// GeoCoordinate defined by Latitude{0}, Longitude{1} and Altitude{2}.<br/><br/>
        /// <b><see cref="GeoBoundaries"/>:</b> <br/>
        /// Boundaries defined by Longitude Min{0}, Longitude Max{1} and Latitude Min{2}, Latitude Max{3}.
        /// </summary>
        public enum URLParametersType
        {
            ZoomXY,
            LatLon,
            LatLonAlt,
            GeoBoundaries
        }

        [SerializeField]
        private Grid2DIndex _scopeGrid2DIndex;
        [SerializeField]
        private URLParametersType _indexUrlParamType;
        [SerializeField]
        private VisibleCameras _visibleCameras;

        public override void Recycle()
        {
            base.Recycle();

            _indexUrlParamType = default;

            _visibleCameras = default;
        }

        public Index2DLoadScope Init(LoaderBase loader, Vector2Int index, Vector2Int dimensions, URLParametersType indexUrlParamType)
        {
            Init(loader);

            this.scopeGrid2DIndex = new Grid2DIndex(index, dimensions);
            this.indexUrlParamType = indexUrlParamType;

            return this;
        }

        public Vector2Int scopeIndex
        {
            get => _scopeGrid2DIndex.index; 
        }

        public Vector2Int scopeDimensions
        {
            get => _scopeGrid2DIndex.dimensions; 
        }

        public Grid2DIndex scopeGrid2DIndex
        {
            get => _scopeGrid2DIndex; 
            private set => _scopeGrid2DIndex = value;
        }

        public override object scopeKey
        {
            get => scopeGrid2DIndex;
        }

        public URLParametersType indexUrlParamType
        {
            get { return _indexUrlParamType; }
            private set { _indexUrlParamType = value; }
        }

        public int GetZoom()
        {
            return MathPlus.GetZoomFromGrid2DDimensions(scopeDimensions);
        }

        public override JSONObject GetLoadScopeFallbackValuesJson()
        {
            JSONObject loadScopeFallbackValuesJson = base.GetLoadScopeFallbackValuesJson();

            if (loader is Index2DLoaderBase)
            {
                Index2DLoaderBase index2DLoader = loader as Index2DLoaderBase;

                //Assets 
                loadScopeFallbackValuesJson[nameof(AssetBase.gridIndexType)] = JsonUtility.ToJson(AssetBase.GridIndexType.Index2D);
                loadScopeFallbackValuesJson[nameof(AssetBase.grid2DDimensions)] = JsonUtility.ToJson(scopeDimensions);
                loadScopeFallbackValuesJson[nameof(AssetBase.grid2DIndex)] = JsonUtility.ToJson(scopeIndex);

                //Objects
                loadScopeFallbackValuesJson[nameof(Object.transform)][nameof(TransformDouble.geoCoordinate)] = JsonUtility.ToJson(MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(scopeIndex.x + 0.5d, scopeIndex.y + 0.5d), scopeDimensions));

                //MeshObjects
                if (index2DLoader is CameraGrid2DLoader)
                {
                    CameraGrid2DLoader cameraGrid2DLoader = index2DLoader as CameraGrid2DLoader;
                    int zoom = GetZoom();
                    if (zoom < cameraGrid2DLoader.collidersRange.x || zoom > cameraGrid2DLoader.collidersRange.y)
                        loadScopeFallbackValuesJson[nameof(MeshObjectBase.useCollider)] = false;
                }
            }

            return loadScopeFallbackValuesJson;
        }

        public override object[] GetURLParams()
        {
            object[] urlParams = null;

            if (_indexUrlParamType == URLParametersType.ZoomXY)
            {
                urlParams = new object[3];
                urlParams[0] = GetZoom();
                urlParams[1] = scopeIndex.x;
                urlParams[2] = scopeIndex.y;
            }
            else if (_indexUrlParamType == URLParametersType.GeoBoundaries)
            {
                urlParams = new object[4];
                GeoCoordinate2Double geoBoundariesMin = MathPlus.GetGeoCoordinate2FromIndex(scopeIndex + new Vector2Int(0, 1), scopeDimensions);
                GeoCoordinate2Double geoBoundariesMax = MathPlus.GetGeoCoordinate2FromIndex(scopeIndex + new Vector2Int(1, 0), scopeDimensions);
                urlParams[0] = geoBoundariesMin.longitude;
                urlParams[1] = geoBoundariesMax.longitude;
                urlParams[2] = geoBoundariesMin.latitude;
                urlParams[3] = geoBoundariesMax.latitude;
            }
            else if (_indexUrlParamType == URLParametersType.LatLon || _indexUrlParamType == URLParametersType.LatLonAlt)
            {
                GeoCoordinate3Double geoCoord = MathPlus.GetGeoCoordinate3FromIndex(scopeIndex, scopeDimensions);

                if (_indexUrlParamType == URLParametersType.LatLonAlt)
                {
                    urlParams = new object[3];
                    urlParams[0] = geoCoord.latitude;
                    urlParams[1] = geoCoord.longitude;
                    urlParams[2] = geoCoord.altitude;
                }
                else
                {
                    urlParams = new object[2];
                    urlParams[0] = geoCoord.latitude;
                    urlParams[1] = geoCoord.longitude;
                }
            }

            return urlParams;
        }

        public override bool IsInScope(IPersistent persistent)
        {
            if (persistent is IGrid2DIndex)
            {
                IGrid2DIndex grid2DObject = persistent as IGrid2DIndex;
                return grid2DObject.IsGridIndexValid() && grid2DObject.grid2DDimensions == scopeDimensions && grid2DObject.grid2DIndex == scopeIndex;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(Vector2Int dimensions, Vector2Int index)
        {
            IEnumerable<int> hashCodes = new int[] { index.x, index.y, dimensions.x, dimensions.y };
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            int i = 0;
            foreach (var hashCode in hashCodes)
            {
                if (i % 2 == 0)
                    hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
                else
                    hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

                ++i;
            }
            return hash1 + (hash2 * 1566083941);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return GetHashCode(scopeDimensions, scopeIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string PropertiesToString()
        {
            return "(Zoom:" + GetZoom() + ", XYTilesRatio: " + MathPlus.GetXYTileRatioFromGrid2DDimensions(scopeDimensions) + ", X: " + scopeIndex.x + ", Y: " + scopeIndex.y + ")";
        }

        public VisibleCameras visibleCameras
        {
            get { return _visibleCameras; }
            set
            {
                if (_visibleCameras == value)
                    return;

                _visibleCameras = value;

                IterateOverPersistents((i, persistent) =>
                {
                    UpdateObjectGridProperties(persistent, _visibleCameras);

                    return true;
                });
            }
        }

        public override bool AddPersistent(IPersistent persistent)
        {
            if (base.AddPersistent(persistent))
            {
                UpdateObjectGridProperties(persistent, visibleCameras);

                return true;
            }

            return false;
        }

        public override bool RemovePersistent(IPersistent persistent, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.RemovePersistent(persistent, disposeContext))
            {
                UpdateObjectGridProperties(persistent);

                return true; 
            }

            return false;
        }

        private void UpdateObjectGridProperties(IPersistent persistent, VisibleCameras visibleCameras = null)
        {
            if (!Disposable.IsDisposed(persistent) && persistent is Object)
                (persistent as Object).SetGridProperties(GetInstanceID(), visibleCameras);
        }
    }
}
