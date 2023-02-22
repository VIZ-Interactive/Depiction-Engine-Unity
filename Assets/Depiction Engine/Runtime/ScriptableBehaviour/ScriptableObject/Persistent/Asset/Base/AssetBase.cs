﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.IO;
using UnityEngine;

namespace DepictionEngine
{
    public class AssetBase : PersistentScriptableObject, IGrid2DIndex
    {
        /// <summary>
        /// Different types of indexes.<br/><br/>
		/// <b><see cref="None"/>:</b> <br/>
        /// No index. <br/><br/>
		/// <b><see cref="Index2D"/>:</b> <br/>
        /// An index composed of 2 components (x,y). <br/><br/>
		/// <b><see cref="Index3D"/>:</b> <br/>
        /// An index composed of 3 components (x,y,z).
        /// </summary>
        public enum GridIndexType
        {
            None,
            Index2D,
            Index3D
        };

        [BeginFoldout("Grid Index")]
        [SerializeField, Tooltip("The number of component required by the grid index.")]
        private GridIndexType _gridIndexType;
        [SerializeField, Tooltip("The horizontal and vertical size of the grid.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowIndex2D))]
#endif
        private Vector2Int _grid2DDimensions;
        [SerializeField, Tooltip("The horizontal and vertical index of this asset on the grid."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowIndex2D))]
#endif
        private Vector2Int _grid2DIndex;

#if UNITY_EDITOR
        [BeginFoldout("Data")]
        [SerializeField, Button(nameof(LoadFromFileBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Load asset data from a file.")]
        private bool _loadFromFile;
        [SerializeField, Button(nameof(SaveToFileBtn)), ConditionalEnable(nameof(GetEnableSaveToFileBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Save the asset data to a file."), EndFoldout]
        private bool _saveToFile;
#endif

#if UNITY_EDITOR
        protected bool GetShowDataField()
        {
            return !isFallbackValues;
        }

        protected bool GetEnableDataField()
        {
            return false;
        }

        private bool GetShowIndex2D()
        {
            return gridIndexType == GridIndexType.Index2D;
        }

        private bool GetEnableSaveToFileBtn()
        {
            return !containsCopyrightedMaterial;
        }

        private void LoadFromFileBtn()
        {
            try
            {
                string assetPath = UnityEditor.EditorUtility.OpenFilePanel("Load Asset File", "Assets/DepictionEngine/Resources", GetSupportedLoadFileExtensions());
                if (!string.IsNullOrEmpty(assetPath))
                {
                    IsUserChange(() => 
                    {
                        Editor.UndoManager.CreateNewGroup("Changed Asset Data in " + name);
                        Editor.UndoManager.RegisterCompleteObjectUndo(this);

                        LoaderBase.DataType dataType = GetDataTypeFromExtension(Path.GetExtension(assetPath));
                        SetData(ProcessDataBytes(File.ReadAllBytes(assetPath), dataType), dataType, InstanceManager.InitializationContext.Editor);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            GUIUtility.ExitGUI();
        }

        private void SaveToFileBtn()
        {
            try
            {
                string assetPath = UnityEditor.EditorUtility.SaveFilePanel("Save Asset File", "Assets/DepictionEngine/Resources", GetDefaultFileName(), GetFileExtension());
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string extension = Path.GetExtension(assetPath);

                    byte[] bytes = GetDataBytes(GetDataTypeFromExtension(extension));
                    if (bytes != null)
                        File.WriteAllBytes(assetPath, bytes);
                    else
                        Debug.LogError("Extension " + extension + " not supported");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            GUIUtility.ExitGUI();
        }

        private string GetDefaultFileName()
        {
            string fileName = name;

            foreach (char c in Path.GetInvalidFileNameChars())
                fileName = fileName.Replace(c, '_');

            return fileName;
        }

        protected virtual string GetSupportedLoadFileExtensions()
        {
            return GetFileExtension();
        }

        protected virtual LoaderBase.DataType GetDataTypeFromExtension(string extension)
        {
            return LoaderBase.DataType.Unknown;
        }
#endif

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                _lastData = _data;
#endif
                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => gridIndexType = value, GridIndexType.None, initializingState);
            InitValue(value => grid2DDimensions = value, Vector2Int.one, initializingState);
            InitValue(value => grid2DIndex = value, Vector2Int.zero, initializingState);
        }

#if UNITY_EDITOR
        private bool _lastData;
        [SerializeField, HideInInspector]
        private bool _data;
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            if (_lastData != _data)
            {
                _data = _lastData;
                DataPropertyAssigned();
            }
        }
#endif

        public bool IsGridIndexValid()
        {
            return gridIndexType == GridIndexType.Index2D;
        }

        private bool IncludeIndex2D()
        {
            return gridIndexType == GridIndexType.Index2D;
        }

        /// <summary>
        /// The number of component required by the grid index.
        /// </summary>
        [Json]
        public GridIndexType gridIndexType
        {
            get { return _gridIndexType; }
            set { SetValue(nameof(gridIndexType), value, ref _gridIndexType); }
        }

        /// <summary>
        /// The horizontal and vertical size of the grid.
        /// </summary>
        [Json(conditionalMethod: nameof(IncludeIndex2D))]
        public Vector2Int grid2DDimensions
        {
            get { return _grid2DDimensions; }
            set
            {
                if (value.x < 1)
                    value.x = 1;
                if (value.y < 1)
                    value.y = 1;
                SetValue(nameof(grid2DDimensions), value, ref _grid2DDimensions);
            }
        }

        /// <summary>
        /// The horizontal and vertical index of this asset on the grid.
        /// </summary>
        [Json(conditionalMethod: nameof(IncludeIndex2D))]
        public Vector2Int grid2DIndex
        {
            get { return _grid2DIndex; }
            set { SetValue(nameof(grid2DIndex), value, ref _grid2DIndex); }
        }

        /// <summary>
        /// The name of the data file (Read Only).
        /// </summary>
        [Json]
        public JSONNode data
        {
            get { return id + "." + GetFileExtension(); }
            set { }
        }

        protected virtual byte[] GetDataBytes(LoaderBase.DataType dataType)
        {
            return null;
        }

        public virtual void SetData(object value, LoaderBase.DataType dataType, InstanceManager.InitializationContext initializingState = InstanceManager.InitializationContext.Programmatically)
        {

        }

        protected virtual object ProcessDataBytes(byte[] value, LoaderBase.DataType dataType)
        {
            return value;
        }

        protected void DataPropertyAssigned()
        {
            DataChanged();

            PropertyAssigned(this, nameof(data), data, data);
        }

        protected virtual void DataChanged()
        {
#if UNITY_EDITOR
            _lastData = _data = !_data;
#endif
        }

        protected virtual string GetFileExtension()
        {
            return "";
        }

        protected void DisposeOldDataAndRegisterNewData<T>(T oldData, T newData, InstanceManager.InitializationContext initializingState) where T : UnityEngine.Object
        {
            if (newData != oldData)
            {
                DisposeManager.DestroyContext destroyContext = DisposeManager.DestroyContext.Unknown;
#if UNITY_EDITOR
                if (initializingState == InstanceManager.InitializationContext.Editor)
                    destroyContext = DisposeManager.DestroyContext.Editor;
#endif
                Dispose(oldData, DisposeManager.DestroyDelay.None, destroyContext);

#if UNITY_EDITOR
                Editor.UndoManager.RegisterCreatedObjectUndo(newData, initializingState);
#endif
            }
        }
    }

    [Serializable]
    public class AssetModifier : PropertyModifier
    {
    }
}
