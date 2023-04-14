// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Generator/Loader/2D/Index/" + nameof(Index2DLoader))]
    public class Index2DLoader : Index2DLoaderBase
    {
        [Serializable]
        private class Index2DReferencesDictionary : SerializableDictionary<Grid2DIndex, ReferencesList> { };

        [BeginFoldout("Indices")]
        [SerializeField, Tooltip("The list of indices to load."), EndFoldout]
        private List<Grid2DIndex> _indices;

        [SerializeField, HideInInspector]
        private Index2DReferencesDictionary _index2DReferences;

        public override void Recycle()
        {
            base.Recycle();

            _indices?.Clear();
            _index2DReferences?.Clear();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
            {
                indices.Clear();
                index2DReferences.Clear();
            }

            if (initializingContext == InitializationContext.Existing)
                PerformAddRemoveReferencesChange(index2DReferences, index2DReferences);

            InitValue(value => indices = value, new List<Grid2DIndex>(), initializingContext);
        }

        protected override bool InitializeLastFields()
        {
            if (base.InitializeLastFields())
            {
#if UNITY_EDITOR
                lastIndexReferences.Clear();
                for (int i = index2DReferences.Count - 1; i >= 0; i--)
                {
                    KeyValuePair<Grid2DIndex, ReferencesList> keyValuePair = index2DReferences.ElementAt(i);
                    ReferencesList referencesList = new();
                    lastIndexReferences.Add(keyValuePair.Key, referencesList);
                    for (int e = keyValuePair.Value.Count - 1; e >= 0; e--)
                        referencesList.Add(keyValuePair.Value[e]);
                }
#endif
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private Index2DReferencesDictionary _lastIndexReferences;
        private Index2DReferencesDictionary lastIndexReferences
        {
            get { _lastIndexReferences ??= new(); return _lastIndexReferences; }
        }

        public override bool UndoRedoPerformed()
        {
            if (base.UndoRedoPerformed())
            {
                PerformAddRemoveReferencesChange(index2DReferences, lastIndexReferences);

                return true;
            }
            return false;
        }
#endif

        protected override void IterateOverLoadScopeKeys(Action<int, object, ReferencesList> callback)
        {
            base.IterateOverLoadScopeKeys(callback);

            if (callback != null)
            {
                for (int i = indices.Count - 1; i >= 0; i--)
                {
                    Grid2DIndex index = indices[i];

                    index2DReferences.TryGetValue(index, out ReferencesList references);

                    callback(i, index, references);
                }
            }
        }

        /// <summary>
        /// The list of indices to load. 
        /// </summary>
        [Json]
        protected List<Grid2DIndex> indices
        {
            get { _indices ??= new List<Grid2DIndex>(); return _indices; }
            set { SetValue(nameof(indices), value, ref _indices); }
        }

        private Index2DReferencesDictionary index2DReferences
        {
            get { _index2DReferences ??= new Index2DReferencesDictionary(); return _index2DReferences; }
        }

        public bool AddIndex(Grid2DIndex index)
        {
            if (!indices.Contains(index))
            {
                indices.Add(index);

                PropertyAssigned(this, nameof(indices), indices, null);

                return true;
            }
            return false;
        }

        public bool RemoveIndex(Grid2DIndex index)
        {
            if (indices.Remove(index))
            {
                PropertyAssigned(this, nameof(indices), indices, null);

                return true;
            }
            return false;
        }

        public override bool AddReference(object loadScopeKey, ReferenceBase reference)
        {
            Grid2DIndex loadScopeGrid2DIndex = (Grid2DIndex)loadScopeKey;
            if (loadScopeGrid2DIndex != Grid2DIndex.Empty)
            {
#if UNITY_EDITOR
                if (!lastIndexReferences.TryGetValue(loadScopeGrid2DIndex, out ReferencesList lastReferences))
                {
                    lastReferences = new ReferencesList();
                    lastIndexReferences[loadScopeGrid2DIndex] = lastReferences;
                }

                if (!lastReferences.Contains(reference))
                    lastReferences.Add(reference);
#endif

                if (!index2DReferences.TryGetValue(loadScopeGrid2DIndex, out ReferencesList references))
                {
                    references = new ReferencesList();
                    index2DReferences[loadScopeGrid2DIndex] = references;
                }

                if (!references.Contains(reference))
                {
                    references.Add(reference);

                    AddIndex(loadScopeGrid2DIndex);

                    return true;
                }
            }
            return false;
        }

        public override bool RemoveReference(object loadScopeKey, ReferenceBase reference, DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.RemoveReference(loadScopeKey, reference, disposeContext))
            {
                Grid2DIndex loadScopeGrid2DIndex = (Grid2DIndex)loadScopeKey;

                if (loadScopeGrid2DIndex != Grid2DIndex.Empty)
                {
#if UNITY_EDITOR
                    RegisterCompleteObjectUndo(disposeContext);

                    if (lastIndexReferences.TryGetValue(loadScopeGrid2DIndex, out ReferencesList lastReferences))
                    {
                        if (lastReferences.Remove(reference) && lastReferences.IsEmpty())
                            lastIndexReferences.Remove(loadScopeGrid2DIndex);
                    }
#endif

                    if (index2DReferences.TryGetValue(loadScopeGrid2DIndex, out ReferencesList references))
                    {
                        if (references.Remove(reference) && references.IsEmpty())
                        {
                            index2DReferences.Remove(loadScopeGrid2DIndex);

                            RemoveIndex(loadScopeGrid2DIndex);

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
                Index2DLoadScope indexLoadScope = loadScope as Index2DLoadScope;

                if (indexLoadScope != Disposable.NULL)
                    return indices.Contains(indexLoadScope.scopeGrid2DIndex);
            }
            return false;
        }

        protected override void IterateOverLoadScopeList(Action<Vector2Int, Vector2Int, Vector2Int> callback)
        {
            foreach (Grid2DIndex grid2DProperties in indices)
                callback(grid2DProperties.index, grid2DProperties.dimensions, Vector2Int.zero);
        }
    }
}
