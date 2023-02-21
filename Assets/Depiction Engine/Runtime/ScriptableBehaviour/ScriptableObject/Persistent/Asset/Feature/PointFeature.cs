// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class PointFeature : Feature
    {
        [SerializeField, HideInInspector]
        private GeoCoordinate3Double[] _geoCoordinates;

        public override void SetData(JSONNode json)
        {
            PointFeatureModifier pointFeatureModifier = PointFeatureProcessingFunctions.CreatePropertyModifier<PointFeatureModifier>();
            if (pointFeatureModifier != Disposable.NULL)
            {
                if (PointFeatureProcessingFunctions.ParseJSON(json, pointFeatureModifier))
                    pointFeatureModifier.ModifyProperties(this);

                DisposeManager.Dispose(pointFeatureModifier);
            }
        }

        public void SetData(GeoCoordinate3Double[] geoCoordinates)
        {
            _geoCoordinates = geoCoordinates;
        }

        public override int featureCount
        {
            get { return _geoCoordinates.Length; }
        }

        public GeoCoordinate3Double GetGeoCoordinate(int index)
        {
            return _geoCoordinates[index];
        }

        protected override JSONObject GetDataJson()
        {
            JSONObject json = base.GetDataJson();

            json["geoCoordinates"] = JsonUtility.ToJson(_geoCoordinates);

            return json;
        }
    }

    public class PointFeatureProcessingFunctions : ProcessingFunctions
    {
        public static bool ParseJSON(JSONNode json, FeatureModifier featureModifier, CancellationTokenSource cancellationTokenSource = null)
        {
            string typeName = nameof(PropertyScriptableObject.type);
            if (json[typeName] != null && json[typeName] == typeof(PointFeature).FullName)
            {
                PointFeatureModifier pointFeatureModifier = featureModifier as PointFeatureModifier;

                JsonUtility.FromJson(out List<GeoCoordinate3Double> geoCoordinates, json["geoCoordinates"]);

                pointFeatureModifier.Init(geoCoordinates);

                return true;
            }
            else if (json["features"] != null && json["features"].IsArray && json["features"].AsArray.Count != 0)
            {
                //OpenStreetMap format
                featureModifier.Init(json["features"].AsArray.Count);

                PointFeatureModifier pointFeatureModifier = featureModifier as PointFeatureModifier;

                foreach (JSONNode geoCoordinateJson in json["features"].AsArray)
                {
                    if (JsonUtility.FromJson(out GeoCoordinate3Double geoCoordinate, geoCoordinateJson))
                        pointFeatureModifier.Add(geoCoordinate);

                    if (cancellationTokenSource != null)
                        cancellationTokenSource.ThrowIfCancellationRequested();
                }

                return true;
            }

            return false;
        }
    }

    [Serializable]
    public class PointFeatureModifier : FeatureModifier
    {
        [SerializeField]
        private List<GeoCoordinate3Double> _geoCoordinates;

        public override void Recycle()
        {
            base.Recycle();

            _geoCoordinates.Clear();
        }

        public override void Initializing()
        {
            base.Initializing();
          
            if (_geoCoordinates == null)
                _geoCoordinates = new List<GeoCoordinate3Double>();
        }

        public override FeatureModifier Init(int featureCount)
        {
            base.Init(featureCount);

            _geoCoordinates.Capacity = featureCount;

            return this;
        }

        public FeatureModifier Init(
            List<GeoCoordinate3Double> geoCoordinates)
        {
            base.Init(geoCoordinates.Count);

            _geoCoordinates = geoCoordinates;

            return this;
        }

        public void Add(GeoCoordinate3Double geoCoordinate)
        {
            _geoCoordinates.Add(geoCoordinate);
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            PointFeature pointFeature = scriptableBehaviour as PointFeature;

            if (pointFeature != Disposable.NULL)
                pointFeature.SetData(_geoCoordinates.ToArray());
        }
    }
}
