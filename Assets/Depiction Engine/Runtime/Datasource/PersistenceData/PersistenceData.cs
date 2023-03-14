// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Class containing information about an <see cref="DepictionEngine.IPersistent"/> object's relationship with a <see cref="DepictionEngine.DatasourceBase"/>.
    /// </summary>
    public class PersistenceData : ScriptableObjectDisposable
    {
        [Serializable]
        private class OutOfSyncDictionary : SerializableDictionary<SerializableGuid, OutOfSyncKeysDictionary> { };
        [Serializable]
        private class OutOfSyncKeysDictionary : SerializableDictionary<int, bool> { };

        [SerializeField]
        private PersistentMonoBehaviour _persistentMonoBehaviour;
        [SerializeField]
        private PersistentScriptableObject _persistentScriptableObject;

        [SerializeField]
        private Datasource _datasource;

        [SerializeField]
        private OutOfSyncDictionary _outOfSyncDictionary;

        [SerializeField]
        private bool _isOutOfSync;
        [SerializeField]
        private bool _canBeAutoDisposed;

        [SerializeField]
        private bool _supportsSave;
        [SerializeField]
        private bool _supportsSynchronize;
        [SerializeField]
        private bool _supportsDelete;

        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.IProperty.PropertyAssignedEvent"/> is dispatched by the encapsulated <see cref="DepictionEngine.IPersistent"/>.
        /// </summary>
        public Action<IProperty, string, object, object> PropertyAssignedEvent;
        /// <summary>
        /// Dispatched when a <see cref="DepictionEngine.IPersistent.PersistenceSaveOperationEvent"/>, <see cref="DepictionEngine.IPersistent.PersistenceSynchronizeOperationEvent"/> or <see cref="DepictionEngine.IPersistent.PersistenceDeleteOperationEvent"/> is dispatched by the encapsulated <see cref="DepictionEngine.IPersistent"/>.
        /// </summary>
        public Action<Datasource.OperationType, IPersistent, Action> PersistenceOperationEvent;
        /// <summary>
        /// Dispatched after the <see cref="DepictionEngine.PersistenceData.canBeAutoDisposed"/> value changed.
        /// </summary>
        public Action<PersistenceData> CanBeAutoDisposedChangedEvent;

        public override void Recycle()
        {
            base.Recycle();

            _persistentMonoBehaviour = default;
            _persistentScriptableObject = default;
            
            _datasource = default;

            _outOfSyncDictionary?.Clear();
        }

        public PersistenceData Init(Datasource datasource, IPersistent persistent)
        {
            this.datasource = datasource;
            this.persistent = persistent;

            UpdateCanBeAutoDisposed();

            return this;
        }

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemovePersistentDelegates(persistent);
                AddPersistentDelegates(persistent);

                return true;
            }
            return false;
        }

        protected virtual bool RemovePersistentDelegates(IPersistent persistent)
        {
            if (persistent is not null)
            {
                persistent.DisposedEvent -= PersistentDisposedHandler;
                persistent.PropertyAssignedEvent -= PersistentPropertyAssignedHandler;
                persistent.UserPropertyAssignedEvent -= PersistentUserPropertyAssignedHandler;

                if (supportsSave)
                    persistent.PersistenceSaveOperationEvent -= PersistenceSaveOperationHandler;
                if (supportsSynchronize)
                    persistent.PersistenceSynchronizeOperationEvent -= PersistenceSynchronizeOperationHandler;
                if (supportsDelete)
                    persistent.PersistenceDeleteOperationEvent -= PersistenceDeleteOperationHandler;

                return true;
            }
            return false;
        }

        protected virtual bool AddPersistentDelegates(IPersistent persistent)
        {
            if (!IsDisposing() && !Disposable.IsDisposed(persistent))
            {
                persistent.DisposedEvent += PersistentDisposedHandler;
                persistent.PropertyAssignedEvent += PersistentPropertyAssignedHandler;
                persistent.UserPropertyAssignedEvent += PersistentUserPropertyAssignedHandler;

                if (supportsSave)
                    persistent.PersistenceSaveOperationEvent += PersistenceSaveOperationHandler;
                if (supportsSynchronize)
                    persistent.PersistenceSynchronizeOperationEvent += PersistenceSynchronizeOperationHandler;
                if (supportsDelete)
                    persistent.PersistenceDeleteOperationEvent += PersistenceDeleteOperationHandler;

                return true;
            }
            return false;
        }

        private void PersistentDisposedHandler(IDisposable disposable, DisposeContext disposeContext)
        {
            Dispose(this, disposeContext);
        }

        private void PersistentPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(PersistentMonoBehaviour.autoDispose))
                UpdateCanBeAutoDisposed();

            PropertyAssignedEvent?.Invoke(property, name, newValue, oldValue);
        }

        private void PersistentUserPropertyAssignedHandler(IJson iJson, PropertyInfo propertyInfo)
        {
            if (name == nameof(PersistentMonoBehaviour.autoDispose))
                UpdateCanBeAutoDisposed();

            if (SetPropertyOutOfSync(iJson, propertyInfo))
                isOutOfSync = true;
        }

        private void PersistenceSaveOperationHandler(IPersistent persistent, Action callback)
        {
            PersistenceOperationEvent?.Invoke(Datasource.OperationType.Save, persistent, callback);
        }

        private void PersistenceSynchronizeOperationHandler(IPersistent persistent, Action callback)
        {
            PersistenceOperationEvent?.Invoke(Datasource.OperationType.Synchronize, persistent, callback);
        }

        private void PersistenceDeleteOperationHandler(IPersistent persistent, Action callback)
        {
            PersistenceOperationEvent?.Invoke(Datasource.OperationType.Delete, persistent, callback);
        }

        public Datasource datasource
        {
            get { return _datasource; }
            set { _datasource = value; }
        }

        public IPersistent persistent
        {
            get
            {
                if (!Object.Equals(_persistentMonoBehaviour, null))
                    return _persistentMonoBehaviour;
                if (!Object.Equals(_persistentScriptableObject, null))
                    return _persistentScriptableObject;
                return null;
            }
            private set
            {
                if (persistent == value)
                    return;

                RemovePersistentDelegates(persistent);

                if (value is PersistentMonoBehaviour)
                    _persistentMonoBehaviour = value as PersistentMonoBehaviour;
                if (value is PersistentScriptableObject)
                    _persistentScriptableObject = value as PersistentScriptableObject;

                AddPersistentDelegates(persistent);
            }
        }

        public bool supportsSave
        {
            get { return _supportsSave; }
            set 
            {
                if (_supportsSave == value)
                    return;

                RemovePersistentDelegates(persistent);

                _supportsSave = value;

                AddPersistentDelegates(persistent);
            }
        }

        public bool supportsSynchronize
        {
            get { return _supportsSynchronize; }
            set
            {
                if (_supportsSynchronize == value)
                    return;

                RemovePersistentDelegates(persistent);

                _supportsSynchronize = value;

                AddPersistentDelegates(persistent);
            }
        }

        public bool supportsDelete
        {
            get { return _supportsDelete; }
            set
            {
                if (_supportsDelete == value)
                    return;

                RemovePersistentDelegates(persistent);

                _supportsDelete = value;

                AddPersistentDelegates(persistent);
            }
        }

        private OutOfSyncDictionary outOfSyncDictionary
        {
            get 
            {
                _outOfSyncDictionary ??= new OutOfSyncDictionary();
                return _outOfSyncDictionary; 
            }
        }

        protected void UpdateIsOutOfSync()
        {
            isOutOfSync = IsOutOfSync();
        }

        public bool isOutOfSync
        {
            get { return _isOutOfSync; }
            private set 
            { 
                SetValue(nameof(isOutOfSync), value, ref _isOutOfSync, (newValue, oldValue) =>
                {
                    if (initialized)
                        UpdateCanBeAutoDisposed();
                });
            }
        }

        public void UpdateCanBeAutoDisposed()
        {
            canBeAutoDisposed = GetCanBeAutoDisposed();
        }

        public bool canBeAutoDisposed
        {
            get { return _canBeAutoDisposed; }
            private set 
            { 
                SetValue(nameof(canBeAutoDisposed), value, ref _canBeAutoDisposed, (oldValue, newValue) => 
                {
                    CanBeAutoDisposedChangedEvent?.Invoke(this);
                }); 
            }
        }

        public virtual bool GetCanBeAutoDisposed()
        {
            return persistent.autoDispose && CanBeDisposed();
        }

        protected virtual bool CanBeDisposed()
        {
            return !IsOutOfSync(true);
        }

        public bool IsOutOfSync(bool autoDispose = false)
        {
            bool isOutOfSync = false;

            IterateOverOutOfSync((iJson, key, preventAutoDispse) =>
            {
                if (!autoDispose || !preventAutoDispse)
                {
                    isOutOfSync = true;
                    return false;
                }
                return true;
            });

            if (isOutOfSync)
                return true;

            return false;
        }

        public bool IsPropertyOutOfSync(IJson iJson, string name)
        {
            if (outOfSyncDictionary.TryGetValue(iJson.id, out OutOfSyncKeysDictionary outOfSyncKeys))
                return outOfSyncKeys.ContainsKey(PropertyMonoBehaviour.GetPropertyKey(name));

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAllSync(IJson iJson = null)
        {
            if (iJson is null)
            {
                outOfSyncDictionary.Clear();

                UpdateIsOutOfSync();
            } 
            else if (outOfSyncDictionary.TryGetValue(iJson.id, out OutOfSyncKeysDictionary outOfSyncKey))
                outOfSyncKey.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAllOutOfSync(IJson iJson = null, bool allowAutoDispose = false)
        {
            bool outOfSynchChanged = false;

            MemberUtility.IterateOverJsonAttribute(!Disposable.IsDisposed(iJson) ? iJson : persistent, (iJson, accessor, name, jsonAttribute, propertyInfo) =>
            {
                if (SetPropertyOutOfSync(iJson, propertyInfo, allowAutoDispose))
                    outOfSynchChanged = true;
            });

            if (outOfSynchChanged)
                UpdateIsOutOfSync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool SetPropertyOutOfSync(IJson iJson, PropertyInfo propertyInfo, bool allowAutoDispose = false)
        {
            bool outOfSyncChanged = false;

            int key = PropertyMonoBehaviour.GetPropertyKey(propertyInfo.Name);
            if (!iJson.IsDynamicProperty(key) && SetKeyOutOfSync(iJson, key, allowAutoDispose))
                outOfSyncChanged = true;

            return outOfSyncChanged;
        }

        private bool SetKeyOutOfSync(IJson iJson, int key, bool allowAutoDispose = false)
        {
            SerializableGuid id = iJson.id;
            if (!outOfSyncDictionary.TryGetValue(id, out OutOfSyncKeysDictionary outOfSyncKeys))
            {
                outOfSyncKeys = new OutOfSyncKeysDictionary();
                outOfSyncDictionary.Add(id, outOfSyncKeys);
            }

            bool outOfSynchChanged = false;

            if (!outOfSyncKeys.ContainsKey(key))
            {
                outOfSyncKeys.Add(key, allowAutoDispose);
                outOfSynchChanged = true;
            }
            else
                outOfSyncKeys[key] = allowAutoDispose;

            return outOfSynchChanged;
        }

        private bool IterateOverOutOfSync(Func<IJson, int, bool, bool> callback)
        {
            foreach (SerializableGuid id in outOfSyncDictionary.Keys)
            {
                OutOfSyncKeysDictionary outOfSyncKeys = outOfSyncDictionary[id];
                IJson iJson = InstanceManager.Instance().GetIJson(id);
                if (iJson != null)
                {
                    foreach (int key in outOfSyncKeys.Keys)
                    {
                        if (!callback(iJson, key, outOfSyncKeys[key]))
                            return false;
                    }
                }
            }

            return true;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                PropertyAssignedEvent = null;
                PersistenceOperationEvent = null;
                CanBeAutoDisposedChangedEvent = null;

                return true;
            }
            return false;
        }
    }
}
