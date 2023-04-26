// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/" + nameof(IdLoader))]
    public class IdLoader : LoaderBase
    {
        [Serializable]
        private class IdReferencesDictionary : SerializableDictionary<SerializableGuid, ReferencesList> { };
        [Serializable]
        private class IdLoadScopeDictionary : SerializableDictionary<SerializableGuid, IdLoadScope> { };

        [BeginFoldout("Ids")]
        [SerializeField, Tooltip("The list of ids to load.")]
        private List<SerializableGuid> _ids;

        [SerializeField, EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private IdLoadScopeDictionary _idLoadScopes;

        [SerializeField, HideInInspector]
        private IdReferencesDictionary _idReferences;

        public override void Recycle()
        {
            base.Recycle();

            _ids?.Clear();
            _idReferences?.Clear();
            _idLoadScopes?.Clear();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                ids.Clear();
                idReferences.Clear();
                idLoadScopes.Clear();
            }

            if (initializingContext == InitializationContext.Existing)
            {
                PerformAddRemoveLoadScopesChange(idLoadScopes, idLoadScopes);
                PerformAddRemoveReferencesChange(idReferences, idReferences);
            }

            InitValue(value => ids = value, new List<SerializableGuid>(), initializingContext);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                lastIdReferences.Clear();
                for (int i = idReferences.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<SerializableGuid, ReferencesList> keyValuePair = idReferences.ElementAt(i);
                    ReferencesList referencesList = new();
                    lastIdReferences.Add(keyValuePair.Key, referencesList);
                    for (int e = keyValuePair.Value.Count - 1; e >= 0; e--)
                        referencesList.Add(keyValuePair.Value[e]);
                }

                lastIdLoadScopes.Clear();
                lastIdLoadScopes.CopyFrom(idLoadScopes);
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private IdLoadScopeDictionary _lastIdLoadScopes;
        private IdLoadScopeDictionary lastIdLoadScopes
        {
            get { _lastIdLoadScopes ??= new(); return _lastIdLoadScopes; }
        }

        private IdReferencesDictionary _lastIdReferences;
        private IdReferencesDictionary lastIdReferences
        {
            get { _lastIdReferences ??= new(); return _lastIdReferences; }
        }

        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                PerformAddRemoveLoadScopesChange(idLoadScopes, lastIdLoadScopes, true);

                PerformAddRemoveReferencesChange(idReferences, lastIdReferences);

                return true;
            }
            return false;
        }

        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                //Trigger Load() on any loadScopes that were interrupted by the assembly reload.
                PerformAddRemoveLoadScopesChange(idLoadScopes, idLoadScopes);

                return true;
            }
            return false;
        }
#endif

        private IdLoadScopeDictionary idLoadScopes
        {
            get { _idLoadScopes ??= new IdLoadScopeDictionary(); return _idLoadScopes; }
            set => _idLoadScopes = value;
        }

        protected override void IterateOverLoadScopeKeys(Action<int, object, ReferencesList> callback)
        {
            base.IterateOverLoadScopeKeys(callback);

            if (callback != null)
            {
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    SerializableGuid id = ids[i];

                    idReferences.TryGetValue(id, out ReferencesList references);
                
                    callback(i, id, references);
                }
            }
        }

        protected override int GetLoadScopeCount()
        {
            return base.GetLoadScopeCount() + idLoadScopes.Count;
        }

        /// <summary>
        /// The list of ids to load. 
        /// </summary>
        [Json]
        protected List<SerializableGuid> ids
        {
            get { _ids ??= new List<SerializableGuid>(); return _ids; }
            set { SetValue(nameof(ids), value, ref _ids); }
        }

        private IdReferencesDictionary idReferences
        {
            get { _idReferences ??= new IdReferencesDictionary(); return _idReferences; }
        }

        public bool AddId(SerializableGuid id)
        {
            if (!ids.Contains(id))
            {
                ids.Add(id);

                PropertyAssigned(this, nameof(ids), ids, null);

                return true;
            }
            return false;
        }

        public bool RemoveId(SerializableGuid id)
        {
            if (ids.Remove(id))
            {
                PropertyAssigned(this, nameof(ids), ids, null);

                return true;
            }
            return false;
        }

        public override bool AddReference(object loadScopeKey, ReferenceBase reference)
        {
            SerializableGuid scopeId = (SerializableGuid)loadScopeKey;
            if (scopeId != SerializableGuid.Empty)
            {
#if UNITY_EDITOR
                if (!lastIdReferences.TryGetValue(scopeId, out ReferencesList lastReferences))
                {
                    lastReferences = new ReferencesList();
                    lastIdReferences[scopeId] = new ReferencesList();
                }

                if (!lastReferences.Contains(reference))
                    lastReferences.Add(reference);
#endif

                if (!idReferences.TryGetValue(scopeId, out ReferencesList references))
                {
                    references = new ReferencesList();
                    idReferences[scopeId] = references;
                }

                if (!references.Contains(reference))
                {
                    references.Add(reference);

                    AddId(scopeId);

                    return true;
                }
            }
            return false;
        }

        public override bool RemoveReference(object loadScopeKey, ReferenceBase reference, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.RemoveReference(loadScopeKey, reference, disposeContext))
            {
                SerializableGuid loadScopeId = (SerializableGuid)loadScopeKey;
                if (loadScopeId != SerializableGuid.Empty)
                {
#if UNITY_EDITOR
                    RegisterCompleteObjectUndo(disposeContext);

                    if (lastIdReferences.TryGetValue(loadScopeId, out ReferencesList lastReferences))
                    {
                        if (lastReferences.Remove(reference) && lastReferences.IsEmpty())
                            lastIdReferences.Remove(loadScopeId);
                    }
#endif

                    if (idReferences.TryGetValue(loadScopeId, out ReferencesList references))
                    {
                        if (references.Remove(reference) && references.IsEmpty())
                        {
                            idReferences.Remove(loadScopeId);

                            RemoveId(loadScopeId);

                            if (GetLoadScope(out LoadScope loadScope, loadScopeKey))
                                DisposeLoadScope(loadScope, disposeContext);

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool IsInList(LoadScope loadScope)
        {
            if (base.IsInList(loadScope))
            {
                IdLoadScope idLoadScope = loadScope as IdLoadScope;

                if (idLoadScope != Disposable.NULL)
                    return ids.Contains(idLoadScope.scopeId);
            }

            return false;
        }

        protected override bool AddLoadScopeInternal(LoadScope loadScope)
        {
            if (base.AddLoadScopeInternal(loadScope))
            {
                if (loadScope is IdLoadScope)
                {
                    IdLoadScope idLoadScope = loadScope as IdLoadScope;

                    SerializableGuid id = idLoadScope.scopeId;
                    if (idLoadScopes.TryAdd(id, idLoadScope))
                    {
#if UNITY_EDITOR
                        lastIdLoadScopes.Add(id, idLoadScope);
#endif
                        return true;
                    }
                }
            }
            return false;
        }

        protected override bool RemoveLoadScopeInternal(object loadScopeKey, out LoadScope loadScope)
        {
            if (base.RemoveLoadScopeInternal(loadScopeKey, out loadScope))
            {
                SerializableGuid id = (SerializableGuid)loadScopeKey;
                if (idLoadScopes.TryGetValue(id, out IdLoadScope idLoadScope) && idLoadScopes.Remove(id))
                {
                    loadScope = idLoadScope;
#if UNITY_EDITOR
                    lastIdLoadScopes.Remove(id);
#endif
                    return true;
                }
            }
            return false;
        }

        public override bool GetLoadScope(out LoadScope loadScope, IPersistent persistent)
        {
            if (GetLoadScope(out LoadScope idLoadScope, persistent.id))
            {
                loadScope = idLoadScope;
                return true;
            }

            loadScope = null;
            return false;
        }

         public override bool GetLoadScope(out LoadScope loadScope, object loadScopeKey, bool reload = false, bool createIfMissing = false, float loadInterval = 0.0f)
        {
            bool load = false;

            loadScope = null;

            SerializableGuid id = (SerializableGuid)loadScopeKey;
            if (id != SerializableGuid.Empty)
            {
                if (idLoadScopes.TryGetValue(id, out IdLoadScope idLoadScope) && idLoadScope != Disposable.NULL)
                {
                    loadScope = idLoadScope;
                    load = reload;
                }
                else if (createIfMissing)
                {
                    if (loadScope is not null)
                        RemoveLoadScope(id);

                    loadScope = CreateIdLoadScope(id);

                    if (AddLoadScope(loadScope))
                        load = true;
                }
            }

            if (loadScope != Disposable.NULL)
            {
                if (load)
                    Load(loadScope);

                return true;
            }
            else
                return false;
        }

        protected override List<LoadScope> GetListedLoadScopes(bool reload)
        {
            List<LoadScope> loadScopes = base.GetListedLoadScopes(reload);

            foreach (SerializableGuid id in ids)
            {
                if (GetLoadScope(out LoadScope loadScope, id, reload, true))
                {
                    loadScopes ??= new List<LoadScope>();
                    loadScopes.Add(loadScope);
                }
            }

            return loadScopes;
        }

        public override bool IterateOverLoadScopes(Func<object, LoadScope, bool> callback)
        {
            if (base.IterateOverLoadScopes(callback))
            {
                if (callback != null)
                {
                    for (int i = idLoadScopes.Count - 1; i >= 0; i--)
                    {
                        KeyValuePair<SerializableGuid, IdLoadScope> idLoadScope = idLoadScopes.ElementAt(i);
                        if (!callback(idLoadScope.Key, idLoadScope.Value))
                            return false;
                    }
                }

                return true;
            }
            return false;
        }

        protected IdLoadScope CreateIdLoadScope(SerializableGuid id)
        {
            return (CreateLoadScope(typeof(IdLoadScope), id.ToString()) as IdLoadScope).Init(this, id);
        }
    }
}
