// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
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
        [SerializeField, Tooltip("The list of ids to load."), EndFoldout]
        private List<SerializableGuid> _ids;

        private IdReferencesDictionary _idReferences;

        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetDebug))]
#endif
        private IdLoadScopeDictionary _idLoadScopes;

        protected override void ClearLoadScopes()
        {
            base.ClearLoadScopes();

            if (_idReferences != null)
                _idReferences.Clear();

            if (_idLoadScopes != null)
                _idLoadScopes.Clear();
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            InitValue(value => ids = value, new List<SerializableGuid>(), initializingState);
        }

        public override bool LateInitialize()
        {
            if (base.LateInitialize())
            {
                if (_ids != null && _idReferences != null)
                {
                    for (int i = _ids.Count - 1; i >= 0; i--)
                    {
                        SerializableGuid id = _ids[i];

                        if (_idReferences.TryGetValue(id, out ReferencesList references))
                        {
                            if (references.RemoveNullReferences())
                            {
                                _idReferences.Remove(id);
                                _ids.RemoveAt(i);

                                if (GetLoadScope(out IdLoadScope loadScope, id))
                                    DisposeLoadScope(loadScope);
                            }
                        }
                    }
                }

                return true;
            }
            return false;
        }

        protected override bool DetectNullLoadScope()
        {
            bool nullDetected = base.DetectNullLoadScope();

            if (!nullDetected)
            {
                foreach (LoadScope loadScope in idLoadScopes.Values)
                {
                    if (loadScope == Disposable.NULL)
                    {
                        nullDetected = true;
                        break;
                    }
                }
            }

            return nullDetected;
        }

        private IdLoadScopeDictionary idLoadScopes
        {
            get 
            {
                if (_idLoadScopes == null)
                    _idLoadScopes = new IdLoadScopeDictionary();
                return _idLoadScopes; 
            }
            set { _idLoadScopes = value; }
        }

        /// <summary>
        /// The list of ids to load. 
        /// </summary>
        [Json]
        protected List<SerializableGuid> ids
        {
            get 
            { 
                if (_ids == null)
                    _ids = new List<SerializableGuid>();
                return _ids; 
            }
            set { SetValue(nameof(ids), value, ref _ids); }
        }

        private IdReferencesDictionary idReferences
        {
            get
            {
                if (_idReferences == null)
                    _idReferences = new IdReferencesDictionary();
                return _idReferences;
            }
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

        public override bool AddReference(LoadScope loadScope, ReferenceBase reference)
        {
            IdLoadScope idLoadScope = loadScope as IdLoadScope;
            if (idLoadScope != Disposable.NULL)
            {
                SerializableGuid scopeId = idLoadScope.scopeId;
                if (scopeId != SerializableGuid.Empty)
                {
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
            }
            return false;
        }

        public override bool RemoveReference(LoadScope loadScope, ReferenceBase reference)
        {
            IdLoadScope idLoadScope = loadScope as IdLoadScope;
            if (idLoadScope != Disposable.NULL)
            {
                SerializableGuid scopeId = idLoadScope.scopeId;
                if (scopeId != SerializableGuid.Empty)
                {
                    if (idReferences.TryGetValue(scopeId, out ReferencesList references) && references.Remove(reference))
                    {
                        if (references.IsEmpty() && RemoveId(scopeId))
                            DisposeLoadScope(loadScope);

                        return true;
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

                    SerializableGuid key = idLoadScope.scopeId;
                    if (!idLoadScopes.ContainsKey(key))
                    {
                        idLoadScopes.Add(key, idLoadScope);
                        return true;
                    }
                }
            }
            return false;
        }

        protected override bool RemoveLoadScopeInternal(LoadScope loadScope)
        {
            if (base.RemoveLoadScopeInternal(loadScope))
            {
                if (loadScope is IdLoadScope)
                {
                    IdLoadScope idLoadScope = loadScope as IdLoadScope;

                    SerializableGuid key = idLoadScope.scopeId;
                    if (idLoadScopes.Remove(key))
                        return true;
                }
            }
            return false;
        }

        public override bool GetLoadScope(out LoadScope loadScope, IPersistent persistent)
        {
            if (GetLoadScope(out IdLoadScope idLoadScope, persistent.id))
            {
                loadScope = idLoadScope;
                return true;
            }

            loadScope = null;
            return false;
        }

        public bool GetLoadScope(out IdLoadScope loadScope, SerializableGuid id, bool reload = false, bool createIfMissing = false)
        {
            bool load = false;

            loadScope = null;

            if (id != SerializableGuid.Empty)
            {
                if (idLoadScopes.TryGetValue(id, out loadScope) && loadScope != Disposable.NULL)
                    load = reload;
                else if (createIfMissing)
                {
                    if (!Object.ReferenceEquals(loadScope, null))
                        RemoveLoadScope(loadScope);

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
                if (GetLoadScope(out IdLoadScope loadScope, id, reload, true))
                {
                    if (loadScopes == null)
                        loadScopes = new List<LoadScope>();
                    loadScopes.Add(loadScope);
                }
            }

            return loadScopes;
        }

        public override bool IterateOverLoadScopes(Func<LoadScope, bool> callback)
        {
            if (base.IterateOverLoadScopes(callback))
            {
                if (callback != null)
                {
                    foreach (IdLoadScope loadScope in idLoadScopes.Values)
                    {
                        if (loadScope != Disposable.NULL && !callback(loadScope))
                            return false;
                    }
                }

                return true;
            }
            return false;
        }

        protected IdLoadScope CreateIdLoadScope(SerializableGuid id)
        {
            return (CreateLoadScope(typeof(IdLoadScope)) as IdLoadScope).Init(this, id);
        }
    }
}
