// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Utility methods to help manage layers.
    /// </summary>
    public class LayerUtility
    {
        /// <summary>
        /// Create a new layer with the given name if it does not exists (Editor Only).
        /// </summary>
        /// <param name="name">The name of the new layer.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void CreateLayer(string name)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentNullException("name", "New layer name string is either null or empty.");

            UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            UnityEditor.SerializedProperty layerProps = tagManager.FindProperty("layers");
            int propCount = layerProps.arraySize;

            UnityEditor.SerializedProperty firstEmptyProp = null;

            for (var i = 0; i < propCount; i++)
            {
                var layerProp = layerProps.GetArrayElementAtIndex(i);

                var stringValue = layerProp.stringValue;

                if (stringValue == name) return;

                if (i < 8 || stringValue != string.Empty) continue;

                if (firstEmptyProp == null)
                    firstEmptyProp = layerProp;
            }

            if (firstEmptyProp == null)
            {
                Debug.LogError("Maximum limit of " + propCount + " layers exceeded. Layer \"" + name + "\" not created.");
                return;
            }

            firstEmptyProp.stringValue = name;
            tagManager.ApplyModifiedProperties();
#endif
        }

        /// <summary>
        /// Returns a layer or create a new one if it does not already exist (The layer will only be created in the Editor).
        /// </summary>
        /// <param name="name">The name of the layer.</param>
        /// <returns>The layer int value.</returns>
        public static int GetLayer(string name)
        {
            CreateLayerIfMissing(name);
            return LayerMask.NameToLayer(name);
        }

        /// <summary>
        /// Convert a layer index value to layer mask.
        /// </summary>
        /// <param name="index">A layer index from 0-31.</param>
        /// <returns>A layer mask value.</returns>
        public static int GetLayerMaskFromLayerIndex(int index)
        {
            return 1 << index;
        }

        private static void CreateLayerIfMissing(string layerMaskName)
        {
            if (LayerMask.NameToLayer(layerMaskName) == -1)
                CreateLayer(layerMaskName);
        }

        public static int GetDefaultLayer(Type type)
        {
            return GetLayer(GetDefaultLayerName(type));
        }

        public static string GetDefaultLayerName(Type type)
        {
            string name = "Default";

            Type uiType = typeof(UIBase);
            if (uiType.IsAssignableFrom(type))
                name = "UI";
            else if (type == typeof(TerrainGridMeshObject))
                name = typeof(TerrainGridMeshObject).Name;
            else if (type == typeof(BuildingGridMeshObject))
                name = typeof(BuildingGridMeshObject).Name;
            else if (type == typeof(AtmosphereGridMeshObject))
                name = typeof(AtmosphereGridMeshObject).Name;
            else if (type == typeof(LevelMeshObject))
                name = typeof(LevelMeshObject).Name;

            return name;
        }
    }
}