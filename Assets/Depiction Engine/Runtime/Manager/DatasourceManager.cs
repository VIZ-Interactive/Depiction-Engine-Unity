// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing <see cref="DepictionEngine.DatasourceBase"/>'s.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(DatasourceManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class DatasourceManager : ManagerBase, ILoadDatasource
    {
        [SerializeField]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDebug))]
#endif
        private Datasource _sceneDatasource;

        /// <summary>
        /// Dispatched after any of the <see cref="DepictionEngine.LoaderBase"/> found in the scene have their <see cref="DepictionEngine.LoaderBase.datasource"/> field value changed.
        /// </summary>
        public static Action<LoaderBase> DatasourceLoadersChangedEvent;

        private static DatasourceManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DatasourceManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL)
                _instance = GetManagerComponent<DatasourceManager>(createIfMissing);
            return _instance;
        }

        public override void Recycle()
        {
            base.Recycle();

            sceneDatasource?.Recycle();
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                sceneDatasource = default;

            sceneDatasource ??= CreateDatasource();
            sceneDatasource.Initialize(this, initializingContext);
        }

#if UNITY_EDITOR
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            sceneDatasource?.UndoRedoPerformed();
        }
#endif

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                sceneDatasource.UpdateAllDelegates(IsDisposing());

                InstanceManager instanceManager = InstanceManager.Instance(false);
                if (instanceManager != Disposable.NULL)
                {
                    instanceManager.IterateOverInstances<GeneratorBase>(
                       (generator) =>
                       {
                           LoaderBase loader = generator as LoaderBase;
                           if (loader != Disposable.NULL)
                           {
                               RemoveLoaderDelegates(loader);
                               AddLoaderDelegates(loader);
                           }

                           return true;
                       });
                }
                return true;
            }
            return false;
        }

        protected override void InstanceRemovedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            if (property is LoaderBase)
            {
                LoaderBase loader = property as LoaderBase;
                RemoveLoaderDelegates(loader);
                DatasourceLoadersChanged(loader);
            }
        }

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            if (property is LoaderBase)
            {
                LoaderBase loader = property as LoaderBase;
                AddLoaderDelegates(loader);
                DatasourceLoadersChanged(loader);
            }
        }

        private void RemoveLoaderDelegates(LoaderBase loader)
        {
            if (loader is not null)
                loader.PropertyAssignedEvent -= LoaderPropertyAssignedHandler;
        }

        private void AddLoaderDelegates(LoaderBase loader)
        {
            if (!IsDisposing() && loader != Disposable.NULL)
                loader.PropertyAssignedEvent += LoaderPropertyAssignedHandler;
        }

        private void LoaderPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            if (name == nameof(LoaderBase.datasource))
                DatasourceLoadersChanged(property as LoaderBase);
        }

        private void DatasourceLoadersChanged(LoaderBase loader)
        {
            DatasourceLoadersChangedEvent?.Invoke(loader);
        }

        public Datasource sceneDatasource
        {
            get => _sceneDatasource;
            private set => _sceneDatasource = value;
        }

        public DatasourceOperationBase Load(Action<List<IPersistent>> operationResult, LoadScope loadScope)
        {
            LoadSceneDatasourceOperation loadSceneDatasourceOperation = CreateLoadSceneDatasourceOperation(loadScope);

            sceneDatasource.Load(loadSceneDatasourceOperation, operationResult, loadScope);

            return loadSceneDatasourceOperation;
        }

        private LoadSceneDatasourceOperation CreateLoadSceneDatasourceOperation(LoadScope loadScope)
        {
            LoadSceneDatasourceOperation loadSceneDatasourceOperation = null;

            FallbackValues firstPersistentFallbackValues = loadScope.GetFirstPersistentFallbackValues();
            if (firstPersistentFallbackValues != Disposable.NULL)
                loadSceneDatasourceOperation = instanceManager.CreateInstance<LoadSceneDatasourceOperation>().Init(firstPersistentFallbackValues.GetFallbackValuesType(), loadScope.GetLoadScopeFallbackValuesJson(), firstPersistentFallbackValues.id, loadScope.seed);

            return loadSceneDatasourceOperation;
        }

        public static FileSystemDatasource GetFileSystemDatasource(string databaseNamespace, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            FileSystemDatasource fileSystemDatasource = null;

            InstanceManager instanceManager = InstanceManager.Instance();
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<DatasourceBase>(
                   (datasource) =>
                   {
                       if (datasource is FileSystemDatasource && datasource.GetDatasourceName() == databaseNamespace)
                       {
                           fileSystemDatasource = datasource as FileSystemDatasource;
                           return false;
                       }

                       return true;
                   });

                if (fileSystemDatasource == Disposable.NULL)
                {
                    fileSystemDatasource = CreateDatasource<FileSystemDatasource>(initializingContext);
                    fileSystemDatasource.databaseNamespace = databaseNamespace;
                }
            }

            return fileSystemDatasource;
        }

        public static RestDatasource GetRestDatasource(string baseAddress, string baseAddress2, string baseAddress3, string baseAddress4, InitializationContext initializingContext = InitializationContext.Programmatically)
        {
            RestDatasource restDatasource = null;

            InstanceManager instanceManager = InstanceManager.Instance();
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<DatasourceBase>(
                    (datasource) =>
                    {
                        if (datasource is RestDatasource && datasource.GetDatasourceName() == baseAddress)
                        {
                            restDatasource = datasource as RestDatasource;
                            return false;
                        }

                        return true;
                    });

                if (restDatasource == Disposable.NULL)
                {
                    restDatasource = CreateDatasource<RestDatasource>(initializingContext);
                    restDatasource.baseAddress = baseAddress;
                    restDatasource.baseAddress2 = baseAddress2;
                    restDatasource.baseAddress3 = baseAddress3;
                    restDatasource.baseAddress4 = baseAddress4;
                }
            }

            return restDatasource;
        }

        private static T CreateDatasource<T>(InitializationContext initializingContext = InitializationContext.Programmatically) where T : DatasourceBase
        {
            T datasource = null;

            string name = "Datasources";
            GameObject datasourcesGo = GameObject.Find(name);
            if (datasourcesGo == null)
            {
                datasourcesGo = new GameObject(name);
#if UNITY_EDITOR
                Editor.UndoManager.RegisterCreatedObjectUndo(datasourcesGo, initializingContext);
#endif
                datasourcesGo.AddSafeComponent<Object>(initializingContext);
            }

            datasource = datasourcesGo.AddSafeComponent<T>(initializingContext);

#if UNITY_EDITOR
            Editor.UndoManager.RegisterCompleteObjectUndo(datasource, initializingContext);
#endif

            return datasource;
        }

        public override bool PostHierarchicalUpdate()
        {
            if (base.PostHierarchicalUpdate())
            {
                instanceManager.IterateOverInstances<DatasourceBase>(
                   (datasource) =>
                   {
                       datasource.ProcessPersistenceOperations();

                       return true;
                   });

                return true;
            }
            return false;
        }

        public static Datasource CreateDatasource()
        {
            return new Datasource();
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                sceneDatasource?.OnDispose(disposeContext);

                return true;
            }
            return false;
        }
    }
}
