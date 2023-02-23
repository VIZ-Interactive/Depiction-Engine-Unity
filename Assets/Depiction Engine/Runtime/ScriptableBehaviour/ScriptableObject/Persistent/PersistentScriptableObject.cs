// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DepictionEngine
{
    public class PersistentScriptableObject : JsonScriptableObject, IPersistent
    {
        public const float DEFAULT_AUTO_SYNCHRONIZE_INTERVAL = 0.0f;
        public const bool DEFAULT_AUTO_DISPOSE = true;
        public const bool DEFAULT_DONT_SAVE_TO_SCENE = false;

        [BeginFoldout("Persistence")]
#if UNITY_EDITOR
        [SerializeField, Button(nameof(SaveBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSaveBtn)), Tooltip("Push this object's values to the datasource."), BeginHorizontalGroup(true)]
        private bool _save;
        [SerializeField, Button(nameof(SynchronizeBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableSynchronizeBtn)), Tooltip("Pull the values from the datasource and use them to update this object.")]
        private bool _synchronize;
        [SerializeField, Button(nameof(DeleteBtn)), ConditionalShow(nameof(IsNotFallbackValues)), ConditionalEnable(nameof(GetEnableDeleteBtn)), Tooltip("Delete from datasource (this object will not be deleted)."), EndHorizontalGroup]
        private bool _delete;
#endif
        [SerializeField, Tooltip(PersistentMonoBehaviour.AUTO_SYNCHRONIZE_INTERVAL_TOOLTIP)]
        private float _autoSynchronizeInterval;
        [SerializeField, Tooltip(PersistentMonoBehaviour.AUTO_DISPOSE_TOOLTIP)]
        private bool _autoDispose;
        [SerializeField, Tooltip(PersistentMonoBehaviour.DONT_SAVE_TO_SCENE_TOOLTIP), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDontSaveToScene))]
#endif
        private bool _dontSaveToScene;

        [BeginFoldout("Persistent Additional Fallback Values")]
        [SerializeField, ConditionalShow(nameof(IsFallbackValues)), EndFoldout]
        private bool _createPersistentIfMissing;

        [SerializeField, HideInInspector]
        private bool _containsCopyrightedMaterial;

        private Tween _autoSynchronizeIntervalTimer;

        private Action<IPersistent, Action> _persistenceSaveOperationEvent;
        private Action<IPersistent, Action> _persistenceSynchronizeOperationEvent;
        private Action<IPersistent, Action> _persistenceDeleteOperationEvent;
        private Action<IJson, PropertyInfo> _userPropertyAssignedEvent;

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
            if (Delete() == 0)
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

        public override void Recycle()
        {
            base.Recycle();

            _containsCopyrightedMaterial = false;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        protected override bool Initialize(InstanceManager.InitializationContext initializingContext)
        {
            if (base.Initialize(initializingContext))
            {
                StartAutoSynchronizeIntervalTimer();

                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => autoDispose = value, DEFAULT_AUTO_DISPOSE, initializingContext);
            InitValue(value => autoSynchronizeInterval = value, DEFAULT_AUTO_SYNCHRONIZE_INTERVAL, initializingContext);
            InitValue(value => dontSaveToScene = value, GetDefaultDontSaveToScene(), initializingContext);
            InitValue(value => createPersistentIfMissing = value, true, initializingContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void UserPropertyAssigned(IJson iJson, string name, JsonAttribute jsonAttribute, PropertyInfo propertyInfo)
        {
            base.UserPropertyAssigned(iJson, name, jsonAttribute, propertyInfo);

            if (UserPropertyAssignedEvent != null)
                UserPropertyAssignedEvent(iJson, propertyInfo);
        }

        protected override void Saving(Scene scene, string path)
        {
            base.Saving(scene, path);

            if (IsFallbackValues() || dontSaveToScene)
                hideFlags |= HideFlags.DontSave;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {

                if (IsFallbackValues())
                {
                    bool debug = false;

                    if (!SceneManager.IsSceneBeingDestroyed())
                        debug = sceneManager.debug;

                    if (!debug)
                        hideFlags |= HideFlags.HideInHierarchy;
                }

                return true;
            }
            return false;
        }

        public Action<IPersistent, Action> PersistenceSaveOperationEvent
        {
            get { return _persistenceSaveOperationEvent; }
            set { _persistenceSaveOperationEvent = value; }
        }

        public Action<IPersistent, Action> PersistenceSynchronizeOperationEvent
        {
            get { return _persistenceSynchronizeOperationEvent; }
            set { _persistenceSynchronizeOperationEvent = value; }
        }

        public Action<IPersistent, Action> PersistenceDeleteOperationEvent
        {
            get { return _persistenceDeleteOperationEvent; }
            set { _persistenceDeleteOperationEvent = value; }
        }

        public Action<IJson, PropertyInfo> UserPropertyAssignedEvent
        {
            get { return _userPropertyAssignedEvent; }
            set { _userPropertyAssignedEvent = value; }
        }

        protected virtual bool GetDefaultDontSaveToScene()
        {
            return IsFallbackValues() || DEFAULT_DONT_SAVE_TO_SCENE;
        }

        /// <summary>
        /// The name of the object.
        /// </summary>
        [Json]
        public new string name
        {
            get { return base.name; }
            set
            {
                string oldValue = base.name;
                if (HasChanged(value, oldValue))
                {
                    base.name = value;
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
            get { return _autoSynchronizeInterval; }
            set
            {
                SetValue(nameof(autoSynchronizeInterval), value, ref _autoSynchronizeInterval, (newValue, oldValue) =>
                {
                    StartAutoSynchronizeIntervalTimer();
                });
            }
        }

        /// <summary>
        /// When enabled the object can be disposed automatically by its datasource if it is no longer required in any loader and does not have any out of synch properties.
        /// </summary>
        [Json]
        public bool autoDispose
        {
            get { return _autoDispose; }
            set { SetValue(nameof(autoDispose), value, ref _autoDispose); }
        }

        /// <summary>
        /// When enabled the object will not be saved as part of the Scene.
        /// </summary>
        [Json]
        public bool dontSaveToScene
        {
            get { return _dontSaveToScene; }
            set
            {
                SetValue(nameof(dontSaveToScene), value, ref _dontSaveToScene, (newValue, oldValue) =>
                {
                    DontSaveToSceneChanged(newValue, oldValue);
                });
            }
        }

        protected virtual void DontSaveToSceneChanged(bool newValue, bool oldValue)
        {
        }

        /// <summary>
        /// When enabled a new <see cref="DepictionEngine.IPersistent"/> will be created if none is present in the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createPersistentIfMissing
        {
            get { return _createPersistentIfMissing; }
            set { SetValue(nameof(_createPersistentIfMissing), value, ref _createPersistentIfMissing); }
        }

        /// <summary>
        /// Whether or not the object contains data that is copyrighted. If true the object may not be persisted in a Scene or <see cref="DepictionEngine.Datasource"/>.
        /// </summary>
        [Json(get:false)]
        public bool containsCopyrightedMaterial
        {
            get { return _containsCopyrightedMaterial; }
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
            get { return _autoSynchronizeIntervalTimer; }
            set
            {
                if (Object.ReferenceEquals(_autoSynchronizeIntervalTimer, value))
                    return;

                Dispose(_autoSynchronizeIntervalTimer);

                _autoSynchronizeIntervalTimer = value;
            }
        }

        public int Save()
        {
            int saved = 0;

            if (PersistenceSaveOperationEvent != null)
                PersistenceSaveOperationEvent(this, () => { saved++; });

            return saved;
        }

        public int Synchronize()
        {
            int synchronized = 0;

            if (PersistenceSynchronizeOperationEvent != null)
                PersistenceSynchronizeOperationEvent(this, () => { synchronized++; });

            return synchronized;
        }

        public int Delete()
        {
            int deleted = 0;

            if (PersistenceDeleteOperationEvent != null)
                PersistenceDeleteOperationEvent(this, () => { deleted++; });

            return deleted;
        }

        public override bool OnDispose()
        {
            if (base.OnDispose())
            {
                autoSynchronizeIntervalTimer = null;

                return true;
            }
            return false;
        }
    }
}
