// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        private List<Type> _requiredComponentTypes;

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

            _containsCopyrightedMaterial = false;
        }

        protected override bool AddInstanceToManager()
        {
            return true;
        }

        protected override bool Initialize(InstanceManager.InitializationContext initializingState)
        {
            if (base.Initialize(initializingState))
            {
                StartAutoSynchronizeIntervalTimer();

                return true;
            }
            return false;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => name = value, base.name, initializingState);
            InitValue(value => autoDispose = value, PersistentScriptableObject.DEFAULT_AUTO_DISPOSE, initializingState);
            InitValue(value => autoSynchronizeInterval = value, PersistentScriptableObject.DEFAULT_AUTO_SYNCHRONIZE_INTERVAL, initializingState);
            InitValue(value => dontSaveToScene = value, GetDefaultDontSaveToScene(), initializingState);
            InitValue(value => createPersistentIfMissing = value, true, initializingState);
        }

        protected override void InitializeDependencies(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeDependencies(initializingState);

            InitRequiredComponentTypes();
            GetRequiredComponentTypes(ref _requiredComponentTypes);
            List<Type> requiredComponentTypes = _requiredComponentTypes;

            if (requiredComponentTypes.Count > 0)
            {
                MonoBehaviourBase[] components = gameObject.GetComponents<MonoBehaviourBase>();

                foreach (Type requiredComponentType in requiredComponentTypes)
                {
                    MonoBehaviourBase component = RemoveComponentFromList(requiredComponentType, components);
                    if (component == Disposable.NULL)
                        gameObject.AddComponent(requiredComponentType);
                }
            }
        }

        private void InitRequiredComponentTypes()
        {
            if (_requiredComponentTypes == null)
                _requiredComponentTypes = new List<Type>();
            _requiredComponentTypes.Clear();
        }

        protected MonoBehaviourBase RemoveComponentFromList(Type type, MonoBehaviourBase[] components)
        {
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviourBase component = components[i];
                if (component != Disposable.NULL)
                {
                    Type componentType = component.GetType();
                    if (componentType == type || componentType.IsSubclassOf(type))
                    {
                        components[i] = null;
                        return component;
                    }
                }
            }
            return null;
        }

        public List<Type> GetRequiredComponentTypes()
        {
            List<Type> requiredComponentTypes = new List<Type>();
            GetRequiredComponentTypes(ref requiredComponentTypes);
            return requiredComponentTypes;
        }

        public void GetRequiredComponentTypes(ref List<Type> types)
        {
            types.Clear();

            IEnumerable<RequireComponent> requiredComponents = MemberUtility.GetAllAttributes<RequireComponent>(this, false);
            foreach (RequireComponent requiredComponent in requiredComponents)
            {
                if (requiredComponent.m_Type0 != null)
                    types.Add(requiredComponent.m_Type0);
                if (requiredComponent.m_Type1 != null)
                    types.Add(requiredComponent.m_Type1);
                if (requiredComponent.m_Type2 != null)
                    types.Add(requiredComponent.m_Type2);
            }

            if (!isFallbackValues)
            {
                IEnumerable<RequireScriptAttribute> requiredScripts = MemberUtility.GetAllAttributes<RequireScriptAttribute>(this, false);
                foreach (RequireScriptAttribute requiredScript in requiredScripts)
                {
                    if (requiredScript.requiredScript != null)
                        types.Add(requiredScript.requiredScript);
                    if (requiredScript.requiredScript2 != null)
                        types.Add(requiredScript.requiredScript2);
                    if (requiredScript.requiredScript3 != null)
                        types.Add(requiredScript.requiredScript3);
                }
            }
        }

        protected override void DetectChanges()
        {
            base.DetectChanges();

            if (_lastName != name)
            {
                string newValue = name;
                base.name = _lastName;
                name = newValue;
            }
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

            if (isFallbackValues || dontSaveToScene)
                gameObject.hideFlags |= HideFlags.DontSave;
        }

        protected override bool UpdateHideFlags()
        {
            if (base.UpdateHideFlags())
            {
                gameObject.hideFlags = hideFlags;

                if (isFallbackValues)
                {
                    bool debug = false;

                    if (!SceneManager.IsSceneBeingDestroyed())
                        debug = sceneManager.debug;

                    if (!debug)
                        gameObject.hideFlags |= HideFlags.HideInHierarchy;
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
            return isFallbackValues || PersistentScriptableObject.DEFAULT_DONT_SAVE_TO_SCENE;
        }

        private string _lastName;
        /// <summary>
        /// The name of the object.
        /// </summary>
        [Json(conditionalMethod: nameof(IsNotFallbackValues))]
        public new string name
        {
            get { return base.name; }
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
        /// The interval (in seconds) at which we call the <see cref="Synchronize"/> function. Automatically calling <see cref="Synchronize"/> can be useful to keep objects in synch with a datasource. Set to zero to deactivate.
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
        /// When enabled, the object can be disposed automatically by its datasource if it is no longer required in any loader and does not have any out of synch properties.
        /// </summary>
        [Json]
        public bool autoDispose
        {
            get { return _autoDispose; }
            set { SetValue(nameof(autoDispose), value, ref _autoDispose); }
        }

        /// <summary>
        /// When enabled, the object will not be saved as part of the Scene.
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
        /// When enabled, a new <see cref="IPersistent"/> will be created if none is present in the datasource.
        /// </summary>
        [Json(conditionalMethod: nameof(IsFallbackValues))]
        public bool createPersistentIfMissing
        {
            get { return _createPersistentIfMissing; }
            set { SetValue(nameof(_createPersistentIfMissing), value, ref _createPersistentIfMissing); }
        }

        /// <summary>
        /// Whether or not the object contains data that is copyrighted. If true the object may not be persisted in a Scene or <see cref="Datasource"/>.
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

        /// <summary>
        /// Push this object's values to the datasource.
        /// </summary>
        /// <returns></returns>
        public int Save()
        {
            int saved = 0;

            if (PersistenceSaveOperationEvent != null)
                PersistenceSaveOperationEvent(this, () => { saved++; });

            return saved;
        }

        /// <summary>
        /// Pull the values from the datasource and use them to update this object.
        /// </summary>
        /// <returns></returns>
        public int Synchronize()
        {
            int synchronized = 0;

            if (PersistenceSynchronizeOperationEvent != null)
                PersistenceSynchronizeOperationEvent(this, () => { synchronized++; });

            return synchronized;
        }

        /// <summary>
        /// Delete from datasource (this object will not be deleted).
        /// </summary>
        /// <returns></returns>
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

        protected override bool OnDisposed(DisposeManager.DestroyContext destroyState)
        {
            if (base.OnDisposed(destroyState))
            { 
                if (destroyState == DisposeManager.DestroyContext.Unknown)
                {
                    InitRequiredComponentTypes();
                    GetRequiredComponentTypes(ref _requiredComponentTypes);

                    MonoBehaviour[] components = gameObject.GetComponents<MonoBehaviour>();
                    for (int i = components.Length - 1; i >= 0; i--)
                    {
                        MonoBehaviour component = components[i];
                        if (component != this)
                        {
                            Type componentType = component.GetType();
                            if (!_requiredComponentTypes.Remove(componentType))
                                Dispose(component);
                        }
                    }
                }

                return true;
            }
            return false;
        }
    }
}
