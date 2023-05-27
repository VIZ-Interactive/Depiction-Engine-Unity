// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Wrapper class for 'UnityEngine.Texture2D' introducing better integrated functionality.
    /// </summary>
    public class Texture : AssetBase
    {
        [BeginFoldout("Texture")]
        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDataField)), ConditionalEnable(nameof(GetEnableDataField))]
#endif
        private Texture2D _unityTexture;
        [SerializeField, Tooltip("Texture coordinate wrapping mode.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowTextureFields))]
#endif
        private TextureWrapMode _wrapMode;
        [SerializeField, Tooltip("Filtering mode of the texture."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowTextureFields))]
#endif
        private FilterMode _filterMode;

        [SerializeField, HideInInspector]
        private int _width;
        [SerializeField, HideInInspector]
        private int _height;
        [SerializeField, HideInInspector]
        private TextureFormat _format;
        [SerializeField, HideInInspector]
        private bool _isDataSRGB;

#if UNITY_EDITOR
        protected override LoaderBase.DataType GetDataTypeFromExtension(string extension)
        {
            return LoaderBase.DataType.TexturePngJpg;
        }

        protected virtual bool GetShowTextureFields()
        {
            return true;
        }

        protected override string GetSupportedLoadFileExtensions()
        {
            return base.GetSupportedLoadFileExtensions() + "," + GetAdditionalSupportedLoadFileExtensions();
        }

        protected virtual string GetAdditionalSupportedLoadFileExtensions()
        {
            return "jpg,jpeg";
        }
#endif

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                unityTexture = InstanceManager.Duplicate(unityTexture, initializingContext);

            InitValue(value => wrapMode = value, TextureWrapMode.Clamp, initializingContext);
            InitValue(value => filterMode = value, GetDefaultFilterMode(), initializingContext);
        }

        protected virtual FilterMode GetDefaultFilterMode()
        {
            return FilterMode.Bilinear;
        }

        public int width
        {
            get => _width;
        }

        public int height
        {
            get => _height;
        }

        public TextureFormat format
        {
            get => _format;
        }

        public bool isDataSRGB
        {
            get => _isDataSRGB;
        }

        public int mipmapCount
        {
            get => unityTexture != null ? unityTexture.mipmapCount : 1;
        }

        /// <summary>
        /// Texture coordinate wrapping mode.
        /// </summary>
        [Json]
        public TextureWrapMode wrapMode
        {
            get => _wrapMode;
            set
            {
                SetValue(nameof(wrapMode), value, ref _wrapMode, (newValue, oldValue) =>
                {
                    UpdateTextureWrapMode();
                });
            }
        }

        /// <summary>
        /// Filtering mode of the texture.
        /// </summary>
        [Json]
        public FilterMode filterMode
        {
            get => _filterMode;
            set
            {
                SetValue(nameof(filterMode), value, ref _filterMode, (newValue, oldValue) =>
                {
                    UpdateTextureFilterMode();
                });
            }
        }

        private void UpdateTextureWrapMode()
        {
            if (unityTexture != null)
                unityTexture.wrapMode = wrapMode;
        }

        private void UpdateTextureFilterMode()
        {
            if (unityTexture != null)
                unityTexture.filterMode = filterMode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetPixel(Vector2Int pixel, out Color color)
        {
            try
            {
                if (_unityTexture != null)
                {
                    color = _unityTexture.GetPixel(pixel.x, pixel.y);
                    return true;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Debug.LogError(e.Message);
            }

            color = Color.clear;
            return false;
        }

        public virtual Vector2Int GetPixelFromNormalizedCoordinate(Vector2 normalizedCoordinate)
        {
            Vector2Int pixel = new Vector2Int(Mathf.FloorToInt(normalizedCoordinate.x * width), Mathf.FloorToInt(normalizedCoordinate.y * height));

            if (pixel.x == width)
                pixel.x -= 1;
            if (pixel.y == height)
                pixel.y -= 1;

            return pixel;
        }

        protected virtual bool IsTextureDataType(LoaderBase.DataType dataType)
        {
            return dataType == LoaderBase.DataType.TexturePngJpg;
        }

        protected override byte[] GetDataBytes(LoaderBase.DataType dataType)
        {
            byte[] bytes = null;

            if (unityTexture != null)
            {
                bytes = unityTexture.GetRawTextureData();

                if (IsTextureDataType(dataType))
                    bytes = ImageConversion.EncodeArrayToPNG(bytes, unityTexture.graphicsFormat, (uint)width, (uint)height);
            }

            return bytes;
        }

        public override void SetData(object value, LoaderBase.DataType dataType, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            SetData(value as byte[], false, width, height, format, mipmapCount, false, initializingContext);
        }

        public void SetData(NativeArray<Color32> textureBytes, int width, int height, TextureFormat format, int mipmapCount, bool linear, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            try
            {
                CreateTextureIfRequired(true, width, height, format, mipmapCount, linear, initializingContext);

                unityTexture.SetPixelData(textureBytes, 0);
                unityTexture.Apply();

                UpdateUnityTextureProperties();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            DataPropertyAssigned();
        }

        public void SetData(byte[] textureBytes, bool isRawTextureBytes, int width, int height, TextureFormat format, int mipmapCount, bool linear, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            try
            {
                CreateTextureIfRequired(isRawTextureBytes, width, height, format, mipmapCount, linear, initializingContext);

                if (isRawTextureBytes)
                {
                    unityTexture.LoadRawTextureData(textureBytes);
                    unityTexture.Apply();
                }
                else
                    unityTexture.LoadImage(textureBytes);

                UpdateUnityTextureProperties();
            }
            catch (Exception e) 
            { 
                Debug.LogError(e.Message); 
            }

            DataPropertyAssigned();
        }

        private void CreateTextureIfRequired(bool isRawTextureBytes, int width, int height, TextureFormat format, int mipmapCount, bool linear, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            bool requiresNewTexture2D = unityTexture == null || this.mipmapCount != mipmapCount || !unityTexture.isDataSRGB != linear;

#if UNITY_EDITOR
            if (initializingContext == InitializationContext.Editor)
                requiresNewTexture2D = true;
#endif
            if (requiresNewTexture2D || (isRawTextureBytes && (this.width != width || this.height != height || this.format != format)))
            {
                if (width == 0)
                    width = 2;
                if (height == 0)
                    height = 2;
                SetData(new Texture2D(width, height, format, mipmapCount, linear, true), initializingContext);
            }
        }

        public void SetData(Texture2D texture)
        {
            SetData(texture, InitializationContext.Programmatically);

            DataPropertyAssigned();
        }

        protected override void DataChanged()
        {
            base.DataChanged();

            UpdateTextureWrapMode();
            UpdateTextureFilterMode();
        }

        protected override string GetFileExtension()
        {
            return "png";
        }

        protected override object GetData()
        {
            return unityTexture;
        }

        protected virtual void SetData(Texture2D texture, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            Texture2D oldUnityTexture = unityTexture;

            unityTexture = texture;

            DisposeOldDataAndRegisterNewData(oldUnityTexture, unityTexture, initializingContext);
        }

        public Texture2D unityTexture
        {
            get => _unityTexture;
            private set
            {
                if (_unityTexture == value)
                    return;

                _unityTexture = value;


                UpdateUnityTextureProperties();
                UpdateTextureWrapMode();
                UpdateTextureFilterMode();
            }
        }

        private void UpdateUnityTextureProperties()
        {
            if (unityTexture != null)
            {
                _width = unityTexture.width;
                _height = unityTexture.height;
                _format = unityTexture.format;
                _isDataSRGB = unityTexture.isDataSRGB;
            }
            else
            {
                _width = 0;
                _height = 0;
                _format = TextureFormat.RGBA32;
                _isDataSRGB = false;
            }
        }

        protected virtual Type GetProcessorParametersType()
        {
            return typeof(TextureParameters);
        }

        protected virtual void InitializeProcessorParameters(ProcessorParameters parameters)
        {
            (parameters as TextureParameters).Init(grid2DIndex, grid2DDimensions);
        }

        protected virtual Type GetDataType()
        {
            return typeof(TextureProcessorOutput);
        }

        protected virtual Func<ProcessorOutput, ProcessorParameters, IEnumerator> GetProcessingFunction()
        {
            return null;
        }

        private static PropertyModifier GetProceduralPropertyModifier(PropertyModifierParameters parameters)
        {
            TextureModifier textureModifier = ProcessingFunctions.CreatePropertyModifier<TextureModifier>();

            if (textureModifier != Disposable.NULL)
            {
                int textureSize = 256;
                textureModifier.Init(PopulateProceduralPixels(parameters, textureSize, textureSize, GetPixel), true, textureSize, textureSize, TextureFormat.RGBA32, 1);
            }

            return textureModifier;
        }

        protected delegate void GetPixelDelegate(PropertyModifierParameters parameters, float x, float y, out byte r, out byte g, out byte b, out byte a);

        protected static byte[] PopulateProceduralPixels(PropertyModifierParameters parameters, int width, int height, GetPixelDelegate pixelCallback)
        {
            byte[] pixels = new byte[width * height * 4];

            if (pixelCallback != null)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pixelCallback(parameters, (x + 0.5f) / width, (y + 0.5f) / height, out byte r, out byte g, out byte b, out byte a);

                        int startIndex = (y * width + x) * 4;
                        pixels[startIndex] = r;
                        pixels[startIndex + 1] = g;
                        pixels[startIndex + 2] = b;
                        pixels[startIndex + 3] = a;
                    }
                }
            }

            return pixels;
        }

        protected static void GetPixel(PropertyModifierParameters parameters, float x, float y, out byte r, out byte g, out byte b, out byte a)
        {
            //Add Procedural Algorithm here
            //Seed can be found in parameters.seed
            r = (byte)(x * 255);
            g = (byte)(y * 255);
            b = 0;
            a = 255;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                if (disposeContext != DisposeContext.Programmatically_Pool)
                    DisposeManager.Dispose(_unityTexture, disposeContext);

                return true;
            }
            return false;
        }
    }

    public class TextureParameters : ProcessorParameters
    {
        private Vector2Int _grid2DIndex;
        private Vector2Int _grid2DDimensions;

        public TextureParameters Init(Vector2Int grid2DIndex, Vector2Int grid2DDimensions)
        {
            _grid2DIndex = grid2DIndex;
            _grid2DDimensions = grid2DDimensions;

            return this;
        }

        public Vector2Int grid2DIndex
        {
            get => _grid2DIndex;
        }

        public Vector2Int grid2DDimensions
        {
            get => _grid2DDimensions;
        }
    }

    public class TextureProcessorOutput : ProcessorOutput
    {
        private TextureModifier _textureModifier;

        public override void Recycle()
        {
            base.Recycle();

            _textureModifier = default;
        }

        public void Init(TextureModifier textureModifier)
        {
            this.textureModifier = textureModifier;
        }

        public TextureModifier textureModifier
        {
            get => _textureModifier;
            private set
            {
                if (_textureModifier == value)
                    return;

                _textureModifier = value;
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                DisposeManager.Dispose(_textureModifier);

                return true;
            }
            return false;
        }
    }

    public class TextureModifier : AssetModifier
    {
        private Texture2D _texture2D;

        private byte[] _textureBytes;
        private bool _isRawTextureBytes;
        private int _width;
        private int _height;
        private TextureFormat _format;
        private int _mipmapCount;
        private bool _linear;

        public override void Recycle()
        {
            base.Recycle();

            _texture2D = default;
            _textureBytes = default;
            _isRawTextureBytes = default;
            _width = default;
            _height = default;
            _format = default;
            _mipmapCount = default;
            _linear = default;
        }

        public TextureModifier Init(Texture2D texture2D)
        {
            _texture2D = texture2D;
            return this;
        }

        public TextureModifier Init(byte[] textureBytes, bool isRawTextureBytes = false, int width = 0, int height = 0, TextureFormat format = TextureFormat.RGB24, int mipmapCount = 1, bool linear = false)
        {
            _textureBytes = textureBytes;
            _isRawTextureBytes = isRawTextureBytes;
            _width = width;
            _height = height;
            _format = format;
            _mipmapCount = mipmapCount;
            _linear = linear;

            return this;
        }

        public TextureModifier Init(int width = 0, int height = 0, TextureFormat format = TextureFormat.RGB24, int mipmapCount = 1, bool linear = false)
        {
            _width = width;
            _height = height;
            _format = format;
            _mipmapCount = mipmapCount;
            _linear = linear;

            return this;
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            Texture texture = scriptableBehaviour as Texture;

            if (!Object.ReferenceEquals(_texture2D, null))
            {
                texture.SetData(_texture2D);
                _texture2D = null;
            }
            else
            {
                if (_textureBytes != null)
                    texture.SetData(_textureBytes, _isRawTextureBytes, _width, _height, _format, _mipmapCount, _linear);
            }
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                DisposeManager.Destroy(_texture2D);

                return true;
            }
            return false;
        }
    }
}
