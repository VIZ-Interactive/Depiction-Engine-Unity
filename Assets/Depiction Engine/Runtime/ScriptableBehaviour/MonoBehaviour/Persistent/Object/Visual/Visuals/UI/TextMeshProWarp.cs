// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using TMPro;
using UnityEngine;

namespace DepictionEngine
{
    public class TextMeshProWarp : TextMeshPro
    {
        private const float curveScale = 1.0f;

        private AnimationCurve _vertexCurve;

        public AnimationCurve vertexCurve
        {
            get { return _vertexCurve; }
            set
            {
                if (_vertexCurve == value)
                    return;
                _vertexCurve = value;
            }
        }

        private bool _preventTextWarp;
        public void ForceMeshUpdate(bool preventTextWarp = false, bool ignoreActiveState = true, bool forceTextReparsing = false)
        {
            _preventTextWarp = preventTextWarp;
            base.ForceMeshUpdate(ignoreActiveState, forceTextReparsing);
        }

        protected override void GenerateTextMesh()
        {
            base.GenerateTextMesh();
            if (!_preventTextWarp)
                WarpText();
            _preventTextWarp = false;
        }

        public void WarpText()
        {
            if (vertexCurve != null)
            {
                UnityEngine.Mesh mesh = textInfo.meshInfo[0].mesh;
                TMP_MeshInfo info = textInfo.meshInfo[0];

                Vector3[] vertices;
                Matrix4x4 matrix;

                //TMP_TextInfo textInfo = textInfo;
                int characterCount = textInfo.characterCount;

                if (characterCount == 0) return;

                float boundsMinX = mesh.bounds.min.x;
                float boundsMaxX = mesh.bounds.max.x;

                for (int i = 0; i < characterCount; i++)
                {
                    if (!textInfo.characterInfo[i].isVisible)
                        continue;

                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the index of the mesh used by this character.
                    int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    vertices = textInfo.meshInfo[meshIndex].vertices;

                    // Compute the baseline mid point for each character
                    Vector3 offsetToMidBaseline = new Vector2((vertices[vertexIndex + 0].x + vertices[vertexIndex + 2].x) / 2, textInfo.characterInfo[i].baseLine);
                    //float offsetY = curve.Evaluate((float)i / characterCount + loopCount / 50f); // Random.Range(-0.25f, 0.25f);

                    // Apply offset to adjust our pivot point.
                    vertices[vertexIndex + 0] += -offsetToMidBaseline;
                    vertices[vertexIndex + 1] += -offsetToMidBaseline;
                    vertices[vertexIndex + 2] += -offsetToMidBaseline;
                    vertices[vertexIndex + 3] += -offsetToMidBaseline;

                    // Compute the angle of rotation for each character based on the animation curve
                    float x0 = (offsetToMidBaseline.x - boundsMinX) / (boundsMaxX - boundsMinX); // Character's position relative to the bounds of the mesh.
                    float x1 = x0 + 0.0001f;
                    float y0 = vertexCurve.Evaluate(x0) * curveScale;
                    float y1 = vertexCurve.Evaluate(x1) * curveScale;

                    Vector3 horizontal = new Vector3(1, 0, 0);
                    //Vector3 normal = new Vector3(-(y1 - y0), (x1 * (boundsMaxX - boundsMinX) + boundsMinX) - offsetToMidBaseline.x, 0);
                    Vector3 tangent = new Vector3(x1 * (boundsMaxX - boundsMinX) + boundsMinX, y1) - new Vector3(offsetToMidBaseline.x, y0);

                    float dot = Mathf.Acos(Vector3.Dot(horizontal, tangent.normalized)) * 57.2957795f;
                    Vector3 cross = Vector3.Cross(horizontal, tangent);
                    float angle = cross.z > 0 ? dot : 360 - dot;

                    matrix = Matrix4x4.TRS(new Vector3(0, y0, 0), Quaternion.Euler(0, 0, angle), Vector3.one);

                    vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0]);
                    vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1]);
                    vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2]);
                    vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3]);

                    vertices[vertexIndex + 0] += offsetToMidBaseline;
                    vertices[vertexIndex + 1] += offsetToMidBaseline;
                    vertices[vertexIndex + 2] += offsetToMidBaseline;
                    vertices[vertexIndex + 3] += offsetToMidBaseline;

                    // Upload the mesh with the revised information
                    UpdateVertexData();
                }
            }
        }
    }
}
