// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/UI/" + nameof(Label))]
    public class Label : UIBase
    {
        [BeginFoldout("Label")]
        [SerializeField, Tooltip("The text color.")]
        private Color _color;
        [SerializeField, Tooltip("The text to display.")]
        private string _text;
        [SerializeField, Tooltip("How many times should the text be repeated (0 - 100).")]
        private int _repeatCount;
        [SerializeField, Tooltip("A value to insert between '"+nameof(repeatCount)+"' repetitions.")]
        private string _repeatSpacer;
        [SerializeField, Tooltip("The text size.")]
        private float _fontSize;
        [SerializeField, Tooltip("The number of lines of text to display.")]
        private int _maxVisibleLines;
        [SerializeField, Tooltip("The text alignment.")]
        private TextAlignmentOptions _alignment;
        [SerializeField, Tooltip("The horizontal size of the label.")]
        private float _width;
        [SerializeField, Tooltip("The vertical size of the label.")]
        private float _height;
        [SerializeField, Tooltip("When enabled the label will start at the transform position and point towards a 3D point in space.")]
        private bool _useEndCoordinate;

        [SerializeField, Tooltip("The 3D point towards which the label should be pointing.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowEndCoordinate))]
#endif
        private Vector3Double _endCoordinate;


#if UNITY_EDITOR
        [SerializeField, Button(nameof(CopyFromCurrentPositionBtn)), ConditionalShow(nameof(GetShowEndCoordinate)), Tooltip("Set the '"+nameof(endCoordinate)+"' to the 'transform.position' value.")]
        private bool _copyFromCurrentPosition;

        private void CopyFromCurrentPositionBtn()
        {
            endCoordinate = transform.position;
        }
#endif

        [SerializeField, Tooltip("A list of 3D points that the label should follow between the transform position and the 'endCoordinate'.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowEndCoordinate))]
#endif
        private List<Vector3Double> _textCurve;

#if UNITY_EDITOR
        [SerializeField, Button(nameof(FlattenCurveCameraZDepthBtn)), ConditionalShow(nameof(GetShowFlattenBtn)), Tooltip("Project the '"+nameof(endCoordinate)+"' and all the curve points to a plane facing the camera.")]
        private bool _flattenCurveCameraZDepth;
        [SerializeField, Button(nameof(FlattenCurveZDepthBtn)), ConditionalShow(nameof(GetShowFlattenBtn)), Tooltip("Bring the '"+nameof(endCoordinate)+"' and all the curve points z component to local zero."), EndFoldout]
        private bool _flattenCurveZDepth;

        private void FlattenCurveCameraZDepthBtn()
        {
            Editor.SceneCamera sceneCamera = Editor.SceneViewDouble.lastActiveSceneViewDouble.camera;
            if (sceneCamera != Disposable.NULL)
                FlattenCurveCameraZDepth(sceneCamera);
        }

        private void FlattenCurveZDepthBtn()
        {
            FlattenCurveZDepth();
        }

        protected bool GetShowEndCoordinate()
        {
            return useEndCoordinate;
        }

        protected bool GetShowFlattenBtn()
        {
            return IsNotFallbackValues() && GetShowEndCoordinate();
        }
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => color = value, Color.white, initializingState);
            InitValue(value => text = value, "My Label", initializingState);
            InitValue(value => repeatCount = value, 0, initializingState);
            InitValue(value => repeatSpacer = value, "      ", initializingState);
            InitValue(value => fontSize = value, 30.0f, initializingState);
            InitValue(value => maxVisibleLines = value, 1, initializingState);
            InitValue(value => alignment = value, TextAlignmentOptions.Capline, initializingState);
            InitValue(value => width = value, 250.0f, initializingState);
            InitValue(value => height = value, 20.0f, initializingState);
            InitValue(value => useEndCoordinate = value, false, initializingState);
            InitValue(value => endCoordinate = value, Vector3Double.zero, initializingState);
            InitValue(value => textCurve = value, new List<Vector3Double>(), initializingState);
        }

#if UNITY_EDITOR
        protected UnityEngine.Object[] GetTextMeshProsAdditionalRecordObjects()
        {
            List<UnityEngine.Object> textMeshPros = new List<UnityEngine.Object>();

            transform.IterateOverRootChildren<UILabel>((uiLabel) =>
            {
                uiLabel.GetTextMeshProWarpIfAvailable((textMeshProWarp) =>
                {
                    textMeshPros.Add(textMeshProWarp);
                });

                return true;
            });

            return textMeshPros.ToArray();
        }
#endif

        /// <summary>
        /// The text color. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public Color color
        {
            get { return _color; }
            set
            {
                SetValue(nameof(color), value, ref _color, (newValue, oldValue) =>
                {
                    UpdateUILabelColor();
                });
            }
        }

        /// <summary>
        /// The text to display. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public string text
        {
            get { return _text; }
            set
            {
                SetValue(nameof(text), value, ref _text, (newValue, oldValue) =>
                {
                    UpdateUILabelText();
                });
            }
        }

        /// <summary>
        /// How many times should the text be repeated (0 - 100). 
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public int repeatCount
        {
            get { return _repeatCount; }
            set
            {
                SetValue(nameof(repeatCount), (int)Mathf.Clamp(value, 0.0f, 100.0f), ref _repeatCount, (newValue, oldValue) =>
                {
                    UpdateUILabelText();
                });
            }
        }

        /// <summary>
        /// A value to insert between <see cref="repeatCount"/> repetitions. 
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public string repeatSpacer
        {
            get { return _repeatSpacer; }
            set 
            { 
                SetValue(nameof(repeatSpacer), value, ref _repeatSpacer, (newValue, oldValue) =>
                {
                    UpdateUILabelText();
                });
            }
        }

        /// <summary>
        /// The text size.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public float fontSize
        {
            get { return _fontSize; }
            set 
            { 
                SetValue(nameof(fontSize), value, ref _fontSize, (newValue, oldValue) =>
                {
                    UpdateUILabelFontSize();
                });
            }
        }

        /// <summary>
        /// The number of lines of text to display.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public int maxVisibleLines
        {
            get { return _maxVisibleLines; }
            set 
            { 
                SetValue(nameof(maxVisibleLines), value, ref _maxVisibleLines, (newValue, oldValue) => 
                {
                    UpdateUILabelMaxVisibleLines();
                }); 
            }
        }

        /// <summary>
        /// The text alignment.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetTextMeshProsAdditionalRecordObjects))]
#endif
        public TextAlignmentOptions alignment
        {
            get { return _alignment; }
            set
            {
                SetValue(nameof(alignment), value, ref _alignment, (newValue, oldValue) =>
                {
                    UpdateUILabelAlignment();
                });
            }
        }

        /// <summary>
        /// The horizontal size of the label.
        /// </summary>
        [Json]
        public float width
        {
            get { return _width; }
            set { SetValue(nameof(width), value, ref _width); }
        }

        /// <summary>
        /// The vertical size of the label.
        /// </summary>
        [Json]
        public float height
        {
            get { return _height; }
            set { SetValue(nameof(height), value, ref _height); }
        }

        /// <summary>
        /// When enabled the label will start at the transform position and point towards a 3D point in space.
        /// </summary>
        [Json]
        public bool useEndCoordinate
        {
            get { return _useEndCoordinate; }
            set { SetValue(nameof(useEndCoordinate), value, ref _useEndCoordinate); }
        }

        /// <summary>
        /// The 3D point towards which the label should be pointing.
        /// </summary>
        [Json]
        public Vector3Double endCoordinate
        {
            get { return _endCoordinate; }
            set { SetValue(nameof(endCoordinate), value, ref _endCoordinate); }
        }

        /// <summary>
        /// A list of 3D points that the label should follow between the transform position and the <see cref="endCoordinate"/>.
        /// </summary>
        [Json]
        public List<Vector3Double> textCurve
        {
            get { return _textCurve; }
            set { SetValue(nameof(textCurve), value, ref _textCurve); }
        }

        protected override bool SetAlpha(float value)
        {
            if (base.SetAlpha(value))
            {
                UpdateUILabelColor();

                return true;
            }
            return false;
        }

        protected override bool SetPopupT(float value)
        {
            if (base.SetPopupT(value))
            {
                UpdateUILabelColor();

                return true;
            }
            return false;
        }

        protected override UIVisual CreateUIVisual(Type visualType, bool useCollider)
        {
            return base.CreateUIVisual(typeof(UILabel), useCollider);
        }

        /// <summary>
        /// Bring the endCoordinate and all the curve points z component to local zero.
        /// </summary>
        public void FlattenCurveZDepth()
        {
            endCoordinate = FlattenCurvePoint(endCoordinate);

            for (int i = 0; i < textCurve.Count; i++)
                textCurve[i] = FlattenCurvePoint(textCurve[i]);
        }

        /// <summary>
        /// Bring the point z component to local zero.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3Double FlattenCurvePoint(Vector3Double point)
        {
            Vector3Double localPoint = transform.InverseTransformPoint(point);
            localPoint.z = 0.0d;
            return transform.TransformPoint(localPoint);
        }

        /// <summary>
        /// Project the endCoordinate and all the curve points to a plane facing the camera.
        /// </summary>
        /// <param name="camera"></param>
        public void FlattenCurveCameraZDepth(Camera camera)
        {
            Vector3Double projectedPoint;

            if (FlattenCurvePoint(out projectedPoint, camera, endCoordinate))
                endCoordinate = projectedPoint;

            for (int i = 0; i < textCurve.Count; i++)
            {
                if (FlattenCurvePoint(out projectedPoint, camera, textCurve[i]))
                    textCurve[i] = projectedPoint;
            }
        }

        /// <summary>
        /// Project the point to a plane facing the camera.
        /// </summary>
        /// <param name="projectedPoint"></param>
        /// <param name="camera"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool FlattenCurvePoint(out Vector3Double projectedPoint, Camera camera, Vector3Double point)
        {
            UILabel uiLabel = GetVisualForCamera(camera) as UILabel;

            if (uiLabel != Disposable.NULL)
                return uiLabel.ProjectPoint(out projectedPoint, point, camera);

            projectedPoint = Vector3Double.negativeInfinity;
            return false;
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            if (visualsChanged)
            {
                UpdateUILabelColor();
                UpdateUILabelText();
                UpdateUILabelFontSize();
                UpdateUILabelMaxVisibleLines();
                UpdateUILabelAlignment();
            }

            transform.IterateOverRootChildren<UILabel>((uiLabel) =>
            {
                uiLabel.fontSharedMaterialOutlineWidth = renderingManager.labelOutlineWidth;
                uiLabel.fontSharedMaterialOutlineColor = renderingManager.labelOutlineColor;

                return true;
            });
        }

        private void UpdateUILabelColor()
        {
            if (initialized)
            {
                transform.IterateOverRootChildren<UILabel>((uiLabel) =>
                {
                    uiLabel.color = color.SetAlpha(GetCurrentAlpha());

                    return true;
                });
            }
        }

        private void UpdateUILabelText()
        {
            if (initialized)
            {
                transform.IterateOverRootChildren<UILabel>((uiLabel) =>
                {
                    string repeatedText = text;
                    for (int i = 0; i < repeatCount; i++)
                        repeatedText += repeatSpacer + text;
                    uiLabel.text = repeatedText;

                    return true;
                });
            }
        }

        private void UpdateUILabelFontSize()
        {
            if (initialized)
            {
                transform.IterateOverRootChildren<UILabel>((uiLabel) =>
                {
                    //TestMesh Pro requires 10x font size magnification to look right, why?
                    uiLabel.fontSize = fontSize * 10.0f;

                    return true;
                });
            }
        }

        private void UpdateUILabelMaxVisibleLines()
        {
            if (initialized)
            {
                transform.IterateOverRootChildren<UILabel>((uiLabel) =>
                {
                    uiLabel.maxVisibleLines = maxVisibleLines;

                    return true;
                });
            }
        }

        private void UpdateUILabelAlignment()
        {
            if (initialized)
            {
                transform.IterateOverRootChildren<UILabel>((uiLabel) =>
                {
                    uiLabel.alignment = alignment;

                    return true;
                });
            }
        }

        public override bool HierarchicalBeginCameraRendering(Camera camera)
        {
            if (base.HierarchicalBeginCameraRendering(camera))
            {
                transform.IterateOverRootChildren<UILabel>((uiLabel) =>
                {
                    uiLabel.SetTextCurve(textCurve, useEndCoordinate, endCoordinate, width, height, camera);

                    return true;
                });

                return true;
            }
            return false;
        }
    }
}
