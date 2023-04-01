// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class BuildingFeature : Feature
    {
        /// <summary>
        /// Building feature shapes
        /// </summary>
        public enum Shape
        {
            Extrusion,
            None,
            Cone,
            Dome,
            Pyramid,
            Sphere,
            Cylinder
        }

        /// <summary>
        /// Building feature roof shapes
        /// </summary>
        public enum RoofShape
        {
            None,
            Cone,
            Pyramid,
            Dome,
            Onion,
            Gabled,
            Hipped,
            HalfHipped,
            Skillion,
            Gambrel,
            Mansard,
            Round,
            Flat
        }

        [SerializeField, HideInInspector]
        private GeoCoordinateGeometries[] _geoCoordinateGeometries;

        [SerializeField, HideInInspector]
        private bool[] _hasRoofColor;
        [SerializeField, HideInInspector]
        private bool[] _hasWallColor;
        [SerializeField, HideInInspector]
        private bool[] _hasRoofDirection;
        [SerializeField, HideInInspector]
        private bool[] _hasHeight;
        [SerializeField, HideInInspector]
        private bool[] _hasMinLevel;
        [SerializeField, HideInInspector]
        private bool[] _hasLevels;

        [SerializeField, HideInInspector]
        private bool[] _materialIsGlass;

        [SerializeField, HideInInspector]
        private Color[] _roofColor;
        [SerializeField, HideInInspector]
        private Color[] _wallColor;

        [SerializeField, HideInInspector]
        private Shape[] _shape;
        [SerializeField, HideInInspector]
        private RoofShape[] _roofShape;
        [SerializeField, HideInInspector]
        private float[] _roofHeight;
        [SerializeField, HideInInspector]
        private int[] _roofLevels;
        [SerializeField, HideInInspector]
        private float[] _roofDirection;

        [SerializeField, HideInInspector]
        private float[] _minHeight;
        [SerializeField, HideInInspector]
        private float[] _height;
        [SerializeField, HideInInspector]
        private int[] _minLevel;
        [SerializeField, HideInInspector]
        private int[] _levels;

        public override void SetData(JSONNode json)
        {
            BuildingFeatureModifier buildingfeatureModifier = BuildingFeatureProcessingFunctions.CreatePropertyModifier<BuildingFeatureModifier>();
            if (buildingfeatureModifier != Disposable.NULL)
            {
                if (BuildingFeatureProcessingFunctions.ParseJSON(json, buildingfeatureModifier))
                    buildingfeatureModifier.ModifyProperties(this);

                DisposeManager.Dispose(buildingfeatureModifier);
            }
        }

        public void SetData(
            GeoCoordinateGeometries[] geoCoordinateGeometries,
            bool[] hasRoofColor,
            bool[] hasWallColor,
            bool[] hasRoofDirection,
            bool[] hasHeight,
            bool[] hasMinLevel,
            bool[] hasLevels,
            bool[] materialIsGlass,
            Color[] roofColor,
            Color[] wallColor,
            Shape[] shape,
            RoofShape[] roofShape,
            float[] roofHeight,
            int[] roofLevels,
            float[] roofDirection,
            float[] minHeight,
            float[] height,
            int[] minLevel,
            int[] levels)
        {
            _geoCoordinateGeometries = geoCoordinateGeometries;

            _hasRoofColor = hasRoofColor;
            _hasWallColor = hasWallColor;
            _hasRoofDirection = hasRoofDirection;
            _hasHeight = hasHeight;
            _hasMinLevel = hasMinLevel;
            _hasLevels = hasLevels;

            _materialIsGlass = materialIsGlass;

            _roofColor = roofColor;
            _wallColor = wallColor;

            _shape = shape;
            _roofShape = roofShape;
            _roofHeight = roofHeight;
            _roofLevels = roofLevels;
            _roofDirection = roofDirection;

            _minHeight = minHeight;
            _height = height;
            _minLevel = minLevel;
            _levels = levels;
        }

        public override int featureCount
        {
            get { return _geoCoordinateGeometries.Length; }
        }

        public GeoCoordinateGeometries GetGeoCoordinateGeometries(int index)
        {
            return _geoCoordinateGeometries[index];
        }

        public bool GetMaterialIsGlass(int index) { return _materialIsGlass[index]; }

        public Shape GetShape(int index) { return _shape[index]; }

        public RoofShape GetRoofShape(int index) { return _roofShape[index]; }

        public bool GetHasRoofDirection(int index) { return _hasRoofDirection[index]; }
        public float GetRoofDirection(int index) { return _roofDirection[index]; }

        public float GetRoofHeight(int index) { return _roofHeight[index]; }

        public int GetRoofLevels(int index) { return _roofLevels[index]; }

        public bool GetHasHeight(int index) { return _hasHeight[index]; }
        public float GetHeight(int index) { return _height[index]; }

        public float GetMinHeight(int index) { return _minHeight[index]; }

        public bool GetHasLevels(int index) { return _hasLevels[index]; }
        public int GetLevels(int index) { return _levels[index]; }

        public bool GetHasMinLevel(int index) { return _hasMinLevel[index]; }
        public int GetMinLevel(int index) { return _minLevel[index]; }

        public Color GetWallColor(int index, Color defaultColor) { return _hasWallColor[index] ? _wallColor[index] : defaultColor; }

        public Color GetRoofColor(int index, Color defaultColor) { return _hasRoofColor[index] ? _roofColor[index] : defaultColor; }

        protected override JSONObject GetDataJson()
        {
            JSONObject json = base.GetDataJson();

            json["geoCoordinateGeometries"] = JsonUtility.ToJson(_geoCoordinateGeometries);

            json["hasRoofColor"] = JsonUtility.ToJson(_hasRoofColor);
            json["hasWallColor"] = JsonUtility.ToJson(_hasWallColor);
            json["hasRoofDirection"] = JsonUtility.ToJson(_hasRoofDirection);
            json["hasHeight"] = JsonUtility.ToJson(_hasHeight);
            json["hasMinLevel"] = JsonUtility.ToJson(_hasMinLevel);
            json["hasLevels"] = JsonUtility.ToJson(_hasLevels);

            json["materialIsGlass"] = JsonUtility.ToJson(_materialIsGlass);

            json["roofColor"] = JsonUtility.ToJson(_roofColor);
            json["wallColor"] = JsonUtility.ToJson(_wallColor);

            json["shape"] = JsonUtility.ToJson(_shape);
            json["roofShape"] = JsonUtility.ToJson(_roofShape);
            json["roofHeight"] = JsonUtility.ToJson(_roofHeight);
            json["roofLevels"] = JsonUtility.ToJson(_roofLevels);
            json["roofDirection"] = JsonUtility.ToJson(_roofDirection);

            json["minHeight"] = JsonUtility.ToJson(_minHeight);
            json["height"] = JsonUtility.ToJson(_height);
            json["minLevel"] = JsonUtility.ToJson(_minLevel);
            json["levels"] = JsonUtility.ToJson(_levels);

            return json;
        }
    }

    public class BuildingFeatureProcessingFunctions : ProcessingFunctions
    {
        private static readonly Dictionary<string, string> MATERIAL_COLORS = new Dictionary<string, string>(){
                { "brick", "#cc7755" },
                { "bronze", "#ffeecc" },
                { "canvas", "#fff8f0" },
                { "concrete", "#999999" },
                { "copper", "#a0e0d0" },
                { "glass", "#e8f8f8" },
                { "gold", "#ffcc00" },
                { "plants", "#009933" },
                { "metal", "#aaaaaa" },
                { "panel", "#fff8f0" },
                { "plaster", "#999999" },
                { "roof_tiles", "#f08060" },
                { "silver", "#cccccc" },
                { "slate", "#666666" },
                { "stone", "#996666" },
                { "tar_paper", "#333333" },
                { "wood", "#deb887" }
              };

        private static readonly Dictionary<string, string> BASE_MATERIALS = new Dictionary<string, string>(){
                { "asphalt", "tar_paper" },
                { "bitumen", "tar_paper" },
                { "block", "stone" },
                { "bricks", "brick" },
                { "glas", "glass" },
                { "glassfront", "glass" },
                { "grass", "plants" },
                { "masonry", "stone" },
                { "granite", "stone" },
                { "panels", "panel" },
                { "paving_stones", "stone" },
                { "plastered", "plaster" },
                { "rooftiles", "roof_tiles" },
                { "roofingfelt", "tar_paper" },
                { "sandstone", "stone" },
                { "sheet", "canvas" },
                { "sheets", "canvas" },
                { "shingle", "tar_paper" },
                { "shingles", "tar_paper" },
                { "slates", "slate" },
                { "steel", "metal" },
                { "tar", "tar_paper" },
                { "tent", "canvas" },
                { "thatch", "plants" },
                { "tile", "roof_tiles" },
                { "tiles", "roof_tiles" }
                // cardboard
                // eternit
                // limestone
                // straw
              };

        public static bool ParseJSON(JSONNode json, FeatureModifier featureModifier, CancellationTokenSource cancellationTokenSource = null)
        {
            string typeName = nameof(PropertyScriptableObject.type);
            if (json[typeName] != null && json[typeName] == typeof(BuildingFeature).FullName)
            {
                BuildingFeatureModifier buildingFeatureModifier = featureModifier as BuildingFeatureModifier;

                JsonUtility.FromJson(out List<GeoCoordinateGeometries> geoCoordinateGeometries, json["geoCoordinateGeometries"]);

                JsonUtility.FromJson(out List<bool> hasRoofColor, json["hasRoofColor"]);
                JsonUtility.FromJson(out List<bool> hasWallColor, json["hasWallColor"]);
                JsonUtility.FromJson(out List<bool> hasRoofDirection, json["hasRoofDirection"]);
                JsonUtility.FromJson(out List<bool> hasHeight, json["hasHeight"]);
                JsonUtility.FromJson(out List<bool> hasMinLevel, json["hasMinLevel"]);
                JsonUtility.FromJson(out List<bool> hasLevels, json["hasLevels"]);

                JsonUtility.FromJson(out List<bool> materialIsGlass, json["materialIsGlass"]);

                JsonUtility.FromJson(out List<Color> roofColor, json["roofColor"]);
                JsonUtility.FromJson(out List<Color> wallColor, json["wallColor"]);

                JsonUtility.FromJson(out List<BuildingFeature.Shape> shape, json["shape"]);
                JsonUtility.FromJson(out List<BuildingFeature.RoofShape> roofShape, json["roofShape"]);
                JsonUtility.FromJson(out List<float> roofHeight, json["roofHeight"]);
                JsonUtility.FromJson(out List<int> roofLevels, json["roofLevels"]);
                JsonUtility.FromJson(out List<float> roofDirection, json["roofDirection"]);

                JsonUtility.FromJson(out List<float> minHeight, json["minHeight"]);
                JsonUtility.FromJson(out List<float> height, json["height"]);
                JsonUtility.FromJson(out List<int> minLevel, json["minLevel"]);
                JsonUtility.FromJson(out List<int> levels, json["levels"]);

                buildingFeatureModifier.Init(
                    geoCoordinateGeometries, 
                    hasRoofColor, 
                    hasWallColor, 
                    hasRoofDirection,
                    hasHeight,
                    hasMinLevel,
                    hasLevels,
                    materialIsGlass,
                    roofColor,
                    wallColor,
                    shape,
                    roofShape,
                    roofHeight,
                    roofLevels,
                    roofDirection,
                    minHeight,
                    height,
                    minLevel,
                    levels);
            }
            else if (json["features"] != null && json["features"].IsArray && json["features"].AsArray.Count != 0)
            {
                //OpenStreetMap format
                featureModifier.Init(json["features"].AsArray.Count);

                foreach (JSONNode featureJson in json["features"].AsArray)
                {
                    AddFeature(featureJson, featureModifier as BuildingFeatureModifier);

                    if (cancellationTokenSource != null)
                        cancellationTokenSource.ThrowIfCancellationRequested();
                }
            }

            return true;
        }

        private static void AddFeature(JSONNode featureJson, BuildingFeatureModifier buildingFeatureModifier)
        {
            JSONObject propertiesJson = featureJson["properties"].AsObject;

            //Material
            string material = null;
            if (!string.IsNullOrEmpty(propertiesJson["material"]))
                material = GetMaterialHexColor(propertiesJson["material"]);

            //Walls Color
            Color? wallColor = null;
            string wallColorStr = null;
            if (!string.IsNullOrEmpty(propertiesJson["wallColor"]))
                wallColorStr = propertiesJson["wallColor"];
            else if (!string.IsNullOrEmpty(propertiesJson["color"]))
                wallColorStr = propertiesJson["color"];
            else if (!string.IsNullOrEmpty(material))
                wallColorStr = material;
            if (!string.IsNullOrEmpty(wallColorStr))
            {
                Color parsedWallColor;
                if (OSMColors.Parse(out parsedWallColor, wallColorStr))
                    wallColor = parsedWallColor;
            }

            //Roof Color
            Color? roofColor = null;
            string roofColorStr = null;
            if (!string.IsNullOrEmpty(propertiesJson["roofColor"]))
                roofColorStr = propertiesJson["roofColor"];
            else if (!string.IsNullOrEmpty(propertiesJson["roofMaterial"]))
                roofColorStr = GetMaterialHexColor(propertiesJson["roofMaterial"]);
            if (!string.IsNullOrEmpty(roofColorStr))
            {
                Color parsedRoofColor;
                if (OSMColors.Parse(out parsedRoofColor, roofColorStr))
                    roofColor = parsedRoofColor;
            }

            //Roof
            string shape = null;
            if (!string.IsNullOrEmpty(propertiesJson["shape"]))
                shape = propertiesJson["shape"].Value;
            string roofShape = null;
            if (!string.IsNullOrEmpty(propertiesJson["roofShape"]))
                roofShape = propertiesJson["roofShape"].Value;

            float? roofDirection = null;
            if (propertiesJson["roofDirection"] != null)
                roofDirection = propertiesJson["roofDirection"];
            float roofHeight = 0.0f;
            if (propertiesJson["roofHeight"] != null)
                roofHeight = propertiesJson["roofHeight"];
            int roofLevels = 0;
            if (propertiesJson["roofLevels"] != null)
                roofLevels = propertiesJson["roofLevels"];

            //Levels/Height
            float? height = null;
            if (propertiesJson["height"] != null)
                height = propertiesJson["height"];
            float minHeight = 0.0f;
            if (propertiesJson["minHeight"] != null)
                minHeight = propertiesJson["minHeight"];
            int? levels = null;
            if (propertiesJson["levels"] != null)
                levels = propertiesJson["levels"];
            int? minLevel = null;
            if (propertiesJson["minLevel"] != null)
                minLevel = propertiesJson["minLevel"];

            buildingFeatureModifier.Add(ParseGeometry(featureJson["geometry"]), material == "glass", roofColor, wallColor, ParseShape(shape), ParseRoofShape(roofShape), roofDirection, roofHeight, roofLevels, height, minHeight, levels, minLevel);
        }

        private static GeoCoordinateGeometries ParseGeometry(JSONNode geometryJson)
        {
            GeoCoordinateGeometry[] geoCoordinateGeometries = null;

            JSONArray coordinatesJson = geometryJson["coordinates"].AsArray;

            switch (geometryJson["type"].Value)
            {
                case "MultiPolygon":
                    geoCoordinateGeometries = new GeoCoordinateGeometry[coordinatesJson.Count];
                    for (int i = 0; i < coordinatesJson.Count; i++)
                        geoCoordinateGeometries[i] = GetGeometryFromCoordinates(coordinatesJson[i].AsArray);
                    break;
                case "Polygon":
                    geoCoordinateGeometries = new GeoCoordinateGeometry[1];
                    geoCoordinateGeometries[0] = GetGeometryFromCoordinates(coordinatesJson.AsArray);
                    break;
            }

            return new GeoCoordinateGeometries(geoCoordinateGeometries);
        }

        private static GeoCoordinateGeometry GetGeometryFromCoordinates(JSONArray polygonsJson)
        {
            GeoCoordinatePolygon[] polygons = new GeoCoordinatePolygon[polygonsJson.Count];

            for (int i = 0; i < polygonsJson.Count; i++)
            {
                JSONArray polygonJSON = polygonsJson[i].AsArray;

                GeoCoordinatePolygon polygon = new GeoCoordinatePolygon(new double[polygonJSON.Count * 2]);
                foreach (JSONNode point in polygonJSON)
                    polygon.Add(point[1], point[0]);

                // outer ring (first ring) needs to be clockwise, inner rings
                // counter-clockwise. If they are not, make them by reverting order.
                if ((i == 0) != IsClockWise(polygon))
                    polygon.Reverse();

                polygons[i] = polygon;
            }

            return new GeoCoordinateGeometry(polygons);
        }

        private static bool IsClockWise(GeoCoordinatePolygon polygon)
        {
            double a = 0.0d;
            int count = polygon.Count / 2;
            for (int i = 0; i < count; i++)
            {
                int index = i * 2;
                double latitude = polygon[index];
                double longitude = polygon[index + 1];
                a += (i < count - 1) ? (polygon[index + 3] - longitude) * (polygon[index + 2] + latitude) : 0.0d;
            }
            return 0.0d < a;
        }

        private static string GetMaterialHexColor(string str)
        {
            str = str.ToLower();
            if (str[0] == '#')
                return str;

            string baseMaterial;
            if (!BASE_MATERIALS.TryGetValue(str, out baseMaterial))
                baseMaterial = str;

            string materialColor;
            MATERIAL_COLORS.TryGetValue(baseMaterial, out materialColor);

            return materialColor;
        }


        public static BuildingFeature.Shape ParseShape(string str)
        {
            BuildingFeature.Shape shape = BuildingFeature.Shape.Extrusion;
            switch (str)
            {
                case "none":
                    shape = BuildingFeature.Shape.None;
                    break;
                case "cone":
                    shape = BuildingFeature.Shape.Cone;
                    break;
                case "dome":
                    shape = BuildingFeature.Shape.Dome;
                    break;
                case "pyramid":
                    shape = BuildingFeature.Shape.Pyramid;
                    break;
                case "sphere":
                    shape = BuildingFeature.Shape.Sphere;
                    break;
                case "cylinder":
                    shape = BuildingFeature.Shape.Cylinder;
                    break;
            }
            return shape;
        }

        public static BuildingFeature.RoofShape ParseRoofShape(string str)
        {
            BuildingFeature.RoofShape roofshape = BuildingFeature.RoofShape.None;
            switch (str)
            {
                case "cone":
                    roofshape = BuildingFeature.RoofShape.Cone;
                    break;
                case "pyramid":
                    roofshape = BuildingFeature.RoofShape.Pyramid;
                    break;
                case "dome":
                    roofshape = BuildingFeature.RoofShape.Dome;
                    break;
                case "onion":
                    roofshape = BuildingFeature.RoofShape.Onion;
                    break;
                case "gabled":
                    roofshape = BuildingFeature.RoofShape.Gabled;
                    break;
                case "hipped":
                    roofshape = BuildingFeature.RoofShape.Hipped;
                    break;
                case "half-hipped":
                    roofshape = BuildingFeature.RoofShape.HalfHipped;
                    break;
                case "skillion":
                    roofshape = BuildingFeature.RoofShape.Skillion;
                    break;
                case "gambrel":
                    roofshape = BuildingFeature.RoofShape.Gambrel;
                    break;
                case "mansard":
                    roofshape = BuildingFeature.RoofShape.Mansard;
                    break;
                case "round":
                    roofshape = BuildingFeature.RoofShape.Round;
                    break;
                case "flat":
                    roofshape = BuildingFeature.RoofShape.Flat;
                    break;
            }
            return roofshape;
        }
    }

    [Serializable]
    public class BuildingFeatureModifier : FeatureModifier
    {
        [SerializeField]
        private List<GeoCoordinateGeometries> _geoCoordinateGeometries;

        [SerializeField]
        private List<bool> _hasRoofColor;
        [SerializeField]
        private List<bool> _hasWallColor;
        [SerializeField]
        private List<bool> _hasRoofDirection;
        [SerializeField]
        private List<bool> _hasHeight;
        [SerializeField]
        private List<bool> _hasMinLevel;
        [SerializeField]
        private List<bool> _hasLevels;

        [SerializeField]
        private List<bool> _materialIsGlass;

        [SerializeField]
        private List<Color> _roofColor;
        [SerializeField]
        private List<Color> _wallColor;

        [SerializeField]
        private List<BuildingFeature.Shape> _shape;
        [SerializeField]
        private List<BuildingFeature.RoofShape> _roofShape;
        [SerializeField]
        private List<float> _roofHeight;
        [SerializeField]
        private List<int> _roofLevels;
        [SerializeField]
        private List<float> _roofDirection;

        [SerializeField]
        private List<float> _minHeight;
        [SerializeField]
        private List<float> _height;
        [SerializeField]
        private List<int> _minLevel;
        [SerializeField]
        private List<int> _levels;

        public override void Recycle()
        {
            base.Recycle();

            _geoCoordinateGeometries.Clear();

            _hasRoofColor.Clear();
            _hasWallColor.Clear();
            _hasRoofDirection.Clear();
            _hasHeight.Clear();
            _hasMinLevel.Clear();
            _hasLevels.Clear();

            _materialIsGlass.Clear();

            _roofColor.Clear();
            _wallColor.Clear();

            _shape.Clear();
            _roofShape.Clear();
            _roofHeight.Clear();
            _roofLevels.Clear();
            _roofDirection.Clear();

            _minHeight.Clear();
            _height.Clear();
            _minLevel.Clear();
            _levels.Clear();
        }

        public override void Initializing()
        {
            base.Initializing();

            _geoCoordinateGeometries ??= new List<GeoCoordinateGeometries>();

            _hasRoofColor ??= new();
            _hasWallColor ??= new();
            _hasRoofDirection ??= new();
            _hasHeight ??= new();
            _hasMinLevel ??= new();
            _hasLevels ??= new();

            _materialIsGlass ??= new();

            _roofColor ??= new();
            _wallColor ??= new();

            _shape ??= new();
            _roofShape ??= new();
            _roofHeight ??= new();
            _roofLevels ??= new();
            _roofDirection ??= new();

            _minHeight ??= new();
            _height ??= new();
            _minLevel ??= new();
            _levels ??= new();
        }

        public override FeatureModifier Init(int featureCount)
        {
            base.Init(featureCount);

            _geoCoordinateGeometries.Capacity = featureCount;

            _hasRoofColor.Capacity = featureCount;
            _hasWallColor.Capacity = featureCount;
            _hasRoofDirection.Capacity = featureCount;
            _hasHeight.Capacity = featureCount;
            _hasMinLevel.Capacity = featureCount;
            _hasLevels.Capacity = featureCount;

            _materialIsGlass.Capacity = featureCount;

            _roofColor.Capacity = featureCount;
            _wallColor.Capacity = featureCount;

            _shape.Capacity = featureCount;
            _roofShape.Capacity = featureCount;
            _roofHeight.Capacity = featureCount;
            _roofLevels.Capacity = featureCount;
            _roofDirection.Capacity = featureCount;

            _minHeight.Capacity = featureCount;
            _height.Capacity = featureCount;
            _minLevel.Capacity = featureCount;
            _levels.Capacity = featureCount;

            return this;
        }

        public FeatureModifier Init(
            List<GeoCoordinateGeometries> geoCoordinateGeometries,
            List<bool> hasRoofColor,
            List<bool> hasWallColor,
            List<bool> hasRoofDirection,
            List<bool> hasHeight,
            List<bool> hasMinLevel,
            List<bool> hasLevels,
            List<bool> materialIsGlass,
            List<Color> roofColor,
            List<Color> wallColor,
            List<BuildingFeature.Shape> shape,
            List<BuildingFeature.RoofShape> roofShape,
            List<float> roofHeight,
            List<int> roofLevels,
            List<float> roofDirection,
            List<float> minHeight,
            List<float> height,
            List<int> minLevel,
            List<int> levels)
        {
            base.Init(geoCoordinateGeometries.Count);

            _geoCoordinateGeometries = geoCoordinateGeometries;

            _hasRoofColor = hasRoofColor;
            _hasWallColor = hasWallColor;
            _hasRoofDirection = hasRoofDirection;
            _hasHeight = hasHeight;
            _hasMinLevel = hasMinLevel;
            _hasLevels = hasLevels;

            _materialIsGlass = materialIsGlass;

            _roofColor = roofColor;
            _wallColor = wallColor;

            _shape = shape;
            _roofShape = roofShape;
            _roofHeight = roofHeight;
            _roofLevels = roofLevels;
            _roofDirection = roofDirection;

            _minHeight = minHeight;
            _height = height;
            _minLevel = minLevel;
            _levels = levels;

            return this;
        }

        public void Add(GeoCoordinateGeometries geoCoordinateGeometries, bool materialIsGlass, Color? roofColor, Color? wallColor, BuildingFeature.Shape shape, BuildingFeature.RoofShape roofShape, float? roofDirection, float roofHeight, int roofLevels, float? height, float minHeight, int? levels, int? minLevel)
        {
            _geoCoordinateGeometries.Add(geoCoordinateGeometries);

            _hasRoofColor.Add(roofColor.HasValue);
            _hasWallColor.Add(wallColor.HasValue);
            _hasRoofDirection.Add(roofDirection.HasValue);
            _hasHeight.Add(height.HasValue);
            _hasMinLevel.Add(minLevel.HasValue);
            _hasLevels.Add(levels.HasValue);

            _materialIsGlass.Add(materialIsGlass);

            _roofColor.Add(roofColor.HasValue ? roofColor.Value : Color.black);
            _wallColor.Add(wallColor.HasValue ? wallColor.Value : Color.black);

            _shape.Add(shape);
            _roofShape.Add(roofShape);
            _roofHeight.Add(roofHeight);
            _roofLevels.Add(roofLevels);
            _roofDirection.Add(roofDirection.HasValue ? roofDirection.Value : 0.0f);

            _minHeight.Add(minHeight);
            _height.Add(height.HasValue ? height.Value : 0.0f);
            _minLevel.Add(minLevel.HasValue ? minLevel.Value : 0);
            _levels.Add(levels.HasValue ? levels.Value : 0);
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            BuildingFeature buildingFeature = scriptableBehaviour as BuildingFeature;
           
            buildingFeature.SetData(
                _geoCoordinateGeometries.ToArray(),
                _hasRoofColor.ToArray(),
                _hasWallColor.ToArray(),
                _hasRoofDirection.ToArray(),
                _hasHeight.ToArray(),
                _hasMinLevel.ToArray(),
                _hasLevels.ToArray(),
                _materialIsGlass.ToArray(),
                _roofColor.ToArray(),
                _wallColor.ToArray(),
                _shape.ToArray(),
                _roofShape.ToArray(),
                _roofHeight.ToArray(),
                _roofLevels.ToArray(),
                _roofDirection.ToArray(),
                _minHeight.ToArray(),
                _height.ToArray(),
                _minLevel.ToArray(),
                _levels.ToArray());
        }
    }
}
