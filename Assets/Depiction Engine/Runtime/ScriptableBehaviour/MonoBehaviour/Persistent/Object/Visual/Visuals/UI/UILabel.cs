// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DepictionEngine
{
    public class UILabel : UIVisual
    {
        private static readonly int KEYFRAME_PROPERTY_COUNT = 4;

        private static readonly Vector2 HALF_VECTOR2 = new Vector2(0.5f, 0.5f);

        private enum KeyframeProperties 
        { 
            Time, 
            Value, 
            InTangent, 
            OutTangent 
        };

        private TextMeshProWarp _textMeshWarpPro;
        public void GetTextMeshProWarpIfAvailable(Action<TextMeshProWarp> callback)
        {
            if (_textMeshWarpPro == null)
            {
                _textMeshWarpPro = gameObject.GetComponent<TextMeshProWarp>();

                if (_textMeshWarpPro == null)
                    _textMeshWarpPro = gameObject.AddComponent<TextMeshProWarp>();
            }

            if (callback != null && _textMeshWarpPro != null)
                callback(_textMeshWarpPro);
        }

        private RectTransform _rectTransform;
        protected RectTransform GetRectTransform()
        {
            if (_rectTransform == null)
                _rectTransform = gameObject.GetComponent<RectTransform>();
            return _rectTransform;
        }

        public int maxVisibleLines
        {
            get 
            {
                int maxVisibleLines = 0;
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => { maxVisibleLines = textMeshProWarp.maxVisibleLines; });
                return maxVisibleLines;
            }
            set { GetTextMeshProWarpIfAvailable((textMeshProWarp) => { textMeshProWarp.maxVisibleLines = value; }); }
        }

        private Vector2 pivot
        {
            get { return GetRectTransform().pivot; }
            set
            {
                RectTransform rectTransform = GetRectTransform();
                if (rectTransform.pivot == value)
                    return;
                rectTransform.pivot = value;
                PropertiesChanged();
            }
        }

        private Vector2 sizeDelta
        {
            get { return GetRectTransform().sizeDelta; }
            set 
            {
                RectTransform rectTransform = GetRectTransform();
                float threshold = 0.01f;
                if (MathPlus.Approximately(rectTransform.sizeDelta.x, value.x, threshold) && MathPlus.Approximately(rectTransform.sizeDelta.y, value.y, threshold))
                    return;

                rectTransform.sizeDelta = value;

                PropertiesChanged();
            }
        }

        public string text
        {
            get 
            {
                string text = string.Empty;
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => { text = textMeshProWarp.text; });
                return text;
            }
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.text == value)
                        return;
                    textMeshProWarp.text = value;
                    PropertiesChanged();
                });
            }
        }

        public float fontSize
        {
            get 
            {
                float fontSize = 0.0f;
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => { fontSize = textMeshProWarp.fontSize; });
                return fontSize;
            }
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.fontSize == value)
                        return;
                    textMeshProWarp.fontSize = value;
                    PropertiesChanged();
                });
            }
        }

        public TextAlignmentOptions alignment
        {
            get 
            {
                TextAlignmentOptions alignment = default(TextAlignmentOptions);
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => { alignment = textMeshProWarp.alignment; });
                return alignment;
            }
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.alignment == value)
                        return;
                    textMeshProWarp.alignment = value;
                    PropertiesChanged();
                });
            }
        }

        public Color color
        {
            get 
            {
                Color color = default(Color);
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => { color = textMeshProWarp.color; });
                return color;
            }
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.color == value)
                        return;
                    textMeshProWarp.color = value;
                });
            }
        }

        public bool fontSharedMaterialEnableOutline
        {
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.fontSharedMaterial != null)
                        textMeshProWarp.fontSharedMaterial.SetKeyword(RenderingManager.outlineOnLocalKeyword, value);
                });
            }
        }

        public float fontSharedMaterialOutlineWidth
        {
            get
            {
                float fontSharedMaterialOutlineWidth = 0.0f;
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => 
                {
                    if (textMeshProWarp.fontSharedMaterial != null)
                        fontSharedMaterialOutlineWidth = textMeshProWarp.fontSharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth);
                });
                return fontSharedMaterialOutlineWidth;
            }
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.fontSharedMaterial != null)
                        textMeshProWarp.fontSharedMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, value);
                });
            }
        }

        public Color fontSharedMaterialOutlineColor
        {
            get
            {
                Color fontSharedMaterialOutlineColor = default(Color);
                GetTextMeshProWarpIfAvailable((textMeshProWarp) => 
                {
                    if (textMeshProWarp.fontSharedMaterial != null)
                        fontSharedMaterialOutlineColor = textMeshProWarp.fontSharedMaterial.GetColor(ShaderUtilities.ID_OutlineColor); 
                });
                return fontSharedMaterialOutlineColor;
            }
            set
            {
                GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    if (textMeshProWarp.fontSharedMaterial != null)
                        textMeshProWarp.fontSharedMaterial.SetColor(ShaderUtilities.ID_OutlineColor, value);
                });
            }
        }

        private const float KEY_THRESHOLD = 0.1f; 
        private void SetKeys(List<float> keys)
        {
            GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
            {
                if (textMeshProWarp.vertexCurve == null)
                    textMeshProWarp.vertexCurve = new AnimationCurve();

                int keysCount = keys.Count / KEYFRAME_PROPERTY_COUNT;

                bool changed = keysCount != textMeshProWarp.vertexCurve.keys.Length;
                if (!changed)
                {
                    for (int i = 0; i < textMeshProWarp.vertexCurve.keys.Length; i++)
                    {
                        Keyframe key = textMeshProWarp.vertexCurve.keys[i];
                        if (!MathPlus.Approximately(key.time, GetKeyProperty(keys, i, KeyframeProperties.Time), KEY_THRESHOLD) || !MathPlus.Approximately(key.value, GetKeyProperty(keys, i, KeyframeProperties.Value), KEY_THRESHOLD) || !MathPlus.Approximately(key.inTangent, GetKeyProperty(keys, i, KeyframeProperties.InTangent), KEY_THRESHOLD) || !MathPlus.Approximately(key.outTangent, GetKeyProperty(keys, i, KeyframeProperties.OutTangent), KEY_THRESHOLD))
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (changed)
                {
                    ClearKeys();

                    for (int i = 0; i < keysCount; i++)
                    {
                        Keyframe key = new Keyframe(GetKeyProperty(keys, i, KeyframeProperties.Time), GetKeyProperty(keys, i, KeyframeProperties.Value), GetKeyProperty(keys, i, KeyframeProperties.InTangent), GetKeyProperty(keys, i, KeyframeProperties.OutTangent));
                        key.weightedMode = WeightedMode.None;
                        textMeshProWarp.vertexCurve.AddKey(key);
                    }

                    PropertiesChanged();
                }
            });
        }

        private void ClearKeys()
        {
            GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
            {
                if (textMeshProWarp.vertexCurve != null && textMeshProWarp.vertexCurve.keys != null)
                {
                    for (int i = textMeshProWarp.vertexCurve.keys.Length; i > 0; i--)
                        textMeshProWarp.vertexCurve.RemoveKey(i - 1);
                }
            });
        }

        private float _textMeshMinX;
        private float _textMeshMaxX;
        private List<float> _keys;
        public void SetTextCurve(List<Vector3Double> textCurve, bool useEndCoordinate, Vector3Double endCoordinate, float width, float height, Camera camera)
        {
            if (!IsIgnoredInRender())
            {
                bool textMeshValid = true;

                if (_keys == null)
                    _keys = new List<float>();

                if (useEndCoordinate)
                {
                    float sizeOnScreen = Vector2.Distance(camera.unityCamera.WorldToScreenPoint(TransformDouble.SubtractOrigin(visualObject.transform.position)), camera.unityCamera.WorldToScreenPoint(TransformDouble.SubtractOrigin(endCoordinate)));
                    if (sizeOnScreen > 25.0f && ProjectPoint(out Vector3 localEndCoordinate, endCoordinate, camera))
                    {
                        float angle = MathPlus.AngleBetween(localEndCoordinate.x, localEndCoordinate.y);

                        bool flipped = Mathf.Abs(angle) <= 90.0f;

                        transform.localRotation *= Quaternion.Euler(0.0f, 0.0f, (flipped ? 0.0f : -180.0f) - angle);

                        pivot = new Vector2(flipped ? 1.0f : 0.0f, 0.5f);
                        sizeDelta = new Vector2(localEndCoordinate.magnitude, height);
               
                        if (ForceMeshUpdateIfPropertiesChanged(true))
                        {
                            PropertiesChanged();
                            GetMeshBoundMinMaxX(out _textMeshMinX, out _textMeshMaxX);
                        }

                        int textCurveCount = textCurve.Count + 2;

                        if (textCurveCount > 2)
                        {
                            if (_keys.Count / KEYFRAME_PROPERTY_COUNT != textCurveCount)
                            {
                                _keys.Clear();
                                for (int i = 0; i < textCurveCount * KEYFRAME_PROPERTY_COUNT; i++)
                                    _keys.Add(0.0f);
                            }

                            float textMeshMinX = flipped ? -_textMeshMaxX : _textMeshMinX;
                            float textMeshMaxX = flipped ? -_textMeshMinX : _textMeshMaxX;

                            float meshWidth = textMeshMaxX - textMeshMinX;

                            float lastTime = (sizeDelta.x - textMeshMinX) / meshWidth;
                            float firstTime = -textMeshMinX / meshWidth;

                            SetKeyTimeValue(_keys, 0, firstTime, 0.0f);

                            float pointOffset = 0.001f;
                            float offsetFirstTime = firstTime + pointOffset;
                            float offsetLastTime = lastTime - textCurve.Count * pointOffset;

                            for (int i = 0; i < textCurve.Count; i++)
                            {
                                if (!ProjectPoint(out Vector3 localCurvePoint, textCurve[i], camera))
                                {
                                    _keys.Clear();
                                    textMeshValid = false;
                                    break;
                                }

                                float time = Mathf.Clamp((localCurvePoint.x - _textMeshMinX) / meshWidth, offsetFirstTime, offsetLastTime);
                                offsetFirstTime = time + pointOffset;

                                SetKeyTimeValue(_keys, i + 1, time, localCurvePoint.y);
                            }

                            if (textMeshValid)
                            {
                                SetKeyTimeValue(_keys, textCurveCount - 1, lastTime, 0.0f);

                                //Apply a Clamped Auto tangent curve smoothing with linear start and end points
                                for (int i = 0; i < textCurveCount; i++)
                                {
                                    float ratio = 1.0f;

                                    int index;

                                    index = i;
                                    if (i != 0)
                                        index -= 1;
                                    float value = GetKeyProperty(_keys, index, KeyframeProperties.Value);
                                    float time = GetKeyProperty(_keys, index, KeyframeProperties.Time);

                                    index = i;
                                    if (i != textCurveCount - 1)
                                        index += 1;
                                    float nextValue = GetKeyProperty(_keys, index, KeyframeProperties.Value);
                                    float nextTime = GetKeyProperty(_keys, index, KeyframeProperties.Time);

                                    if (i != 0 && i != textCurveCount - 1)
                                    {
                                        float min = Mathf.Min(value, nextValue);
                                        float max = Mathf.Max(value, nextValue);
                                        float diff = max - min;
                                        float normalized = diff == 0.0f ? 0.0f : Mathf.Clamp01((GetKeyProperty(_keys, i, KeyframeProperties.Value) - min) / diff);
                                        ratio = 1.0f - Math.Abs((normalized - 0.5f) * 2.0f);
                                    }

                                    float tangent = Mathf.SmoothStep(0.0f, (value - nextValue) / (time - nextTime), ratio);
                                    SetKeyTangents(_keys, i, tangent, tangent);
                                }
                            }
                        }
                        else
                            _keys.Clear();
                    }
                    else
                    {
                        _keys.Clear();
                        textMeshValid = false;
                    }
                }
                else
                {
                    _keys.Clear();

                    pivot = HALF_VECTOR2;
                    sizeDelta = new Vector2(width, height);
                }

                SetKeys(_keys);

                ForceMeshUpdateIfPropertiesChanged();

                if (!textMeshValid)
                    IgnoreInRender();
            }
        }


        private void GetMeshBoundMinMaxX(out float minX, out float maxX)
        {
            float meshMinX = 0.0f;
            float meshMaxX = 0.0f;

            GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
            {
                UnityEngine.Mesh mesh = textMeshProWarp.textInfo.meshInfo[0].mesh;
                if (mesh != null)
                {
                    meshMinX = (float)Math.Round(mesh.bounds.min.x, 1);
                    meshMaxX = (float)Math.Round(mesh.bounds.max.x, 1);
                }
            });

            minX = meshMinX;
            maxX = meshMaxX;
        }

        private static float GetKeyProperty(List<float> keys, int index, KeyframeProperties property)
        {
            return keys[index * KEYFRAME_PROPERTY_COUNT + (int)property];
        }

        private void SetKeyTimeValue(List<float> keys, int index, float time, float value)
        {
            SetKeyProperty(keys, index, KeyframeProperties.Time, time);
            SetKeyProperty(keys, index, KeyframeProperties.Value, value);
        }

        private void SetKeyTangents(List<float> keys, int index, float inTangent, float outTangent)
        {
            SetKeyProperty(keys, index, KeyframeProperties.InTangent, inTangent);
            SetKeyProperty(keys, index, KeyframeProperties.OutTangent, outTangent);
        }

        private static void SetKeyProperty(List<float> keys, int index, KeyframeProperties property, float value)
        {
            keys[index * KEYFRAME_PROPERTY_COUNT + (int)property] = value;
        }

        private bool ProjectPoint(out Vector3 projectedLocalPoint, Vector3Double point, Camera camera)
        {
            if (ProjectPoint(out Vector3Double projectedPoint, point, camera))
            {
                projectedLocalPoint = transform.InverseTransformPoint(TransformDouble.SubtractOrigin(projectedPoint));
                return true;
            }
            projectedLocalPoint = Vector3.negativeInfinity;
            return false;
        }

        public bool ProjectPoint(out Vector3Double projectedPoint, Vector3Double point, Camera camera)
        {
            return camera.ProjectPoint(out projectedPoint, point, transform.rotation * Vector3.back, visualObject.transform.position);
        }

        private bool _propertiesChanged;
        private void PropertiesChanged()
        {
            GetTextMeshProWarpIfAvailable((textMeshProWarp) => { textMeshProWarp.havePropertiesChanged = true; });
            _propertiesChanged = true;
        }

        private bool ForceMeshUpdateIfPropertiesChanged(bool preventTextWarp = false)
        {
            bool hasChanged = false;

            GetTextMeshProWarpIfAvailable((textMeshProWarp) => 
            {
                if (_propertiesChanged || textMeshProWarp.havePropertiesChanged)
                {
                    textMeshProWarp.ForceMeshUpdate(preventTextWarp);
                    _propertiesChanged = textMeshProWarp.havePropertiesChanged = false;

                    hasChanged = true;
                }
            });

            return hasChanged;
        }
    }
}
