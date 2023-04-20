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
        [SerializeField, Tooltip("A Factor by which we multiply the elevation value to accentuate or reduce its magnitude.")]
        private float _elevationMultiplier;
        [SerializeField, Tooltip("When enabled the pixel values will be flipped horizontally.")]
        private bool _xFlip;
        [SerializeField, Tooltip("When enabled the pixel values will be flipped vertically.")]
        private bool _yFlip;
        [SerializeField, Tooltip("The lowest point supported by the elevation encoding mode."), EndFoldout]
        private float _minElevation;

        [SerializeField, HideInInspector]
        private int _rgbComponentOffset;

        private byte[] _elevation;
        
#if UNITY_EDITOR
        protected override LoaderBase.DataType GetDataTypeFromExtension(string extension)
        {
            LoaderBase.DataType dataType = base.GetDataTypeFromExtension(extension);

            switch (extension)
            {
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
            _rgbComponentOffset = default;
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
            InitValue(value => xFlip = value, false, initializingContext);
            InitValue(value => yFlip = value, false, initializingContext);
            InitValue(value => minElevation = value, MIN_ELEVATION, initializingContext);
        }

#if UNITY_EDITOR
        private float _lastElevationMultiplier;
        protected override void UpdateUndoRedoSerializedFields()
        {
            base.UpdateUndoRedoSerializedFields();
           
            SerializationUtility.PerformUndoRedoPropertyChange((value) => { elevationMultiplier = value; }, ref _elevationMultiplier, ref _lastElevationMultiplier);
        }
#endif

        /// <summary>
        /// A Factor by which we multiply the elevation value to accentuate or reduce its magnitude.
        /// </summary>
        [Json]
        public float elevationMultiplier
        {
            get { return _elevationMultiplier; }
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
        /// When enabled the pixel values will be flipped horizontally.
        /// </summary>
        [Json]
        public bool xFlip
        {
            get { return _xFlip; }
            set { SetValue(nameof(xFlip), value, ref _xFlip); }
        }

        /// <summary>
        /// When enabled the pixel values will be flipped vertically.
        /// </summary>
        [Json]
        public bool yFlip
        {
            get { return _yFlip; }
            set { SetValue(nameof(yFlip), value, ref _yFlip); }
        }

        /// <summary>
        /// The lowest point supported by the elevation encoding mode.
        /// </summary>
        [Json]
        public float minElevation
        {
            get { return _minElevation; }
            set { SetValue(nameof(minElevation), value, ref _minElevation); }
        }

        public int rgbComponentOffset
        {
            get { return _rgbComponentOffset; }
            set
            {
                if (_rgbComponentOffset == value)
                    return;

                _rgbComponentOffset = value;
            }
        }

        public static float GetMinElevationFromDataType(LoaderBase.DataType dataType)
        {
            return dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw || dataType == LoaderBase.DataType.ElevationTerrainRGBWebP ? RGB_TERRAIN_MIN_ELEVATION : MIN_ELEVATION;
        }

        public static int GetRGBComponentOffsetFromDataType(LoaderBase.DataType dataType)
        {
            return dataType == LoaderBase.DataType.ElevationEsriLimitedErrorRasterCompression || dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw ? 1 : 0;
        }

        protected override bool IsTextureDataType(LoaderBase.DataType dataType)
        {
            return dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw || dataType == LoaderBase.DataType.ElevationTerrainRGBPngRaw;
        }

        public override void SetData(object value, LoaderBase.DataType dataType, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            base.SetData(value, dataType, initializingContext);

            minElevation = GetMinElevationFromDataType(dataType);

            rgbComponentOffset = GetRGBComponentOffsetFromDataType(dataType);
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
        public float GetElevation(float x, float y, bool clamp = false)
        {
            try
            {
                if (_elevation != null && _elevation.Length > 0)
                {
                    Vector2Int pixel = GetPixelFromNormalized(x, y, clamp, xFlip, yFlip);

                    int startIndex = (pixel.y * width + pixel.x) * 4;

                    startIndex += rgbComponentOffset;

                    return ElevationUtility.DecodeToFloat(_elevation[startIndex], _elevation[startIndex + 1], _elevation[startIndex + 2], _minElevation) * _elevationMultiplier;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError(e.Message);
            }
            return 0.0f;
        }

        private static PropertyModifier GetProceduralPropertyModifier(PropertyModifierParameters parameters)
        {
            TextureModifier textureModifier = ProcessingFunctions.CreatePropertyModifier<TextureModifier>();

            int textureSize = 256;
            textureModifier.Init(PopulateProceduralPixels(parameters, textureSize, textureSize, GetPixel), true, textureSize, textureSize, TextureFormat.RGBA32, false, true);

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
        private int _rgbComponentOffset;

        public override void Recycle()
        {
            base.Recycle();

            _minElevation = default;
            _rgbComponentOffset = default;
        }

        public ElevationModifier Init(float minElevation, int rgbComponentOffset, Texture2D texture)
        {
            base.Init(texture);

            _minElevation = minElevation;
            _rgbComponentOffset = rgbComponentOffset;

            return this;
        }

        public ElevationModifier Init(float minElevation, int rgbComponentOffset, byte[] textureData, bool isRawTextureData = false, int width = 0, int height = 0, TextureFormat format = TextureFormat.RGBA32, bool mipChain = false, bool linear = true)
        {
            base.Init(textureData, isRawTextureData, width, height, format, mipChain, linear);

            _minElevation = minElevation;
            _rgbComponentOffset = rgbComponentOffset;

            return this;
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            if (scriptableBehaviour is Elevation)
            {
                Elevation elevation = scriptableBehaviour as Elevation;

                elevation.minElevation = _minElevation;
                elevation.rgbComponentOffset = _rgbComponentOffset;
            }
        }
    }
}
