// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/" + nameof(GridGenerator))]
    public class GridGenerator : GeneratorBase
    {
        /// <summary>
        /// The different tile shapes. <br/><br/>
        /// <b><see cref="Rectangle"/>:</b> <br/>
        /// Rectangular tile.
        /// </summary>
        public enum TileShape
        {
            Rectangle
        }

        [BeginFoldout("Tiles")]
        [SerializeField, Tooltip("The shape of the tiles.")]
        private TileShape _tileShape;
        [SerializeField, Tooltip("The horizontal and vertical size of the grid.")]
        private Vector2Int _grid2DDimensions;
#if UNITY_EDITOR
        [SerializeField, Button(nameof(GenerateGridBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Generate a grid based on the specified parameters."), EndFoldout]
        private bool _generate;

        public void GenerateGridBtn()
        {
            GenerateGrid(tileShape, grid2DDimensions);
        }
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => tileShape = value, TileShape.Rectangle, initializingContext);
            InitValue(value => grid2DDimensions = Vector2Int.one, 16, initializingContext);
        }

        /// <summary>
        /// The shape of the tiles.
        /// </summary>
        [Json]
        public TileShape tileShape
        {
            get { return _tileShape; }
            set { SetValue(nameof(tileShape), value, ref _tileShape); }
        }

        /// <summary>
        /// The horizontal and vertical size of the grid.
        /// </summary>
        [Json]
        public Vector2Int grid2DDimensions
        {
            get { return _grid2DDimensions; }
            set 
            {
                if (value.x < 1)
                    value.x = 1;
                if (value.y < 1)
                    value.y = 1;
                SetValue(nameof(grid2DDimensions), value, ref _grid2DDimensions); 
            }
        }

        /// <summary>
        /// Generate a grid based on the specified parameters.
        /// </summary>
        /// <param name="tileShape"></param>
        /// <param name="grid2DDimensions"></param>
        public void GenerateGrid(TileShape tileShape, Vector2Int grid2DDimensions)
        {
            FallbackValues persistentFallbackValues = null;

            IterateOverFallbackValues<PersistentMonoBehaviour, PersistentScriptableObject>((fallbackValues) => 
            {
                if (fallbackValues.GetProperty(out bool createPersistentIfMissing, nameof(PersistentMonoBehaviour.createPersistentIfMissing)) && createPersistentIfMissing)
                {
                    persistentFallbackValues = fallbackValues;
                    return false;
                }
                return true;
            });

            if (persistentFallbackValues != Disposable.NULL)
            {
                Type fallbackType = persistentFallbackValues.GetFallbackValuesType();

                if (fallbackType != null)
                {
                    switch (tileShape)
                    {
                        case TileShape.Rectangle:

                            GeoAstroObject geoAstroObject = objectBase is GeoAstroObject ? objectBase as GeoAstroObject : transform.parentGeoAstroObject;
                            if (geoAstroObject != Disposable.NULL)
                            {
                                grid2DDimensions = new Vector2Int(Mathf.ClosestPowerOfTwo(grid2DDimensions.x), Mathf.ClosestPowerOfTwo(grid2DDimensions.y));

                                for (int y = 0; y < grid2DDimensions.y; y++)
                                {
                                    for (int x = 0; x < grid2DDimensions.x; x++)
                                    {
                                        JSONObject jsonTransform = new()
                                        {
                                            [nameof(TransformBase.parent)] = transform.id.ToString()
                                        };

                                        GeoCoordinate3Double geoCoordinate = MathPlus.GetGeoCoordinate3FromIndex(new Vector2Double(x, y) + MathPlus.HALF_INDEX, grid2DDimensions);

                                        if (objectBase.transform.isGeoCoordinateTransform)
                                            jsonTransform[nameof(TransformDouble.geoCoordinate)] = JsonUtility.ToJson(geoCoordinate);
                                        else
                                            jsonTransform[nameof(TransformDouble.localPosition)] = JsonUtility.ToJson(transform.parent.InverseTransformPoint(geoAstroObject.GetPointFromGeoCoordinate(geoCoordinate)));

                                        QuaternionDouble rotation = geoAstroObject.GetUpVectorFromGeoCoordinate(geoCoordinate);
                                        jsonTransform[nameof(TransformDouble.localRotation)] = JsonUtility.ToJson(QuaternionDouble.Inverse(transform.rotation) * rotation);

                                        JSONObject json = new()
                                        {
                                            [nameof(IGrid2DIndex.grid2DDimensions)] = JsonUtility.ToJson(grid2DDimensions),
                                            [nameof(Object.transform)] = jsonTransform
                                        };

                                        List<PropertyModifier> propertyModifiers = null;

                                        if (seed != -1)
                                        {
                                            PropertyModifierIndex2DParameters propertyModifierIndex2DParameters = Processor.GetParametersInstance(typeof(PropertyModifierIndex2DParameters)) as PropertyModifierIndex2DParameters;
                                            propertyModifierIndex2DParameters.Init(fallbackType, json, SerializableGuid.Empty, seed);
                                            propertyModifierIndex2DParameters.Init(new Vector2Int(x, y), grid2DDimensions);

                                            PropertyModifier propertyModifier = PropertyModifierDataProcessingFunctions.GetProceduralPropertyModifier(fallbackType, propertyModifierIndex2DParameters);

                                            DisposeManager.Dispose(propertyModifierIndex2DParameters);

                                            if (propertyModifier != Disposable.NULL)
                                            {
                                                propertyModifiers = new List<PropertyModifier>
                                                {
                                                    propertyModifier
                                                };
                                            }
                                        }

                                        persistentFallbackValues.ApplyFallbackValuesToJson(json);

                                        GeneratePersistent(fallbackType, json, propertyModifiers);
                                    }
                                }
                            }

                            break;
                        //TODO: Add Support for Truncated Icosahedron
                        default:
                            break;
                    }
                }
            }
        }
    }
}
