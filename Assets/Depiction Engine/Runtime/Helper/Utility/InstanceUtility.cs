// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the creation of instances.
    /// </summary>
    public static class InstanceUtility
    {
        /// <summary>
        /// Create a <see cref="Camera"/> equipped with a <see cref="CameraController"/> and target.
        /// </summary>
        /// <param name="parent">The parent of the Transform.</param>
        /// <param name="initializationState"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns></returns>
        public static Camera CreateTargetCamera(Transform parent, InstanceManager.InitializationContext initializationState = InstanceManager.InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false)
        {
            Camera camera = null;

            InstanceManager instanceManager = InstanceManager.Instance();
            if (instanceManager != Disposable.NULL)
            {
                Object target = instanceManager.CreateInstance<VisualObject>(parent, json: "Target", initializingState: initializationState, setParentAndAlign: setParentAndAlign, moveToView: moveToView);
                target.CreateScript<GeoCoordinateController>(initializationState);
                target.CreateScript<TransformAnimator>(initializationState);

                camera = instanceManager.CreateInstance<Camera>(parent, json: "Camera", initializingState: initializationState);

                CameraController cameraController = camera.CreateScript<CameraController>(initializationState);
                cameraController.targetId = target.id;
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(cameraController, initializationState);
#endif

                camera.CreateScript<TargetControllerAnimator>(initializationState);
            }

            return camera;
        }

        /// <summary>
        /// Create a planet.
        /// </summary>
        /// <param name="parent">The parent of the Transform.</param>
        /// <param name="name">The name of the planet.</param>
        /// <param name="spherical">Display as a sphere (true) or flat (false)?</param>
        /// <param name="size">The size (radius in spherical mode or width in flat mode), in world units.</param>
        /// <param name="mass">Used to determine the amount of gravitational force to apply when <see cref="Object.useGravity"/> is enabled.</param>
        /// <param name="sizeMultiplier">A factor by which the grid size will be multiplied.</param>
        /// <param name="sphericalSubdivision">The minimum number of subdivisions the tile geometry will have when in spherical mode.</param>
        /// <param name="flatSubdivision">The minimum number of subdivisions the tile geometry will have when in flat mode.</param>
        /// <param name="minMaxZoom">A min and max clamping values for zoom.</param>
        /// <param name="colorTextureLoaderDatasourceId">The id of the datasource from which we will be loading the color texture.</param>
        /// <param name="colorTextureLoadEndpoint">The endpoint that will be used by the <see cref="RestDatasource"/> when loading the color texture.</param>
        /// <param name="colorTextureMinMaxZoom">A min and max clamping values for the color texture zoom.</param>
        /// <param name="additionalTextureLoaderDatasourceId">The id of the datasource from which we will be loading the additional texture.</param>
        /// <param name="additionalTextureLoadEndpoint">The endpoint that will be used by the <see cref="RestDatasource"/> when loading the additional texture.</param>
        /// <param name="additionalTextureMinMaxZoom">A min and max clamping values for the additional texture zoom.</param>
        /// <param name="elevationLoaderDatasourceId">The id of the datasource from which we will be loading the elevation.</param>
        /// <param name="elevationLoadEndpoint">The endpoint that will be used by the <see cref="RestDatasource"/> when loading the elevation.</param>
        /// <param name="elevationMinMaxZoom">A min and max clamping values for the elevation zoom.</param>
        /// <param name="elevationMultiplier">A Factor by which we multiply the elevation value to accentuate or reduce its magnitude.</param>
        /// <param name="elevationDataType">The type of the elevation we expect the loading operation to return.</param>
        /// <param name="xFlip">When enabled the elevation pixel values will be flipped horizontally.</param>
        /// <param name="yFlip">When enabled the elevation pixel values will be flipped vertically.</param>
        /// <param name="surfaceTypeTextureLoaderDatasourceId">The id of the datasource from which we will be loading the surface type texture.</param>
        /// <param name="surfaceTypeTextureLoadEndpoint">The endpoint that will be used by the <see cref="RestDatasource"/> when loading the surface type texture.</param>
        /// <param name="surfaceTypeTextureMinMaxZoom">A min and max clamping values for the surface type texture zoom.</param>
        /// <param name="initializingState"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <returns></returns>
        public static List<JsonMonoBehaviour> CreatePlanet(
            Transform parent,
            string name,
            bool spherical,
            double size,
            double mass,
            float sizeMultiplier,
            int sphericalSubdivision,
            int flatSubdivision,
            Vector2Int minMaxZoom,
            SerializableGuid colorTextureLoaderDatasourceId = new SerializableGuid(),
            string colorTextureLoadEndpoint = "",
            Vector2Int colorTextureMinMaxZoom = new Vector2Int(),
            SerializableGuid additionalTextureLoaderDatasourceId = new SerializableGuid(),
            string additionalTextureLoadEndpoint = "",
            Vector2Int additionalTextureMinMaxZoom = new Vector2Int(),
            SerializableGuid elevationLoaderDatasourceId = new SerializableGuid(),
            string elevationLoadEndpoint = "",
            Vector2Int elevationMinMaxZoom = new Vector2Int(),
            float elevationMultiplier = 1.0f,
            LoaderBase.DataType elevationDataType = LoaderBase.DataType.TexturePngJpg,
            bool xFlip = false,
            bool yFlip = false,
            SerializableGuid surfaceTypeTextureLoaderDatasourceId = default(SerializableGuid),
            string surfaceTypeTextureLoadEndpoint = "",
            Vector2Int surfaceTypeTextureMinMaxZoom = new Vector2Int(),
            InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically,
            bool setParentAndAlign = false,
            bool moveToView = false)
        {
            List<JsonMonoBehaviour> createdComponents = new List<JsonMonoBehaviour>();

            InstanceManager instanceManager = InstanceManager.Instance();

            //Add Planet 
            Planet planet = instanceManager.CreateInstance<Planet>(parent, name, initializingState: initializingState, setParentAndAlign: setParentAndAlign, moveToView: moveToView);
            planet.SetSpherical(spherical, false);
            if (spherical)
                planet.transform.localRotation = QuaternionDouble.Euler(-90.0d, 0.0d, 0.0d);
            createdComponents.Add(planet);
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(planet.transform, initializingState);
#endif

            planet.size = size;
            planet.mass = mass;
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(planet, initializingState);
#endif

            //Add Planet -> Color Texture Index2DLoader
            SerializableGuid colorTextureLoaderId = SerializableGuid.Empty;
            if (!string.IsNullOrEmpty(colorTextureLoadEndpoint))
            {
                SerializableGuid colorTextureFallbackValuesId = SerializableGuid.NewGuid();

                Index2DLoader colorTextureLoader = planet.CreateScript<Index2DLoader>(initializingState);
                colorTextureLoaderId = colorTextureLoader.id;
                colorTextureLoader.dataType = LoaderBase.DataType.TexturePngJpg;
                colorTextureLoader.fallbackValuesId = new List<SerializableGuid> { colorTextureFallbackValuesId };
                colorTextureLoader.minMaxZoom = colorTextureMinMaxZoom;
                colorTextureLoader.datasourceId = colorTextureLoaderDatasourceId;
                colorTextureLoader.loadEndpoint = colorTextureLoadEndpoint;
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(colorTextureLoader, initializingState);
#endif
                createdComponents.Add(colorTextureLoader);

                JSONObject colorTextureFallbackValuesJson = new JSONObject();
                colorTextureFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(colorTextureFallbackValuesId);
                FallbackValues colorTextureFallbackValues = planet.CreateFallbackValues<Texture>(colorTextureFallbackValuesJson, initializingState);
                createdComponents.Add(colorTextureFallbackValues);
            }

            //Add Planet -> Additional Texture Index2DLoader
            SerializableGuid additionalTextureLoaderId = SerializableGuid.Empty;
            if (!string.IsNullOrEmpty(additionalTextureLoadEndpoint))
            {
                SerializableGuid additionalTextureFallbackValuesId = SerializableGuid.NewGuid();

                Index2DLoader additionalTextureLoader = planet.CreateScript<Index2DLoader>(initializingState);
                additionalTextureLoaderId = additionalTextureLoader.id;
                additionalTextureLoader.dataType = LoaderBase.DataType.TexturePngJpg;
                additionalTextureLoader.fallbackValuesId = new List<SerializableGuid> { additionalTextureFallbackValuesId };
                additionalTextureLoader.minMaxZoom = additionalTextureMinMaxZoom;
                additionalTextureLoader.datasourceId = additionalTextureLoaderDatasourceId;
                additionalTextureLoader.loadEndpoint = additionalTextureLoadEndpoint;
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(additionalTextureLoader, initializingState);
#endif
                createdComponents.Add(additionalTextureLoader);

                JSONObject additionalTextureFallbackValuesJson = new JSONObject();
                additionalTextureFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(additionalTextureFallbackValuesId);
                FallbackValues additionalTextureFallbackValues = planet.CreateFallbackValues<Texture>(additionalTextureFallbackValuesJson, initializingState);
                createdComponents.Add(additionalTextureFallbackValues);
            }

            //Add Planet -> Elevation Index2DLoader
            SerializableGuid elevationLoaderId = SerializableGuid.Empty;
            if (!string.IsNullOrEmpty(elevationLoadEndpoint))
            {
                SerializableGuid elevationFallbackValuesId = SerializableGuid.NewGuid();

                Index2DLoader elevationLoader = planet.CreateScript<Index2DLoader>(initializingState);
                elevationLoaderId = elevationLoader.id;
                elevationLoader.dataType = elevationDataType;
                elevationLoader.fallbackValuesId = new List<SerializableGuid> { elevationFallbackValuesId };
                elevationLoader.minMaxZoom = elevationMinMaxZoom;
                elevationLoader.datasourceId = elevationLoaderDatasourceId;
                elevationLoader.loadEndpoint = elevationLoadEndpoint;
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(elevationLoader, initializingState);
#endif
                createdComponents.Add(elevationLoader);

                JSONObject elevationFallbackValuesJson = new JSONObject();
                elevationFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(elevationFallbackValuesId);
                FallbackValues elevationFallbackValues = planet.CreateFallbackValues<Elevation>(elevationFallbackValuesJson, initializingState);
                elevationFallbackValues.SetProperty(nameof(Elevation.elevationMultiplier), elevationMultiplier);
                elevationFallbackValues.SetProperty(nameof(Elevation.xFlip), xFlip);
                elevationFallbackValues.SetProperty(nameof(Elevation.yFlip), yFlip);
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(elevationFallbackValues, initializingState);
#endif
                createdComponents.Add(elevationFallbackValues);
            }

            //Add Planet -> Surface Type Texture Index2DLoader
            SerializableGuid surfaceTypeTextureLoaderId = SerializableGuid.Empty;
            if (!string.IsNullOrEmpty(surfaceTypeTextureLoadEndpoint))
            {
                SerializableGuid surfaceTypeTextureFallbackValuesId = SerializableGuid.NewGuid();

                Index2DLoader surfaceTypeTextureLoader = planet.CreateScript<Index2DLoader>(initializingState);
                surfaceTypeTextureLoaderId = surfaceTypeTextureLoader.id;
                surfaceTypeTextureLoader.dataType = LoaderBase.DataType.TexturePngJpg;
                surfaceTypeTextureLoader.fallbackValuesId = new List<SerializableGuid> { surfaceTypeTextureFallbackValuesId };
                surfaceTypeTextureLoader.minMaxZoom = surfaceTypeTextureMinMaxZoom;
                surfaceTypeTextureLoader.datasourceId = surfaceTypeTextureLoaderDatasourceId;
                surfaceTypeTextureLoader.loadEndpoint = surfaceTypeTextureLoadEndpoint;
                createdComponents.Add(surfaceTypeTextureLoader);
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCompleteObjectUndo(surfaceTypeTextureLoader, initializingState);
#endif

                JSONObject surfaceTypeTextureFallbackValuesJson = new JSONObject();
                surfaceTypeTextureFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(surfaceTypeTextureFallbackValuesId);
                FallbackValues surfaceTypeTextureFallbackValues = planet.CreateFallbackValues<Texture>(surfaceTypeTextureFallbackValuesJson, initializingState);
                createdComponents.Add(surfaceTypeTextureFallbackValues);
            }

            //Add TerrainRoot
            DatasourceRoot terrainRoot = instanceManager.CreateInstance<DatasourceRoot>(planet.gameObject.transform, json: "Terrain", initializingState: initializingState);
            createdComponents.Add(terrainRoot);

            //Add TerrainRoot -> TerrainGridMeshObject CameraGrid2DLoader
            SerializableGuid terrainGridMeshObjectFallbackValuesId = SerializableGuid.NewGuid();
            CameraGrid2DLoader cameraGrid2DTerrainLoader = terrainRoot.CreateScript<CameraGrid2DLoader>(initializingState);
            cameraGrid2DTerrainLoader.sizeMultiplier = sizeMultiplier;
            cameraGrid2DTerrainLoader.fallbackValuesId = new List<SerializableGuid> { terrainGridMeshObjectFallbackValuesId };
            cameraGrid2DTerrainLoader.minMaxZoom = minMaxZoom;
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(cameraGrid2DTerrainLoader, initializingState);
#endif
            createdComponents.Add(cameraGrid2DTerrainLoader);

            List<SerializableGuid> terrainGridMeshObjectAssetReferencesFallbackValuesId = new List<SerializableGuid>() { SerializableGuid.NewGuid(), SerializableGuid.NewGuid(), SerializableGuid.NewGuid(), SerializableGuid.NewGuid() };

            JSONObject terrainGridMeshObjectFallbackValuesJson = new JSONObject();
            terrainGridMeshObjectFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(terrainGridMeshObjectFallbackValuesId);
            FallbackValues terrainGridMeshObjectFallbackValues = terrainRoot.CreateFallbackValues<TerrainGridMeshObject>(terrainGridMeshObjectFallbackValuesJson, initializingState);
            terrainGridMeshObjectFallbackValues.SetProperty(nameof(Object.referencesId), terrainGridMeshObjectAssetReferencesFallbackValuesId);
            terrainGridMeshObjectFallbackValues.SetProperty(nameof(TerrainGridMeshObject.sphericalSubdivision), sphericalSubdivision);
            terrainGridMeshObjectFallbackValues.SetProperty(nameof(TerrainGridMeshObject.flatSubdivision), flatSubdivision);
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(terrainGridMeshObjectFallbackValues, initializingState);
#endif
            createdComponents.Add(terrainGridMeshObjectFallbackValues);

            JSONObject elevationAssetReferenceFallbackValuesJson = new JSONObject();
            elevationAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(terrainGridMeshObjectAssetReferencesFallbackValuesId[0]);
            FallbackValues elevationAssetReferenceFallbackValues = terrainRoot.CreateFallbackValues<AssetReference>(elevationAssetReferenceFallbackValuesJson, initializingState);
            elevationAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), elevationLoaderId);
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(elevationAssetReferenceFallbackValues, initializingState);
#endif
            createdComponents.Add(elevationAssetReferenceFallbackValues);

            JSONObject colorTextureAssetReferenceFallbackValuesJson = new JSONObject();
            colorTextureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(terrainGridMeshObjectAssetReferencesFallbackValuesId[1]);
            FallbackValues colorTextureAssetReferenceFallbackValues = terrainRoot.CreateFallbackValues<AssetReference>(colorTextureAssetReferenceFallbackValuesJson, initializingState);
            colorTextureAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), colorTextureLoaderId);
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(colorTextureAssetReferenceFallbackValues, initializingState);
#endif
            createdComponents.Add(colorTextureAssetReferenceFallbackValues);

            JSONObject additionalTextureAssetReferenceFallbackValuesJson = new JSONObject();
            additionalTextureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(terrainGridMeshObjectAssetReferencesFallbackValuesId[2]);
            FallbackValues additionalTextureAssetReferenceFallbackValues = terrainRoot.CreateFallbackValues<AssetReference>(additionalTextureAssetReferenceFallbackValuesJson, initializingState);
            additionalTextureAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), additionalTextureLoaderId);
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(additionalTextureAssetReferenceFallbackValues, initializingState);
#endif
            createdComponents.Add(additionalTextureAssetReferenceFallbackValues);

            JSONObject surfaceTypeTextureAssetReferenceFallbackValuesJson = new JSONObject();
            surfaceTypeTextureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(terrainGridMeshObjectAssetReferencesFallbackValuesId[3]);
            FallbackValues surfaceTypeTextureAssetReferenceFallbackValues = terrainRoot.CreateFallbackValues<AssetReference>(surfaceTypeTextureAssetReferenceFallbackValuesJson, initializingState);
            surfaceTypeTextureAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), surfaceTypeTextureLoaderId);
#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(surfaceTypeTextureAssetReferenceFallbackValues, initializingState);
#endif
            createdComponents.Add(surfaceTypeTextureAssetReferenceFallbackValues);

            return createdComponents;
        }
    }
}
