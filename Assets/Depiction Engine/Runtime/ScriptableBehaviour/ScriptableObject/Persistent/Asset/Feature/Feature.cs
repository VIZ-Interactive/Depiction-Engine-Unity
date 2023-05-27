// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DepictionEngine
{
    public class Feature : AssetBase
    {
#if UNITY_EDITOR
        protected override LoaderBase.DataType GetDataTypeFromExtension(string extension)
        {
            return LoaderBase.DataType.Json;
        }
#endif

        public virtual int featureCount
        {
            get => 0;
        }

        public override void SetData(object value, LoaderBase.DataType dataType, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            SetData(JSONObject.Parse(Encoding.Default.GetString(value as byte[])));

            DataPropertyAssigned();
        }

        public virtual void SetData(JSONNode json)
        {

        }

        protected override byte[] GetDataBytes(LoaderBase.DataType dataType)
        {
            if (JsonUtility.FromJson(out string jsonStr, GetDataJson()))
                return Encoding.ASCII.GetBytes(jsonStr);
            return new byte[0];
        }

        protected virtual JSONObject GetDataJson()
        {
            JSONObject json = new()
            {
                ["type"] = GetType().FullName
            };

            return json;
        }

        protected override string GetFileExtension()
        {
            return "json";
        }
    }

    /// <summary>
    /// A list of <see cref="DepictionEngine.GeoCoordinateGeometry"/>.
    /// </summary>
    [Serializable]
    public struct GeoCoordinateGeometries
    {
        [SerializeField]
        public GeoCoordinateGeometry[] geometries;

        public GeoCoordinateGeometries(GeoCoordinateGeometry[] geometries)
        {
            this.geometries = geometries;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < geometries.Length; i++)
            {
                sb.Append(geometries[i]);
                if (i < geometries.Length - 1)
                    sb.Append(", ");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A geometry composed of polygons.
    /// </summary>
    [Serializable]
    public struct GeoCoordinateGeometry
    {
        [SerializeField]
        private GeoCoordinatePolygon[] _polygons;

        [SerializeField]
        private int _currentIndex;

        public GeoCoordinateGeometry(GeoCoordinatePolygon[] polygons)
        {
            _polygons = polygons;
            _currentIndex = 0;
        }

        public GeoCoordinatePolygon[] polygons { get { return _polygons; } }

        public void Add(GeoCoordinatePolygon polygon)
        {
            _polygons[_currentIndex] = polygon;
            _currentIndex += 2;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < _polygons.Length; i++)
            {
                sb.Append(_polygons[i]);
                if (i < _polygons.Length - 1)
                    sb.Append(", ");
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A geometry composed of polygons.
    /// </summary>
    [Serializable]
    public struct VectorGeometry
    {
        [SerializeField]
        private SphericalBBox _bbox;

        [SerializeField]
        private VectorPolygon[] _polygons;

        [SerializeField]
        private int _currentIndex;

        [SerializeField]
        private bool _bboxInit;

        public VectorGeometry(VectorPolygon[] polygons)
        {
            _bbox = SphericalBBox.empty;
            _polygons = polygons;
            _currentIndex = 0;

            _bboxInit = false;
        }

        public void CalculateBoundingBox()
        {
            _bbox = new SphericalBBox(_polygons[0]);
        }

        public SphericalBBox bbox
        {
            get
            {
                if (!_bboxInit)
                {
                    _bboxInit = true;
                    _bbox = new SphericalBBox(_polygons[0]);
                }
                return _bbox;
            }
        }

        public VectorPolygon[] polygons  { get => _polygons; }

        public Vector2 center { get => bbox.center; }

        public float radius { get => bbox.radius; }

        public void Add(VectorPolygon value)
        {
            _polygons[_currentIndex] = value;
            _currentIndex++;
        }
    }

    /// <summary>
    /// A polygon described by GeoCoordinate points.
    /// </summary>
    [Serializable]
    public struct GeoCoordinatePolygon
    {
        [SerializeField]
        public double[] latitudeLongitude;
        [SerializeField]
        private int _currentIndex;

        public GeoCoordinatePolygon(double[] latitudeLongitude)
        {
            this.latitudeLongitude = latitudeLongitude;
            _currentIndex = 0;
        }

        public double this[int index]
        {
            get { return latitudeLongitude[index]; }
        }

        public int Count
        {
            get { return latitudeLongitude.Length; }
        }

        public void Add(double latitude, double longitude)
        {
            latitudeLongitude[_currentIndex] = latitude;
            latitudeLongitude[_currentIndex + 1] = longitude;
            _currentIndex += 2;
        }

        public void Reverse()
        {
            Array.Reverse(latitudeLongitude);
            for (int i = 0; i < latitudeLongitude.Length / 2; i++)
            {
                int index = i * 2;
                (latitudeLongitude[index + 1], latitudeLongitude[index]) = (latitudeLongitude[index], latitudeLongitude[index + 1]);
            }
        }

        public void IterateOverGeoCoordinates(Action<double, double> func)
        {
            if (func != null)
            {
                for (int i = 0; i < Count / 2; i++)
                {
                    int index = i * 2;
                    func(latitudeLongitude[index], latitudeLongitude[index + 1]);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < latitudeLongitude.Length; i += 2)
            {
                sb.Append("Lat: ");
                sb.Append(latitudeLongitude[i]);
                sb.Append(", Lon: ");
                sb.Append(latitudeLongitude[i + 1]);
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// A polygon described by 3D vector points.
    /// </summary>
    [Serializable]
    public struct VectorPolygon
    {
        [SerializeField]
        private List<Vector3> _vectors;

        public VectorPolygon(List<Vector3> vectors)
        {
            _vectors = vectors;
        }

        public List<Vector3> vectors { get { return _vectors; } }
    }

    /// <summary>
    /// Spherical bounding box.
    /// </summary>
    [Serializable]
    public struct SphericalBBox
    {
        public static SphericalBBox empty = new(new VectorPolygon(new List<Vector3>()));
      
        [SerializeField]
        private Vector2 _center;
        [SerializeField]
        private float _radius;

        public SphericalBBox(VectorPolygon polygon)
        {
            float
                x = float.PositiveInfinity, y = float.PositiveInfinity,
                X = float.NegativeInfinity, Y = float.NegativeInfinity;

            for (int i = 0; i < polygon.vectors.Count; i++)
            {
                x = Mathf.Min(x, polygon.vectors[i].x);
                y = Mathf.Min(y, polygon.vectors[i].y);

                X = Mathf.Max(X, polygon.vectors[i].x);
                Y = Mathf.Max(Y, polygon.vectors[i].y);
            }

            _center = new Vector2(x + (X - x) / 2.0f, y + (Y - y) / 2.0f);
            _radius = (float)(X - x) / 2.0f;
        }

        public Vector2 center { get => _center; }

        public float radius { get => _radius; }
    }

    /// <summary>
    /// A 3D segment composed of a start and end point.
    /// </summary>
    [Serializable]
    public struct Segment
    {
        [SerializeField]
        public Vector3 start;
        [SerializeField]
        public Vector3 end;

        public Segment(Vector3 start, Vector3 end)
        {
            this.start = start;
            this.end = end;
        }
    }

    public class OSMColors
    {
        private static Dictionary<string, string> w3cColors = new()
            {
                { "aliceblue", "#f0f8ff" },
                { "antiquewhite", "#faebd7" },
                { "aqua", "#00ffff" },
                { "aquamarine", "#7fffd4" },
                { "azure", "#f0ffff" },
                { "beige", "#f5f5dc" },
                { "bisque", "#ffe4c4" },
                { "black", "#000000" },
                { "blanchedalmond", "#ffebcd" },
                { "blue", "#0000ff" },
                { "blueviolet", "#8a2be2" },
                { "brown", "#a52a2a" },
                { "burlywood", "#deb887" },
                { "cadetblue", "#5f9ea0" },
                { "chartreuse", "#7fff00" },
                { "chocolate", "#d2691e" },
                { "coral", "#ff7f50" },
                { "cornflowerblue", "#6495ed" },
                { "cornsilk", "#fff8dc" },
                { "crimson", "#dc143c" },
                { "cyan", "#00ffff" },
                { "darkblue", "#00008b" },
                { "darkcyan", "#008b8b" },
                { "darkgoldenrod", "#b8860b" },
                { "darkgray", "#a9a9a9" },
                { "darkgrey", "#a9a9a9" },
                { "darkgreen", "#006400" },
                { "darkkhaki", "#bdb76b" },
                { "darkmagenta", "#8b008b" },
                { "darkolivegreen", "#556b2f" },
                { "darkorange", "#ff8c00" },
                { "darkorchid", "#9932cc" },
                { "darkred", "#8b0000" },
                { "darksalmon", "#e9967a" },
                { "darkseagreen", "#8fbc8f" },
                { "darkslateblue", "#483d8b" },
                { "darkslategray", "#2f4f4f" },
                { "darkslategrey", "#2f4f4f" },
                { "darkturquoise", "#00ced1" },
                { "darkviolet", "#9400d3" },
                { "deeppink", "#ff1493" },
                { "deepskyblue", "#00bfff" },
                { "dimgray", "#696969" },
                { "dimgrey", "#696969" },
                { "dodgerblue", "#1e90ff" },
                { "firebrick", "#b22222" },
                { "floralwhite", "#fffaf0" },
                { "forestgreen", "#228b22" },
                { "fuchsia", "#ff00ff" },
                { "gainsboro", "#dcdcdc" },
                { "ghostwhite", "#f8f8ff" },
                { "gold", "#ffd700" },
                { "goldenrod", "#daa520" },
                { "gray", "#808080" },
                { "grey", "#808080" },
                { "green", "#008000" },
                { "greenyellow", "#adff2f" },
                { "honeydew", "#f0fff0" },
                { "hotpink", "#ff69b4" },
                { "indianred", "#cd5c5c" },
                { "indigo", "#4b0082" },
                { "ivory", "#fffff0" },
                { "khaki", "#f0e68c" },
                { "lavender", "#e6e6fa" },
                { "lavenderblush", "#fff0f5" },
                { "lawngreen", "#7cfc00" },
                { "lemonchiffon", "#fffacd" },
                { "lightblue", "#add8e6" },
                { "lightcoral", "#f08080" },
                { "lightcyan", "#e0ffff" },
                { "lightgoldenrodyellow", "#fafad2" },
                { "lightgray", "#d3d3d3" },
                { "lightgrey", "#d3d3d3" },
                { "lightgreen", "#90ee90" },
                { "lightpink", "#ffb6c1" },
                { "lightsalmon", "#ffa07a" },
                { "lightseagreen", "#20b2aa" },
                { "lightskyblue", "#87cefa" },
                { "lightslategray", "#778899" },
                { "lightslategrey", "#778899" },
                { "lightsteelblue", "#b0c4de" },
                { "lightyellow", "#ffffe0" },
                { "lime", "#00ff00" },
                { "limegreen", "#32cd32" },
                { "linen", "#faf0e6" },
                { "magenta", "#ff00ff" },
                { "maroon", "#800000" },
                { "mediumaquamarine", "#66cdaa" },
                { "mediumblue", "#0000cd" },
                { "mediumorchid", "#ba55d3" },
                { "mediumpurple", "#9370db" },
                { "mediumseagreen", "#3cb371" },
                { "mediumslateblue", "#7b68ee" },
                { "mediumspringgreen", "#00fa9a" },
                { "mediumturquoise", "#48d1cc" },
                { "mediumvioletred", "#c71585" },
                { "midnightblue", "#191970" },
                { "mintcream", "#f5fffa" },
                { "mistyrose", "#ffe4e1" },
                { "moccasin", "#ffe4b5" },
                { "navajowhite", "#ffdead" },
                { "navy", "#000080" },
                { "oldlace", "#fdf5e6" },
                { "olive", "#808000" },
                { "olivedrab", "#6b8e23" },
                { "orange", "#ffa500" },
                { "orangered", "#ff4500" },
                { "orchid", "#da70d6" },
                { "palegoldenrod", "#eee8aa" },
                { "palegreen", "#98fb98" },
                { "paleturquoise", "#afeeee" },
                { "palevioletred", "#db7093" },
                { "papayawhip", "#ffefd5" },
                { "peachpuff", "#ffdab9" },
                { "peru", "#cd853f" },
                { "pink", "#ffc0cb" },
                { "plum", "#dda0dd" },
                { "powderblue", "#b0e0e6" },
                { "purple", "#800080" },
                { "rebeccapurple", "#663399" },
                { "red", "#ff0000" },
                { "rosybrown", "#bc8f8f" },
                { "royalblue", "#4169e1" },
                { "saddlebrown", "#8b4513" },
                { "salmon", "#fa8072" },
                { "sandybrown", "#f4a460" },
                { "seagreen", "#2e8b57" },
                { "seashell", "#fff5ee" },
                { "sienna", "#a0522d" },
                { "silver", "#c0c0c0" },
                { "skyblue", "#87ceeb" },
                { "slateblue", "#6a5acd" },
                { "slategray", "#708090" },
                { "slategrey", "#708090" },
                { "snow", "#fffafa" },
                { "springgreen", "#00ff7f" },
                { "steelblue", "#4682b4" },
                { "tan", "#d2b48c" },
                { "teal", "#008080" },
                { "thistle", "#d8bfd8" },
                { "tomato", "#ff6347" },
                { "turquoise", "#40e0d0" },
                { "violet", "#ee82ee" },
                { "wheat", "#f5deb3" },
                { "white", "#ffffff" },
                { "whitesmoke", "#f5f5f5" },
                { "yellow", "#ffff00" },
                { "yellowgreen", "#9acd32" }
            };

        public static bool Parse(out Color color, string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                str = str.ToLower();
                if (str[0] != '#' && w3cColors.TryGetValue(str, out string w3cColor))
                    str = w3cColor;

                if (!string.IsNullOrEmpty(str))
                {
                    Match m = Regex.Match(str, @"^#?(\w{2})(\w{2})(\w{2})$");

                    if (m.Success)
                    {
                        color = new Color(Convert.ToInt32(m.Groups[1].Value, 16) / 255.0f, Convert.ToInt32(m.Groups[2].Value, 16) / 255.0f, Convert.ToInt32(m.Groups[3].Value, 16) / 255.0f);
                        return true;
                    }

                    m = Regex.Match(str, @"^#?(\w)(\w)(\w)$");
                    if (m.Success)
                    {
                        color = new Color(Convert.ToInt32(m.Groups[1].Value + m.Groups[1].Value, 16) / 255.0f, Convert.ToInt32(m.Groups[2].Value + m.Groups[2].Value, 16) / 255.0f, Convert.ToInt32(m.Groups[3].Value + m.Groups[3].Value, 16) / 255.0f);
                        return true;
                    }

                    m = Regex.Match(str, @"rgba?\((\d+)\D+(\d+)\D+(\d+)(\D+([\d.]+))?\)");
                    if (m.Success)
                    {
                        color = new Color(
                        float.Parse(m.Groups[1].Value) / 255,
                        float.Parse(m.Groups[2].Value) / 255,
                        float.Parse(m.Groups[3].Value) / 255,
                        !string.IsNullOrEmpty(m.Groups[4].Value) ? float.Parse(m.Groups[5].Value) : 1
                        );
                        return true;
                    }
                }
            }
            color = Color.black;
            return false;
        }
    }

    [Serializable]
    public class FeatureModifier : AssetModifier
    {
        public virtual FeatureModifier Init(int featureCount)
        {
            return this;
        }
    }
}
