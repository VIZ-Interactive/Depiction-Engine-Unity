// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DepictionEngine.Editor
{
    //Drawers are all handled through this one central class to allow multiple attributes to be used at the same time
    [CustomPropertyDrawer(typeof(Vector3)), CustomPropertyDrawer(typeof(Vector3Int)), CustomPropertyDrawer(typeof(Vector3Double))]
    [CustomPropertyDrawer(typeof(Vector2)), CustomPropertyDrawer(typeof(Vector2Int)), CustomPropertyDrawer(typeof(Vector2Double))]
    [CustomPropertyDrawer(typeof(GeoCoordinate3)), CustomPropertyDrawer(typeof(GeoCoordinate3Double))]
    [CustomPropertyDrawer(typeof(GeoCoordinate2)), CustomPropertyDrawer(typeof(GeoCoordinate2Double))]
    [CustomPropertyDrawer(typeof(SerializableGuid))]
    [CustomPropertyDrawer(typeof(Datasource))]
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    [CustomPropertyDrawer(typeof(MaskAttribute))]
    [CustomPropertyDrawer(typeof(ConditionalEnableAttribute))]
    [CustomPropertyDrawer(typeof(ConditionalShowAttribute))]
    public class PropertyDrawerBase : PropertyDrawer
    {
        private const float SUB_LABEL_SPACING = 4.0f;
        private const float INDENT_WIDTH = 15.0f;
        private const string VECTOR_MIN_NAME = "x";
        private const string VECTOR_MAX_NAME = "y";
        private const float FLOAT_FIELD_WIDTH = 30.0f;
        private const float SPACING = 2.0f;
        private const float ROUNDING_VALUE = 100.0f;

        public override void OnGUI(Rect position, SerializedProperty serializedProperty, GUIContent label)
        {
            IEnumerable<CustomAttribute> customAttributes = MemberUtility.GetMemberAttributes<CustomAttribute>(serializedProperty.serializedObject.targetObject, serializedProperty.propertyPath);

            if (EditorBase.GetConditionalShowAttributeMethodValue(customAttributes, serializedProperty))
            {
                bool lastEnabled = GUI.enabled;
                GUI.enabled = EditorBase.GetConditionalEnableAttributeMethodValue(customAttributes, serializedProperty);

                bool propertyFieldsOverrided = false;

                switch (serializedProperty.type)
                {
                    case nameof(Vector3): case nameof(Vector3Int): case nameof(Vector3Double):
                        propertyFieldsOverrided = AddVectorPropertyFields(position, serializedProperty, label, true);
                        break;
                    case nameof(Vector2): case nameof(Vector2Int): case nameof(Vector2Double):
                        propertyFieldsOverrided = AddVectorPropertyFields(position, serializedProperty, label);
                        break;
                    case nameof(GeoCoordinate3): case nameof(GeoCoordinate3Double):
                        propertyFieldsOverrided = AddGeoCoordinatePropertyFields(position, serializedProperty, label, true);
                        break;
                    case nameof(GeoCoordinate2): case nameof(GeoCoordinate2Double):
                        propertyFieldsOverrided = AddGeoCoordinatePropertyFields(position, serializedProperty, label);
                        break;
                    case nameof(SerializableGuid):
                        propertyFieldsOverrided = AddSerializableGuidPropertyField(position, serializedProperty, label, customAttributes);
                        break;
                    case "PPtr<$" + nameof(Datasource) + ">":
                        propertyFieldsOverrided = AddDatasourcePropertyField(position, serializedProperty, label);
                        break;
                    case "bool":
                        propertyFieldsOverrided = AddBoolPropertyFields(position, serializedProperty, label);
                        break;
                    case "int":
                        propertyFieldsOverrided = AddIntPropertyFields(position, serializedProperty, label);
                        break;
                }

                if (!propertyFieldsOverrided)
                    AddPropertyField(position, serializedProperty, label);
             
                GUI.enabled = lastEnabled;
            }
        }

        private bool AddVectorPropertyFields(Rect position, SerializedProperty serializedProperty, GUIContent label, bool includeZ = false)
        {
            if (attribute is MinMaxRangeAttribute)
            {
                float min;
                float max;

                if (serializedProperty.type == typeof(Vector2).Name)
                {
                    Vector2 v = serializedProperty.vector2Value;
                    min = v.x;
                    max = v.y;
                }
                else if (serializedProperty.type == typeof(Vector2Int).Name)
                {
                    min = serializedProperty.FindPropertyRelative("x").intValue;
                    max = serializedProperty.FindPropertyRelative("y").intValue;
                }
                else
                    return true;

                float ppp = EditorGUIUtility.pixelsPerPoint;
                float spacing = SPACING * ppp;
                float fieldWidth = FLOAT_FIELD_WIDTH * ppp;

                var lastIndentLevel = EditorGUI.indentLevel;

                var attr = attribute as MinMaxRangeAttribute;

                var r = EditorGUI.PrefixLabel(position, label);

                Rect sliderPos = r;

                sliderPos.x += fieldWidth + spacing;
                sliderPos.width -= (fieldWidth + spacing) * 2;

                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel = 0;
                EditorGUI.MinMaxSlider(sliderPos, ref min, ref max, attr.min, attr.max);
                EditorGUI.indentLevel = lastIndentLevel;
                if (EditorGUI.EndChangeCheck())
                    SetVectorValue(serializedProperty, min, max);

                Rect minPos = r;
                minPos.width = fieldWidth;

                var vectorMinProp = serializedProperty.FindPropertyRelative(VECTOR_MIN_NAME);
                EditorGUI.showMixedValue = vectorMinProp.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel = 0;
                min = EditorGUI.DelayedFloatField(minPos, min);
                EditorGUI.indentLevel = lastIndentLevel;
                if (EditorGUI.EndChangeCheck())
                {
                    min = Mathf.Max(min, attr.min);
                    min = Mathf.Min(min, max);
                    SetVectorValue(serializedProperty, min, max);
                }

                Rect maxPos = position;
                maxPos.x += maxPos.width - fieldWidth;
                maxPos.width = fieldWidth;

                var vectorMaxProp = serializedProperty.FindPropertyRelative(VECTOR_MAX_NAME);
                EditorGUI.showMixedValue = vectorMaxProp.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                EditorGUI.indentLevel = 0;
                max = EditorGUI.DelayedFloatField(maxPos, max);
                EditorGUI.indentLevel = lastIndentLevel;
                if (EditorGUI.EndChangeCheck())
                {
                    max = Mathf.Min(max, attr.max);
                    max = Mathf.Max(max, min);
                    SetVectorValue(serializedProperty, min, max);
                }

                EditorGUI.showMixedValue = false;
            }
            else
            {
                var contentRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                GUIContent[] labels;
                SerializedProperty[] properties;

                if (includeZ)
                {
                    labels = new[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
                    properties = new[] { serializedProperty.FindPropertyRelative("x"), serializedProperty.FindPropertyRelative("y"), serializedProperty.FindPropertyRelative("z") };
                }
                else
                {
                    labels = new[] { new GUIContent("X"), new GUIContent("Y") };
                    properties = new[] { serializedProperty.FindPropertyRelative("x"), serializedProperty.FindPropertyRelative("y") };
                }

                AddMultipleHorizontalPropertyFields(contentRect, labels, properties);
            }

            return true;
        }

        private bool AddGeoCoordinatePropertyFields(Rect position, SerializedProperty serializedProperty, GUIContent label, bool includeAltitude = false)
        {
            bool pasted = false;
            Event e = Event.current;
            if (e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand || e.type == EventType.KeyDown)
            {
                if ((e.control && e.keyCode == KeyCode.V) || (e.commandName != null && e.commandName == "Paste"))
                    pasted = true;
            }

            EditorGUI.BeginChangeCheck();
            var contentRect = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            GUIContent[] labels; 
            SerializedProperty[] properties;

            if (includeAltitude)
            {
                labels = new[] { new GUIContent("Lat"), new GUIContent("Lon"), new GUIContent("Alt") };
                properties = new[] { serializedProperty.FindPropertyRelative("latitude"), serializedProperty.FindPropertyRelative("longitude"), serializedProperty.FindPropertyRelative("altitude") };
            }
            else
            {
                labels = new[] { new GUIContent("Lat"), new GUIContent("Lon") };
                properties = new[] { serializedProperty.FindPropertyRelative("latitude"), serializedProperty.FindPropertyRelative("longitude")};
            }

            //Used to assign focus to the first field in the GeoCoordinatePopup(MenuItems)
            GUI.SetNextControlName("GeoCoordinate_Lat");

            AddMultipleHorizontalPropertyFields(contentRect, labels, properties);
            if (EditorGUI.EndChangeCheck())
            {
                SerializedProperty latitude = serializedProperty.FindPropertyRelative("latitude");
                SerializedProperty longitude = serializedProperty.FindPropertyRelative("longitude");
                if (pasted)
                {
                    string[] latitudeLongitudeAltitude = EditorGUIUtility.systemCopyBuffer.Split(',');
                    
                    if (latitudeLongitudeAltitude.Length == 2)
                    {
                        if (serializedProperty.GetType() == typeof(GeoCoordinate3))
                        {
                            if (latitudeLongitudeAltitude[0].Length != 0 && latitudeLongitudeAltitude[1].Length != 0 && float.TryParse(latitudeLongitudeAltitude[0], out float latitudeParsed) && float.TryParse(latitudeLongitudeAltitude[1], out float longitudeParsed))
                            {
                                latitude.floatValue = GeoCoordinate3.ValidateLatitude(latitudeParsed);
                                longitude.floatValue = GeoCoordinate3.ValidateLongitude(longitudeParsed);
                            }
                        }
                        else
                        {
                            if (latitudeLongitudeAltitude[0].Length != 0 && latitudeLongitudeAltitude[1].Length != 0 && double.TryParse(latitudeLongitudeAltitude[0], out double latitudeParsed) && double.TryParse(latitudeLongitudeAltitude[1], out double longitudeParsed))
                            {
                                latitude.doubleValue = GeoCoordinate3Double.ValidateLatitude(latitudeParsed);
                                longitude.doubleValue = GeoCoordinate3Double.ValidateLongitude(longitudeParsed);
                            }
                        }
                    }

                    if (includeAltitude && latitudeLongitudeAltitude.Length == 3)
                    {
                        SerializedProperty altitude = serializedProperty.FindPropertyRelative("altitude");

                        if (serializedProperty.GetType() == typeof(GeoCoordinate3))
                        {
                            if (latitudeLongitudeAltitude[2].Length != 0 && float.TryParse(latitudeLongitudeAltitude[2], out float altitudeParsed))
                                altitude.floatValue = altitudeParsed;
                        }
                        else
                        {
                            if (latitudeLongitudeAltitude[2].Length != 0 && double.TryParse(latitudeLongitudeAltitude[2], out double altitudeParsed))
                                altitude.doubleValue = altitudeParsed;
                        }
                    }
                }
                else
                {
                    if (serializedProperty.GetType() == typeof(GeoCoordinate3))
                    {
                        latitude.floatValue = GeoCoordinate3.ValidateLatitude(latitude.floatValue);
                        longitude.floatValue = GeoCoordinate3.ValidateLongitude(longitude.floatValue);
                    }
                    else
                    {
                        latitude.doubleValue = GeoCoordinate3Double.ValidateLatitude(latitude.doubleValue);
                        longitude.doubleValue = GeoCoordinate3Double.ValidateLongitude(longitude.doubleValue);
                    }
                }
            }

            return true;
        }

        private bool AddSerializableGuidPropertyField(Rect position, SerializedProperty serializedProperty, GUIContent label, IEnumerable<CustomAttribute> customAttributes)
        {
            Color lastBackgroundColor = GUI.backgroundColor;

            SerializedProperty idStrSerializedProperty = serializedProperty.FindPropertyRelative(SerializableGuid.GUID_STR_FIELD_NAME);

            string guidStr = idStrSerializedProperty.stringValue;

            ComponentReferenceAttribute componentReferenceAttribute = MemberUtility.GetFirstAttribute<ComponentReferenceAttribute>(customAttributes);
            if (componentReferenceAttribute != null && SerializableGuid.TryParse(guidStr, out SerializableGuid id) && id != SerializableGuid.Empty)
            {
                InstanceManager instanceManager = InstanceManager.Instance(false);
                if (instanceManager != Disposable.NULL)
                {
                    PropertyMonoBehaviour componentReference = (PropertyMonoBehaviour)instanceManager.GetIJson(id);
                    if (componentReference == Disposable.NULL)
                        GUI.backgroundColor = new Color(255.0f / 255.0f, 100.0f / 255.0f, 100.0f / 255.0f);
                }
            }

            if (position.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
                {
                    if (DragAndDrop.objectReferences.Length == 1)
                    {
                        UnityEngine.Object obj = DragAndDrop.objectReferences[0];
                        IProperty properties = null;
                        if (obj is IProperty)
                            properties = obj as IProperty;
                        else if (obj is GameObject)
                            properties = (obj as GameObject).GetComponent(typeof(Object)) as IProperty;
                        if (properties != null)
                        {
                            if (Event.current.type == EventType.DragUpdated)
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            else if (Event.current.type == EventType.DragPerform)
                            {
                                guidStr = properties.id.ToString();
                                GUI.changed = true;
                            }
                            Event.current.Use();
                        }
                    }
                }
            }

            if (!SerializableGuid.TryParse(guidStr, out SerializableGuid parsedSerializableGuid))
                parsedSerializableGuid = SerializableGuid.Empty;

            idStrSerializedProperty.stringValue = parsedSerializableGuid.ToString();

            AddPropertyField(position, idStrSerializedProperty, label);

            GUI.backgroundColor = lastBackgroundColor;

            return true;
        }

        private bool AddDatasourcePropertyField(Rect position, SerializedProperty serializedProperty, GUIContent label)
        {
            Datasource datasource = EditorBase.GetDatasourceFromSerializedProperty(serializedProperty.serializedObject.targetObject);

            position.y += 5.0f;

            position.height = EditorGUIUtility.singleLineHeight;

            float totalHeight = 0.0f;
            datasource.IterateOverPersistents((persistentId, persistent) =>
            {
                GUI.SetNextControlName(Selection.PERSISTENT_PREFIX + "" + persistent.id.ToString());
                EditorGUI.ObjectField(position, persistent as UnityEngine.Object, persistent.GetType(), true);
                float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                totalHeight += height;
                position.y += height;

                return true;
            });

            if (AddButton(position, "Dispose Selected Item", label.tooltip))
            {
                IPersistent persistent = Selection.GetSelectedDatasourcePersistent();
                if (!Disposable.IsDisposed(persistent))
                    DisposeManager.Dispose(persistent is MonoBehaviourDisposable ? (persistent as MonoBehaviourDisposable).gameObject : persistent, DisposeContext.Editor_Destroy);
            }

            GUILayout.Space(20.0f);

            return true;
        }

        private bool AddBoolPropertyFields(Rect position, SerializedProperty serializedProperty, GUIContent label)
        {
            if (attribute is ButtonAttribute)
            {
                ButtonAttribute buttonAttribute = attribute as ButtonAttribute;

                if (AddButton(position, label.text, label.tooltip))
                {
                    foreach (UnityEngine.Object targetObject in serializedProperty.serializedObject.targetObjects)
                    {
                        string methodName = buttonAttribute.methodName;
                        MethodInfo eventMethodInfo = MemberUtility.GetMethodInfoFromMethodName(targetObject, methodName);
                        if (eventMethodInfo != null)
                            eventMethodInfo.Invoke(targetObject, null);
                        else
                            Debug.LogWarning(string.Format("InspectorButton: Unable to find method {0} in {1}", methodName, targetObject.GetType()));
                    }
                }

                return true;
            }

            return false;
        }

        private bool AddIntPropertyFields(Rect position, SerializedProperty serializedProperty, GUIContent label)
        {
            if (attribute is LayerAttribute)
            {
                serializedProperty.intValue = EditorGUI.LayerField(position, label, serializedProperty.intValue);
                return true;
            }
            else if (attribute is MaskAttribute)
            {
                serializedProperty.intValue = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(~EditorGUI.MaskField(position, label, ~UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(serializedProperty.intValue), UnityEditorInternal.InternalEditorUtility.layers));
                return true;
            }
            else
                return false;
        }

        private void AddMultipleHorizontalPropertyFields(Rect position, GUIContent[] subLabels, SerializedProperty[] serializedProperties)
        {
            var lastIndentLevel = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;

            var propsCount = serializedProperties.Length;
            var width = (position.width - (propsCount - 1) * SUB_LABEL_SPACING) / propsCount;
            var contentPos = new Rect(position.x, position.y, width, position.height);
            EditorGUI.indentLevel = 0;
            for (var i = 0; i < propsCount; i++)
            {
                EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(subLabels[i]).x;
                AddPropertyField(contentPos, serializedProperties[i], subLabels[i]);
                contentPos.x += width + SUB_LABEL_SPACING;
            }

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = lastIndentLevel;
        }

        private void AddPropertyField(Rect position, SerializedProperty serializedProperty)
        {
            AddPropertyField(position, serializedProperty, new GUIContent(serializedProperty.displayName));
        }

        private void AddPropertyField(Rect position, SerializedProperty serializedProperty, GUIContent label)
        {
            EditorGUI.PropertyField(position, serializedProperty, label, true);
        }

        private void AddPropertyField(ref Rect position, SerializedProperty serializedProperty)
        {
            AddPropertyField(ref position, serializedProperty, new GUIContent(serializedProperty.displayName));
        }

        private void AddPropertyField(ref Rect position, SerializedProperty serializedProperty, GUIContent label)
        {
            float height = GetPropertyHeight(serializedProperty, label);
            position.height = height;
            EditorGUI.PropertyField(position, serializedProperty, label, true);
            position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
        }

        private bool AddButton(Rect position, string text, string tooltip)
        {
            float indentedX = (EditorGUI.indentLevel + 1.0f) * INDENT_WIDTH;
            if (position.x < indentedX)
            {
                float deltaX = indentedX - position.x;
                position.x += deltaX;
                position.width -= deltaX;
            }

            return GUI.Button(position, new GUIContent(text, tooltip));
        }

        private bool SetVectorValue(SerializedProperty serializedProperty, float min, float max)
        {
            if (serializedProperty.type == typeof(Vector2).Name)
            {
                min = Round(min, ROUNDING_VALUE);
                max = Round(max, ROUNDING_VALUE);
                serializedProperty.vector2Value = new Vector2(min, max);
            }
            else if (serializedProperty.type == typeof(Vector2Int).Name)
            {
                serializedProperty.FindPropertyRelative("x").intValue = (int)min;
                serializedProperty.FindPropertyRelative("y").intValue = (int)max;
            }

            return true;
        }

        private float Round(float value, float roundingValue)
        {
            return roundingValue == 0 ? value : Mathf.Round(value * roundingValue) / roundingValue;
        }

        public override float GetPropertyHeight(SerializedProperty serializedProperty, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(serializedProperty);

            switch (serializedProperty.type)
            {
                case nameof(Vector3): 
                case nameof(Vector3Int): 
                case nameof(Vector3Double):
                case nameof(Vector2):
                case nameof(Vector2Int):
                case nameof(Vector2Double):
                case nameof(SerializableGuid):

                    height = GetDefaultSingleLineHeight();

                    break;

                case "PPtr<$" + nameof(Datasource) + ">":

                    Datasource datasource = EditorBase.GetDatasourceFromSerializedProperty(serializedProperty.serializedObject.targetObject); ;

                    float totalHeight = 0.0f;

                    if (datasource is not null)
                    {
                        datasource.IterateOverPersistents((persistentId, persistent) =>
                        {
                            totalHeight += GetDefaultSingleLineHeight();

                            return true;
                        });
                    }

                    height = totalHeight;

                    break;
            }
 
            return height;
        }

        private float GetDefaultSingleLineHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
