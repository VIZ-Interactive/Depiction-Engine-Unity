/*
Copyright 2016 Esri
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
A local copy of the license and additional notices are located with the
source distribution at:
http://github.com/Esri/lerc/
Contributors:  Thomas Maurer, Wenxue Ju
*/

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DepictionEngine
{
    public class LercDecode
    {
        const string lercDll = "Assets/DepictionEngine/Library/Lerc64.dll";

        // from Lerc_c_api.h :
        // 
        // typedef unsigned int lerc_status;
        //
        // // Call this function to get info about the compressed Lerc blob. Optional. 
        // // Info returned in infoArray is { version, dataType, nDim, nCols, nRows, nBands, nValidPixels, blobSize }, see Lerc_types.h .
        // // Info returned in dataRangeArray is { zMin, zMax, maxZErrorUsed }, see Lerc_types.h .
        // // If more than 1 band the data range [zMin, zMax] is over all bands. 
        //
        // lerc_status lerc_getBlobInfo(const unsigned char* pLercBlob, unsigned int blobSize, 
        //   unsigned int* infoArray, double* dataRangeArray, int infoArraySize, int dataRangeArraySize);

        [DllImport(lercDll)]
        public static extern UInt32 lerc_getBlobInfo(byte[] pLercBlob, UInt32 blobSize, UInt32[] infoArray, double[] dataRangeArray, int infoArraySize, int dataRangeArraySize);

        public enum DataType { dt_char, dt_uchar, dt_short, dt_ushort, dt_int, dt_uint, dt_float, dt_double }

        // Lerc decode functions for all Lerc compressed data types

        // from Lerc_c_api.h :
        // 
        // // Decode the compressed Lerc blob into a raw data array.
        // // The data array must have been allocated to size (nDim * nCols * nRows * nBands * sizeof(dataType)).
        // // The valid bytes array, if not 0, must have been allocated to size (nCols * nRows). 
        //
        // lerc_status lerc_decode(
        //   const unsigned char* pLercBlob,      // Lerc blob to decode
        //   unsigned int blobSize,               // blob size in bytes
        //   unsigned char* pValidBytes,          // gets filled if not null ptr, even if all valid
        //   int nDim,                            // number of values per pixel (new)
        //   int nCols, int nRows, int nBands,    // number of columns, rows, bands
        //   unsigned int dataType,               // data type of outgoing array
        //   void* pData);                        // outgoing data array

        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, sbyte[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, byte[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, short[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, ushort[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, Int32[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, UInt32[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, float[] pData);
        [DllImport(lercDll)]
        public static extern UInt32 lerc_decode(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, int dataType, double[] pData);

        // if you are lazy, don't want to deal with generic / templated code, and don't care about wasting memory: 
        // this function decodes the pixel values into a tile of data type double, independent of the compressed data type.

        [DllImport(lercDll)]
        public static extern UInt32 lerc_decodeToDouble(byte[] pLercBlob, UInt32 blobSize, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands, double[] pData);
    }

    class GenericPixelLoop<T>
    {
        public static void GetMinMax(T[] pData, byte[] pValidBytes, int nDim, int nCols, int nRows, int nBands)
        {
            double zMin = 1e30;
            double zMax = -zMin;

            // access the pixels; here, get the data range over all bands
            for (int iBand = 0; iBand < nBands; iBand++)
            {
                int k0 = nCols * nRows * iBand;
                for (int k = 0, i = 0; i < nRows; i++)
                    for (int j = 0; j < nCols; j++, k++)
                        if (1 == pValidBytes[k])    // pixel is valid
                        {
                            for (int m = 0; m < nDim; m++)
                            {
                                double z = Convert.ToDouble(pData[(k0 + k) * nDim + m]);
                                zMin = Math.Min(zMin, z);
                                zMax = Math.Max(zMax, z);
                            }
                        }
            }

            Debug.LogError("[zMin, zMax] = ["+ zMin + ", "+ zMax + "]");
        }
    }
}