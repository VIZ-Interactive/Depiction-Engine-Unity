// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Texture containing elevation data.
    /// </summary>
    public class Elevation : Texture
    {
        private const float RGB_TERRAIN_MIN_ELEVATION = -10000.0f;

        public const float MIN_ELEVATION = -500000.0f;

        [BeginFoldout("Elevation")]
        [SerializeField, Tooltip("A factor by which we multiply the elevation value to accentuate or reduce its magnitude.")]
        private float _elevationMultiplier;
        [SerializeField, Tooltip("The lowest point supported by the elevation encoding mode.")]
        private float _minElevation;
        [SerializeField, Tooltip("The texture includes a 1-pixel buffer around the edges to enable adjacent tile interpolation."), EndFoldout]
        private bool _pixelBuffer;

        private byte[] _elevation;

#if UNITY_EDITOR
        protected override LoaderBase.DataType GetDataTypeFromExtension(string extension)
        {
            LoaderBase.DataType dataType = base.GetDataTypeFromExtension(extension);

            switch (extension)
            {
                case ".png":
                case ".pngraw":

                    dataType = LoaderBase.DataType.ElevationTerrainRGBPngRaw;
                    break;

                default:

                    dataType = LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression;
                    break;
            }

            return dataType;
        }

        protected override bool GetShowTextureFields()
        {
            return false;
        }

        protected override string GetAdditionalSupportedLoadFileExtensions()
        {
            return "";
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            _elevation = default;
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastElevationMultiplier = elevationMultiplier;
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            UpdateElevation();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);
           
            InitValue(value => elevationMultiplier = value, 1.0f, initializingContext);
            InitValue(value => minElevation = value, MIN_ELEVATION, initializingContext);
            InitValue(value => pixelBuffer = value, false, initializingContext);
        }

        protected override FilterMode GetDefaultFilterMode()
        {
            return FilterMode.Point;
        }

#if UNITY_EDITOR
        private float _lastElevationMultiplier;
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                SerializationUtility.PerformUndoRedoPropertyAssign((value) => { elevationMultiplier = value; }, ref _elevationMultiplier, ref _lastElevationMultiplier);

                return true;
            }
            return false;
        }
#endif

        /// <summary>
        /// A Factor by which we multiply the elevation value to accentuate or reduce its magnitude.
        /// </summary>
        [Json]
        public float elevationMultiplier
        {
            get => _elevationMultiplier;
            set 
            { 
                SetValue(nameof(elevationMultiplier), value, ref _elevationMultiplier, (newValue, oldValue) => 
                {
#if UNITY_EDITOR
                    _lastElevationMultiplier = newValue;
#endif
                }); 
            }
        }

        /// <summary>
        /// The lowest point supported by the elevation encoding mode.
        /// </summary>
        [Json]
        public float minElevation
        {
            get => _minElevation;
            set => SetValue(nameof(minElevation), value, ref _minElevation);
        }

        /// <summary>
        /// The texture includes a 1-pixel buffer around the edges to enable adjacent tile interpolation.
        /// </summary>
        [Json]
        public bool pixelBuffer
        {
            get => _pixelBuffer;
            set => SetValue(nameof(pixelBuffer), value, ref _pixelBuffer);
        }

        protected override void SetData(Texture2D texture, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            //A temporary solution used to convert the sRGB textures produced by the DownloadHandlerTexture, until Unity adds parameters to let us specify the colorSpace to create linear textures.
            if (texture.isDataSRGB)
                base.SetData(texture.GetRawTextureData<Color32>(), texture.width, texture.height, texture.format, texture.mipmapCount, true, initializingContext);
            else
                base.SetData(texture, initializingContext);
        }

        public static float GetMinElevationFromDataType(LoaderBase.DataType dataType)
        {
            return dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw || dataType == LoaderBase.DataType.ElevationTerrainRGBWebP ? RGB_TERRAIN_MIN_ELEVATION : MIN_ELEVATION;
        }

        protected override bool IsTextureDataType(LoaderBase.DataType dataType)
        {
            return dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw || dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw;
        }

        public override void SetData(object value, LoaderBase.DataType dataType, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            base.SetData(value, dataType, initializingContext);

            minElevation = GetMinElevationFromDataType(dataType);
        }

        protected override object ProcessDataBytes(byte[] value, LoaderBase.DataType dataType)
        {
            if (dataType == LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression)
                return ElevationUtility.DecodeEsriLERCToFloat(value);
            return value;
        }

        protected override void DataChanged()
        {
            base.DataChanged();

            UpdateElevation();
        }

        protected override string GetFileExtension()
        {
            return "pngraw";
        }

        private void UpdateElevation()
        {
            _elevation = unityTexture != null ? unityTexture.GetRawTextureData() : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetElevation(Vector2Int pixel, out float elevation)
        {
            try
            {
                if (_elevation != null)
                {
                    int startIndex = (pixel.y * width + pixel.x) * 4;

                    if (format == TextureFormat.ARGB32)
                        startIndex += 1;

                    elevation = ElevationUtility.DecodeToFloat(_elevation[startIndex], _elevation[startIndex + 1], _elevation[startIndex + 2], _minElevation) * _elevationMultiplier;
                    return true;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError(e.Message);
            }

            elevation = 0.0f;
            return false;
        }

        public override Vector2Int GetPixelFromNormalizedCoordinate(Vector2 normalizedCoordinate)
        {
            if (pixelBuffer)
            {
                normalizedCoordinate.x = normalizedCoordinate.x * (width - 2) / width + 1.0f / width;
                normalizedCoordinate.y = normalizedCoordinate.y * (height - 2) / height + 1.0f / height;
            }
            return base.GetPixelFromNormalizedCoordinate(normalizedCoordinate);
        }

        private static PropertyModifier GetProceduralPropertyModifier(PropertyModifierParameters parameters)
        {
            TextureModifier textureModifier = ProcessingFunctions.CreatePropertyModifier<TextureModifier>();

            if (textureModifier != Disposable.NULL)
            {
                int textureSize = 256;
                textureModifier.Init(PopulateProceduralPixels(parameters, textureSize, textureSize, GetPixel), true, textureSize, textureSize, TextureFormat.RGBA32, 1, true);
            }

            return textureModifier;
        }

        protected new static void GetPixel(PropertyModifierParameters parameters, float x, float y, out byte r, out byte g, out byte b, out byte a)
        {
            //Add Procedural Algorithm here
            //Seed can be found in parameters.seed
            float elevation = Vector2.Distance(new Vector2(x, y), Vector2.zero) * 10000.0f;

            r = 0;
            ElevationUtility.EncodeToRGBByte(elevation, out g, out b, out a);
        }
    }

    public class ElevationModifier : TextureModifier
    {
        private float _minElevation;

        public override void Recycle()
        {
            base.Recycle();

            _minElevation = default;
        }

        public ElevationModifier Init(float minElevation, Texture2D texture)
        {
            base.Init(texture);

            _minElevation = minElevation;

            return this;
        }

        public ElevationModifier Init(float minElevation, byte[] textureData, bool isRawTextureData = false, int width = 0, int height = 0, TextureFormat format = TextureFormat.RGBA32, bool linear = true)
        {
            base.Init(textureData, isRawTextureData, width, height, format, 1, linear);

            _minElevation = minElevation;

            return this;
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            if (scriptableBehaviour is Elevation)
            {
                Elevation elevation = scriptableBehaviour as Elevation;

                elevation.minElevation = _minElevation;
            }
        }
    }
}
