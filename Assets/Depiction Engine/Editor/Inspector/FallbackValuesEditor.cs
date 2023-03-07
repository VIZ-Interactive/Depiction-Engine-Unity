// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace DepictionEngine.Editor
{
    [CustomEditor(typeof(FallbackValues), true)]
    [CanEditMultipleObjects]
    public class FallbackValuesEditor : EditorBase
    {
        private static string[] _instanceTypes;

        private UnityEngine.Object _fallbackValuesObject;
        private UnityEditor.Editor _fallbackValuesObjectEditor;

        private bool _fallbackValuesObjectEditorChanged;

        private UnityEngine.Object fallbackValuesObject
        {
            get { return _fallbackValuesObject; }
            set
            {
                if (_fallbackValuesObject == value)
                    return;

               FallbackValues fallbackValues = serializedObject.targetObject as FallbackValues;

                if (fallbackValues != Disposable.NULL)
                    fallbackValues.ReleaseFallbackValuesObject(_fallbackValuesObject);

                DisposeManager.Destroy(_fallbackValuesObjectEditor);

                _fallbackValuesObject = value;

                _fallbackValuesObjectEditorChanged = true;

                if (_fallbackValuesObject != null)
                    CreateCachedEditor(_fallbackValuesObject, null, ref _fallbackValuesObjectEditor);
            }
        }

        private UnityEditor.Editor fallbackValuesObjectEditor
        {
            get { return _fallbackValuesObjectEditor; }
        }

        protected override void AddProperty(SerializedProperty serializedProperty, string labelOverride)
        {
            if (serializedProperty.name == "_" + nameof(FallbackValues.instanceType))
            {
                FallbackValues fallbackValues = serializedProperty.serializedObject.targetObject as FallbackValues;

                if (serializedProperty.serializedObject.targetObjects.Length <= 1)
                {
                    EditorGUI.BeginChangeCheck();

                    if (_instanceTypes == null)
                    {
                        List<Type> persistentTypes = GetAllTypeThatInheritType(typeof(PersistentMonoBehaviour));
                        persistentTypes.AddRange(GetAllTypeThatInheritType(typeof(PersistentScriptableObject)));
                        persistentTypes.AddRange(GetAllTypeThatInheritType(typeof(Script)));

                        List<string> instanceTypes = new();

                        foreach (Type type in persistentTypes)
                        {
                            if (FallbackValues.IsValidFallbackValuesType(type))
                                instanceTypes.Add(type.FullName);
                        }

                        instanceTypes.OrderBy(x => x);
                        instanceTypes.Insert(0, "Select");

                        _instanceTypes = instanceTypes.ToArray();
                    }

                    int currentIndex = EditorGUILayout.Popup(0, _instanceTypes);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedProperty.stringValue = null;

                        fallbackValues.SetPendingInspectorTypeChange(FallbackValues.ParseType(_instanceTypes[currentIndex]));
                    }
                }

                Type fallbackValuesObjectType = fallbackValuesObject != null ? fallbackValuesObject.GetType() : null;

                Type newFallbackValuesObjectType = null;

                if (serializedProperty.serializedObject.targetObjects.Length <= 1)
                    newFallbackValuesObjectType = fallbackValues.GetFallbackValuesType();
                else
                    EditorGUILayout.LabelField("Cannot edit multiple FallbackValues simultaneously");

                if (fallbackValuesObjectType != newFallbackValuesObjectType)
                    fallbackValuesObject = fallbackValues.GetFallbackValuesObject(newFallbackValuesObjectType, fallbackValues.fallbackValuesJson);
            }
            else
                base.AddProperty(serializedProperty, labelOverride);
        }

        protected override void AddAdditionalEditor(SerializedProperty serializedProperty)
        {
            base.AddAdditionalEditor(serializedProperty);

            EditorBase fallbackValuesObjectEditorBase = fallbackValuesObjectEditor as EditorBase;
            if (fallbackValuesObjectEditorBase != null)
            {
                EditorGUI.BeginChangeCheck();

                fallbackValuesObjectEditorBase.OnInspectorGUI();

                bool propertyChanged = EditorGUI.EndChangeCheck();
                if (_fallbackValuesObjectEditorChanged || propertyChanged)
                {
                    if (fallbackValuesObject is IJson)
                    {
                        FallbackValues fallbackValues = serializedProperty.serializedObject.targetObject as FallbackValues;

                        if (propertyChanged)
                            RecordObject(fallbackValues);

                        SetPropertyValue(fallbackValues, MemberUtility.GetMemberInfoFromMemberName<PropertyInfo>(fallbackValues.GetType(), nameof(FallbackValues.fallbackValuesJson)), (fallbackValuesObject as IJson).GetJson());
                    }

                    _fallbackValuesObjectEditorChanged = false;
                }
            }
        }

        private List<Type> GetAllTypeThatInheritType(Type type)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(currentType => currentType.IsSubclassOf(type)).ToList();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            fallbackValuesObject = null;
        }
    }
}