// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DepictionEngine
{
    public class GeneratorBase : Script
    {
        [BeginFoldout("Fallback Values")]
        [SerializeField, ComponentReference, Tooltip("An ids list of the fallbackValues scripts containing default values to use on newly created objects."), EndFoldout]
        private List<SerializableGuid> _fallbackValuesId;

        [BeginFoldout("Procedural")]
        [SerializeField, Tooltip("Use a random number other then -1 to create objects procedurally. Only works when no datasource is specified."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalEnable(nameof(GetEnableSeed))]
#endif
        private int _seed;

        [SerializeField, HideInInspector]
        private List<FallbackValues> _fallbackValues;

#if UNITY_EDITOR
        protected virtual bool GetEnableSeed()
        {
            return true;
        }
#endif

        protected override void IterateOverComponentReference(Action<SerializableGuid, Action> callback)
        {
            base.IterateOverComponentReference(callback);

            if (_fallbackValuesId != null)
            {
                foreach (SerializableGuid componentId in _fallbackValuesId)
                    callback(componentId, UpdateFallbackValues);
            }
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => fallbackValuesId = value, GetDefaultFallbackValuesId(), () => { return GetDuplicateComponentReferenceId(fallbackValuesId, fallbackValues, initializingContext); }, initializingContext);
            InitValue(value => seed = value, -1, initializingContext);
        }

        protected virtual List<SerializableGuid> GetDefaultFallbackValuesId()
        {
            return new List<SerializableGuid> { SerializableGuid.Empty };
        }

        private List<FallbackValues> fallbackValues
        {
            get { return _fallbackValues; }
        }

        /// <summary>
        /// An ids list of the fallbackValues scripts containing default values to use on newly created objects. 
        /// </summary>
        [Json]
        public List<SerializableGuid> fallbackValuesId
        {
            get { return _fallbackValuesId; }
            set 
            { 
                SetValue(nameof(fallbackValuesId), value, ref _fallbackValuesId, (newValue, oldValue) =>
                {
                    UpdateFallbackValues();
                });
            }
        }

        private void UpdateFallbackValues()
        {
            SetValue(nameof(fallbackValues), GetComponentFromId<FallbackValues>(fallbackValuesId), ref _fallbackValues);
        }

        /// <returns>The total number of loadScopes currently loading in the scene.</returns>
        public static int GetTotalLoadingCount()
        {
            int loadingCount = 0;

            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<GeneratorBase>(
                    (generator) =>
                    {
                        LoaderBase loader = generator as LoaderBase;
                        if (loader != Disposable.NULL)
                            loadingCount += loader.loadingCount;

                        return true;
                    });
            }

            return loadingCount;
        }

        /// <returns>The total number of loadScopes currently loaded in the scene.</returns>
        public static int GetTotalLoadedCount()
        {
            int loadedCount = 0;

            InstanceManager instanceManager = InstanceManager.Instance(false);
            if (instanceManager != Disposable.NULL)
            {
                instanceManager.IterateOverInstances<GeneratorBase>(
                   (generator) =>
                   {
                       LoaderBase loader = generator as LoaderBase;
                       if (loader != Disposable.NULL)
                           loadedCount += loader.loadedCount;

                       return true;
                   });
            }

            return loadedCount;
        }

        /// <summary>
        /// Use a random number other then -1 to create objects procedurally. Only works when no datasource is specified.
        /// </summary>
        [Json]
        public int seed
        {
            get { return _seed; }
            set { SetValue(nameof(seed), value, ref _seed); }
        }

        public void IterateOverFallbackValues<T>(Func<FallbackValues, bool> callback)
        {
            IterateOverFallbackValues(callback, typeof(T));
        }

        public void IterateOverFallbackValues<T, T1>(Func<FallbackValues, bool> callback)
        {
            IterateOverFallbackValues(callback, typeof(T), typeof(T1));
        }

        public void IterateOverFallbackValues(Func<FallbackValues, bool> callback, Type type, Type type1 = null)
        {
            foreach (SerializableGuid fallbackValueId in fallbackValuesId)
            {
                FallbackValues fallbackValues = instanceManager.GetFallbackValues(fallbackValueId);
                if (fallbackValues != Disposable.NULL)
                {
                    Type fallbackValuesType = fallbackValues.GetFallbackValuesType();
                    if (fallbackValuesType != null && ((type != null && type.IsAssignableFrom(fallbackValuesType)) || (type1 != null && type1.IsAssignableFrom(fallbackValuesType))) && !callback(fallbackValues))
                        break;
                }
            }
        }

        protected bool GeneratePersistent(Type type, JSONObject json, List<PropertyModifier> propertyModifiers = null)
        {
            return GeneratePersistent(out _, type, json, propertyModifiers);
        }

        public bool GeneratePersistent(out IPersistent persistent, Type type, JSONObject json, List<PropertyModifier> propertyModifiers = null)
        {
            persistent = instanceManager.GetPersistent(json);
            if (!Disposable.IsDisposed(persistent))
                return false;

            persistent = (IPersistent)instanceManager.CreateInstance(type, null, json, propertyModifiers, InitializationContext.Programmatically);

            if (!Disposable.IsDisposed(persistent))
                return true;

            persistent = null;
            return false;
        }
    }

    public class PropertyModifierDataProcessingFunctions : ProcessingFunctions
    {
        public static IEnumerator PopulatePropertyModifier(ProcessorOutput data, ProcessorParameters parameters)
        {
            foreach (object enumeration in PopulatePropertyModifier(data as PropertyModifierData, parameters as PropertyModifierParameters))
                yield return enumeration;
        }

        private static IEnumerable PopulatePropertyModifier(PropertyModifierData data, PropertyModifierParameters parameters)
        {
            data.Init(parameters.type, parameters.jsonFallback, parameters.persistentFallbackValuesId, GetProceduralPropertyModifier(parameters.type, parameters));

            yield break;
        }

        public static PropertyModifier GetProceduralPropertyModifier(Type type, PropertyModifierParameters parameters)
        {
            PropertyModifier propertyModifier = null;

            string proceduralMethodName = "GetProceduralPropertyModifier";

            MethodInfo methodInfo = type.GetMethod(proceduralMethodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (methodInfo != null)
                propertyModifier = methodInfo.Invoke(null, new object[] { parameters }) as PropertyModifier;
            else
                Debug.LogError("Missing "+ proceduralMethodName + " method for type: " + type.Name);

            return propertyModifier;
        }
    }

    public class PropertyModifierData : ProcessorOutput
    {
        public Type type;
        public JSONObject jsonFallback;
        public SerializableGuid persistentFallbackValuesId;
        public PropertyModifier propertyModifier;

        public void Init(Type type, JSONObject jsonFallback, SerializableGuid persistentFallbackValuesId, PropertyModifier propertyModifier)
        {
            this.type = type;
            this.jsonFallback = jsonFallback;
            this.persistentFallbackValuesId = persistentFallbackValuesId;
            this.propertyModifier = propertyModifier;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                DisposeManager.Dispose(propertyModifier);

                return true;
            }
            return false;
        }
    }

    public class PropertyModifierParameters : ProcessorParameters
    {
        public Type type;
        public JSONObject jsonFallback;
        public SerializableGuid persistentFallbackValuesId;
        public int seed;

        public void Init(Type type, JSONObject jsonFallback, SerializableGuid persistentFallbackValuesId, int seed)
        {
            this.type = type;
            this.jsonFallback = jsonFallback;
            this.persistentFallbackValuesId = persistentFallbackValuesId;
            this.seed = seed;
        }
    }

    public class PropertyModifierIndex2DParameters : PropertyModifierParameters
    {
        public Vector2Int grid2DIndex;
        public Vector2Int grid2DDimensions;

        public void Init(Vector2Int grid2DIndex, Vector2Int grid2DDimensions)
        {
            this.grid2DIndex = grid2DIndex;
            this.grid2DDimensions = grid2DDimensions;
        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                grid2DIndex = Vector2Int.zero;
                grid2DDimensions = Vector2Int.zero;

                return true;
            }
            return false;
        }
    }
}
