// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class FeatureMesh : Mesh
    {
        [SerializeField, HideInInspector]
        private List<int> _featureLastIndex;

        public override void Recycle()
        {
            base.Recycle();

            _featureLastIndex = default;
        }

        public int GetLastIndex(int featureIndex)
        {
            return featureLastIndex != null ?  featureLastIndex[featureIndex] : -1;
        }

        public int GetFeatureIndex(int triangleIndex)
        {
            if (featureLastIndex != null)
            {
                int featureIndex = featureLastIndex.BinarySearch(unityMesh.triangles[triangleIndex * 3]);

                if (featureIndex < 0)
                    featureIndex = ~featureIndex;

                return featureIndex;
            }

            return -1;
        }

        public List<int> featureLastIndex
        {
            get { return _featureLastIndex; }
            set
            {
                if (_featureLastIndex == value)
                    return;
                _featureLastIndex = value;
            }
        }
    }

    [Serializable]
    public class FeatureMeshModifier : MeshModifier
    {
        private List<int> _featureLastIndex;

        private List<int> featureLastIndex
        { 
            get { return _featureLastIndex; }
            set { _featureLastIndex = value; }
        }

        public override void Recycle()
        {
            base.Recycle();

            featureLastIndex = default;
        }

        public void FeatureComplete()
        {
            if (featureLastIndex == null)
                featureLastIndex = new List<int>();
            featureLastIndex.Add(vertices.Count);
        }

        public override Type GetMeshType()
        {
            return typeof(FeatureMesh);
        }

        public override void ModifyProperties(IScriptableBehaviour scriptableBehaviour)
        {
            base.ModifyProperties(scriptableBehaviour);

            if (scriptableBehaviour is FeatureMesh)
            {
                FeatureMesh featureMesh = scriptableBehaviour as FeatureMesh;

                featureMesh.featureLastIndex = featureLastIndex;
            }
        }
    }
}
