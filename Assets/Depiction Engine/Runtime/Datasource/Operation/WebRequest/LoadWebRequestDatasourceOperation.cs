// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using WebP;

namespace DepictionEngine
{
    public class LoadWebRequestDatasourceOperation : WebRequestDatasourceOperationBase
    {
        private LoaderBase.DataType _dataType;
        private JSONObject _jsonFallback;
        private JSONArray _persistentFallbackValues;

        public void Init(LoaderBase.DataType dataType = LoaderBase.DataType.Json, JSONObject jsonFallback = null, JSONArray persistentFallbackValues = null)
        {
            _dataType = dataType;
            _jsonFallback = jsonFallback;
            _persistentFallbackValues = persistentFallbackValues;
        }

        protected override UnityWebRequest CreateUnityWebRequest(string uri = null, int timeout = 60, List<string> headers = null, byte[] bodyData = null, HTTPMethod httpMethod = HTTPMethod.Get)
        {
            return base.CreateUnityWebRequest(uri, timeout, headers, bodyData, _dataType == LoaderBase.DataType.TexturePngJpg || _dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw ? HTTPMethod.GetTexture : HTTPMethod.Get);
        }

        protected override IEnumerator WebRequestDataProcessorFunction(ProcessorOutput data, ProcessorParameters parameters)
        {
            return LoadWebRequestProcessingFunctions.PopulateOperationResult(data, parameters);
        }

        protected override Type InitWebRequestProcessorParametersType()
        {
            return typeof(LoadWebRequestProcessorParameters);
        }

        protected override void InitWebRequestProcessorParameters(ProcessorParameters parameters)
        {
            base.InitWebRequestProcessorParameters(parameters);

            LoadWebRequestProcessorParameters loadWebRequestProcessorParameters = parameters as LoadWebRequestProcessorParameters;

            loadWebRequestProcessorParameters.Init(_dataType, _jsonFallback, _persistentFallbackValues);
        }

        public class LoadWebRequestProcessorParameters : WebRequestProcessorParameters
        {
            private LoaderBase.DataType _dataType;
            private JSONObject _jsonFallback;
            private JSONArray _persistentFallbackValues;

            public LoadWebRequestProcessorParameters Init(LoaderBase.DataType dataType, JSONObject jsonFallback, JSONArray persistentFallbackValues)
            {
                _dataType = dataType;
                _jsonFallback = jsonFallback;
                _persistentFallbackValues = persistentFallbackValues;

                return this;
            }

            public LoaderBase.DataType dataType
            {
                get => _dataType;
                private set => _dataType = value;
            }

            public JSONObject jsonFallback
            {
                get => _jsonFallback;
                private set => _jsonFallback = value;
            }

            public JSONArray persistentFallbackValues
            {
                get => _persistentFallbackValues;
                private set => _persistentFallbackValues = value;
            }
        }

        public class LoadWebRequestProcessingFunctions : DatasourceOperationProcessingFunctions
        {
            public static IEnumerator PopulateOperationResult(ProcessorOutput data, ProcessorParameters parameters)
            {
                foreach (object enumeration in PopulateOperationResult(data as OperationResult, parameters as WebRequestProcessorParameters))
                    yield return enumeration;
            }

            private static IEnumerable PopulateOperationResult(OperationResult operationResult, WebRequestProcessorParameters webRequestProcessorParameters)
            {
                if (operationResult is not null)
                {
                    LoadWebRequestProcessorParameters loadWebRequestProcessorParameters = webRequestProcessorParameters as LoadWebRequestProcessorParameters;

                    switch (loadWebRequestProcessorParameters.dataType)
                    {
                        case LoaderBase.DataType.Json:

                            JSONNode jsonResult = ParseJSON(webRequestProcessorParameters.text);

                            if (jsonResult != null)
                            {
                                if (jsonResult["levels"] != null)
                                {
                                    jsonResult["type"] = typeof(Building).FullName;
                                    jsonResult["children"] = new JSONArray();

                                    JSONArray levelMeshObjectsJsonResult = jsonResult["levels"].AsArray;
                                    for (int i = levelMeshObjectsJsonResult.Count - 1; i >= 0; i--)
                                    {
                                        JSONNode levelMeshObjectJsonResult = jsonResult["levels"][i];

                                        JSONObject levelJsonResult = new()
                                        {
                                            ["id"] = SerializableGuid.NewGuid().ToString(),
                                            ["name"] = levelMeshObjectJsonResult["name"]
                                        };
                                        jsonResult["children"].Add(levelJsonResult);

                                        levelJsonResult["type"] = typeof(Level).FullName;
                                        levelJsonResult["children"] = new JSONArray();

                                        levelMeshObjectJsonResult["type"] = typeof(LevelMeshObject).FullName;
                                        levelMeshObjectJsonResult["name"] = typeof(LevelMeshObject).Name;
                                        levelMeshObjectJsonResult.Remove("latitude");
                                        levelMeshObjectJsonResult.Remove("longitude");
                                        levelJsonResult["children"].Add(levelMeshObjectJsonResult);
                                    }

                                    jsonResult.Remove("levels");
                                }

                                if (jsonResult["type"] == "FeatureCollection")
                                    jsonResult.Remove("type");

                                if (jsonResult.IsArray)
                                {
                                    foreach (JSONObject jsonResultItem in jsonResult.AsArray)
                                        AddResponseDataToOperationResult(GetLoadResultDataFromJson(loadWebRequestProcessorParameters, jsonResultItem), operationResult);
                                }
                                else
                                    AddResponseDataToOperationResult(GetLoadResultDataFromJson(loadWebRequestProcessorParameters, jsonResult as JSONObject), operationResult);
                            }

                            break;

                        case LoaderBase.DataType.TexturePngJpg:
                        case LoaderBase.DataType.ElevationTerrainRGBPngRaw:
                        case LoaderBase.DataType.ElevationTerrainRGBWebP:
                        case LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression:

                            AddResponseDataToOperationResult(GetLoadResultDataFromAsset(loadWebRequestProcessorParameters), operationResult);

                            break;
                    }
                }

                yield break;
            }

            private static LoadResultData GetLoadResultDataFromJson(LoadWebRequestProcessorParameters loadOperationResultParameters, JSONObject json)
            {
                LoadResultData loadResultData = null;

                Type type = GetType(json["type"], out SerializableGuid persistentFallbackValuesId, loadOperationResultParameters);

                if (type != null)
                {
                    loadOperationResultParameters.cancellationTokenSource?.ThrowIfCancellationRequested();

                    JSONObject jsonFallback = loadOperationResultParameters.jsonFallback;

                    List<PropertyModifier> propertyModifiers = null;

                    if (json["longitude"] != null && json["latitude"] != null)
                    {
                        JSONObject transformJson = new()
                        {
                            ["type"] = typeof(TransformDouble).FullName,
                            ["geoCoordinate"] = JsonUtility.ToJson(new GeoCoordinate3Double(json["latitude"], json["longitude"]))
                        };
                        json.Remove("latitude");
                        json.Remove("longitude");
                        json["transform"] = transformJson;
                    }

                    if (typeof(LevelMeshObject).IsAssignableFrom(type))
                    {
                        LevelModifier levelModifier = CreatePropertyModifier<LevelModifier>();
                        if (levelModifier != Disposable.NULL)
                        {
                            MeshObjectProcessorOutput meshObjectProcessorOutput = GetInstance<MeshObjectProcessorOutput>();
                            if (meshObjectProcessorOutput != Disposable.NULL)
                                LevelProcessingFunctions.ParseJSON(json, levelModifier.Init(meshObjectProcessorOutput), loadOperationResultParameters.cancellationTokenSource);

                            json.Remove("floorBuffers");
                            json.Remove("wallsBuffers");
                            json.Remove("ceilingBuffers");

                            propertyModifiers ??= new();
                            propertyModifiers.Add(levelModifier);
                        }
                    }
                    else if (typeof(BuildingFeature).IsAssignableFrom(type))
                    {
                        BuildingFeatureModifier buildingFeatureModifier = CreatePropertyModifier<BuildingFeatureModifier>();
                        if (buildingFeatureModifier != Disposable.NULL && BuildingFeatureProcessingFunctions.ParseJSON(json, buildingFeatureModifier, loadOperationResultParameters.cancellationTokenSource))
                        {
                            propertyModifiers ??= new();
                            propertyModifiers.Add(buildingFeatureModifier);
                        }
                    }

                    List<LoadResultData> children = null;

                    if (json["children"] != null && json["children"].IsArray)
                    {
                        JSONArray childrenJson = json["children"].AsArray;
                        foreach (JSONObject jsonItem in childrenJson)
                        {
                            children ??= new();
                            children.Add(GetLoadResultDataFromJson(loadOperationResultParameters, jsonItem));
                        }
                    }

                    loadResultData = GetInstance<LoadResultData>().Init(type, json, jsonFallback, persistentFallbackValuesId, propertyModifiers, children);
                }

                return loadResultData;
            }

            private static LoadResultData GetLoadResultDataFromAsset(LoadWebRequestProcessorParameters loadOperationResultParameters)
            {
                LoadResultData loadResultData = null;

                Type type = GetType(null, out SerializableGuid persistentFallbackValuesId, loadOperationResultParameters);

                if (type != null)
                {
                    if (typeof(AssetBase).IsAssignableFrom(type))
                    {
                        JSONObject json = new();
                        JSONObject jsonFallback = loadOperationResultParameters.jsonFallback;
                        List<PropertyModifier> propertyModifiers = null;

                        if (typeof(Texture).IsAssignableFrom(type))
                        {
                            TextureModifier textureModifier = null;

                            if (typeof(Elevation).IsAssignableFrom(type))
                            {
                                float minElevation = Elevation.GetMinElevationFromDataType(loadOperationResultParameters.dataType);
                                
                                json[nameof(Elevation.minElevation)] = minElevation;

                                if (loadOperationResultParameters.texture != null)
                                {
                                    Texture2D texture = loadOperationResultParameters.texture;
                                    loadOperationResultParameters.SetTexture(null, false);

                                    textureModifier = CreatePropertyModifier<ElevationModifier>().Init(minElevation, texture);
                                }
                                else if (loadOperationResultParameters.data != null && loadOperationResultParameters.data.Length != 0)
                                {
                                    byte[] elevationData = loadOperationResultParameters.data;

                                    int width;
                                    int height;

                                    switch (loadOperationResultParameters.dataType)
                                    {
                                        case LoaderBase.DataType.ElevationTerrainRGBPngRaw:

                                            textureModifier = CreatePropertyModifier<ElevationModifier>().Init(minElevation, elevationData);

                                            break;

                                        case LoaderBase.DataType.ElevationTerrainRGBWebP:

                                            elevationData = LoadRGBAFromWebP(out width, out height, elevationData);

                                            if (width != 0 && height != 0)
                                                textureModifier = CreatePropertyModifier<ElevationModifier>().Init(minElevation, elevationData, true, width, height);

                                            break;

                                        case LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression:

                                            float[] elevation = ElevationUtility.DecodeEsriLERCToFloat(elevationData);

                                            width = height = (int)Mathf.Sqrt(elevation.Length);

                                            elevationData = ElevationUtility.EncodeToRGBBytes(elevation, width, height, minElevation);

                                            if (width != 0 && height != 0)
                                                textureModifier = CreatePropertyModifier<ElevationModifier>().Init(minElevation, elevationData, true, width, height);

                                            break;
                                    }
                                }
                            }
                            else
                            {
                                if (loadOperationResultParameters.texture != null)
                                {
                                    Texture2D texture2D = loadOperationResultParameters.texture;
                                    loadOperationResultParameters.SetTexture(null, false);
                                    textureModifier = CreatePropertyModifier<TextureModifier>().Init(texture2D);
                                }
                                else if (loadOperationResultParameters.data != null && loadOperationResultParameters.data.Length != 0)
                                {
                                    byte[] textureData = loadOperationResultParameters.data;

                                    bool isRawData = false;

                                    int width = 0;
                                    int height = 0;

                                    int mipmapCount = 1;

                                    if (loadOperationResultParameters.dataType == LoaderBase.DataType.TextureWebP)
                                    {
                                        textureData = LoadRGBAFromWebP(out width, out height, textureData);
                                        isRawData = true;
                                    }

                                    textureModifier = CreatePropertyModifier<TextureModifier>().Init(textureData, isRawData, width, height, TextureFormat.RGB24, mipmapCount);
                                }
                            }

                            if (textureModifier != Disposable.NULL)
                            {
                                propertyModifiers ??= new List<PropertyModifier>();
                                propertyModifiers.Add(textureModifier);
                            }
                        }
                        else
                            Debug.LogWarning("Asset type '" + type.Name + "' not currently supported");

                        loadResultData = GetInstance<LoadResultData>().Init(type, json, jsonFallback, persistentFallbackValuesId, propertyModifiers);
                    }
                }

                return loadResultData;
            }

            private static byte[] LoadRGBAFromWebP(out int width, out int height, byte[] textureData)
            {
                width = 0;
                height = 0;

                try
                {
                    Texture2DExt.GetWebPDimensions(textureData, out width, out height);

                    return Texture2DExt.LoadRGBAFromWebP(textureData, ref width, ref height, false, out Error error);
                }
                catch (Exception)
                {

                }

                return null;
            }

            private static Type GetType(string typeName, out SerializableGuid persistentFallbackValuesId, LoadWebRequestProcessorParameters loadOperationResultParameters)
            {
                persistentFallbackValuesId = SerializableGuid.Empty;

                if (!string.IsNullOrEmpty(typeName))
                {
                    foreach (JSONObject persistentFallbackValues in loadOperationResultParameters.persistentFallbackValues)
                    {
                        if (SerializableGuid.TryParse(persistentFallbackValues[nameof(PropertyMonoBehaviour.id)], out SerializableGuid parsedPersistentFallbackValuesId))
                        {
                            string fallbackTypeStr = persistentFallbackValues[nameof(PropertyMonoBehaviour.type)];
                            if (typeName == fallbackTypeStr)
                            {
                                typeName = fallbackTypeStr;
                                persistentFallbackValuesId = parsedPersistentFallbackValuesId;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(typeName))
                {
                    foreach (JSONObject persistentFallbackValues in loadOperationResultParameters.persistentFallbackValues)
                    {
                        if (SerializableGuid.TryParse(persistentFallbackValues[nameof(PropertyMonoBehaviour.id)], out SerializableGuid parsedPersistentFallbackValuesId))
                        {
                            string fallbackTypeStr = persistentFallbackValues[nameof(PropertyMonoBehaviour.type)];
                            if (typeof(IPersistent).IsAssignableFrom(Type.GetType(fallbackTypeStr)) && persistentFallbackValues[nameof(PersistentMonoBehaviour.createPersistentIfMissing)])
                            {
                                typeName = fallbackTypeStr;
                                persistentFallbackValuesId = parsedPersistentFallbackValuesId;
                                break;
                            }
                        }
                    }
                }

                return !string.IsNullOrEmpty(typeName) ? Type.GetType(typeName) : null;
            }
        }
    }
}

