// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Wrapper class for 'UnityEngine.ReflectionProbe' introducing better integrated functionality.
    /// </summary>
    [CreateComponent(typeof(UnityEngine.ReflectionProbe))]
    public class ReflectionProbe : Object
    {
        [BeginFoldout("ReflectionProbe")]
        [SerializeField, Tooltip("Should reflection probe texture be generated in the Editor (ReflectionProbeMode.Baked) or should probe use custom specified texure (ReflectionProbeMode.Custom)?")]
        private UnityEngine.Rendering.ReflectionProbeMode _reflectionProbeType;
        [SerializeField, Tooltip("Sets the way the probe will refresh.See Also: ReflectionProbeRefreshMode.")]
        private UnityEngine.Rendering.ReflectionProbeRefreshMode _refreshMode;
        [SerializeField, Tooltip("Sets this probe time-slicing modeSee Also: ReflectionProbeTimeSlicingMode."), EndFoldout]
        private UnityEngine.Rendering.ReflectionProbeTimeSlicingMode _timeSlicing;

        [BeginFoldout("Runtime Settings")]
        [SerializeField, Tooltip("Reflection probe importance.")]
        private int _importance;
        [SerializeField, Tooltip("The intensity modifier that is applied to the texture of reflection probe in the shader.")]
        private float _intensity;
        [SerializeField, Tooltip("Should this reflection probe use box projection?")]
        private bool _boxProjection;
        [SerializeField, Tooltip("Distance around probe used for blending (used in deferred probes).")]
        private float _blendDistance;
        [SerializeField, Tooltip("The size of the box area in which reflections will be applied to the objects. Measured in the probes's local space.")]
        private Vector3 _boxSize;
        [SerializeField, Tooltip("The center of the box area in which reflections will be applied to the objects. Measured in the probes's local space."), EndFoldout]
        private Vector3 _boxOffset;

        [BeginFoldout("Cubemap Capture Settings")]
        [SerializeField, Tooltip("Resolution of the underlying reflection texture in pixels.")]
        private int _resolution;
        [SerializeField, Tooltip("Should this reflection probe use HDR rendering?")]
        private bool _hdr;
        [SerializeField, Tooltip("Shadow drawing distance when rendering the probe.")]
        private float _shadowDistance;
        [SerializeField, Tooltip("How the reflection probe clears the background.")]
        private UnityEngine.Rendering.ReflectionProbeClearFlags _clearFlags;
        [SerializeField, Tooltip("The color with which the texture of reflection probe will be cleared.")]
        private Color _backgroundColor;
        [SerializeField, Mask, Tooltip("This is used to render parts of the reflecion probe's surrounding selectively.")]
        private int _cullingMask;
        [SerializeField, Tooltip("The near clipping plane distance when rendering the probe.")]
        private float _nearClipPlane;
        [SerializeField, Tooltip("The far clipping plane distance when rendering the probe."), EndFoldout]
        private float _farClipPlane;

        private UnityEngine.ReflectionProbe _reflectionProbe;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => reflectionProbeType = value, UnityEngine.Rendering.ReflectionProbeMode.Baked, initializingContext);
            InitValue(value => refreshMode = value, UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake, initializingContext);
            InitValue(value => timeSlicing = value, UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce, initializingContext);

            InitValue(value => importance = value, 1, initializingContext);
            InitValue(value => intensity = value, 1.0f, initializingContext);
            InitValue(value => boxProjection = value, false, initializingContext);
            InitValue(value => blendDistance = value, 1.0f, initializingContext);
            InitValue(value => boxSize = value, new Vector3(10.0f, 10.0f, 10.0f), initializingContext);
            InitValue(value => boxOffset = value, Vector3.zero, initializingContext);

            InitValue(value => resolution = value, 128, initializingContext);
            InitValue(value => hdr = value, true, initializingContext);
            InitValue(value => shadowDistance = value, 100.0f, initializingContext);
            InitValue(value => clearFlags = value, UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox, initializingContext);
            InitValue(value => backgroundColor = value, new Color(0.1921569f, 0.3019608f, 0.4745098f, 0.0f), initializingContext);
            InitValue(value => cullingMask = value, 0, initializingContext);
            InitValue(value => nearClipPlane = value, 0.3f, initializingContext);
            InitValue(value => farClipPlane = value, 1000.0f, initializingContext);
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            UpdateReflectionProbeEnabled();
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                if (!SceneManager.Debugging() && reflectionProbe != null)
                    reflectionProbe.hideFlags = HideFlags.HideInInspector;

                return true;
            }
            return false;
        }

        public override bool RequiresPositioning()
        {
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (initialized)
                UpdateReflectionProbeEnabled();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (initialized)
                UpdateReflectionProbeEnabled();
        }

        private void UpdateReflectionProbeEnabled()
        {
            if (reflectionProbe != null)
                reflectionProbe.enabled = enabled;
        }

        public void RenderProbe()
        {
            reflectionProbe.RenderProbe();
        }

        public UnityEngine.ReflectionProbe reflectionProbe
        {
            get 
            {
                if (_reflectionProbe == null)
                    _reflectionProbe = gameObject.GetComponent<UnityEngine.ReflectionProbe>();
                return _reflectionProbe; 
            }
        }

#if UNITY_EDITOR
        protected UnityEngine.Object[] GetReflectionProbeAdditionalRecordObjects()
        {
            if (reflectionProbe != null)
                return new UnityEngine.Object[] { reflectionProbe  };
            return null;
        }
#endif

        /// <summary>
        /// Should reflection probe texture be generated in the Editor (ReflectionProbeMode.Baked) or should probe use custom specified texure (ReflectionProbeMode.Custom)?
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public UnityEngine.Rendering.ReflectionProbeMode reflectionProbeType
        {
            get { return _reflectionProbeType; }
            set
            {
                SetValue(nameof(reflectionProbeType), value, ref _reflectionProbeType, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.mode != newValue)
                        reflectionProbe.mode = newValue;
                });
            }
        }

        /// <summary>
        /// Sets the way the probe will refresh.See Also: ReflectionProbeRefreshMode.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public UnityEngine.Rendering.ReflectionProbeRefreshMode refreshMode
        {
            get { return _refreshMode; }
            set
            {
                SetValue(nameof(refreshMode), value, ref _refreshMode, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.refreshMode != newValue)
                        reflectionProbe.refreshMode = newValue;
                });
            }
        }

        /// <summary>
        /// Sets this probe time-slicing modeSee Also: ReflectionProbeTimeSlicingMode.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public UnityEngine.Rendering.ReflectionProbeTimeSlicingMode timeSlicing
        {
            get { return _timeSlicing; }
            set
            {
                SetValue(nameof(timeSlicing), value, ref _timeSlicing, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.timeSlicingMode != newValue)
                        reflectionProbe.timeSlicingMode = newValue;
                });
            }
        }

        /// <summary>
        /// Reflection probe importance.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public int importance
        {
            get { return _importance; }
            set
            {
                SetValue(nameof(importance), value, ref _importance, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.importance != newValue)
                        reflectionProbe.importance = newValue;
                });
            }
        }

        /// <summary>
        /// The intensity modifier that is applied to the texture of reflection probe in the shader.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public float intensity
        {
            get { return _intensity; }
            set
            {
                SetValue(nameof(intensity), value, ref _intensity, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.intensity != newValue)
                        reflectionProbe.intensity = newValue;
                });
            }
        }

        /// <summary>
        /// Should this reflection probe use box projection?
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public bool boxProjection
        {
            get { return _boxProjection; }
            set
            {
                SetValue(nameof(boxProjection), value, ref _boxProjection, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.boxProjection != newValue)
                        reflectionProbe.boxProjection = newValue;
                });
            }
        }

        /// <summary>
        /// Distance around probe used for blending (used in deferred probes).
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public float blendDistance
        {
            get { return _blendDistance; }
            set
            {
                SetValue(nameof(blendDistance), value, ref _blendDistance, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.blendDistance != newValue)
                        reflectionProbe.blendDistance = newValue;
                });
            }
        }

        /// <summary>
        /// The size of the box area in which reflections will be applied to the objects. Measured in the probes's local space.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public Vector3 boxSize
        {
            get { return _boxSize; }
            set
            {
                SetValue(nameof(boxSize), value, ref _boxSize, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.size != newValue)
                        reflectionProbe.size = newValue;
                });
            }
        }

        /// <summary>
        /// The center of the box area in which reflections will be applied to the objects. Measured in the probes's local space.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public Vector3 boxOffset
        {
            get { return _boxOffset; }
            set
            {
                SetValue(nameof(boxOffset), value, ref _boxOffset, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.center != newValue)
                        reflectionProbe.center = newValue;
                });
            }
        }

        /// <summary>
        /// Resolution of the underlying reflection texture in pixels.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public int resolution
        {
            get { return _resolution; }
            set
            {
                SetValue(nameof(resolution), value, ref _resolution, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.resolution != newValue)
                        reflectionProbe.resolution = newValue;
                });
            }
        }

        /// <summary>
        /// Should this reflection probe use HDR rendering?
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public bool hdr
        {
            get { return _hdr; }
            set
            {
                SetValue(nameof(hdr), value, ref _hdr, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.hdr != newValue)
                        reflectionProbe.hdr = newValue;
                });
            }
        }

        /// <summary>
        /// Shadow drawing distance when rendering the probe.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public float shadowDistance
        {
            get { return _shadowDistance; }
            set
            {
                SetValue(nameof(shadowDistance), value, ref _shadowDistance, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.shadowDistance != newValue)
                        reflectionProbe.shadowDistance = newValue;
                });
            }
        }

        /// <summary>
        /// How the reflection probe clears the background.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public UnityEngine.Rendering.ReflectionProbeClearFlags clearFlags
        {
            get { return _clearFlags; }
            set
            {
                SetValue(nameof(clearFlags), value, ref _clearFlags, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.clearFlags != newValue)
                        reflectionProbe.clearFlags = newValue;
                });
            }
        }

        /// <summary>
        /// The color with which the texture of reflection probe will be cleared.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public Color backgroundColor
        {
            get { return _backgroundColor; }
            set
            {
                SetValue(nameof(backgroundColor), value, ref _backgroundColor, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.backgroundColor != newValue)
                        reflectionProbe.backgroundColor = newValue;
                });
            }
        }

        /// <summary>
        /// This is used to render parts of the reflecion probe's surrounding selectively.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public int cullingMask
        {
            get { return _cullingMask; }
            set
            {
                SetValue(nameof(cullingMask), value, ref _cullingMask, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.cullingMask != newValue)
                        reflectionProbe.cullingMask = newValue;
                });
            }
        }

        /// <summary>
        /// The near clipping plane distance when rendering the probe.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public float nearClipPlane
        {
            get { return _nearClipPlane; }
            set
            {
                SetValue(nameof(nearClipPlane), value, ref _nearClipPlane, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.nearClipPlane != newValue)
                        reflectionProbe.nearClipPlane = newValue;
                });
            }
        }

        /// <summary>
        /// The far clipping plane distance when rendering the probe.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetReflectionProbeAdditionalRecordObjects))]
#endif
        public float farClipPlane
        {
            get { return _farClipPlane; }
            set
            {
                SetValue(nameof(farClipPlane), value, ref _farClipPlane, (newValue, oldValue) =>
                {
                    if (reflectionProbe != null && reflectionProbe.farClipPlane != newValue)
                        reflectionProbe.farClipPlane = newValue;
                });
            }
        }
    }
}
