// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class Stack : MonoBehaviour
    {
        /// <summary>
        /// Is this the main Camera Stack?
        /// </summary>
        [BeginFoldout("Stack")]
        [SerializeField, Tooltip("Is this the main Camera Stack?"), EndFoldout]
        public bool main;

        /// <summary>
        /// Should the Camera renderer properties (useOcclusionCulling, allowHDR, renderShadows, volumeLayerMask, volumeTrigger, renderPostProcessing) be synched with the stacked cameras?
        /// </summary>
        [BeginFoldout("Camera")]
        [SerializeField, Tooltip("Should the Camera renderer properties (useOcclusionCulling, allowHDR, renderShadows, volumeLayerMask, volumeTrigger, renderPostProcessing) be synched with the stacked cameras?")]
        public bool synchRenderProperties;
        /// <summary>
        /// Should the Camera optical properties (fieldOfView, orthographic, orthographicSize) be synched with the stacked cameras?
        /// </summary>
        [SerializeField, Tooltip("Should the Camera optical properties (fieldOfView, orthographic, orthographicSize) be synched with the stacked cameras?")]
        public bool synchOpticalProperties;
        /// <summary>
        /// Should the Camera optical properties (aspect) be synched with the stacked cameras?
        /// </summary>
        [SerializeField, Tooltip("Should the Camera optical properties (aspect) be synched with the stacked cameras?")]
        public bool synchAspectProperty;
        /// <summary>
        /// Should the Camera optical properties (clearFlags, backgroundColor) be synched with the stacked cameras?
        /// </summary>
        [SerializeField, Tooltip("Should the Camera optical properties (clearFlags, backgroundColor) be synched with the stacked cameras?")]
        public bool synchBackgroundProperties;
        /// <summary>
        /// Should the Camera optical properties (farClipPlane, nearClipPlane) be synched with the stacked cameras?
        /// </summary>
        [SerializeField, Tooltip("Should the Camera optical properties (farClipPlane, nearClipPlane) be synched with the stacked cameras?")]
        public bool synchClipPlaneProperties;
        /// <summary>
        /// Should the Camera optical properties (cullingMask) be synched with the stacked cameras?
        /// </summary>
        [SerializeField, Tooltip("Should the Camera optical properties (cullingMask) be synched with the stacked cameras?"), EndFoldout]
        public bool synchCullingMaskProperty;

        private int _index;

        public int index
        {
            get { return _index; }
            set { _index = value; }
        }
    }
}
