// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;

namespace DepictionEngine
{
    public class LoadSceneDatasourceOperation : SceneDatasourceOperationBase
    {
        //2x2 White Texture
        private static readonly byte[] WHITE_RAW_TEXTURE_BYTES = new byte[]
                        {
                            255, 255, 255, 255,
                            255, 255, 255, 255,
                            255, 255, 255, 255,
                            255, 255, 255, 255,
                            205, 205, 205, 205,
                        };

        private Type _type;
        private JSONObject _jsonFallback;
        private SerializableGuid _persistentFallbackValuesId;
        private int _seed;

        private Processor _proceduralDataProcessor;

        public LoadSceneDatasourceOperation Init(Type type, JSONObject jsonFallback, SerializableGuid persistentFallbackValuesId, int seed)
        {
            _type = type;
            _jsonFallback = jsonFallback;
            _persistentFallbackValuesId = persistentFallbackValuesId;
            _seed = seed;

            return this;
        }

        protected Processor proceduralDataProcessor
        {
            get { return _proceduralDataProcessor; }
            set
            {
                if (Object.ReferenceEquals(_proceduralDataProcessor, value))
                    return;

                if (_proceduralDataProcessor != null)
                    _proceduralDataProcessor.Cancel();

                _proceduralDataProcessor = value;
            }
        }

        private Vector2Int grid2DDimensions
        {
            get 
            {
                if (JsonUtility.FromJson(out Vector2Int grid2DDimensions, _jsonFallback[nameof(AssetBase.grid2DDimensions)]) && grid2DDimensions != Vector2Int.minusOne)
                    return grid2DDimensions;
                return Vector2Int.minusOne;
            }
        }

        private Vector2Int grid2DIndex
        {
            get
            {
                if (JsonUtility.FromJson(out Vector2Int grid2DIndex, _jsonFallback[nameof(AssetBase.grid2DIndex)]) && grid2DIndex != Vector2Int.minusOne)
                    return grid2DIndex;
                return Vector2Int.minusOne;
            }
        }

        protected override void KillLoading()
        {
            base.KillLoading();

            _proceduralDataProcessor?.Dispose();
        }

        private bool IsProcedural()
        {
            return _seed != -1;
        }

        public override DatasourceOperationBase Execute(Action<bool, OperationResult> operationResultCallback)
        {
            base.Execute(operationResultCallback);

            if (IsProcedural())
            {
                Type proceduralDataProcessorParametersType = typeof(PropertyModifierParameters);
                if (_jsonFallback != null && grid2DDimensions != Vector2Int.minusOne && grid2DIndex != Vector2Int.minusOne)
                    proceduralDataProcessorParametersType = typeof(PropertyModifierIndex2DParameters);

                if (proceduralDataProcessor == null)
                    proceduralDataProcessor = InstanceManager.Instance(false)?.CreateInstance<Processor>();

                proceduralDataProcessor.StartProcessing(PropertyModifierDataProcessingFunctions.PopulatePropertyModifier, typeof(PropertyModifierData), proceduralDataProcessorParametersType, InitProceduralModifierParameters,
                    (data, errorMsg) =>
                    {
                        PropertyModifierData propertyModifierData = data as PropertyModifierData;

                        OperationResult operationResult = null;

                        if (string.IsNullOrEmpty(errorMsg))
                        {
                            operationResult = CreateOperationResult<OperationResult>();

                            List<PropertyModifier> propertyModifiers = new List<PropertyModifier>();

                            if (propertyModifierData.propertyModifier != Disposable.NULL)
                                propertyModifiers.Add(propertyModifierData.propertyModifier);

                            operationResult.Add(CreateResultData<LoadResultData>().Init(propertyModifierData.type, new JSONObject(), propertyModifierData.jsonFallback, propertyModifierData.persistentFallbackValuesId, propertyModifiers));
                        }

                        OperationDone(new OperationDoneResult(true, operationResult));
                    }, sceneManager.enableMultithreading ? Processor.ProcessingType.AsyncTask : Processor.ProcessingType.Sync);
            }
            else
            {
                OperationResult operationResult = CreateOperationResult<OperationResult>();

                PropertyModifier propertyModifier = null;

                if(typeof(Texture).IsAssignableFrom(_type))
                {
                    if (typeof(Elevation).IsAssignableFrom(_type))
                    {
                        float minElevation = Elevation.MIN_ELEVATION;

                        InstanceManager instanceManager = InstanceManager.Instance(false);
                        if (instanceManager != Disposable.NULL)
                        {
                            FallbackValues fallbackValues = instanceManager.GetFallbackValues(_persistentFallbackValuesId);
                            if (fallbackValues != Disposable.NULL && fallbackValues.GetProperty(out float fallbackMinElevation, nameof(Elevation.minElevation)))
                                minElevation = fallbackMinElevation;
                        }

                        ElevationUtility.EncodeToRGBByte(0.0f, out byte r, out byte g, out byte b, minElevation);

                        //2x2 0 Elevation Texture
                        byte[] rgbElevation = new byte[16];
                        ElevationUtility.AddRGBToByteArray(r, g, b, rgbElevation, 0);
                        ElevationUtility.AddRGBToByteArray(r, g, b, rgbElevation, 4);
                        ElevationUtility.AddRGBToByteArray(r, g, b, rgbElevation, 8);
                        ElevationUtility.AddRGBToByteArray(r, g, b, rgbElevation, 12);

                        propertyModifier = ProcessingFunctions.CreatePropertyModifier<ElevationModifier>().Init(minElevation, 1, rgbElevation, true);
                    }
                    else
                        propertyModifier = ProcessingFunctions.CreatePropertyModifier<TextureModifier>().Init(WHITE_RAW_TEXTURE_BYTES, true);
                }

                operationResult.Add(CreateResultData<LoadResultData>().Init(_type, new JSONObject(), _jsonFallback, _persistentFallbackValuesId, propertyModifier != Disposable.NULL ? new List<PropertyModifier>() { propertyModifier} : null));

                DisposeManager.Dispose(propertyModifier);

                OperationDone(new OperationDoneResult(true, operationResult));
            }

            return this;
        }

        protected void InitProceduralModifierParameters(ProcessorParameters parameters)
        {
            (parameters as PropertyModifierParameters).Init(_type, _jsonFallback, _persistentFallbackValuesId, _seed);
            if (parameters is PropertyModifierIndex2DParameters)
                (parameters as PropertyModifierIndex2DParameters).Init(grid2DDimensions, grid2DIndex);
        }

        public override bool LoadingWasCompromised()
        {
            return base.LoadingWasCompromised() && IsProcedural() && (proceduralDataProcessor == null || proceduralDataProcessor.ProcessingWasCompromised());
        }
    }
}
