// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    [CustomEditor(typeof(UnityEngine.Object), true)]
    [CanEditMultipleObjects]
    public class EditorBase : UnityEditor.Editor
    {
        public static Action LeftMouseUpInSceneOrInspector;

        protected virtual void OnSceneGUI()
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                SceneManager.LeftMouseUpInSceneOrInspectorEvent?.Invoke();
        }

        public override void OnInspectorGUI()
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                SceneManager.LeftMouseUpInSceneOrInspectorEvent?.Invoke();

            if (SceneManager.Instance(false) != Disposable.NULL)
            {
                if (serializedObject.targetObject != null)
                {
                    EditorGUI.indentLevel = 1;
                    serializedObject.Update();

                    SerializedProperty serializedProperty = serializedObject.GetIterator();
                    if (serializedProperty.NextVisible(true))
                        AddPropertyFields(serializedProperty);
                }
            }
            else
                base.OnInspectorGUI();
        }

        protected virtual void AddPropertyFields(SerializedProperty serializedProperty)
        {
            //Top Menu Button Bar
            if (!IsFallbackValues(serializedProperty.serializedObject.targetObject) && (serializedObject.targetObject is IProperty || serializedObject.targetObject is TransformBase || serializedObject.targetObject is Script))
            {
                IProperty property = serializedObject.targetObject as IProperty;

                int width = 45;
                float inspectorWidth = GUILayoutUtility.GetRect(0.0f, 0.0f, 0.0f, 0.0f).width;

                RenderingManager renderingManager = RenderingManager.Instance();
                if (renderingManager != Disposable.NULL)
                {
                    Texture2D[] headerTextures = renderingManager.headerTextures;

                    Texture2D headerTexture = null;
                    Texture2D headerLineTexture = null;
                    if (property is TransformBase)
                    {
                        headerTexture = headerTextures[0];
                        headerLineTexture = headerTextures[1];
                    }
                    else if (property is IPersistent)
                    {
                        headerTexture = headerTextures[2];
                        headerLineTexture = headerTextures[3];
                    }
                    else if (property is Script)
                    {
                        headerTexture = headerTextures[4];
                        headerLineTexture = headerTextures[5];
                    }
                    else if (property is Visual)
                    {
                        headerTexture = headerTextures[6];
                        headerLineTexture = headerTextures[7];
                    }
                    else if (property is ManagerBase)
                    {
                        headerTexture = headerTextures[8];
                        headerLineTexture = headerTextures[9];
                    }
                    GUI.DrawTexture(new Rect(0.0f, 0.0f, EditorGUIUtility.currentViewWidth, 25.0f), headerTexture);
                    GUI.DrawTexture(new Rect(0.0f, 24.0f, EditorGUIUtility.currentViewWidth, 1.0f), headerLineTexture);
                }

                if (SceneManager.Debugging() && property.notPoolable)
                    GUI.Label(new Rect(20, 5, 150, 15), "Not Poolable");

                Rect position = new Rect(inspectorWidth - 27.0f, 3.0f, width, EditorGUIUtility.singleLineHeight);

                if (GUI.Button(position, new GUIContent("ID", property.id.ToString())))
                {
                    string ids = null;
                    for (int i = 0; i < serializedObject.targetObjects.Length; i++)
                    {
                        if (i != 0)
                            ids += ", ";
                        ids += (serializedObject.targetObjects[i] as IProperty).id.ToString();
                    }
                    GUIUtility.systemCopyBuffer = ids;
                }
                position.x -= width;

                if (property is IPersistent || property is ManagerBase)
                {
                    if (GUI.Button(position, new GUIContent("JSON")))
                    {
                        string json = null;

                        int targetObjectCount = serializedObject.targetObjects.Length;
                        for (int i = 0; i < targetObjectCount; i++)
                        {
                            JSONNode targetObjectJson = (serializedObject.targetObjects[i] as IJson).GetJson();

                            if (targetObjectJson != null)
                            {
                                JsonUtility.FromJson(out string jsonObject, targetObjectJson);

                                if (targetObjectCount > 1)
                                {
                                    if (i == 0)
                                        json = "[";
                                    json += jsonObject;
                                    json += i == targetObjectCount - 1 ? "]" : ",";
                                }
                                else
                                    json = jsonObject;
                            }
                        }

                        GUIUtility.systemCopyBuffer = json;
                    }
                }

                GUILayout.Space(24.0f);
            }

            GUIStyle foldoutHeaderStyle = null;

            BeginFoldoutAttribute beginFoldoutAttribute = null;

            BeginHorizontalGroupAttribute beginHorizontalGroupAttribute = null;

            while (serializedProperty.NextVisible(false))
            {
                if (serializedObject.targetObject != null)
                {
                    IEnumerable<CustomAttribute> customAttributes = MemberUtility.GetMemberAttributes<CustomAttribute>(serializedObject.targetObject, serializedProperty.propertyPath);

                    if (beginFoldoutAttribute == null)
                    {
                        beginFoldoutAttribute = MemberUtility.GetFirstAttribute<BeginFoldoutAttribute>(customAttributes);
                        if (beginFoldoutAttribute != null)
                        {
                            beginFoldoutAttribute.serializedPropertyPath = serializedProperty.propertyPath;
                            beginFoldoutAttribute.foldoutCreated = false;
                        }
                    }

                    SerializedProperty beginFoloutSerializedProperty = null;
                    if (beginFoldoutAttribute != null)
                        beginFoloutSerializedProperty = serializedObject.FindProperty(beginFoldoutAttribute.serializedPropertyPath);

                    if (beginHorizontalGroupAttribute == null)
                    {
                        beginHorizontalGroupAttribute = MemberUtility.GetFirstAttribute<BeginHorizontalGroupAttribute>(customAttributes);
                        if (beginHorizontalGroupAttribute != null)
                            beginHorizontalGroupAttribute.horizontalGroupCreated = false;
                    }

                    if (IncludeField(customAttributes, serializedProperty))
                    {
                        if (beginFoldoutAttribute != null && !beginFoldoutAttribute.foldoutCreated)
                        {
                            Rect position = GUILayoutUtility.GetRect(0.0f, 0.0f);

                            position.y += position.height + EditorGUIUtility.standardVerticalSpacing - 1.0f;
                            position.x += 2.0f;
                            position.height = 22.0f;

                            RenderingManager renderingManager = RenderingManager.Instance(false);
                            if (renderingManager != Disposable.NULL)
                                GUI.DrawTexture(position, renderingManager.headerTextures[10]);

                            if (foldoutHeaderStyle == null)
                                foldoutHeaderStyle = new GUIStyle(EditorStyles.foldoutHeader);

                            foldoutHeaderStyle.fixedWidth = position.width - 5.0f;
                            beginFoloutSerializedProperty.isExpanded = EditorGUILayout.Foldout(beginFoloutSerializedProperty.isExpanded, beginFoldoutAttribute.label, true, foldoutHeaderStyle);

                            EditorGUILayout.Space(8.0f);

                            if (beginFoloutSerializedProperty.isExpanded)
                                EditorGUI.indentLevel++;

                            beginFoldoutAttribute.foldoutCreated = true;
                        }

                        if (beginHorizontalGroupAttribute != null && !beginHorizontalGroupAttribute.horizontalGroupCreated)
                        {
                            if (beginHorizontalGroupAttribute.hideLabels)
                                EditorGUIUtility.labelWidth = 0.0001f;
                            EditorGUIUtility.fieldWidth = 30.0f;
                            EditorGUILayout.BeginHorizontal();

                            beginHorizontalGroupAttribute.horizontalGroupCreated = true;
                        }

                        if (beginFoldoutAttribute == null || beginFoloutSerializedProperty.isExpanded)
                        {
                            string labelOverride = null;
                            LabelOverrideAttribute labelOverrideAttribute = MemberUtility.GetFirstAttribute<LabelOverrideAttribute>(customAttributes);
                            if (labelOverrideAttribute != null)
                                labelOverride = labelOverrideAttribute.label;

                            EditorGUI.BeginChangeCheck();

                            UnityEngine.Object[] targetObjects = serializedProperty.serializedObject.targetObjects;

                            string typeStr = serializedProperty.type;
                            Type type = typeStr.StartsWith("PPtr<$") ? typeof(OptionalPropertiesBase).Assembly.GetType(typeof(SceneManager).Namespace + "." + typeStr.Substring(6, typeStr.Length - 7)) : null;

                            if (type != null && typeof(OptionalPropertiesBase).IsAssignableFrom(type))
                                AddOptionalProperties(serializedProperty);
                            else
                                AddProperty(serializedProperty, labelOverride);

                            if (EditorGUI.EndChangeCheck())
                            {
                                for (int i = 0; i < targetObjects.Length; i++)
                                {
                                    UnityEngine.Object targetObject = targetObjects[i];
                                    if (targetObject is OptionalPropertiesBase)
                                        targetObjects[i] = (targetObject as OptionalPropertiesBase).parent;
                                }

                                UnityEngine.Object firstTargetObject = targetObjects[0];

                                PropertyInfo propertyInfo = MemberUtility.GetMemberInfoFromMemberName<PropertyInfo>(firstTargetObject.GetType(), GetName(serializedProperty.propertyPath));

                                if (propertyInfo != null && propertyInfo.PropertyType != typeof(Datasource))
                                {
                                    if (propertyInfo.CanWrite)
                                    {
                                        if (targetObjects.Length > 0)
                                        {
                                            object value = GetValue(serializedProperty);

                                            string undoGroupName = "Modified Property in " + (targetObjects.Length == 1 ? targetObjects[0].name : "Multiple Objects");

                                            if (propertyInfo.Name == TransformDouble.GetLocalEditorEulerAnglesHintName())
                                                undoGroupName = "Set Rotation";
                                            else if (propertyInfo.Name == nameof(TransformDouble.localPosition) || propertyInfo.Name == nameof(TransformDouble.localScale))
                                            {
                                                undoGroupName = "Set ";

                                                string valueStr = ((Vector3Double)value).ToString("F2");
                                                if (propertyInfo.Name == nameof(TransformDouble.localPosition))
                                                    undoGroupName += "Position to " + valueStr + " in ";
                                                else
                                                    undoGroupName += "Scale to " + valueStr + " in ";

                                                undoGroupName += serializedProperty.serializedObject.targetObjects.Length == 1 ? firstTargetObject.name : "Selected Objects";
                                            }

                                            UndoManager.QueueSetCurrentGroupName(undoGroupName);

                                            foreach (UnityEngine.Object targetObject in targetObjects)
                                                SetPropertyValue(targetObject, propertyInfo, value);
                                        }
                                    }
                                    else
                                    {
                                        if (propertyInfo.Name != nameof(DatasourceBase.datasource))
                                            Debug.LogWarning("Property '" + propertyInfo.Name + "' is readOnly");
                                    }
                                }
                                else
                                {
                                    if (serializedProperty.serializedObject.targetObject != null)
                                    {
                                        FieldInfo fieldInfo = MemberUtility.GetMemberInfoFromMemberName<FieldInfo>(serializedProperty.serializedObject.targetObject.GetType(), serializedProperty.propertyPath);

                                        ButtonAttribute btnAttribute = null;

                                        if (fieldInfo != null)
                                            btnAttribute = fieldInfo.GetCustomAttribute<ButtonAttribute>();

                                        if (btnAttribute == null)
                                            Debug.LogWarning("Missing property '" + serializedProperty.propertyPath + "'");
                                    }
                                }
                            }
                        }
                    }

                    EndHorizontalGroupAttribute endHorizontalGroupAttribute = MemberUtility.GetFirstAttribute<EndHorizontalGroupAttribute>(customAttributes);
                    if (endHorizontalGroupAttribute != null)
                    {
                        if (beginHorizontalGroupAttribute != null && beginHorizontalGroupAttribute.horizontalGroupCreated)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUIUtility.labelWidth = 0.0f;
                        }

                        beginHorizontalGroupAttribute = null;
                    }

                    EndFoldoutAttribute endFoldoutAttribute = MemberUtility.GetFirstAttribute<EndFoldoutAttribute>(customAttributes);
                    if (endFoldoutAttribute != null)
                    {
                        if (beginFoldoutAttribute != null && beginFoldoutAttribute.foldoutCreated)
                        {
                            if (beginFoloutSerializedProperty.isExpanded)
                            {
                                EditorGUILayout.Space(endFoldoutAttribute.space);
                                EditorGUI.indentLevel--;
                            }

                            EditorGUILayout.EndFoldoutHeaderGroup();
                        }

                        beginFoldoutAttribute = null;
                    }
                }
            }

            AddAdditionalEditor(serializedProperty);
        
            if (!IsFallbackValues(serializedProperty.serializedObject.targetObject))
                AddHelpBox();
        }

        protected virtual void AddAdditionalEditor(SerializedProperty serializedProperty)
        {

        }

        protected void SetPropertyValue(UnityEngine.Object targetObject, PropertyInfo propertyInfo, object value)
        {
            if (IsFallbackValues(targetObject))
            {
                UndoManager.RecordObject(targetObject);
                if (targetObject is Object)
                    UndoManager.RecordObject((targetObject as Object).objectAdditionalFallbackValues);
                propertyInfo.SetValue(targetObject, value);
            }
            else
            {
                UndoManager.QueueRecordObjectWrapper(targetObject, () =>
                {
                    UnityEngine.Object[] additionalTargetObjects = null;

                    RecordAdditionalObjectsAttribute recordAdditionalObjectsAttribute = propertyInfo.GetCustomAttribute<RecordAdditionalObjectsAttribute>();
                    if (recordAdditionalObjectsAttribute != null)
                    {
                        MethodInfo methodInfo = MemberUtility.GetMethodInfoFromMethodName(targetObject, recordAdditionalObjectsAttribute.methodName);
                        if (methodInfo != null)
                            additionalTargetObjects = methodInfo.Invoke(targetObject, null) as UnityEngine.Object[];
                        else
                            Debug.LogError("RecordAdditionalObjectsAttribute method '" + recordAdditionalObjectsAttribute.methodName + "' not found!");
                    }

                    if (additionalTargetObjects != null && additionalTargetObjects.Length > 0)
                        UndoManager.RecordObjects(additionalTargetObjects);

                    SceneManager.UserContext(() => { propertyInfo.SetValue(targetObject, value); });
                });
            }
        }

        private static string GetName(string name)
        {
            if (name.Length > 0 && name[0] == '_')
                name = name.Substring(1);

            return name;
        }

        public static object GetValue(SerializedProperty serializedProperty)
        {
            if (serializedProperty.isArray && serializedProperty.arrayElementType != "char")
            {
                Type genericListType = typeof(List<>).MakeGenericType(GetType(serializedProperty.arrayElementType));
                IList list = (IList)Activator.CreateInstance(genericListType);

                for (int i = 0; i < serializedProperty.arraySize; i++)
                    list.Add(GetValue(serializedProperty.GetArrayElementAtIndex(i)));

                return list;
            }
            else if (serializedProperty.type == nameof(SerializableGuid))
            {
                SerializableGuid parsedSerializableGuid = SerializableGuid.Empty;

                SerializableGuid.TryParse(serializedProperty.FindPropertyRelative(SerializableGuid.GUID_STR_FIELD_NAME).stringValue, out parsedSerializableGuid);

                return parsedSerializableGuid;
            }
            else
            {
                return serializedProperty.propertyType switch
                {
                    SerializedPropertyType.Float =>
                        GetFloat(serializedProperty),
                    SerializedPropertyType.Integer =>
                        GetInteger(serializedProperty),
                    SerializedPropertyType.Boolean =>
                        serializedProperty.boolValue,
                    SerializedPropertyType.String =>
                        serializedProperty.stringValue,
                    SerializedPropertyType.Color =>
                        serializedProperty.colorValue,
                    SerializedPropertyType.ObjectReference =>
                        serializedProperty.objectReferenceValue,
                    SerializedPropertyType.LayerMask =>
                        serializedProperty.intValue,
                    SerializedPropertyType.Enum =>
                        serializedProperty.enumValueFlag,
                    SerializedPropertyType.Vector2 =>
                        serializedProperty.vector2Value,
                    SerializedPropertyType.Vector3 =>
                        serializedProperty.vector3Value,
                    SerializedPropertyType.Vector4 =>
                        serializedProperty.vector4Value,
                    SerializedPropertyType.Rect =>
                        serializedProperty.rectValue,
                    SerializedPropertyType.ArraySize =>
                        serializedProperty.arraySize,
                    SerializedPropertyType.Character =>
                        serializedProperty.intValue,
                    SerializedPropertyType.AnimationCurve =>
                        serializedProperty.animationCurveValue,
                    SerializedPropertyType.Bounds =>
                        serializedProperty.boundsValue,
                    SerializedPropertyType.Gradient =>
                        GetGradient(serializedProperty),
                    SerializedPropertyType.Quaternion =>
                        serializedProperty.quaternionValue,
                    SerializedPropertyType.ExposedReference =>
                        serializedProperty.exposedReferenceValue,
                    SerializedPropertyType.FixedBufferSize =>
                        serializedProperty.fixedBufferSize,
                    SerializedPropertyType.Vector2Int =>
                        serializedProperty.vector2IntValue,
                    SerializedPropertyType.Vector3Int =>
                        serializedProperty.vector3IntValue,
                    SerializedPropertyType.RectInt =>
                        serializedProperty.rectIntValue,
                    SerializedPropertyType.BoundsInt =>
                        serializedProperty.boundsIntValue,
                    SerializedPropertyType.ManagedReference =>
                        serializedProperty.managedReferenceValue,
                    SerializedPropertyType.Hash128 =>
                        serializedProperty.hash128Value,
                    SerializedPropertyType.Generic =>
                        Convert.ChangeType(serializedProperty.boxedValue, GetType(serializedProperty.type)),
                    _ => null
                };
            }
        }

        private static object GetFloat(SerializedProperty serializedProperty)
        {
            if (serializedProperty.numericType == SerializedPropertyNumericType.Double)
                return serializedProperty.doubleValue;
            return serializedProperty.floatValue;
        }

        private static object GetInteger(SerializedProperty serializedProperty)
        {
            if (serializedProperty.numericType == SerializedPropertyNumericType.UInt32)
                return (UInt32)serializedProperty.intValue;
            return serializedProperty.intValue;
        } 

        private static Gradient GetGradient(SerializedProperty gradientProperty)
        {
            PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty("gradientValue",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (propertyInfo == null) return null;

            return propertyInfo.GetValue(gradientProperty, null) as Gradient;
        }

        private static Type GetType(string typeStr)
        {
            if (JsonUtility.FromJson(out Type type, typeStr))
                return type;
            else
                return null;
        }

        protected virtual void AddHelpBox()
        {
            List<(string, MessageType)> helpBoxes = new List<(string, MessageType)>();

            UnityEngine.Object targetObject = target;

            if (targetObject is Star)
            {
                Star star = targetObject as Star;
                if (star != Disposable.NULL)
                {
                    if (star.useOcclusion)
                        helpBoxes.Add(("LensFlare Occlusion only work against MeshRenderers equipped with a collider", MessageType.Warning));
                }
            }
            else if (targetObject is FallbackValues)
            {
                FallbackValues fallbackValues = targetObject as FallbackValues;

                if (fallbackValues != Disposable.NULL)
                {
                    Type type = fallbackValues.GetFallbackValuesType();
                    if (type != null && typeof(TerrainGridMeshObject).IsAssignableFrom(type) && fallbackValues.GetProperty(out int layer, nameof(Object.layer)) && layer != LayerUtility.GetDefaultLayer(typeof(TerrainGridMeshObject)))
                        helpBoxes.Add(("Changing Terrain layer might break some features", MessageType.Warning));
                }
            }
            else if (targetObject is AtmosphereEffect)
            {
                AtmosphereEffect atmopshereEffect = targetObject as AtmosphereEffect;

                Star star = InstanceManager.Instance(false)?.GetStar();

                if (star == Disposable.NULL)
                    helpBoxes.Add(("Add a 'Star' to the Scene to control light direction", MessageType.Warning));

                GeoAstroObject geoAstroObject = atmopshereEffect != Disposable.NULL ? atmopshereEffect.geoAstroObject : null;
                Vector3Double lossyScale = geoAstroObject != Disposable.NULL ? geoAstroObject.transform.lossyScale : Vector3Double.one;
                if (!lossyScale.IsUniform() || geoAstroObject == Disposable.NULL || !geoAstroObject.IsSpherical())
                    helpBoxes.Add(("Atmosphere requires a 'GeoAstroObject' with a sphericalRatio of 1.0 and a uniform scale", MessageType.Warning));
            }
            else if (targetObject is CameraController)
            {
                CameraController cameraController = targetObject as CameraController;

                if (cameraController.duration != 0.0f && cameraController.target != Disposable.NULL && cameraController.objectBase != Disposable.NULL)
                {
                    if (cameraController.objectBase.animator == Disposable.NULL || !(cameraController.objectBase.animator is TargetControllerAnimator) || cameraController.target.animator == Disposable.NULL || !(cameraController.target.animator is TransformAnimator))
                        helpBoxes.Add(("Animation requires a '" + typeof(TargetControllerAnimator).Name + "' in the 'Camera' and a '" + typeof(TransformAnimator).Name + "' in the 'Target'", MessageType.Warning));
                }
            }
            else if (targetObject is CameraGrid2DLoader)
            {
                CameraGrid2DLoader cameraGrid2DLoader = targetObject as CameraGrid2DLoader;

                GeoAstroObject geoAstroObject = cameraGrid2DLoader != Disposable.NULL && cameraGrid2DLoader.objectBase != Disposable.NULL ? cameraGrid2DLoader.objectBase.transform.GetParentGeoAstroObject() : null;
                Vector3Double lossyScale = geoAstroObject != Disposable.NULL ? geoAstroObject.transform.lossyScale : Vector3Double.one;
                if (!lossyScale.IsUniform())
                    helpBoxes.Add(("Non-uniform " + typeof(GeoAstroObject).Name + " scaling may result in incorrect index being loaded. For uniform 'AstroObject' scaling use 'AstroObject.size' instead", MessageType.Warning));
            }
            else if (targetObject is GeoCoordinateController)
            {
                GeoCoordinateController geoCoordinateController = targetObject as GeoCoordinateController;

                if (geoCoordinateController != Disposable.NULL && geoCoordinateController.objectBase != Disposable.NULL && geoCoordinateController.objectBase.transform != Disposable.NULL && !geoCoordinateController.objectBase.transform.isGeoCoordinateTransform)
                    helpBoxes.Add(("Requires GeoCoordinate Transform", MessageType.Warning));
            }
            else if (targetObject is Grid2DMeshObjectBase)
            {
                Grid2DMeshObjectBase grid2DMeshObject = targetObject as Grid2DMeshObjectBase;

                if (grid2DMeshObject.transform != Disposable.NULL && !grid2DMeshObject.transform.isGeoCoordinateTransform)
                    helpBoxes.Add((nameof(TerrainGridMeshObject)+" requires a parent "+nameof(GeoAstroObject)+" for proper positioning", MessageType.Warning));
            }
            else if (targetObject is GridGenerator)
            {
                GridGenerator gridGenerator = targetObject as GridGenerator;

                GeoAstroObject geoAstroObject = gridGenerator.objectBase is GeoAstroObject ? gridGenerator.objectBase as GeoAstroObject : gridGenerator.transform.parentGeoAstroObject;
                if (geoAstroObject == Disposable.NULL)
                    helpBoxes.Add((typeof(GridGenerator).Name + " requires a parent 'GeoAstroObject'", MessageType.Warning));
            }
            else if (targetObject is OrbitController)
            {
                OrbitController orbitController = targetObject as OrbitController;

                if (orbitController != Disposable.NULL && orbitController.GetStarSystem() == Disposable.NULL)
                    helpBoxes.Add(("Requires a 'StarSystem' as a parent", MessageType.Warning));
            }
            else if (targetObject is RenderingManager)
            {
                RenderingManager renderingManager = targetObject as RenderingManager;

                if (renderingManager != Disposable.NULL)
                {
                    if (renderingManager.dynamicFocusDistance && (Camera.main == Disposable.NULL || !(Camera.main.controller is TargetControllerBase)))
                        helpBoxes.Add(("DynamicFocusDistance requires a Main Camera with a '" + nameof(TargetController) + "' or '" + nameof(CameraController) + "'", MessageType.Warning));
                }
            }
            else if (targetObject is StarSystem)
            {
                helpBoxes.Add(("Only child 'AstroObject' with an 'OrbitController' will be affected by the 'StarSystem'", MessageType.Info));
            }
            else if (targetObject is XYZController)
            {
                XYZController controller = targetObject as XYZController;

                if (controller != Disposable.NULL && controller.objectBase != Disposable.NULL && controller.objectBase.transform != Disposable.NULL && controller.objectBase.transform.isGeoCoordinateTransform)
                    helpBoxes.Add(("Requires XYZ Transform", MessageType.Warning));
            }
            else if (targetObject is VolumeMaskBase)
            {
                if (!RenderingManager.COMPUTE_BUFFER_SUPPORTED)
                    helpBoxes.Add(("VolumeMask's are not currently supported for the current build platform.", MessageType.Error));
            }

            foreach ((string, MessageType) helpBoxData in helpBoxes)
                EditorGUILayout.HelpBox(helpBoxData.Item1, helpBoxData.Item2);
        }

        protected bool IncludeField(IEnumerable<CustomAttribute> customAttributes, SerializedProperty serializedProperty)
        {
            return GetConditionalShowAttributeMethodValue(customAttributes, serializedProperty);
        }

        public static bool IsFallbackValues(UnityEngine.Object targetObject)
        {
            return targetObject is IScriptableBehaviour ? (targetObject as IScriptableBehaviour).isFallbackValues : false;
        }

        [Serializable]
        private class OptionalPropertiesEditorDictionary : SerializableDictionary<OptionalPropertiesBase, UnityEditor.Editor> { };

        private OptionalPropertiesEditorDictionary _optionalPropertiesEditors;
        protected virtual void AddOptionalProperties(SerializedProperty serializedProperty)
        {
            if (_optionalPropertiesEditors == null)
                _optionalPropertiesEditors = new OptionalPropertiesEditorDictionary();

            OptionalPropertiesBase optionalProperties = serializedProperty.objectReferenceValue as OptionalPropertiesBase;
            if (!_optionalPropertiesEditors.TryGetValue(optionalProperties, out UnityEditor.Editor optionalPropertiesEditor))
            {
                CreateCachedEditor(optionalProperties, null, ref optionalPropertiesEditor);
                _optionalPropertiesEditors[optionalProperties] = optionalPropertiesEditor;
            }

            optionalPropertiesEditor.OnInspectorGUI();
        }

        protected virtual void AddProperty(SerializedProperty serializedProperty, string labelOverride)
        {
            GUIContent guiContent = null;
            if (labelOverride != null)
                guiContent = new GUIContent(labelOverride, serializedProperty.tooltip);

            EditorGUILayout.PropertyField(serializedProperty, guiContent);
        }

        public static bool GetConditionalShowAttributeMethodValue(IEnumerable<CustomAttribute> customAttributes, SerializedProperty serializedProperty)
        {
            bool show = true;

            ConditionalShowAttribute conditionShowAttribute = MemberUtility.GetFirstAttribute<ConditionalShowAttribute>(customAttributes);
            if (conditionShowAttribute != null)
                show = GetConditionalAttributeMethodValue(conditionShowAttribute, serializedProperty);

            return show;
        }

        public static bool GetConditionalEnableAttributeMethodValue(IEnumerable<CustomAttribute> customAttributes, SerializedProperty serializedProperty)
        {
            bool enable = true;

            ConditionalEnableAttribute conditionShowAttribute = MemberUtility.GetFirstAttribute<ConditionalEnableAttribute>(customAttributes);
            if (conditionShowAttribute != null)
                enable = GetConditionalAttributeMethodValue(conditionShowAttribute, serializedProperty);

            if (enable)
            {
                if (serializedProperty.type == "PPtr<$" + nameof(Datasource) + ">")
                {
                    Datasource datasource = GetDatasourceFromSerializedProperty(serializedProperty.serializedObject.targetObject);

                    if (datasource != null && datasource.persistentCount == 0)
                        enable = false;
                }
            }

            return enable;
        }

        public static Datasource GetDatasourceFromSerializedProperty(UnityEngine.Object targetObject)
        {
            Datasource datasource = null;

            if (targetObject is DatasourceBase)
                datasource = (targetObject as DatasourceBase).datasource;
            if (targetObject is DatasourceManager)
                datasource = (targetObject as DatasourceManager).sceneDatasource;

            return datasource;
        }

        public static bool GetConditionalAttributeMethodValue(ConditionalAttribute conditionalAttribute, SerializedProperty serializedProperty)
        {
            bool value = true;

            if (!string.IsNullOrEmpty(conditionalAttribute.methodName))
            {
                UnityEngine.Object targetObject = serializedProperty.serializedObject.targetObject;

                MethodInfo methodInfo = MemberUtility.GetMethodInfoFromMethodName(targetObject, conditionalAttribute.methodName);

                if (methodInfo != null)
                {
                    if (methodInfo.GetParameters().Length == 1)
                        value = (bool)methodInfo.Invoke(targetObject, new object[] { serializedProperty });
                    else
                        value = (bool)methodInfo.Invoke(targetObject, null);
                }
                else
                    Debug.LogWarning("Attempting to use a ConditionalAttribute but no matching field '" + conditionalAttribute.methodName + "' found in object '" + targetObject.name + "'");
            }

            return value;
        }

        protected virtual void OnDisable()
        {
            if (_optionalPropertiesEditors != null)
            {
                foreach (UnityEditor.Editor optionalPropertiesEditor in _optionalPropertiesEditors.Values)
                    DisposeManager.Destroy(optionalPropertiesEditor);
            }
        }

        protected virtual void OnDestroy()
        {

        }
    }
}
