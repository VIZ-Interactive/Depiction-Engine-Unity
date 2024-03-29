﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DepictionEngine
{
    public class PersistentMonoBehaviour : JsonMonoBehaviour, IPersistent
    {
        public const string AUTO_SYNCHRONIZE_INTERVAL_TOOLTIP = "The interval (in seconds) at which we call the '"+nameof(Synchronize)+ "' function. Automatically calling '"+nameof(Synchronize)+"' can be useful to keep objects in synch with a datasource. Set to zero to deactivate.";
        public const string AUTO_DISPOSE_TOOLTIP = "When enabled, the object can be disposed automatically by its datasource if it is no longer required in any loader and does not have any out of synch properties.";
        public const string DONT_SAVE_TO_SCENE_TOOLTIP = "When enabled, the object will not be saved as part of the Scene.";

        [BeginFoldout("Persistence")]
#if UNITY_EDITOR
        [SerializeField, Button(nameof(SaveBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSaveBtn)), Tooltip("Push this object's values to the datasource."), BeginHorizontalGroup(true)]
        private bool _save;
        [SerializeField, Button(nameof(SynchronizeBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSynchronizeBtn)), Tooltip("Pull the values from the datasource and use them to update this object.")]
        private bool _synchronize;
        [SerializeField, Button(nameof(DeleteBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableDeleteBtn)), Tooltip("Delete from datasource (this object will not be deleted)."), EndHorizontalGroup]
        private bool _delete;
#endif
        [SerializeField, Tooltip(AUTO_SYNCHRONIZE_INTERVAL_TOOLTIP)]
        private float _autoSynchronizeInterval;
        [SerializeField, Tooltip(AUTO_DISPOSE_TOOLTIP)]
        private bool _autoDispose;
        [SerializeField, Tooltip(DONT_SAVE_TO_SCENE_TOOLTIP), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDontSaveToScene))]
#endif
        private bool _dontSaveToScene;

        [BeginFoldout("Persistent Additional Fallback Values")]
        [SerializeField, ConditionalShow(nameof(IsFallbackValues)), Tooltip("When enabled, a new '"+nameof(IPersistent)+"' will be created if none is present in the datasource."), EndFoldout]
        private bool _createPersistentIfMissing;

        [SerializeField, HideInInspector]
        private bool _containsCopyrightedMaterial;

        private Tween _autoSynchronizeIntervalTimer;

        private Action<IPersistent, Action> _persistenceSaveOperationEvent;
        private Action<IPersistent, Action> _persistenceSynchronizeOperationEvent;
        private Action<IPersistent, Action> _persistenceDeleteOperationEvent;

#if UNITY_EDITOR
        private void SaveBtn()
        {
            if (Save() == 0)
                Debug.Log(DatasourceBase.NOTHING_TO_SAVE_MSG);
        }

        private void SynchronizeBtn()
        {
            if (Synchronize() == 0)
                Debug.Log(DatasourceBase.NOTHING_TO_SYNCHRONIZE_MSG);
        }

        private void DeleteBtn()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Delete", "Are you sure you want to Delete '" + name + "' from all Datasources?", "Ok", "Cancel") && Delete() == 0)
                Debug.Log(DatasourceBase.NOTHING_TO_DELETE_MSG);
        }

        private bool GetEnableSaveBtn()
        {
            return Datasource.EnablePersistenceOperations() && PersistenceSaveOperationEvent != null;
        }

        private bool GetEnableSynchronizeBtn()
        {
            return Datasource.EnablePersistenceOperations() && PersistenceSynchronizeOperationEvent != null;
        }

        private bool GetEnableDeleteBtn()
        {
            return Datasource.EnablePersistenceOperations() && PersistenceDeleteOperationEvent != null;
        }

        private bool GetShowDontSaveToScene()
        {
            return !containsCopyrightedMaterial;
        }
#endif

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
                _lastName = name;

                return true;
            }
            return false;
        }

        public override void Recycle()
        {
            base.Recycle();

            _containsCopyrightedMaterial = default;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        protected override bool Initialize(InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                StartAutoSynchronizeIntervalTimer();

                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => name = value, base.name, initializingContext);
            InitValue(value => autoDispose = value, PersistentScriptableObject.DEFAULT_AUTO_DISPOSE, initializingContext);
            InitValue(value => autoSynchronizeInterval = value, PersistentScriptableObject.DEFAULT_AUTO_SYNCHRONIZE_INTERVAL, initializingContext);
            InitValue(value => dontSaveToScene = value, GetDefaultDontSaveToScene(), initializingContext);
            InitValue(value => createPersistentIfMissing = value, true, initializingContext);
        }

        public override bool DetectUserGameObjectChanges()
        {
            if (base.DetectUserGameObjectChanges())
            {
                if (_lastName != name)
                {
                    string newValue = name;
                    base.name = _lastName;
                    name = newValue;
                }

                return true;
            }
            return false;
        }

        protected override void Saving(Scene scene, string path)
        {
            base.Saving(scene, path);

            if (GetDontSaveToScene())
                gameObject.hideFlags |= HideFlags.DontSave;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                gameObject.hideFlags = hideFlags;

                if (isFallbackValues)
                {
                    gameObject.hideFlags |= HideFlags.DontSave;
                    if (!SceneManager.Debugging())
                        gameObject.hideFlags |= HideFlags.HideInHierarchy;
                }

                return true;
            }
            return false;
        }

        public Action<IPersistent, Action> PersistenceSaveOperationEvent
        {
            get => _persistenceSaveOperationEvent; 
            set => _persistenceSaveOperationEvent = value; 
        }

        public Action<IPersistent, Action> PersistenceSynchronizeOperationEvent
        {
            get => _persistenceSynchronizeOperationEvent;
            set => _persistenceSynchronizeOperationEvent = value;
        }

        public Action<IPersistent, Action> PersistenceDeleteOperationEvent
        {
            get => _persistenceDeleteOperationEvent;
            set => _persistenceDeleteOperationEvent = value;
        }

        protected virtual bool GetDefaultDontSaveToScene()
        {
            return IsFallbackValues() || PersistentScriptableObject.DEFAULT_DONT_SAVE_TO_SCENE;
        }

        private string _lastName;
        /// <summary>
        /// The name of the object.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsNotFallbackValues))]
        public new string name
        {
            get => base.name;
            set 
            {
                string oldValue = base.name;
                if (HasChanged(value, oldValue))
                {
                    _lastName = base.name = value;
                    PropertyAssigned(this, nameof(name), value, oldValue);
                }
            }
        }

        /// <summary>
        /// The interval (in seconds) at which we call the <see cref="DepictionEngine.IPersistent.Synchronize"/> function. Automatically calling <see cref="DepictionEngine.IPersistent.Synchronize"/> can be useful to keep objects in synch with a datasource. Set to zero to deactivate.
        /// </summary>
        [Json]
        public float autoSynchronizeInterval
        {
            get => _autoSynchronizeInterval;
            set
            {
                SetValue(nameof(autoSynchronizeInterval), value, ref _autoSynchronizeInterval, (newValue, oldValue) =>
                {
                    StartAutoSynchronizeIntervalTimer();
                });
            }
        }

        /// <summary>
        /// When enabled, the object can be disposed automatically by its datasource if it is no longer required in any loader and does not have any out of synch properties.
        /// </summary>
        [Json]
        public bool autoDispose
        {
            get => _autoDispose;
            set => SetValue(nameof(autoDispose), value, ref _autoDispose);
        }

        /// <summary>
        /// When enabled, the object will not be saved as part of the Scene.
        /// </summary>
        [Json]
        public bool dontSaveToScene
        {
            get => _dontSaveToScene;
            set
            {
                SetValue(nameof(dontSaveToScene), value, ref _dontSaveToScene, (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    if (initialized)
                        SceneManager.MarkSceneDirty();
#endif
                });
            }
        }

        protected virtual bool GetDontSaveToScene()
        {
            return dontSaveToScene || isFallbackValues || containsCopyrightedMaterial;
        }

        /// <summary>
        /// When enabled, a new <see cref="DepictionEngine.IPersistent"/> will be created if none is present in the datasource.
        /// </summary>
        [Json(conditionalGetMethod: nameof(IsFallbackValues))]
        public bool createPersistentIfMissing
        {
            get => _createPersistentIfMissing;
            set => SetValue(nameof(_createPersistentIfMissing), value, ref _createPersistentIfMissing);
        }

        /// <summary>
        /// Whether or not the object contains data that is copyrighted. If true the object may not be persisted in a Scene or <see cref="DepictionEngine.Datasource"/>.
        /// </summary>
        [Json(get:false)]
        public bool containsCopyrightedMaterial
        {
            get => _containsCopyrightedMaterial;
            private set
            {
                if (!value || _containsCopyrightedMaterial == value)
                    return;

                _containsCopyrightedMaterial = value;
            }
        }

        private void StartAutoSynchronizeIntervalTimer()
        {
            autoSynchronizeIntervalTimer = autoSynchronizeInterval != 0.0f ? tweenManager.DelayedCall(autoSynchronizeInterval, null, () =>
            {
                Synchronize();
                StartAutoSynchronizeIntervalTimer();
            }, () => autoSynchronizeIntervalTimer = null) : null;
        }

        private Tween autoSynchronizeIntervalTimer
        {
            get => _autoSynchronizeIntervalTimer;
            set
            {
                if (Object.ReferenceEquals(_autoSynchronizeIntervalTimer, value))
                    return;

                DisposeManager.Dispose(_autoSynchronizeIntervalTimer);

                _autoSynchronizeIntervalTimer = value;
            }
        }

        public int Save()
        {
            int saved = 0;

            PersistenceSaveOperationEvent?.Invoke(this, () => { saved++; });

            return saved;
        }

        public int Synchronize()
        {
            int synchronized = 0;

            PersistenceSynchronizeOperationEvent?.Invoke(this, () => { synchronized++; });

            return synchronized;
        }

        public int Delete()
        {
            int deleted = 0;

            PersistenceDeleteOperationEvent?.Invoke(this, () => { deleted++; });

            return deleted;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                autoSynchronizeIntervalTimer = null;

                PersistenceSaveOperationEvent = null;
                PersistenceSynchronizeOperationEvent = null;
                PersistenceDeleteOperationEvent = null;

                return true;
            }
            return false;
        }
    }
}
