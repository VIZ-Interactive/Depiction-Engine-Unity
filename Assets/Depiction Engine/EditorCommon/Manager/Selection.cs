// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine.Editor
{
    public class Selection : ScriptableObjectBase
    {
        public const string PERSISTENT_PREFIX = "#id:";

        [NonSerialized]
        private static List<TransformDouble> _transformDoublesList;
        private static bool _selectionOriginShiftDirty;

        public static void SelectionChanged()
        {
            _selectionOriginShiftDirty = true;
        }

        public static bool Contains(TransformDouble transformDouble)
        {
            //Do not use UnityEditor.Selection.Contains as it is not always up to date and will cause some problems with some of the SceneView Functions such as 'FrameSelected()'
            if (transformDouble != Disposable.NULL)
            {
                foreach (Transform transform in UnityEditor.Selection.transforms)
                {
                    if (transformDouble.transform == transform)
                        return true;
                }
            }

            return false;
        }

        public static TransformDouble activeTransform
        {
            get 
            {
                TransformDouble activeTransformDouble = null;

                if (UnityEditor.Selection.activeTransform != null)
                    activeTransformDouble = UnityEditor.Selection.activeTransform.GetComponent<TransformDouble>();
                
                return activeTransformDouble;
            }
            set { UnityEditor.Selection.activeTransform = value != Disposable.NULL ? value.transform : null; }
        }

        public static List<TransformDouble> GetTransforms()
        {
            if (_transformDoublesList == null)
                _transformDoublesList = new List<TransformDouble>();
            _transformDoublesList.Clear();

            foreach (Transform transform in UnityEditor.Selection.transforms)
            {
                TransformDouble transformDouble = transform.GetComponent<TransformDouble>();
                if (transformDouble != Disposable.NULL)
                    _transformDoublesList.Add(transformDouble);
            }
            return _transformDoublesList;
        }

        public static int GetTransformDoubleSelectionCount()
        {
            int transformDoubleSelectionCount = 0;

            foreach (Transform transform in UnityEditor.Selection.transforms)
            {
                if (transform.GetComponent<TransformDouble>() != Disposable.NULL)
                    transformDoubleSelectionCount++;
            }

            return transformDoubleSelectionCount;
        }

        private static bool IsValidPosition(Vector3 position)
        {
            return position != Vector3.positiveInfinity && position.magnitude < 9999.0d;
        }

        public static bool MoveOriginCloserToPoint(Func<Vector3> callback, Func<Vector3, Vector3> recursingCallback = null)
        {
            if (callback == null)
                return false;

            Vector3 point = callback();
            bool isValidPoint = IsValidPosition(point);

            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager != Disposable.NULL && renderingManager.originShifting)
            {
                int recursiveCount = 0;
                while (!isValidPoint && recursiveCount < 10)
                {
                    recursiveCount++;

                    if (recursingCallback != null)
                        point = recursingCallback(point);

                    ApplyOriginShifting(TransformDouble.AddOrigin(point));

                    point = callback();
                    isValidPoint = IsValidPosition(point);
                }
            }

            return isValidPoint;
        }

        private static List<TransformDouble> _rootTransformDoubles;
        public static bool ApplyOriginShifting(Vector3Double origin)
        {
            bool selectionOnly = false;

            RenderingManager renderingManager = RenderingManager.Instance(false);
            if (renderingManager != Disposable.NULL && renderingManager.originShifting)
            {
                List<TransformDouble> rootTransformDoubles;
                bool selectionOriginShiftDirty;

                if (UnityEditor.Selection.transforms.Length != 0 && UnityEditor.Selection.transforms.Length == GetTransformDoubleSelectionCount())
                {
                    selectionOnly = true;

                    selectionOriginShiftDirty = _selectionOriginShiftDirty;

                    if (_rootTransformDoubles == null)
                        _rootTransformDoubles = new List<TransformDouble>();
                    _rootTransformDoubles.Clear();

                    rootTransformDoubles = GetRootTransforms(ref _rootTransformDoubles);
                }
                else
                {
                    selectionOriginShiftDirty = false;

                    rootTransformDoubles = null;
                }

                _selectionOriginShiftDirty = false;

                TransformDouble.ApplyOriginShifting(rootTransformDoubles, origin, true, selectionOriginShiftDirty);
            }

            return selectionOnly;
        }

        private static List<TransformDouble> GetRootTransforms(ref List<TransformDouble> rootTransforms)
        {
            foreach (Transform transform in UnityEditor.Selection.transforms)
            {
                TransformDouble rootTransform = transform.GetComponent<TransformDouble>();

                if (rootTransform != Disposable.NULL)
                {
                    while (rootTransform.parent != Disposable.NULL)
                        rootTransform = rootTransform.parent;

                    if (!rootTransforms.Contains(rootTransform))
                        rootTransforms.Add(rootTransform);
                }
            }

            return rootTransforms;
        }

        public static IPersistent GetSelectedDatasourcePersistent()
        {
            string focused = GUI.GetNameOfFocusedControl();
            if (!string.IsNullOrEmpty(focused))
            {
                int index = focused.IndexOf(PERSISTENT_PREFIX);
                if (index == 0)
                {
                    if (SerializableGuid.TryParse(focused.Substring(PERSISTENT_PREFIX.Length), out SerializableGuid id))
                    {
                        IPersistent persistent = InstanceManager.Instance().GetPersistent(id);
                        if (!Disposable.IsDisposed(persistent))
                            return persistent;
                    }
                }
            }

            return null;
        }
    }
}
#endif
