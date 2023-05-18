// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help with the manipulation of elevation data.
    /// </summary>
    public class ElevationUtility
    {
        private static readonly string[] INFO_LABELS = { "version", "data type", "nDim", "nCols", "nRows", "nBands", "num valid pixels", "blob size" };
        private static readonly string[] DATA_RANGE_LABELS = { "zMin", "zMax", "maxZErrorUsed" };

        private static readonly int INFO_ARR_SIZE = INFO_LABELS.Count();
        private static readonly int DATA_RANGE_ARR_SIZE = DATA_RANGE_LABELS.Count();

        /// <summary>
        /// Decodes a byte array in Esri Limited Error Raster Compression(LERC) format into an array of elevation values.
        /// </summary>
        /// <param name="elevation">Elevation values, in Esri Limited Error Raster Compression(LERC) format.</param>
        /// <returns>An array of elevation values in world unit.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static float[] DecodeEsriLERCToFloat(byte[] elevation)
        {
            float[] floatElevation;

            UInt32[] infoArr = new UInt32[INFO_ARR_SIZE];
            double[] dataRangeArr = new double[DATA_RANGE_ARR_SIZE];

            UInt32 hr = LercDecode.lerc_getBlobInfo(elevation, (UInt32)elevation.Length, infoArr, dataRangeArr, INFO_ARR_SIZE, DATA_RANGE_ARR_SIZE);
            if (hr > 0)
                throw new InvalidOperationException("Function lerc_getBlobInfo(...) failed with error code " + hr + ".");

            int lercVersion = (int)infoArr[0];
            int dataType = (int)infoArr[1];
            int nDim = (int)infoArr[2];
            int nCols = (int)infoArr[3];
            int nRows = (int)infoArr[4];
            int nBands = (int)infoArr[5];

            byte[] pValidBytes = new byte[nCols * nRows];
            uint nValues = (uint)(nDim * nCols * nRows * nBands);

            if ((LercDecode.DataType)dataType == LercDecode.DataType.dt_float)
            {
                floatElevation = new float[nValues];
                hr = LercDecode.lerc_decode(elevation, (UInt32)elevation.Length, pValidBytes, nDim, nCols, nRows, nBands, dataType, floatElevation);
                //if (hr == 0)
                //    GenericPixelLoop<float>.GetMinMax(_elevation, pValidBytes, nDim, nCols, nRows, nBands);
            }
            else
                throw new InvalidOperationException("Elevation requires float data type.");

            return floatElevation;
        }

        /// <summary>
        /// Encodes an array of elevation values, to texture RGB byte array.
        /// </summary>
        /// <param name="elevation">Elevation values, in world units.</param>
        /// <param name="width">Width of the texture.</param>
        /// <param name="height">Height of the texture.</param>
        /// <param name="minElevation">The lowest point supported by the elevation encoding mode.</param>
        /// <returns>A texture byte array.</returns>
        public static byte[] EncodeToRGBBytes(float[] elevation, int width, int height, float minElevation = Elevation.MIN_ELEVATION)
        {
            byte[] rgbElevation = new byte[width * height * 4];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    EncodeToRGBByte(elevation[index], out byte r, out byte g, out byte b, minElevation);

                    int startIndex = (((height - 1 - y) * width) + x) * 4;
                    AddRGBToByteArray(r, g, b, rgbElevation, startIndex);
                }
            }

            return rgbElevation;
        }

        /// <summary>
        /// Add RGB bytes to a byte array.
        /// </summary>
        /// <param name="r">Red byte value.</param>
        /// <param name="g">Green byte value.</param>
        /// <param name="b">Blue byte value.</param>
        /// <param name="rgbElevation">The byte array to which we add the RGB values</param>
        /// <param name="index">The starting index in the array at which to insert the RGB values.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRGBToByteArray(byte r, byte g, byte b, byte[] rgbElevation, int index)
        {
            rgbElevation[index] = r;
            rgbElevation[index + 1] = g;
            rgbElevation[index + 2] = b;
            rgbElevation[index + 3] = 0;
        }

        /// <summary>
        /// Decode RGB bytes to elevation value.
        /// </summary>
        /// <param name="r">Red byte value.</param>
        /// <param name="g">Green byte value.</param>
        /// <param name="b">Blue byte value.</param>
        /// <param name="minElevation">The lowest point supported by the elevation encoding mode.</param>
        /// <returns>An Elevation value, in world units.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DecodeToFloat(byte r, byte g, byte b, float minElevation = Elevation.MIN_ELEVATION)
        {
            return minElevation + (r * 65536.0f + g * 256 + b) * 0.1f;
        }

        /// <summary>
        /// Encode an elevation value to RGB bytes.
        /// </summary>
        /// <param name="elevation">An elevation value, in world units.</param>
        /// <param name="r">Red byte value.</param>
        /// <param name="g">Green byte value.</param>
        /// <param name="b">Blue byte value.</param>
        /// <param name="minElevation">The lowest point supported by the elevation encoding mode.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EncodeToRGBByte(float elevation, out byte r, out byte g, out byte b, float minElevation = Elevation.MIN_ELEVATION)
        {
            float value = (elevation - minElevation) * 10.0f;

            r = (byte)(value / 65536.0f);

            value -= r * 65536.0f;
            g = (byte)(value / 256.0f);

            value -= g * 256.0f;
            b = (byte)value;
        }
    }
}
