// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [CreateComponent(typeof(GeoCoordinateController))]
    public class Building : Object
    {
        [BeginFoldout("Building")]
        [SerializeField, ComponentReference, Tooltip("The Id of the ground level."), EndFoldout]
        private SerializableGuid _groundLevelId;

        [SerializeField, HideInInspector]
        private Level _currentLevel;

        [SerializeField, HideInInspector]
        private Level _groundLevel;

        protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {
            base.IterateOverComponentReference(callback);

            if (_groundLevelId != null)
                callback(_groundLevelId, UpdateGroundLevel);
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => groundLevelId = value, SerializableGuid.Empty, () => { return GetDuplicateComponentReferenceId(groundLevelId, groundLevel, initializingContext); }, initializingContext);
        }

        protected override void CreateComponents(InitializationContext initializingContext)
        {
            base.CreateComponents(initializingContext);

            GeoCoordinateController controller = GetComponent<GeoCoordinateController>();
            controller.groundSnapOffset = 1.0f;
        }

        protected override bool AddInputManagerDelegates()
        {
            return true;
        }

        protected override void InputManagerOnMouseClickedHandler(RaycastHitDouble hit)
        {
            base.InputManagerOnMouseClickedHandler(hit);

            MeshRendererVisual meshRendererVisual = hit.meshRendererVisual;

            if (meshRendererVisual != Disposable.NULL && meshRendererVisual.visualObject is LevelMeshObject)
            {
                transform.IterateOverChildrenObject<Level>((level) =>
                {
                    if (meshRendererVisual.GetComponentInParent<Building>() == this)
                    {
                        currentLevel = level;
                        return false;
                    }
                    return true;
                });
            }
        }

#if UNITY_EDITOR
        private UnityEngine.Object[] GetGroundLevelIdAdditionalRecordObjects()
        {
            List<UnityEngine.Object> levels = null;

            transform.IterateOverChildrenObject<Level>((level) => 
            {
                levels ??= new List<UnityEngine.Object>();

                levels.Add(level.gameObject);
                levels.Add(level);
                return true;
            });

            return levels?.ToArray();
        }
#endif

        private Level groundLevel
        {
            get { return _groundLevel; }
            set { groundLevelId = value != Disposable.NULL ? value.id : SerializableGuid.Empty; }
        }

        /// <summary>
        /// The Id of the ground level.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetGroundLevelIdAdditionalRecordObjects))]
#endif
        public SerializableGuid groundLevelId
        {
            get { return _groundLevelId; }
            set 
            { 
                SetValue(nameof(groundLevelId), value, ref _groundLevelId, (newValue, oldValue) => 
                {
                    UpdateGroundLevel();
                }); 
            }
        }

        private void UpdateGroundLevel()
        {
            SetValue(nameof(groundLevel), GetComponentFromId<Level>(groundLevelId), ref _groundLevel, (newValue, oldValue) =>
            {
                UpdateLevels();
            });
        }

        public Level currentLevel
        {
            get { return _currentLevel; }
            set 
            {
                if (_currentLevel == value)
                    return;

                if (_currentLevel != Disposable.NULL)
                    _currentLevel.Closed();

                _currentLevel = value;

                if (_currentLevel != Disposable.NULL)
                    _currentLevel.Opened();

                UpdateLevels();
            }
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                UpdateLevels();

                return true;
            }
            return false;
        }

        private void UpdateLevels()
        {
            if (initialized)
            {
                bool levelsInitialized = false;

                transform.IterateOverChildrenObject<Level>((level) =>
                {
                    if (level.GetLevelMeshObjectInitialized())
                    {
                        levelsInitialized = true;
                        return true;
                    }
                    else
                    {
                        levelsInitialized = false;
                        return false;
                    }
                });

                if (levelsInitialized)
                {
                    IterateOverLevels((level) =>
                    {
                        level.gameObject.SetActive(currentLevel == Disposable.NULL || level.transform.index >= currentLevel.transform.index);
                    });

                    float cumulativeHeight = 0.0f;

                    SerializableGuid groundLevelId = this.groundLevelId;

                    float groundLevelHeight = 0.0f;
                    IterateOverLevels((level) =>
                    {
                        if (groundLevelId == SerializableGuid.Empty)
                            groundLevelId = level.id;

                        if (groundLevelId == level.id)
                            groundLevelHeight = cumulativeHeight;

                        cumulativeHeight += level.GetHeight();
                    });

                    cumulativeHeight = 0.0f;
                    IterateOverLevels((level) =>
                    {
                        level.groundY = cumulativeHeight - groundLevelHeight;
                        cumulativeHeight += level.GetHeight();
                    });
                }
            }
        }

        public void IterateOverLevels(Action<Level> callback)
        {
            for(int i = gameObject.transform.childCount - 1 ; i >= 0 ; i--)
            {
                Level level = gameObject.transform.GetChild(i).GetComponent<Level>();
                if (level != Disposable.NULL)
                    callback(level);
            }
        }
    }
}