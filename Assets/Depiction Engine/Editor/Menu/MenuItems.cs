// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class MenuItems
    {
        private const string MAPBOX_KEY = "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA";

        //Tip: If context menu order changes do not immediately show up in the editor, change the MenuItem itemName for changes to take effect

        //Depiction Engine Object
        [MenuItem("GameObject/Depiction Engine/Object", false, 12)]
        private static void CreateObject(MenuCommand menuCommand) 
        {
            string name = nameof(Object);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<Object>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/VisualObject", false, 13)]
        private static void CreateVisualObject(MenuCommand menuCommand)
        {
            string name = nameof(VisualObject);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<VisualObject>(GetContextTransform(menuCommand), name);
        }

        //Depiction Engine Light
        [MenuItem("GameObject/Depiction Engine/Light/Star", false, 24)]
        private static void CreateStar(MenuCommand menuCommand)
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL && instanceManager.StarExists())
            {
                Debug.LogError(InstanceManager.GetMultipleInstanceErrorMsg(typeof(Star).Name));
                return;
            }

            string name = nameof(Star);
            UndoManager.CreateNewGroup("Create " + name);

            CreateStar(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/Light/Reflection Probe", false, 25)]
        private static void CreateReflectionProbe(MenuCommand menuCommand)
        {
            string name = nameof(ReflectionProbe);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<ReflectionProbe>(GetContextTransform(menuCommand), name);
        }

        //Depiction Engine Astro
        [MenuItem("GameObject/Depiction Engine/Astro/Star System", false, 36)]
        private static void CreateStarSystem(MenuCommand menuCommand)
        {
            string name = nameof(StarSystem);
            UndoManager.CreateNewGroup("Create " + name);

            StarSystem starSystem = CreateObject<StarSystem>(GetContextTransform(menuCommand), name);
            CreateScript<StarSystemAnimator>(starSystem);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Planet (Empty)", false, 37)]
        private static void CreatePlanet(MenuCommand menuCommand)
        {
            bool spherical = true;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreatePlanet(GetContextTransform(menuCommand), "Planet", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Earth (Realistic)", false, 38)]
        private static void CreatePlanetEarthRealistic(MenuCommand menuCommand)
        {
            bool spherical = true;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreateEarthRealistic(GetContextTransform(menuCommand), "Earth", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }


        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Earth (Basic)", false, 38)]
        private static void CreatePlanetEarthBasic(MenuCommand menuCommand)
        {
            bool spherical = true;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreateEarthBasic(GetContextTransform(menuCommand), "Earth", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Moon (Realistic)", false, 39)]
        private static void CreatePlanetMoonRealistic(MenuCommand menuCommand)
        {
            bool spherical = true;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreateMoonRealistic(GetContextTransform(menuCommand), "Moon", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Map (Empty)", false, 40)]
        private static void CreateMap(MenuCommand menuCommand)
        {
            bool spherical = false;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreatePlanet(GetContextTransform(menuCommand), "Map", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Earth (Realistic)", false, 41)]
        private static void CreateMapEarthRealistic(MenuCommand menuCommand)
        {
            bool spherical = false;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreateEarthRealistic(GetContextTransform(menuCommand), "Earth", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Earth (Basic)", false, 41)]
        private static void CreateMapEarthBasic(MenuCommand menuCommand)
        {
            bool spherical = false;
            InitializeSceneCameraSkybox(!spherical);

            List<JsonMonoBehaviour> createdComponents = CreateEarthBasic(GetContextTransform(menuCommand), "Earth", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Moon (Realistic)", false, 42)]
        private static void CreateMapMoonRealistic(MenuCommand menuCommand)
        {
            bool spherical = false;
            InitializeSceneCameraSkybox(!spherical);

            List <JsonMonoBehaviour> createdComponents = CreateMoonRealistic(GetContextTransform(menuCommand), "Moon", spherical);
            GeoAstroObject geoAstroObject = createdComponents[0] as Planet;
            SetAlignViewToGeoAstroObject(geoAstroObject);
        }

        private static void InitializeSceneCameraSkybox(bool atmosphere = false)
        {
            if (atmosphere && SceneManager.Instance(false) == Disposable.NULL)
                CameraManager.Instance().skyboxMaterialPath = RenderingManager.MATERIAL_BASE_PATH + (atmosphere ? "Skybox/Atmosphere-Skybox" : "Skybox/Star-Skybox");
        }

        //Depiction Engine Camera
        [MenuItem("GameObject/Depiction Engine/Camera/Camera", false, 53)]
        private static void CreateCamera(MenuCommand menuCommand)
        {
            string name = "Camera";
            UndoManager.CreateNewGroup("Create " + name);

            Camera camera = CreateObject<Camera>(GetContextTransform(menuCommand), name);

            InitMainCamera(camera);

            camera.PreventCameraStackBug();
        }

        [MenuItem("GameObject/Depiction Engine/Camera/Target Camera", false, 54)]
        private static void CreateTargetCamera(MenuCommand menuCommand)
        {
            UndoManager.CreateNewGroup("Create Camera Target");

            Camera camera = InstanceUtility.CreateTargetCamera(GetContextTransform(menuCommand), InstanceManager.InitializationContext.Editor, true, true);

            InitMainCamera(camera);

            SelectObject((camera.controller as CameraController).target);

            camera.PreventCameraStackBug();
        }

        private static void InitMainCamera(Camera camera)
        {
            if (Camera.main == null)
            {
                camera.tag = "MainCamera";
                UndoManager.RegisterCompleteObjectUndo(camera);
            }
        }

        //Depiction Engine UI
        [MenuItem("GameObject/Depiction Engine/UI/label", false, 65)]
        private static void CreateLabel(MenuCommand menuCommand) 
        {
            string name = "Label";
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<Label>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/UI/Marker", false, 66)]
        private static void CreateMarker(MenuCommand menuCommand) 
        {
            string name = "Marker";
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<Marker>(GetContextTransform(menuCommand), name);
        }

        //Depiction Engine Datasource
        [MenuItem("GameObject/Depiction Engine/DatasourceRoot", false, 77)]
        private static void CreateDatasourceRoot(MenuCommand menuCommand)
        {
            string name = nameof(DatasourceRoot);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<DatasourceRoot>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/Datasource/RestDatasource", false, 78)]
        private static void CreateRestDatasource(MenuCommand menuCommand)
        {
            string name = "RestDatasource";
            UndoManager.CreateNewGroup("Create " + name);

            Object datasourceObject = CreateObject<Object>(GetContextTransform(menuCommand), name);
            CreateScript<RestDatasource>(datasourceObject);
        }

        [MenuItem("GameObject/Depiction Engine/Datasource/FileSystemDatasource", false, 79)]
        private static void CreateFileSystemDatasource(MenuCommand menuCommand)
        {
            string name = "FileSystemDatasource";
            UndoManager.CreateNewGroup("Create " + name);

            Object datasourceObject = CreateObject<Object>(GetContextTransform(menuCommand), name);
            CreateScript<FileSystemDatasource>(datasourceObject);
        }

        private static List<JsonMonoBehaviour> CreatePlanet(Transform parent, string name, bool spherical)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            return CreateEmptyPlanet(
                parent, 
                name, 
                spherical,
                GeoAstroObject.DEFAULT_SIZE,
                AstroObject.DEFAULT_MASS,
                2.0f,
                2,
                2,
                new Vector2Int(0, 19),
                
                SerializableGuid.Empty, 
                "my/terrain/tile/service/endpoint/{0}/{1}/{2}",
                new Vector2Int(0, 19),
                
                SerializableGuid.Empty,
                null,
                Vector2Int.zero,

                SerializableGuid.Empty, 
                "my/elevation/tile/service/endpoint/{0}/{1}/{2}",
                new Vector2Int(0, 19));
        }

        private static List<JsonMonoBehaviour> CreateEarthBasic(Transform parent, string name, bool spherical)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            //Add Earth
            DatasourceBase mapboxDatasource = GetRestDatasource("https://api.mapbox.com/");
            List<JsonMonoBehaviour> createdComponents = CreateEmptyPlanet(
                parent,
                name,
                spherical,
                GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Earth),
                GeoAstroObject.GetPlanetMass(AstroObject.PlanetType.Earth),
                2.0f,
                1,
                1,
                new Vector2Int(0, 17),

                mapboxDatasource.id,
                "styles/v1/mapbox/streets-v11/tiles/{0}/{1}/{2}?access_token=" + MAPBOX_KEY,
                new Vector2Int(0, 30));

            Planet earth = createdComponents[0] as Planet;

            CameraGrid2DLoader terrainCameraGrid2DLoader = createdComponents[4] as CameraGrid2DLoader;
            UndoManager.RecordObject(terrainCameraGrid2DLoader);
            terrainCameraGrid2DLoader.cascades = new Vector2Int(0, 4);

            FallbackValues terrainFallbackValues = createdComponents[5] as FallbackValues;
            UndoManager.RecordObject(terrainFallbackValues);
            if (!spherical)
                terrainFallbackValues.SetProperty(nameof(TerrainGridMeshObject.subdivisionZoomFactor), 1.0f);
            terrainFallbackValues.SetProperty(nameof(TerrainGridMeshObject.edgeDepth), 0.0f);
            terrainFallbackValues.SetProperty(nameof(TerrainGridMeshObject.normalsType), TerrainGridMeshObject.NormalsType.SurfaceUp);

            //Add Earth => BuildingFeature Index2DLoader
            SerializableGuid buildingFeatureFallbackValuesId = SerializableGuid.NewGuid();
            DatasourceBase buildingFeatureDatasource = GetRestDatasource(
                "https://a-data.3dbuildings.com/",
                "https://b-data.3dbuildings.com/",
                "https://c-data.3dbuildings.com/",
                "https://d-data.3dbuildings.com/");
            Index2DLoader buildingFeatureDataLoader = CreateScript<Index2DLoader>(earth);
            buildingFeatureDataLoader.dataType = LoaderBase.DataType.Json;
            buildingFeatureDataLoader.fallbackValuesId = new List<SerializableGuid> { buildingFeatureFallbackValuesId };
            buildingFeatureDataLoader.datasourceId = buildingFeatureDatasource.id;
            buildingFeatureDataLoader.loadEndpoint = "tile/{0}/{1}/{2}.json?token=dixw8kmb";
            UndoManager.RegisterCompleteObjectUndo(buildingFeatureDataLoader);
            createdComponents.Add(buildingFeatureDataLoader);

            JSONObject buildingFeatureFallbackValuesJson = new JSONObject();
            buildingFeatureFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingFeatureFallbackValuesId);
            FallbackValues buildingFeatureFallbackValues = CreateFallbackValues<BuildingFeature>(earth, buildingFeatureFallbackValuesJson);
            createdComponents.Add(buildingFeatureFallbackValues);

            //Add Earth -> BuildingsRoot
            SerializableGuid buildingMeshObjectFallbackValuesId = SerializableGuid.NewGuid();
            DatasourceRoot buildingsRoot = CreateObject<DatasourceRoot>(earth.gameObject.transform, "Buildings", false, false, false);
            createdComponents.Add(buildingsRoot);

            //Add BuildingsRoot -> BuildingsMeshObject CameraGrid2DLoader
            CameraGrid2DLoader buildingMeshObjectLoader = CreateScript<CameraGrid2DLoader>(buildingsRoot);
            buildingMeshObjectLoader.fallbackValuesId = new List<SerializableGuid> { buildingMeshObjectFallbackValuesId };
            buildingMeshObjectLoader.minMaxZoom = new Vector2Int(14, 14);
            buildingMeshObjectLoader.cascades = Vector2Int.zero;
            buildingMeshObjectLoader.sizeMultiplier = 2.0f;
            UndoManager.RegisterCompleteObjectUndo(buildingMeshObjectLoader);
            createdComponents.Add(buildingMeshObjectLoader);

            List<SerializableGuid> buildingGridMeshObjectAssetReferencesFallbackValuesId = new List<SerializableGuid>() { SerializableGuid.NewGuid(), SerializableGuid.NewGuid() };

            JSONObject buildingGridMeshObjectFallbackValuesJson = new JSONObject();
            buildingGridMeshObjectFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingMeshObjectFallbackValuesId);
            FallbackValues buildingGridMeshObjectFallbackValues = CreateFallbackValues<BuildingGridMeshObject>(buildingsRoot, buildingGridMeshObjectFallbackValuesJson);
            buildingGridMeshObjectFallbackValues.SetProperty(nameof(Object.referencesId), buildingGridMeshObjectAssetReferencesFallbackValuesId);
            buildingGridMeshObjectFallbackValues.SetProperty(nameof(BuildingGridMeshObject.color), Color.white);
            UndoManager.RegisterCompleteObjectUndo(buildingGridMeshObjectFallbackValues);
            createdComponents.Add(buildingGridMeshObjectFallbackValues);

            //Add BuildingsGridMeshObject CameraGrid2DLoader -> Asset References
            JSONObject elevationAssetReferenceFallbackValuesJson = new JSONObject();
            elevationAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingGridMeshObjectAssetReferencesFallbackValuesId[0]);
            FallbackValues elevationAssetReferenceFallbackValues = buildingsRoot.CreateFallbackValues<AssetReference>(elevationAssetReferenceFallbackValuesJson);
            elevationAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), SerializableGuid.Empty);
            UndoManager.RegisterCompleteObjectUndo(elevationAssetReferenceFallbackValues);
            createdComponents.Add(elevationAssetReferenceFallbackValues);

            JSONObject buildingFeatureAssetReferenceFallbackValuesJson = new JSONObject();
            buildingFeatureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingGridMeshObjectAssetReferencesFallbackValuesId[1]);
            FallbackValues buildingFeatureAssetReferenceFallbackValues = buildingsRoot.CreateFallbackValues<AssetReference>(buildingFeatureAssetReferenceFallbackValuesJson);
            buildingFeatureAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), buildingFeatureDataLoader.id);
            UndoManager.RegisterCompleteObjectUndo(buildingFeatureAssetReferenceFallbackValues);
            createdComponents.Add(buildingFeatureAssetReferenceFallbackValues);

            return createdComponents;
        }

        private static List<JsonMonoBehaviour> CreateEarthRealistic(Transform parent, string name, bool spherical)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            //Add Earth
            DatasourceBase mapboxDatasource = GetRestDatasource("https://api.mapbox.com/");
            List<JsonMonoBehaviour> createdComponents = CreateEmptyPlanet(
                parent,
                name,
                spherical,
                GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Earth),
                GeoAstroObject.GetPlanetMass(AstroObject.PlanetType.Earth),
                2.0f,
                2,
                2,
                new Vector2Int(0, 19),

                mapboxDatasource.id,
                "v4/mapbox.satellite/{0}/{1}/{2}@2x.jpg90?access_token=" + MAPBOX_KEY,
                new Vector2Int(0, 30),

                SerializableGuid.Empty,
                null,
                Vector2Int.zero,

                mapboxDatasource.id,
                //"raster/v1/mapbox.mapbox-terrain-dem-v1/{0}/{1}/{2}.webp?sku=101WQxhVS07ft&access_token=" + MAPBOX_KEY,
                "v4/mapbox.terrain-rgb/{0}/{1}/{2}.pngraw?access_token=" + MAPBOX_KEY,
                new Vector2Int(0, 13),
                1.0f,
                false,
                false,
                //DataType.ElevationMapboxTerrainRGBWebP,
                LoaderBase.DataType.ElevationMapboxTerrainRGBPngRaw,

                mapboxDatasource.id,
                "styles/v1/mapbox/streets-v11/tiles/{0}/{1}/{2}?access_token=" + MAPBOX_KEY,
                new Vector2Int(0, 14));

            Planet earth = createdComponents[0] as Planet;
            Index2DLoader colorTextureLoader = createdComponents[1] as Index2DLoader;
            Index2DLoader elevationLoader = createdComponents[3] as Index2DLoader;

            //Add Earth => BuildingFeature Index2DLoader
            SerializableGuid buildingFeatureFallbackValuesId = SerializableGuid.NewGuid();
            DatasourceBase buildingFeatureDatasource = GetRestDatasource(
                "https://a-data.3dbuildings.com/", 
                "https://b-data.3dbuildings.com/", 
                "https://c-data.3dbuildings.com/", 
                "https://d-data.3dbuildings.com/");
            Index2DLoader buildingFeatureDataLoader = CreateScript<Index2DLoader>(earth);
            buildingFeatureDataLoader.dataType = LoaderBase.DataType.Json;
            buildingFeatureDataLoader.fallbackValuesId = new List<SerializableGuid> { buildingFeatureFallbackValuesId };
            buildingFeatureDataLoader.datasourceId = buildingFeatureDatasource.id;
            buildingFeatureDataLoader.loadEndpoint = "tile/{0}/{1}/{2}.json?token=dixw8kmb";
            UndoManager.RegisterCompleteObjectUndo(buildingFeatureDataLoader);
            createdComponents.Add(buildingFeatureDataLoader);

            JSONObject buildingFeatureFallbackValuesJson = new JSONObject();
            buildingFeatureFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingFeatureFallbackValuesId);
            FallbackValues buildingFeatureFallbackValues = CreateFallbackValues<BuildingFeature>(earth, buildingFeatureFallbackValuesJson);
            createdComponents.Add(buildingFeatureFallbackValues);

            //Add Earth -> BuildingsRoot
            SerializableGuid buildingMeshObjectFallbackValuesId = SerializableGuid.NewGuid();
            DatasourceRoot buildingsRoot = CreateObject<DatasourceRoot>(earth.gameObject.transform, "Buildings", false, false, false);
            createdComponents.Add(buildingsRoot);

            //Add BuildingsRoot -> BuildingsMeshObject CameraGrid2DLoader
            CameraGrid2DLoader buildingMeshObjectLoader = CreateScript<CameraGrid2DLoader>(buildingsRoot);
            buildingMeshObjectLoader.fallbackValuesId = new List<SerializableGuid> { buildingMeshObjectFallbackValuesId };
            buildingMeshObjectLoader.minMaxZoom = new Vector2Int(14, 14);
            buildingMeshObjectLoader.cascades = Vector2Int.zero;
            buildingMeshObjectLoader.sizeMultiplier = 5.0f;
            UndoManager.RegisterCompleteObjectUndo(buildingMeshObjectLoader);
            createdComponents.Add(buildingMeshObjectLoader);

            List<SerializableGuid> buildingGridMeshObjectAssetReferencesFallbackValuesId = new List<SerializableGuid>() { SerializableGuid.NewGuid(), SerializableGuid.NewGuid(), SerializableGuid.NewGuid(), SerializableGuid.NewGuid() };

            JSONObject buildingGridMeshObjectFallbackValuesJson = new JSONObject();
            buildingGridMeshObjectFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingMeshObjectFallbackValuesId);
            FallbackValues buildingGridMeshObjectFallbackValues = CreateFallbackValues<BuildingGridMeshObject>(buildingsRoot, buildingGridMeshObjectFallbackValuesJson);
            buildingGridMeshObjectFallbackValues.SetProperty(nameof(Object.referencesId), buildingGridMeshObjectAssetReferencesFallbackValuesId);
            UndoManager.RegisterCompleteObjectUndo(buildingGridMeshObjectFallbackValues);
            createdComponents.Add(buildingGridMeshObjectFallbackValues);

            //Add BuildingsGridMeshObject CameraGrid2DLoader -> Asset References
            JSONObject elevationAssetReferenceFallbackValuesJson = new JSONObject();
            elevationAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingGridMeshObjectAssetReferencesFallbackValuesId[0]);
            FallbackValues elevationAssetReferenceFallbackValues = buildingsRoot.CreateFallbackValues<AssetReference>(elevationAssetReferenceFallbackValuesJson);
            elevationAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), elevationLoader.id);
            UndoManager.RegisterCompleteObjectUndo(elevationAssetReferenceFallbackValues);
            createdComponents.Add(elevationAssetReferenceFallbackValues);

            JSONObject buildingFeatureAssetReferenceFallbackValuesJson = new JSONObject();
            buildingFeatureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingGridMeshObjectAssetReferencesFallbackValuesId[1]);
            FallbackValues buildingFeatureAssetReferenceFallbackValues = buildingsRoot.CreateFallbackValues<AssetReference>(buildingFeatureAssetReferenceFallbackValuesJson);
            buildingFeatureAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), buildingFeatureDataLoader.id);
            UndoManager.RegisterCompleteObjectUndo(buildingFeatureAssetReferenceFallbackValues);
            createdComponents.Add(buildingFeatureAssetReferenceFallbackValues);

            JSONObject colorTextureAssetReferenceFallbackValuesJson = new JSONObject();
            colorTextureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingGridMeshObjectAssetReferencesFallbackValuesId[2]);
            FallbackValues colorTextureAssetReferenceFallbackValues = buildingsRoot.CreateFallbackValues<AssetReference>(colorTextureAssetReferenceFallbackValuesJson);
            colorTextureAssetReferenceFallbackValues.SetProperty(nameof(AssetReference.loaderId), colorTextureLoader.id);
            UndoManager.RegisterCompleteObjectUndo(colorTextureAssetReferenceFallbackValues);
            createdComponents.Add(colorTextureAssetReferenceFallbackValues);

            JSONObject additionalTextureAssetReferenceFallbackValuesJson = new JSONObject();
            additionalTextureAssetReferenceFallbackValuesJson[nameof(IPersistent.id)] = JsonUtility.ToJson(buildingGridMeshObjectAssetReferencesFallbackValuesId[3]);
            FallbackValues additionalTextureAssetReferenceFallbackValues = buildingsRoot.CreateFallbackValues<AssetReference>(additionalTextureAssetReferenceFallbackValuesJson);
            createdComponents.Add(additionalTextureAssetReferenceFallbackValues);

            //Add Earth -> Reflection
            createdComponents.Add(CreateScript<TerrainSurfaceReflectionEffect>(earth));

            //Add Earth -> Atmosphere
            createdComponents.Add(CreateScript<AtmosphereEffect>(earth));

            return createdComponents;
        }

        private static List<JsonMonoBehaviour> CreateMoonRealistic(Transform parent, string name, bool spherical)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            RestDatasource arcGISDatasource = GetRestDatasource("https://tiles.arcgis.com/");
            return CreateEmptyPlanet(
                parent, 
                name, 
                spherical,
                GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Moon),
                GeoAstroObject.GetPlanetMass(AstroObject.PlanetType.Moon),
                3.0f,
                1,
                1,
                new Vector2Int(0, 7),

                arcGISDatasource.id,
                "tiles/WQ9KVmV6xGGMnCiQ/arcgis/rest/services/Moon_Basemap_Tile0to9/MapServer/tile/{0}/{2}/{1}",
                new Vector2Int(0, 7),
                
                SerializableGuid.Empty,
                null,
                Vector2Int.zero,

                arcGISDatasource.id, 
                "tiles/WQ9KVmV6xGGMnCiQ/arcgis/rest/services/Moon_Elevation_Surface/ImageServer/tile/{0}/{2}/{1}?blankTile=false",
                new Vector2Int(0, 7),
                0.2f,
                false,
                false,
                LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression);
        }

        private static Star CreateStarIfMissing(Transform parent)
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL && instanceManager.StarExists())
                return null;

            Star star = CreateStar(parent, "Star", false);
            star.SetPlanetPreset(AstroObject.PlanetType.Sun);
            star.transform.localPosition = new Vector3Double(-100000000000.0d, 100000000000.0d, 0.0d);
                
            UndoManager.RegisterCompleteObjectUndo(star.transform);

            return star;
        }

        private static Star CreateStar(Transform parent, string name, bool registerCompleteUndo = true)
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL && instanceManager.StarExists())
            {
                Debug.LogError(InstanceManager.GetMultipleInstanceErrorMsg(typeof(Star).Name));
                return null;
            }

            Star star = CreateObject<Star>(parent, name);
            star.transform.localRotation = QuaternionDouble.Euler(-90.0d, 0.0d, 0.0d);

            if (registerCompleteUndo)
                UndoManager.RegisterCompleteObjectUndo(star.transform);

            return star;
        }

        private static Transform GetContextTransform(MenuCommand menuCommand)
        {
            return menuCommand.context != null ? (menuCommand.context as GameObject).transform : null;
        }

        private static List<JsonMonoBehaviour> CreateEmptyPlanet(Transform parent, string name, bool spherical, double size, double mass, float sizeMultiplier, int sphericalSubdivision, int flatSubdivision, Vector2Int minMaxZoom, SerializableGuid colorTextureLoaderDatasourceId = new SerializableGuid(), string colorTextureLoadEndpoint = "", Vector2Int colorTextureMinMaxZoom = new Vector2Int(), SerializableGuid additionalTextureLoaderDatasourceId = new SerializableGuid(), string additionalTextureLoadEndpoint = "", Vector2Int additionalTextureMinMaxZoom = new Vector2Int(), SerializableGuid elevationLoaderDatasourceId = new SerializableGuid(), string elevationLoadEndpoint = "", Vector2Int elevationMinMaxZoom = new Vector2Int(), float elevationMultiplier = 1.0f, bool xFlip = false, bool yFlip = false, LoaderBase.DataType elevationDataType = LoaderBase.DataType.ElevationMapboxTerrainRGBPngRaw, SerializableGuid surfaceTypeTextureLoaderDatasourceId = new SerializableGuid(), string surfaceTypeTextureLoadEndpoint = "", Vector2Int surfaceTypeTextureMinMaxZoom = new Vector2Int(), bool setParentAndAlign = true, bool moveToView = true, bool selectGameObject = true)
        {
            List<JsonMonoBehaviour> createdComponents = InstanceUtility.CreatePlanet(
                parent, 
                name, 
                spherical,
                size,
                mass,
                sizeMultiplier,
                sphericalSubdivision,
                flatSubdivision,
                minMaxZoom, 

                colorTextureLoaderDatasourceId, 
                colorTextureLoadEndpoint,
                colorTextureMinMaxZoom,

                additionalTextureLoaderDatasourceId,
                additionalTextureLoadEndpoint,
                additionalTextureMinMaxZoom,

                elevationLoaderDatasourceId, 
                elevationLoadEndpoint,
                elevationMinMaxZoom,
                elevationMultiplier,
                elevationDataType,
                xFlip,
                yFlip,

                surfaceTypeTextureLoaderDatasourceId,
                surfaceTypeTextureLoadEndpoint,
                surfaceTypeTextureMinMaxZoom,

                InstanceManager.InitializationContext.Editor, 
                setParentAndAlign, 
                moveToView);
            Planet planet = createdComponents[0] as Planet;

            if (selectGameObject)
                SelectObject(planet);

            return createdComponents;
        }

        private static T CreateObject<T>(Transform parent, string name = "", bool setParentAndAlign = true, bool moveToView = true, bool selectGameObject = true) where T : Object
        {
            T objectBase = InstanceManager.Instance().CreateInstance<T>(parent, name, initializingState: InstanceManager.InitializationContext.Editor, setParentAndAlign: setParentAndAlign, moveToView: moveToView);

            if (selectGameObject)
                SelectObject(objectBase);

            return objectBase;
        }

        private static FallbackValues CreateFallbackValues<T>(Object objectBase, JSONObject json = null)
        {
            return objectBase.CreateFallbackValues<T>(json, InstanceManager.InitializationContext.Editor);
        }

        private static T CreateScript<T>(Object objectBase, JSONObject json = null) where T : Script
        {
            return objectBase.CreateScript<T>(json, InstanceManager.InitializationContext.Editor);
        }

        private static void SelectObject(Object objectBase)
        {
            if (objectBase != Disposable.NULL)
                Selection.activeTransform = objectBase.transform;
        }

        private const string ALIGN_VIEW_TO_SELECTED_GEOASTROOBJECT_MENU_ITEM = "GameObject/Align View to Selected GeoAstroObject";
        [MenuItem(ALIGN_VIEW_TO_SELECTED_GEOASTROOBJECT_MENU_ITEM, false, 90)]
        private static void AlignViewToSelectedGeoAstroObject(MenuCommand menuCommand)
        {
            SceneViewDouble sceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;
            if (sceneViewDouble != Disposable.NULL)
            {
                GeoAstroObject selectedGeoAstroObject = null;

                if (sceneViewDouble.alignViewToGeoAstroObject == Disposable.NULL)
                {
                    Object selectedObject = null;
                    if (UnityEditor.Selection.activeGameObject != null)
                    {
                        GameObject activeGameObject = UnityEditor.Selection.activeGameObject;
                        selectedObject = GetSafeComponent<Object>(activeGameObject);
                        if (selectedObject == Disposable.NULL)
                        {
                            MeshRendererVisual meshRendererVisual = GetSafeComponent<MeshRendererVisual>(activeGameObject);
                            if (meshRendererVisual != Disposable.NULL)
                                selectedObject = meshRendererVisual.visualObject;
                        }
                    }
                    if (selectedObject != Disposable.NULL)
                        selectedGeoAstroObject = (selectedObject is GeoAstroObject ? selectedObject : selectedObject.transform.parentGeoAstroObject) as GeoAstroObject;
                }

                SetAlignViewToGeoAstroObject(sceneViewDouble, sceneViewDouble.camera, selectedGeoAstroObject);
            }
        }

        [MenuItem(ALIGN_VIEW_TO_SELECTED_GEOASTROOBJECT_MENU_ITEM, true)]
        private static bool AlignViewToSelectedGeoAstroObject()
        {
            Menu.SetChecked(ALIGN_VIEW_TO_SELECTED_GEOASTROOBJECT_MENU_ITEM, IsAlignedToGeoAstroObject());
            return HasSceneViewDouble();
        }

        private const string AUTO_SNAP_VIEW_TO_TERRAIN_MENU_ITEM = "GameObject/Auto Snap View to Terrain";
        [MenuItem(AUTO_SNAP_VIEW_TO_TERRAIN_MENU_ITEM, false, 91)]
        private static void AutoSnapViewToTerrain(MenuCommand menuCommand)
        {
            SceneViewDouble sceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;
            if (sceneViewDouble != Disposable.NULL)
                SetAlignViewToGeoAstroObject(sceneViewDouble, sceneViewDouble.camera, sceneViewDouble.alignViewToGeoAstroObject, !sceneViewDouble.autoSnapViewToTerrain);
        }

        private static void SetAlignViewToGeoAstroObject(GeoAstroObject geoAstroObject)
        {
            SceneViewDouble sceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;
            if (sceneViewDouble != Disposable.NULL)
                SetAlignViewToGeoAstroObject(sceneViewDouble, sceneViewDouble.camera, geoAstroObject);
        }

        private static void SetAlignViewToGeoAstroObject(SceneViewDouble sceneViewDouble, SceneCamera activeSceneCamera, GeoAstroObject geoAstroObject, bool autoSnapViewToTerrain = true)
        {
            if (sceneViewDouble != Disposable.NULL)
            {
                if (geoAstroObject != Disposable.NULL)
                {
                    if (sceneViewDouble.alignViewToGeoAstroObject != geoAstroObject || (!sceneViewDouble.autoSnapViewToTerrain && autoSnapViewToTerrain))
                    {
                        activeSceneCamera.targetController.GetGeoAstroObjectSurfaceComponents(out Vector3Double targetPosition, out QuaternionDouble rotation, out double cameraDistance, geoAstroObject);
                        sceneViewDouble.SetComponents(targetPosition, rotation, cameraDistance);
                    }

                    sceneViewDouble.alignViewToGeoAstroObject = geoAstroObject;
                    sceneViewDouble.autoSnapViewToTerrain = autoSnapViewToTerrain;
                }
                else
                {
                    if (sceneViewDouble.alignViewToGeoAstroObject != Disposable.NULL)
                        sceneViewDouble.alignViewToGeoAstroObject = null;
                    else
                        Debug.LogWarning("No Selected "+typeof(GeoAstroObject).Name+"!");
                }
            }
        }

        [MenuItem(AUTO_SNAP_VIEW_TO_TERRAIN_MENU_ITEM, true)]
        private static bool AutoSnapViewToTerrain()
        {
            SceneViewDouble sceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;
            if (sceneViewDouble != Disposable.NULL)
                Menu.SetChecked(AUTO_SNAP_VIEW_TO_TERRAIN_MENU_ITEM, sceneViewDouble.autoSnapViewToTerrain);
            
            return IsAlignedToGeoAstroObject();
        }

        [MenuItem("GameObject/Move View to GeoCoordinate", false, 92)]
        private static void MoveViewToGeoCoordinate(MenuCommand menuCommand)
        {
            GeoCoordinatePopup geoCoordiantePopup = ScriptableObject.CreateInstance<GeoCoordinatePopup>();
            geoCoordiantePopup.minSize = Vector2.zero;
            Rect mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            float width = 700.0f;
            float height = 23.0f;
            geoCoordiantePopup.position = new Rect(mainWindowRect.x + (mainWindowRect.width / 2.0f) - (width / 2.0f), mainWindowRect.y + (mainWindowRect.height / 2.0f) - (height / 2.0f), width, height);
            geoCoordiantePopup.ShowPopup();
            geoCoordiantePopup.FocusOnLatControl();
        }

        [MenuItem("GameObject/Move View to GeoCoordinate", true)]
        private static bool MoveViewToGeoCoordinate()
        {
            return IsAlignedToGeoAstroObject();
        }

        private static bool IsAlignedToGeoAstroObject()
        {
            SceneViewDouble sceneViewDouble = SceneViewDouble.lastActiveSceneViewDouble;
            if (sceneViewDouble != Disposable.NULL)
                return sceneViewDouble.alignViewToGeoAstroObject != Disposable.NULL;
            return false;
        }

        private static bool HasSceneViewDouble()
        {
            return SceneViewDouble.lastActiveSceneViewDouble != Disposable.NULL;
        }

        private static T GetSafeComponent<T>(GameObject gameObject) where T : Component
        {
            return gameObject.GetSafeComponent<T>(InstanceManager.InitializationContext.Editor);
        }

        private static RestDatasource GetRestDatasource(string baseAddress, string baseAddress2 = "", string baseAddress3 = "", string baseAddress4 = "")
        {
            return DatasourceManager.GetRestDatasource(baseAddress, baseAddress2, baseAddress3, baseAddress4, InstanceManager.InitializationContext.Editor);
        }
    }
}
