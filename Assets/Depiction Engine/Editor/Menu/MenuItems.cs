// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class MenuItems
    {
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

        [MenuItem("GameObject/Depiction Engine/Instantiator VisualObject", false, 14)]
        private static void CreateInstantiatorVisualObject(MenuCommand menuCommand)
        {
            string name = nameof(InstantiatorVisualObject);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<InstantiatorVisualObject>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/Reflection Probe", false, 15)]
        private static void CreateReflectionProbe(MenuCommand menuCommand)
        {
            string name = nameof(ReflectionProbe);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<ReflectionProbe>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/VolumeMask/Rectangular", false, 16)]
        private static void CreateRectangularVolumeMask(MenuCommand menuCommand)
        {
            string name = nameof(RectangularVolumeMask);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<RectangularVolumeMask>(GetContextTransform(menuCommand), name);
        }

        //Depiction Engine Astro
        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Planet (Empty)", false, 27)]
        private static void CreatePlanet(MenuCommand menuCommand)
        {
            bool spherical = true;
            InitializeSceneCameraSkybox(!spherical);

            SetAlignViewToGeoAstroObject(CreatePlanetEmpty(GetContextTransform(menuCommand), "Planet", spherical));
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Earth (Realistic)", false, 28)]
        private static void CreatePlanetEarthRealistic(MenuCommand menuCommand)
        {
            APIKeyInputsPopup textInputPopup = ShowMapAPIKeyDialog(new List<string> { "Mapbox API Key", "OSMBuildings API Key" }, new List<string> { "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA", "dixw8kmb" });
            textInputPopup.ClosedEvent = (closeState, keys) =>
            {
                if (closeState == APIKeyInputsPopup.DialogCloseState.Ok)
                {
                    bool spherical = true;
                    InitializeSceneCameraSkybox(!spherical);

                    SetAlignViewToGeoAstroObject(CreateEarthRealistic(GetContextTransform(menuCommand), "Earth", spherical, keys[0], keys[1]));
                }
            };
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Earth (Basic)", false, 29)]
        private static void CreatePlanetEarthBasic(MenuCommand menuCommand)
        {
            bool spherical = true;
            InitializeSceneCameraSkybox(!spherical);

            SetAlignViewToGeoAstroObject(CreateEarthBasic(GetContextTransform(menuCommand), "Earth", spherical));
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Planet/Moon (Realistic)", false, 30)]
        private static void CreatePlanetMoonRealistic(MenuCommand menuCommand)
        {
            APIKeyInputsPopup textInputPopup = ShowMapAPIKeyDialog(new List<string> { "ArcGIS API Key" }, new List<string> { "WQ9KVmV6xGGMnCiQ" });
            textInputPopup.ClosedEvent = (closeState, keys) =>
            {
                if (closeState == APIKeyInputsPopup.DialogCloseState.Ok)
                {
                    bool spherical = true;
                    InitializeSceneCameraSkybox(!spherical);

                    SetAlignViewToGeoAstroObject(CreateMoonRealistic(GetContextTransform(menuCommand), "Moon", spherical, keys[0]));
                }
            };
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Map (Empty)", false, 31)]
        private static void CreateMap(MenuCommand menuCommand)
        {
            bool spherical = false;
            InitializeSceneCameraSkybox(!spherical);

            SetAlignViewToGeoAstroObject(CreatePlanetEmpty(GetContextTransform(menuCommand), "Map", spherical));
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Earth (Realistic)", false, 32)]
        private static void CreateMapEarthRealistic(MenuCommand menuCommand)
        {
            APIKeyInputsPopup textInputPopup = ShowMapAPIKeyDialog(new List<string> { "Mapbox API Key", "OSMBuildings API Key" }, new List<string> { "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA", "dixw8kmb" });
            textInputPopup.ClosedEvent = (closeState, keys) =>
            {
                if (closeState == APIKeyInputsPopup.DialogCloseState.Ok)
                {
                    bool spherical = false;
                    InitializeSceneCameraSkybox(!spherical);

                    SetAlignViewToGeoAstroObject(CreateEarthRealistic(GetContextTransform(menuCommand), "Earth", spherical, keys[0], keys[1]));
                }
            };
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Earth (Basic)", false, 33)]
        private static void CreateMapEarthBasic(MenuCommand menuCommand)
        {
            bool spherical = false;
            InitializeSceneCameraSkybox(!spherical);

            SetAlignViewToGeoAstroObject(CreateEarthBasic(GetContextTransform(menuCommand), "Earth", spherical));
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Map/Moon (Realistic)", false, 34)]
        private static void CreateMapMoonRealistic(MenuCommand menuCommand)
        {
            APIKeyInputsPopup textInputPopup = ShowMapAPIKeyDialog(new List<string> { "ArcGIS API Key"}, new List<string> { "WQ9KVmV6xGGMnCiQ" });
            textInputPopup.ClosedEvent = (closeState, keys) =>
            {
                if (closeState == APIKeyInputsPopup.DialogCloseState.Ok)
                {
                    bool spherical = false;
                    InitializeSceneCameraSkybox(!spherical);

                    SetAlignViewToGeoAstroObject(CreateMoonRealistic(GetContextTransform(menuCommand), "Moon", spherical, keys[0]));
                }
            };
        }

        private static void InitializeSceneCameraSkybox(bool atmosphere = false)
        {
            if (atmosphere && SceneManager.Instance(false) == Disposable.NULL)
                CameraManager.Instance().skyboxMaterialPath = RenderingManager.MATERIAL_BASE_PATH + (atmosphere ? "Skybox/Atmosphere-Skybox" : "Skybox/Star-Skybox");
        }

        [MenuItem("GameObject/Depiction Engine/Astro/Star", false, 35)]
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

        [MenuItem("GameObject/Depiction Engine/Astro/Star System", false, 36)]
        private static void CreateStarSystem(MenuCommand menuCommand)
        {
            string name = nameof(StarSystem);
            UndoManager.CreateNewGroup("Create " + name);

            StarSystem starSystem = CreateObject<StarSystem>(GetContextTransform(menuCommand), name);
            CreateScript<StarSystemAnimator>(starSystem);
        }

        //Depiction Engine Camera
        [MenuItem("GameObject/Depiction Engine/Camera/Camera", false, 47)]
        private static void CreateCamera(MenuCommand menuCommand)
        {
            string name = "Camera";
            UndoManager.CreateNewGroup("Create " + name);

            Camera camera = CreateObject<Camera>(GetContextTransform(menuCommand), name);

            InitMainCamera(camera);

            camera.PreventCameraStackBug();
        }

        [MenuItem("GameObject/Depiction Engine/Camera/Target Camera", false, 48)]
        private static void CreateTargetCamera(MenuCommand menuCommand)
        {
            UndoManager.CreateNewGroup("Create Camera Target");

            Camera camera = InstanceUtility.CreateTargetCamera(GetContextTransform(menuCommand), InitializationContext.Editor, true, true);

            InitMainCamera(camera);

            SelectObject((camera.controller as CameraController).target);

            camera.PreventCameraStackBug();
        }

        private static void InitMainCamera(Camera camera)
        {
            if (Camera.main == null)
                camera.tag = "MainCamera";
        }

        //Depiction Engine UI
        [MenuItem("GameObject/Depiction Engine/UI/label", false, 59)]
        private static void CreateLabel(MenuCommand menuCommand) 
        {
            string name = "Label";
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<Label>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/UI/Marker", false, 60)]
        private static void CreateMarker(MenuCommand menuCommand) 
        {
            string name = "Marker";
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<Marker>(GetContextTransform(menuCommand), name);
        }

        //Depiction Engine Datasource
        [MenuItem("GameObject/Depiction Engine/DatasourceRoot", false, 71)]
        private static void CreateDatasourceRoot(MenuCommand menuCommand)
        {
            string name = nameof(DatasourceRoot);
            UndoManager.CreateNewGroup("Create " + name);

            CreateObject<DatasourceRoot>(GetContextTransform(menuCommand), name);
        }

        [MenuItem("GameObject/Depiction Engine/Datasource/FileSystemDatasource", false, 72)]
        private static void CreateFileSystemDatasource(MenuCommand menuCommand)
        {
            string name = "FileSystemDatasource";
            UndoManager.CreateNewGroup("Create " + name);

            Object datasourceObject = CreateObject<Object>(GetContextTransform(menuCommand), name);
            CreateScript<FileSystemDatasource>(datasourceObject);
        }

        [MenuItem("GameObject/Depiction Engine/Datasource/RestDatasource", false, 73)]
        private static void CreateRestDatasource(MenuCommand menuCommand)
        {
            string name = "RestDatasource";
            UndoManager.CreateNewGroup("Create " + name);

            Object datasourceObject = CreateObject<Object>(GetContextTransform(menuCommand), name);
            CreateScript<RestDatasource>(datasourceObject);
        }

        //Depiction Engine Managers
        [MenuItem("GameObject/Depiction Engine/Managers", false, 84)]
        private static void CreateManagers(MenuCommand menuCommand)
        {
            if (menuCommand is null)
            {
                throw new System.ArgumentNullException(nameof(menuCommand));
            }

            SceneManager.Instance();
        }

        private static Planet CreatePlanetEmpty(Transform parent, string name, bool spherical)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            //Planet
            JSONObject planetJson = new();

            //Add Planet -> Color Texture Index2DLoader
            JSONArray colorTextureLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Texture));
            string colorTextureLoaderId = colorTextureLoaderJson[0][nameof(Index2DLoader.id)];
            colorTextureLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 19));
            colorTextureLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.TexturePngJpg);
            colorTextureLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "my/terrain/tile/service/endpoint/{0}/{1}/{2}";

            InstanceUtility.MergeComponentsToObjectInitializationJson(colorTextureLoaderJson, planetJson);

            //Add Planet -> Elevation Index2DLoader
            JSONArray elevationLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Elevation));
            string elevationLoaderId = elevationLoaderJson[0][nameof(Index2DLoader.id)];
            elevationLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 19));
            elevationLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.ElevationTerrainRGBPngRaw);
            elevationLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "my/elevation/tile/service/endpoint/{0}/{1}/{2}";

            InstanceUtility.MergeComponentsToObjectInitializationJson(elevationLoaderJson, planetJson);

            Planet planet = CreatePlanet(parent, name, spherical, GeoAstroObject.DEFAULT_SIZE, AstroObject.DEFAULT_MASS, planetJson);

            //Add TerrainRoot -> TerrainGridMeshObject CameraGrid2DLoader
            JSONObject terrainJson = new();

            JSONArray terrainGridMeshObjectCameraGrid2DLoaderJson = InstanceUtility.GetLoaderJson(typeof(CameraGrid2DLoader), typeof(TerrainGridMeshObject));
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.sizeMultiplier)] = 2.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 15));
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.sphericalSubdivision)] = 2;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.flatSubdivision)] = 2;
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = ElevationGridMeshObjectBase.ELEVATION_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = elevationLoaderId;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = TerrainGridMeshObject.COLORMAP_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = colorTextureLoaderId;

            InstanceUtility.MergeComponentsToObjectInitializationJson(terrainGridMeshObjectCameraGrid2DLoaderJson, terrainJson);

            CreateLayer(planet, "Terrain", terrainJson);

            return planet;
        }

        private static Planet CreateEarthBasic(Transform parent, string name, bool spherical)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            DatasourceBase mapboxDatasource = GetRestDatasource("https://api.mapbox.com/");

            DatasourceBase buildingFeatureDatasource = GetRestDatasource(
            "https://a-data.3dbuildings.com/",
            "https://b-data.3dbuildings.com/",
            "https://c-data.3dbuildings.com/",
            "https://d-data.3dbuildings.com/");

            //Add Earth
            JSONObject earthJson = new();

            //Add Earth -> Color Texture Index2DLoader
            JSONArray colorTextureLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Texture));
            string colorTextureLoaderId = colorTextureLoaderJson[0][nameof(Index2DLoader.id)];
            colorTextureLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(mapboxDatasource.id);
            colorTextureLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 30));
            colorTextureLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.TexturePngJpg);
            colorTextureLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "styles/v1/mapbox/streets-v11/tiles/{0}/{1}/{2}?access_token=";

            InstanceUtility.MergeComponentsToObjectInitializationJson(colorTextureLoaderJson, earthJson);

            //Add Earth -> Elevation Index2DLoader
            JSONArray elevationLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Elevation));
            string elevationLoaderId = elevationLoaderJson[0][nameof(Index2DLoader.id)];
            elevationLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 19));
            elevationLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.TexturePngJpg);
            elevationLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "my/elevation/tile/service/endpoint/{0}/{1}/{2}";

            InstanceUtility.MergeComponentsToObjectInitializationJson(elevationLoaderJson, earthJson);

            //Add Earth => BuildingFeature Index2DLoader
            JSONArray buildingFeatureDataLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(BuildingFeature));
            string buildingFeatureDataLoaderId = buildingFeatureDataLoaderJson[0][nameof(Index2DLoader.id)];
            buildingFeatureDataLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(buildingFeatureDatasource.id);
            buildingFeatureDataLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "tile/{0}/{1}/{2}.json?token=";

            InstanceUtility.MergeComponentsToObjectInitializationJson(buildingFeatureDataLoaderJson, earthJson);

            Planet earth = CreatePlanet(parent, name, spherical, GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Earth), GeoAstroObject.GetPlanetMass(AstroObject.PlanetType.Earth), earthJson);

            //Add TerrainRoot -> TerrainGridMeshObject CameraGrid2DLoader
            JSONObject terrainJson = new();

            JSONArray terrainGridMeshObjectCameraGrid2DLoaderJson = InstanceUtility.GetLoaderJson(typeof(CameraGrid2DLoader), typeof(TerrainGridMeshObject));
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.sizeMultiplier)] = 2.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 15));
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.cascades)] = JsonUtility.ToJson(new Vector2Int(0, 4));
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.sphericalSubdivision)] = 1;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.flatSubdivision)] = 1;
            if (!spherical)
                terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.subdivisionZoomFactor)] = 1.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.edgeDepth)] = 0.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.normalsType)] = JsonUtility.ToJson(TerrainGridMeshObject.NormalsType.SurfaceUp);
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = ElevationGridMeshObjectBase.ELEVATION_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = elevationLoaderId;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = TerrainGridMeshObject.COLORMAP_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = colorTextureLoaderId;

            InstanceUtility.MergeComponentsToObjectInitializationJson(terrainGridMeshObjectCameraGrid2DLoaderJson, terrainJson);

            CreateLayer(earth, "Terrain", terrainJson);

            //Add BuildingsRoot -> BuildingGridMeshObject CameraGrid2DLoader
            JSONObject buildingsJson = new();

            JSONArray buildingMeshObjectCameraGrid2DLoaderJson = InstanceUtility.GetLoaderJson(typeof(CameraGrid2DLoader), typeof(BuildingGridMeshObject));
            buildingMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.sizeMultiplier)] = 2.0f;
            buildingMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(14, 14));
            buildingMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.cascades)] = JsonUtility.ToJson(Vector2Int.zero);
            buildingMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(BuildingGridMeshObject.color)] = JsonUtility.ToJson(Color.white);
            buildingMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = FeatureGridMeshObjectBase.FEATURE_REFERENCE_DATATYPE;
            buildingMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = buildingFeatureDataLoaderId;

            InstanceUtility.MergeComponentsToObjectInitializationJson(buildingMeshObjectCameraGrid2DLoaderJson, buildingsJson);

            CreateLayer(earth, "Buildings", buildingsJson);

            return earth;
        }

        private static Planet CreateEarthRealistic(Transform parent, string name, bool spherical, string mapboxKey, string osmBuildingsKey)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            DatasourceBase mapboxDatasource = GetRestDatasource("https://api.mapbox.com/");

            DatasourceBase arcgisDatasource = GetRestDatasource("https://services.arcgisonline.com/");

            DatasourceBase buildingFeatureDatasource = GetRestDatasource(
            "https://a-data.3dbuildings.com/",
            "https://b-data.3dbuildings.com/",
            "https://c-data.3dbuildings.com/",
            "https://d-data.3dbuildings.com/");

            //Add Earth
            JSONObject earthJson = new();

            List<string> headers = new List<string>();
            //Forcing a different Referer spams warnings but it is only ment to be used as an Editor demo, A real development key should be used.
            if (mapboxKey == "pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA")
                headers.Add("Referer#https://www.mapbox.com/");

            //Add Earth -> Color Texture Index2DLoader
            JSONArray colorTextureLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Texture));
            string colorTextureLoaderId = colorTextureLoaderJson[0][nameof(Index2DLoader.id)];
            colorTextureLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(mapboxDatasource.id);
            colorTextureLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 30));
            colorTextureLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.TexturePngJpg);
            colorTextureLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "v4/mapbox.satellite/{0}/{1}/{2}@2x.jpg90?access_token=" + mapboxKey;
            colorTextureLoaderJson[0][nameof(LoaderBase.headers)] = JsonUtility.ToJson(headers);

            InstanceUtility.MergeComponentsToObjectInitializationJson(colorTextureLoaderJson, earthJson);

            //Add Earth -> Elevation Index2DLoader
            JSONArray elevationLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Elevation));
            string elevationLoaderId = elevationLoaderJson[0][nameof(Index2DLoader.id)];
            elevationLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(mapboxDatasource.id);
            elevationLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 13));
            elevationLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.ElevationTerrainRGBPngRaw);
            elevationLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "v4/mapbox.terrain-rgb/{0}/{1}/{2}.pngraw?access_token=" + mapboxKey;
            //Alternative Elevation
            //LoaderBase.DataType.ElevationMapboxTerrainRGBWebP
            //"raster/v1/mapbox.mapbox-terrain-dem-v1/{0}/{1}/{2}.webp?sku=101WQxhVS07ft&access_token=" + mapboxKey
            elevationLoaderJson[0][nameof(LoaderBase.headers)] = JsonUtility.ToJson(headers);

            InstanceUtility.MergeComponentsToObjectInitializationJson(elevationLoaderJson, earthJson);

            //Add Earth -> Surface Texture Index2DLoader
            JSONArray surfaceLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Texture));
            string surfaceLoaderId = surfaceLoaderJson[0][nameof(Index2DLoader.id)];
            surfaceLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(arcgisDatasource.id);
            surfaceLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 12));
            surfaceLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.TexturePngJpg);
            surfaceLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "arcgis/rest/services/Canvas/World_Dark_Gray_Base/MapServer/tile/{0}/{2}/{1}";
            surfaceLoaderJson[0][nameof(LoaderBase.headers)] = JsonUtility.ToJson(headers);

            InstanceUtility.MergeComponentsToObjectInitializationJson(surfaceLoaderJson, earthJson);

            //Add Earth => BuildingFeature Index2DLoader
            string buildingFeatureDataLoaderId = null;
            if (!string.IsNullOrEmpty(osmBuildingsKey))
            {
                JSONArray buildingFeatureDataLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(BuildingFeature));
                buildingFeatureDataLoaderId = buildingFeatureDataLoaderJson[0][nameof(Index2DLoader.id)];
                buildingFeatureDataLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(buildingFeatureDatasource.id);
                buildingFeatureDataLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "tile/{0}/{1}/{2}.json?token=" + osmBuildingsKey;

                InstanceUtility.MergeComponentsToObjectInitializationJson(buildingFeatureDataLoaderJson, earthJson);
            }

            //Add Earth -> Reflection
            InstanceUtility.MergeComponentsToObjectInitializationJson(InstanceUtility.GetComponentJson(typeof(TerrainSurfaceReflectionEffect)), earthJson);

            //Add Earth -> Atmosphere
            InstanceUtility.MergeComponentsToObjectInitializationJson(InstanceUtility.GetComponentJson(typeof(AtmosphereEffect)), earthJson);

            Planet earth = CreatePlanet(parent, name, spherical, GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Earth), GeoAstroObject.GetPlanetMass(AstroObject.PlanetType.Earth), earthJson);

            //Add TerrainRoot -> TerrainGridMeshObject CameraGrid2DLoader
            JSONObject terrainJson = new();

            JSONArray terrainGridMeshObjectCameraGrid2DLoaderJson = InstanceUtility.GetLoaderJson(typeof(CameraGrid2DLoader), typeof(TerrainGridMeshObject));
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.sizeMultiplier)] = 2.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 15));
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.sphericalSubdivision)] = 2;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.flatSubdivision)] = 2;
            if (!spherical)
                terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.subdivisionZoomFactor)] = 1.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.normalsType)] = JsonUtility.ToJson(TerrainGridMeshObject.NormalsType.SurfaceUp);
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = ElevationGridMeshObjectBase.ELEVATION_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = elevationLoaderId;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = TerrainGridMeshObject.COLORMAP_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = colorTextureLoaderId;
            terrainGridMeshObjectCameraGrid2DLoaderJson[5][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = TerrainGridMeshObject.SURFACETYPEMAP_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[5][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = surfaceLoaderId;

            InstanceUtility.MergeComponentsToObjectInitializationJson(terrainGridMeshObjectCameraGrid2DLoaderJson, terrainJson);

            CreateLayer(earth, "Terrain", terrainJson);

            //Add BuildingsRoot -> BuildingGridMeshObject CameraGrid2DLoader
            if (!string.IsNullOrEmpty(buildingFeatureDataLoaderId))
            {
                JSONObject buildingsJson = new();

                JSONArray buildingMeshObjectCameraGrid2DLoaderJson = InstanceUtility.GetLoaderJson(typeof(CameraGrid2DLoader), typeof(BuildingGridMeshObject));
                buildingMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.sizeMultiplier)] = 5.0f;
                buildingMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(14, 14));
                buildingMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.cascades)] = JsonUtility.ToJson(Vector2Int.zero);
                buildingMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = ElevationGridMeshObjectBase.ELEVATION_REFERENCE_DATATYPE;
                buildingMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = elevationLoaderId;
                buildingMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = FeatureGridMeshObjectBase.FEATURE_REFERENCE_DATATYPE;
                buildingMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = buildingFeatureDataLoaderId;
                buildingMeshObjectCameraGrid2DLoaderJson[4][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = BuildingGridMeshObject.COLORMAP_REFERENCE_DATATYPE;
                buildingMeshObjectCameraGrid2DLoaderJson[4][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = colorTextureLoaderId;

                InstanceUtility.MergeComponentsToObjectInitializationJson(buildingMeshObjectCameraGrid2DLoaderJson, buildingsJson);

                CreateLayer(earth, "Buildings", buildingsJson);
            }

            return earth;
        }

        private static Planet CreateMoonRealistic(Transform parent, string name, bool spherical, string arcGISKey)
        {
            UndoManager.CreateNewGroup("Create " + name);

            CreateStarIfMissing(parent);

            RestDatasource arcGISDatasource = GetRestDatasource("https://tiles.arcgis.com/");

            //Planet
            JSONObject planetJson = new();

            //Add Planet -> Color Texture Index2DLoader
            JSONArray colorTextureLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Texture));
            string colorTextureLoaderId = colorTextureLoaderJson[0][nameof(Index2DLoader.id)];
            colorTextureLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 7));
            colorTextureLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.TexturePngJpg);
            colorTextureLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "tiles/"+ arcGISKey + "/arcgis/rest/services/Moon_Basemap_Tile0to9/MapServer/tile/{0}/{2}/{1}";
            colorTextureLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(arcGISDatasource.id);

            InstanceUtility.MergeComponentsToObjectInitializationJson(colorTextureLoaderJson, planetJson);

            //Add Planet -> Elevation Index2DLoader
            JSONArray elevationLoaderJson = InstanceUtility.GetLoaderJson(typeof(Index2DLoader), typeof(Elevation));
            string elevationLoaderId = elevationLoaderJson[0][nameof(Index2DLoader.id)];
            elevationLoaderJson[0][nameof(Index2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 7));
            elevationLoaderJson[0][nameof(Index2DLoader.dataType)] = JsonUtility.ToJson(LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression);
            elevationLoaderJson[0][nameof(Index2DLoader.loadEndpoint)] = "tiles/"+ arcGISKey + "/arcgis/rest/services/Moon_Elevation_Surface/ImageServer/tile/{0}/{2}/{1}?blankTile=false";
            elevationLoaderJson[0][nameof(Index2DLoader.datasourceId)] = JsonUtility.ToJson(arcGISDatasource.id);
            
            elevationLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(Elevation.elevationMultiplier)] = 0.2f;

            InstanceUtility.MergeComponentsToObjectInitializationJson(elevationLoaderJson, planetJson);

            Planet planet = CreatePlanet(parent, name, spherical, GeoAstroObject.GetAstroObjectSize(AstroObject.PlanetType.Moon), GeoAstroObject.GetPlanetMass(AstroObject.PlanetType.Moon), planetJson);

            //Add TerrainRoot -> TerrainGridMeshObject CameraGrid2DLoader
            JSONObject terrainJson = new();

            JSONArray terrainGridMeshObjectCameraGrid2DLoaderJson = InstanceUtility.GetLoaderJson(typeof(CameraGrid2DLoader), typeof(TerrainGridMeshObject));
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.sizeMultiplier)] = 3.0f;
            terrainGridMeshObjectCameraGrid2DLoaderJson[0][nameof(CameraGrid2DLoader.minMaxZoom)] = JsonUtility.ToJson(new Vector2Int(0, 7));
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.sphericalSubdivision)] = 1;
            terrainGridMeshObjectCameraGrid2DLoaderJson[1][nameof(FallbackValues.fallbackValuesJson)][nameof(TerrainGridMeshObject.flatSubdivision)] = 1;
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = ElevationGridMeshObjectBase.ELEVATION_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[2][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = elevationLoaderId;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.dataType)] = TerrainGridMeshObject.COLORMAP_REFERENCE_DATATYPE;
            terrainGridMeshObjectCameraGrid2DLoaderJson[3][nameof(FallbackValues.fallbackValuesJson)][nameof(AssetReference.loaderId)] = colorTextureLoaderId;

            InstanceUtility.MergeComponentsToObjectInitializationJson(terrainGridMeshObjectCameraGrid2DLoaderJson, terrainJson);

            CreateLayer(planet, "Terrain", terrainJson);

            return planet;
        }

        private static Star CreateStarIfMissing(Transform parent)
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL && instanceManager.StarExists())
                return null;

            Star star = CreateStar(parent, "Star");
            star.SetPlanetPreset(AstroObject.PlanetType.Sun);
            star.transform.localPosition = new Vector3Double(-100000000000.0d, 100000000000.0d, 0.0d);
                
            return star;
        }

        private static Star CreateStar(Transform parent, string name)
        {
            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL && instanceManager.StarExists())
            {
                Debug.LogError(InstanceManager.GetMultipleInstanceErrorMsg(typeof(Star).Name));
                return null;
            }

            Star star = CreateObject<Star>(parent, name);
            star.transform.localRotation = QuaternionDouble.Euler(-90.0d, 0.0d, 0.0d);

            return star;
        }

        private static Transform GetContextTransform(MenuCommand menuCommand)
        {
            return menuCommand.context != null ? (menuCommand.context as GameObject).transform : null;
        }

        private static Planet CreatePlanet(
            Transform parent, 
            string name, 
            bool spherical, 
            double size, 
            double mass, 
            JSONNode scriptsJson, 
            bool setParentAndAlign = true, 
            bool moveToView = true, 
            bool selectGameObject = true)
        {
            Planet planet = InstanceUtility.CreatePlanet(
                parent,
                name,
                spherical,
                size,
                mass,
                scriptsJson,
                InitializationContext.Editor, 
                setParentAndAlign, 
                moveToView);

            if (selectGameObject)
                SelectObject(planet);

            return planet;
        }

        private static DatasourceRoot CreateLayer(Planet planet, string name, JSONNode json)
        {
            return InstanceUtility.CreateLayer(planet, name, json, InitializationContext.Editor);
        }

        private static T CreateObject<T>(Transform parent, string name = "", bool setParentAndAlign = true, bool moveToView = true, bool selectGameObject = true) where T : Object
        {
            T objectBase = InstanceManager.Instance().CreateInstance<T>(parent, name, initializingContext: InitializationContext.Editor, setParentAndAlign: setParentAndAlign, moveToView: moveToView);

            if (selectGameObject)
                SelectObject(objectBase);

            return objectBase;
        }

        private static T CreateScript<T>(Object objectBase, JSONObject json = null) where T : Script
        {
            return objectBase.CreateScript<T>(json, InitializationContext.Editor);
        }

        private static void SelectObject(Object objectBase)
        {
            if (objectBase != Disposable.NULL)
                UndoManager.QueueSetActiveTransform(objectBase.transform);
        }

        private const string ALIGN_VIEW_TO_SELECTED_GEOASTROOBJECT_MENU_ITEM = "GameObject/Align View to Selected GeoAstroObject";
        [MenuItem(ALIGN_VIEW_TO_SELECTED_GEOASTROOBJECT_MENU_ITEM, false, 95)]
        private static void AlignViewToSelectedGeoAstroObject(MenuCommand menuCommand)
        {
            if (menuCommand is null)
            {
                throw new System.ArgumentNullException(nameof(menuCommand));
            }

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
        [MenuItem(AUTO_SNAP_VIEW_TO_TERRAIN_MENU_ITEM, false, 96)]
        private static void AutoSnapViewToTerrain(MenuCommand menuCommand)
        {
            if (menuCommand is null)
            {
                throw new System.ArgumentNullException(nameof(menuCommand));
            }

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
                        Debug.Log("No selected "+typeof(GeoAstroObject).Name+"!");
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

        [MenuItem("GameObject/Move View to GeoCoordinate", false, 97)]
        private static void MoveViewToGeoCoordinate(MenuCommand menuCommand)
        {
            if (menuCommand is null)
            {
                throw new System.ArgumentNullException(nameof(menuCommand));
            }

            GeoCoordinatePopup geoCoordiantePopup = ScriptableObject.CreateInstance<GeoCoordinatePopup>();
            PositionPopup(geoCoordiantePopup);
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

        private static APIKeyInputsPopup ShowMapAPIKeyDialog(List<string> labels, List<string> inputs)
        {
            APIKeyInputsPopup textInputPopup = ScriptableObject.CreateInstance<APIKeyInputsPopup>();
            textInputPopup.labels = labels;
            textInputPopup.inputs = inputs;
            PositionPopup(textInputPopup, 20.0f * (labels.Count + 1) + 3);
            textInputPopup.ShowPopup();
            return textInputPopup;
        }

        private static void PositionPopup(EditorWindow popup, float height = 23.0f)
        {
            popup.minSize = Vector2.zero;
            Rect mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            float width = 700.0f;
            popup.position = new Rect(mainWindowRect.x + (mainWindowRect.width / 2.0f) - (width / 2.0f), mainWindowRect.y + (mainWindowRect.height / 2.0f) - (height / 2.0f), width, height);
        }

        private static bool HasSceneViewDouble()
        {
            return SceneViewDouble.lastActiveSceneViewDouble != Disposable.NULL;
        }

        private static T GetSafeComponent<T>(GameObject gameObject) where T : Component
        {
            return gameObject.GetSafeComponent<T>(InitializationContext.Editor);
        }

        private static RestDatasource GetRestDatasource(string baseAddress, string baseAddress2 = "", string baseAddress3 = "", string baseAddress4 = "")
        {
            return DatasourceManager.GetRestDatasource(baseAddress, baseAddress2, baseAddress3, baseAddress4, InitializationContext.Editor);
        }
    }
}
