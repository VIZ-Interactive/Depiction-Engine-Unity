// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    //Editor_Duplicate can be a Copy Paste / Duplicate Menu Item / Draging Droping Component
    /// <summary>
    /// The different types of initialization context. <br/><br/>
    /// <b><see cref="Unknown"/>:</b> <br/>
    /// The context is unknown. <br/><br/>
    /// <b><see cref="Programmatically"/>:</b> <br/>
    /// The initialization was triggered programmatically. <br/><br/>
    /// <b><see cref="Programmatically_Duplicate"/>:</b> <br/>
    /// The initialization was triggered programmatically and the object is a duplicate. <br/><br/>
    /// <b><see cref="Editor"/>:</b> <br/>
    /// The initialization was triggered in the editor. <br/><br/>
    /// <b><see cref="Editor_Duplicate"/>:</b> <br/>
    /// The initialization was triggered in the editor and the object is a duplicate. Duplication can come from a copy paste / duplicate menu item or draging droping of a component. <br/><br/>
    /// <b><see cref="Existing"/>:</b> <br/>
    /// The initialization was triggered by a loading scene or was triggered in the editor as a result of an undo or redo action. <br/><br/>
    /// <b><see cref="Reset"/>:</b> <br/>
    /// The object properties are reseted to their default values.
    /// </summary> 
    public enum InitializationContext
    {
        Unknown,
        Programmatically,
        Programmatically_Duplicate,
        Editor,
        Editor_Duplicate,
        Existing,
        Reset
    };

    /// <summary>
    /// Singleton managing instances.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(InstanceManager))]
    [RequireComponent(typeof(SceneManager))]
    [DisallowMultipleComponent]
    public class InstanceManager : ManagerBase
    {
        [Serializable]
        private class TransformDictionary : SerializableDictionary<SerializableGuid, TransformBase> { };
        [Serializable]
        private class PersistentMonoBehaviourDictionary : SerializableDictionary<SerializableGuid, PersistentMonoBehaviour> { };
        [Serializable]
        private class PersistentScriptableObjectDictionary : SerializableDictionary<SerializableGuid, PersistentScriptableObject> { };
        [Serializable]
        private class TerrainGridMeshObjectDictionary : SerializableDictionary<SerializableGuid, TerrainGridMeshObject> { };
        [Serializable]
        private class VisualObjectDictionary : SerializableDictionary<SerializableGuid, VisualObject> { };
        [Serializable]
        private class AstroObjectDictionary : SerializableDictionary<SerializableGuid, AstroObject> { };
        [Serializable]
        private class ManagerDictionary : SerializableDictionary<SerializableGuid, ManagerBase> { };

        [Serializable]
        private class AnimatorDictionary : SerializableDictionary<SerializableGuid, AnimatorBase> { };
        [Serializable]
        private class ControllerDictionary : SerializableDictionary<SerializableGuid, ControllerBase> { };
        [Serializable]
        private class GeneratorDictionary : SerializableDictionary<SerializableGuid, GeneratorBase> { };
        [Serializable]
        private class ReferenceDictionary : SerializableDictionary<SerializableGuid, ReferenceBase> { };
        [Serializable]
        private class EffectDictionary : SerializableDictionary<SerializableGuid, EffectBase> { };
        [Serializable]
        private class FallbackValuesDictionary : SerializableDictionary<SerializableGuid, FallbackValues> { };
        [Serializable]
        private class DatasourceDictionary : SerializableDictionary<SerializableGuid, DatasourceBase> { };

        public const string GLOBAL_LAYER = " Global";

        //Transform
        private TransformDictionary _transforms;

        //Persistent
        private PersistentMonoBehaviourDictionary _persistentMonoBehaviours;
        private PersistentScriptableObjectDictionary _persistentScriptableObjects;
        private TerrainGridMeshObjectDictionary _terrainGridMeshObjects;
        private VisualObjectDictionary _visualObjects;
        private AstroObjectDictionary _astroObjects;
        private List<Star> _stars;
        private List<StarSystem> _starSystems;
        //Camera
        private List<Camera> _cameras;
        private List<int> _camerasInstanceIds;

        //Script
        private AnimatorDictionary _animators;
        private ControllerDictionary _controllers;
        private GeneratorDictionary _generators;
        private ReferenceDictionary _references;
        private EffectDictionary _effects;
        private FallbackValuesDictionary _fallbackValues;
        private DatasourceDictionary _datasources;

        //Manager
        private ManagerDictionary _managers;

        [ThreadStatic]
        private static InitializationContext _initializingContext = InitializationContext.Editor;
        public static InitializationContext initializingContext
        {
            get { return _initializingContext; }
            set
            {
                if (_initializingContext == value)
                    return;
                _initializingContext = value;
            }
        }
        [ThreadStatic]
        public static JSONNode initializeJSON;
        [ThreadStatic]
        public static List<PropertyModifier> initializePropertyModifiers;
        [ThreadStatic]
        public static bool initializeIsFallbackValues;

        /// <summary>
        /// Dispatched when a new instance was created and added to the Scene.
        /// </summary>
        public static Action<IProperty> AddedEvent;

        /// <summary>
        /// Dispatched when an instance has been disposed from to the Scene.
        /// </summary>
        public static Action<IProperty> RemovedEvent;

        private static InstanceManager _instance;
        /// <summary>
        /// Get a singleton of the manager.
        /// </summary>
        /// <param name="createIfMissing">If true a new instance will be created if one doesn't already exist. </param>
        /// <returns>An instance of the manager if the context allows.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static InstanceManager Instance(bool createIfMissing = true)
        {
            if (_instance == Disposable.NULL && createIfMissing)
                _instance = GetManagerComponent<InstanceManager>();
            return _instance;
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.TransformDouble"/>.
        /// </summary>
        public int transformsCount { get { return transforms.Count; } }
        private TransformDictionary transforms
        {
            get 
            {
                _transforms ??= new TransformDictionary();
                return _transforms;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.PersistentMonoBehaviour"/>.
        /// </summary>
        public int persistentMonoBehavioursCount { get { return persistentMonoBehaviours.Count; } }
        private PersistentMonoBehaviourDictionary persistentMonoBehaviours
        {
            get
            {
                _persistentMonoBehaviours ??= new PersistentMonoBehaviourDictionary();
                return _persistentMonoBehaviours;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.PersistentScriptableObject"/>.
        /// </summary>
        public int persistentScriptableObjectsCount { get { return persistentScriptableObjects.Count; } }
        private PersistentScriptableObjectDictionary persistentScriptableObjects
        {
            get
            {
                _persistentScriptableObjects ??= new PersistentScriptableObjectDictionary();
                return _persistentScriptableObjects;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.TerrainGridMeshObject"/>.
        /// </summary>
        public int terrainGridMeshObjectsCount { get { return terrainGridMeshObjects.Count; } }
        private TerrainGridMeshObjectDictionary terrainGridMeshObjects
        {
            get 
            {
                _terrainGridMeshObjects ??= new TerrainGridMeshObjectDictionary();
                return _terrainGridMeshObjects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.AstroObject"/>.
        /// </summary>
        public int astroObjectsCount { get { return astroObjects.Count; } }
        private AstroObjectDictionary astroObjects
        {
            get 
            {
                _astroObjects ??= new AstroObjectDictionary();
                return _astroObjects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.StarSystem"/>.
        /// </summary>
        public int starSystemsCount { get { return starSystems.Count; } }
        private List<StarSystem> starSystems
        {
            get 
            {
                _starSystems ??= new List<StarSystem>();
                return _starSystems; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.Star"/>.
        /// </summary>
        public int starsCount { get { return stars.Count; } }
        private List<Star> stars
        {
            get 
            {
                _stars ??= new List<Star>();
                return _stars; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.VisualObject"/>.
        /// </summary>
        public int visualObjectsCount { get { return visualObjects.Count; } }
        private VisualObjectDictionary visualObjects
        {
            get 
            {
                _visualObjects ??= new VisualObjectDictionary();
                return _visualObjects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.AnimatorBase"/>.
        /// </summary>
        public int animatorsCount { get { return animators.Count; } }
        private AnimatorDictionary animators
        {
            get 
            {
                _animators ??= new AnimatorDictionary();
                return _animators; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.ControllerBase"/>.
        /// </summary>
        public int controllersCount { get { return controllers.Count; } }
        private ControllerDictionary controllers
        {
            get 
            {
                _controllers ??= new ControllerDictionary();
                return _controllers; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.GeneratorBase"/>.
        /// </summary>
        public int generatorsCount { get { return generators.Count; } }
        private GeneratorDictionary generators
        {
            get 
            {
                _generators ??= new GeneratorDictionary();
                return _generators; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.ReferenceBase"/>.
        /// </summary>
        public int referencesCount { get { return references.Count; } }
        private ReferenceDictionary references
        {
            get 
            {
                _references ??= new ReferenceDictionary();
                return _references; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.EffectBase"/>.
        /// </summary>
        public int effectsCount { get { return effects.Count; } }
        private EffectDictionary effects
        {
            get 
            {
                _effects ??= new EffectDictionary();
                return _effects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.FallbackValues"/>.
        /// </summary>
        public int fallbackValuesCount { get { return fallbackValues.Count; } }
        private FallbackValuesDictionary fallbackValues
        {
            get 
            {
                _fallbackValues ??= new FallbackValuesDictionary();
                return _fallbackValues; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.DatasourceBase"/>.
        /// </summary>
        public int datasourcesCount { get { return datasources.Count; } }
        private DatasourceDictionary datasources
        {
            get 
            {
                _datasources ??= new DatasourceDictionary();
                return _datasources; 
            }
        }

        /// <summary>
        /// Returns a list of camera instance Id.
        /// </summary>
        public List<int> camerasInstanceIds
        {
            get 
            {
                _camerasInstanceIds ??= new List<int>();
                return _camerasInstanceIds; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.Camera"/>.
        /// </summary>
        public int camerasCount { get { return cameras.Count; } }
        private List<Camera> cameras
        {
            get 
            {
                _cameras ??= new List<Camera>();
                return _cameras; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DepictionEngine.ManagerBase"/>.
        /// </summary>
        public int managersCount { get { return managers.Count; } }
        private ManagerDictionary managers
        {
            get 
            {
                _managers ??= new ManagerDictionary();
                return _managers; 
            }
        }

        public void IterateOverInstances<T>(Func<T, bool> callback) where T : IProperty
        {
            IterateOverInstances(typeof(T), (property) => 
            {
                return callback((T)property);
            });
        }

        public void IterateOverInstances(Type type, Func<IProperty, bool> callback)
        {
            if (type == null || !typeof(IProperty).IsAssignableFrom(type))
            {
                Debug.LogWarning(type != null ? type.Name : "Null" + " is no a valid "+nameof(IProperty)+"!");
                return;
            }

            if (typeof(TransformBase).IsAssignableFrom(type))
                IterateOverEnumerable(type, transforms.Values, callback);

            if (typeof(IPersistent).IsAssignableFrom(type))
            {
                if (typeof(PersistentMonoBehaviour).IsAssignableFrom(type))
                    IterateOverEnumerable(type, persistentMonoBehaviours.Values, callback);
                if (typeof(PersistentScriptableObject).IsAssignableFrom(type))
                    IterateOverEnumerable(type, persistentScriptableObjects.Values, callback);
            }
            
            if (typeof(Script).IsAssignableFrom(type))
            {
                if (typeof(AnimatorBase).IsAssignableFrom(type))
                    IterateOverEnumerable(type, animators.Values, callback);
                if (typeof(ControllerBase).IsAssignableFrom(type))
                    IterateOverEnumerable(type, controllers.Values, callback);
                if (typeof(GeneratorBase).IsAssignableFrom(type))
                    IterateOverEnumerable(type, generators.Values, callback);
                if (typeof(ReferenceBase).IsAssignableFrom(type))
                    IterateOverEnumerable(type, references.Values, callback);
                if (typeof(EffectBase).IsAssignableFrom(type))
                    IterateOverEnumerable(type, effects.Values, callback);
                if (typeof(FallbackValues).IsAssignableFrom(type))
                    IterateOverEnumerable(type, fallbackValues.Values, callback);
                if (typeof(DatasourceBase).IsAssignableFrom(type))
                    IterateOverEnumerable(type, datasources.Values, callback);
            }
            
            if (typeof(ManagerBase).IsAssignableFrom(type))
                IterateOverEnumerable(type, managers.Values, callback);
        }

        private void IterateOverEnumerable(Type type, IEnumerable<IProperty> iProperties, Func<IProperty, bool> callback)
        {
            foreach (IProperty iProperty in iProperties)
            {
                if (!Disposable.IsDisposed(iProperty) && type.IsAssignableFrom(iProperty.GetType()) && !callback(iProperty))
                    return;
            }
        }

        /// <summary>
        /// Returns the <see cref="DepictionEngine.Star"/> if one exists.
        /// </summary>
        /// <returns></returns>
        public Star GetStar()
        {
            return stars.Count > 0 ? stars[0] : null;
        }

        public bool StarExists()
        {
            return GetStar() != Disposable.NULL;
        }

        public Camera GetCameraFromUnityCamera(UnityEngine.Camera unityCamera)
        {
            foreach (Camera camera in cameras)
            {
                if (camera != Disposable.NULL && camera.unityCamera == unityCamera)
                    return camera;
            }

            return null;
        }

        public IJson GetIJson(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                IJson iJson = instanceManager.GetPersistent(id);

                if (Disposable.IsDisposed(iJson))
                    iJson = instanceManager.GetTransform(id);
                if (Disposable.IsDisposed(iJson))
                    iJson = instanceManager.GetScript(id);
                if (Disposable.IsDisposed(iJson))
                    iJson = instanceManager.GetManager(id);

                return iJson;
            }

            return null;
        }

        public TransformBase GetTransform(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (transforms.TryGetValue(id, out TransformBase transform) && transform != Disposable.NULL)
                    return transform;

                if (persistentMonoBehaviours.TryGetValue(id, out PersistentMonoBehaviour persistentMonoBehaviour))
                {
                    Object objectBase = persistentMonoBehaviour as Object;
                    if (objectBase != Disposable.NULL && objectBase.transform != Disposable.NULL)
                        return objectBase.transform;
                }
            }

            return null;
        }

        public IPersistent GetPersistent(JSONNode json)
        {
            if (json != null && json[nameof(IPersistent.id)] != null)
            {
                if (SerializableGuid.TryParse(json[nameof(IPersistent.id)], out SerializableGuid id))
                    return GetPersistent(id);
            }

            return null;
        }

        public IPersistent GetPersistent(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (persistentMonoBehaviours.TryGetValue(id, out PersistentMonoBehaviour persistentMonoBehaviour) && persistentMonoBehaviour != Disposable.NULL)
                    return persistentMonoBehaviour;

                if (persistentScriptableObjects.TryGetValue(id, out PersistentScriptableObject persistentScriptableObject) && persistentScriptableObject != Disposable.NULL)
                    return persistentScriptableObject;
            }
            return null;
        }

        public Script GetScript(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (animators.TryGetValue(id, out AnimatorBase animator) && animator != Disposable.NULL)
                    return animator;

                if (controllers.TryGetValue(id, out ControllerBase controller) && controller != Disposable.NULL)
                    return controller;

                if (generators.TryGetValue(id, out GeneratorBase generator) && generator != Disposable.NULL)
                    return generator;

                if (references.TryGetValue(id, out ReferenceBase reference) && reference != Disposable.NULL)
                    return reference;

                if (effects.TryGetValue(id, out EffectBase effect) && effect != Disposable.NULL)
                    return effect;

                if (fallbackValues.TryGetValue(id, out FallbackValues fallbackValue) && fallbackValue != Disposable.NULL)
                    return fallbackValue;

                if (datasources.TryGetValue(id, out DatasourceBase datasource) && datasource != Disposable.NULL)
                    return datasource;
            }

            return null;
        }

        public T GetScript<T>(SerializableGuid id) where T : Script
        {
            return GetScript(typeof(T), id) as T;
        }

        public Script GetScript(Type type, SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (typeof(AnimatorBase).IsAssignableFrom(type))
                {
                    if (animators.TryGetValue(id, out AnimatorBase animator) && animator != Disposable.NULL)
                        return animator;
                }
                else if (typeof(ControllerBase).IsAssignableFrom(type))
                {
                    if (controllers.TryGetValue(id, out ControllerBase controller) && controller != Disposable.NULL)
                        return controller;
                }
                else if (typeof(GeneratorBase).IsAssignableFrom(type))
                {
                    if (generators.TryGetValue(id, out GeneratorBase generator) && generator != Disposable.NULL)
                        return generator;
                }
                else if (typeof(ReferenceBase).IsAssignableFrom(type))
                {
                    if (references.TryGetValue(id, out ReferenceBase reference) && reference != Disposable.NULL)
                        return reference;
                }
                else if (typeof(EffectBase).IsAssignableFrom(type))
                {
                    if (effects.TryGetValue(id, out EffectBase effect) && effect != Disposable.NULL)
                        return effect;
                }
                else if (typeof(FallbackValues).IsAssignableFrom(type))
                {
                    if (fallbackValues.TryGetValue(id, out FallbackValues fallbackValue) && fallbackValue != Disposable.NULL)
                        return fallbackValue;
                }
                else if (typeof(DatasourceBase).IsAssignableFrom(type))
                {
                    if (datasources.TryGetValue(id, out DatasourceBase datasource) && datasource != Disposable.NULL)
                        return datasource;
                }
            }

            return null;
        }

        public AstroObject GetAstroObject(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (astroObjects.TryGetValue(id, out AstroObject astroObject) && astroObject != Disposable.NULL)
                    return astroObject;
            }

            return null;
        }

        public ManagerBase GetManager(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (managers.TryGetValue(id, out ManagerBase manager) && manager != Disposable.NULL)
                    return manager;
            }

            return null;
        }

        public AnimatorBase GetAnimator(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (animators.TryGetValue(id, out AnimatorBase animator) && animator != Disposable.NULL)
                    return animator;
            }

            return null;
        }

        public ControllerBase GetController(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (controllers.TryGetValue(id, out ControllerBase controller) && controller != Disposable.NULL)
                    return controller;
            }

            return null;
        }

        public GeneratorBase GetGenerator(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (generators.TryGetValue(id, out GeneratorBase generator) && generator != Disposable.NULL)
                    return generator;
            }

            return null;
        }

        public ReferenceBase GetReference(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {            
                if (references.TryGetValue(id, out ReferenceBase reference) && reference != Disposable.NULL)
                    return reference;
            }

            return null;
        }

        public EffectBase GetEffect(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (effects.TryGetValue(id, out EffectBase effect) && effect != Disposable.NULL)
                    return effect;
            }

            return null;
        }

        public FallbackValues GetFallbackValues(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (fallbackValues.TryGetValue(id, out FallbackValues fallbackValue) && fallbackValue != Disposable.NULL)
                    return fallbackValue;
            }

            return null;
        }

        public DatasourceBase GetDatasource(SerializableGuid id)
        {
            if (id != SerializableGuid.Empty)
            {
                if (datasources.TryGetValue(id, out DatasourceBase datasource) && datasource != Disposable.NULL)
                    return datasource;
            }

            return null;
        }

        public bool Add(IProperty property)
        {
            string errorMsg = null;

            SerializableGuid id = property.id;

            bool added = false;
            if (property is TransformBase)
            {
                if (!transforms.ContainsKey(id))
                {
                    added = true;
                    transforms[id] = property as TransformBase;
                }
            }
            else if (property is IPersistent)
            {
                if (property is PersistentMonoBehaviour)
                {
                    if (!persistentMonoBehaviours.ContainsKey(id))
                    {
                        if (property is VisualObject)
                        {
                            if (!visualObjects.ContainsKey(id))
                            {
                                visualObjects[id] = property as VisualObject;
                                if (property is TerrainGridMeshObject)
                                    terrainGridMeshObjects[id] = property as TerrainGridMeshObject;
                                added = true;
                            }
                        }
                        else if (property is AstroObject)
                        {
                            if (!astroObjects.ContainsKey(id))
                            {
                                if (property is Star)
                                {
                                    Star star = property as Star;
                                    if (!stars.Contains(star))
                                    {
                                        if (stars.Count == 0)
                                        {
                                            stars.Add(star);
                                            added = true;
                                        }
                                        else
                                            errorMsg = GetMutlipleInstanceErrorMsg(property);
                                    }
                                }
                                else
                                    added = true;

                                if (added)
                                    astroObjects[id] = property as AstroObject;
                            }
                        }
                        else if (property is StarSystem)
                        {
                            StarSystem starSystem = property as StarSystem;
                            if (!starSystems.Contains(starSystem))
                            {
                                starSystems.Add(starSystem);
                                added = true;
                            }
                        }
                        else if (property is Camera)
                        {
                            Camera camera = property as Camera;
                            if (!cameras.Contains(camera))
                            {
                                cameras.Add(camera);
                                camerasInstanceIds.Add(camera.GetCameraInstanceID());
                                added = true;
                            }
                        }
                        else
                            added = true;

                        if (added)
                            persistentMonoBehaviours[id] = property as PersistentMonoBehaviour;
                    }
                }
                else if (property is PersistentScriptableObject)
                {
                    if (!persistentScriptableObjects.ContainsKey(id))
                    {
                        persistentScriptableObjects[id] = property as PersistentScriptableObject;
                        added = true;
                    }
                }
            }
            else if (property is Script)
            {
                if (property is AnimatorBase)
                {
                    if (!animators.ContainsKey(id))
                    {
                        added = true;
                        animators[id] = property as AnimatorBase;
                    }
                }
                else if (property is ControllerBase)
                {
                    if (!controllers.ContainsKey(id))
                    {
                        added = true;
                        controllers[id] = property as ControllerBase;
                    }
                }
                else if (property is GeneratorBase)
                {
                    if (!generators.ContainsKey(id))
                    {
                        added = true;
                        generators[id] = property as GeneratorBase;
                    }
                }
                else if (property is ReferenceBase)
                {
                    if (!references.ContainsKey(id))
                    {
                        added = true;
                        references[id] = property as ReferenceBase;
                    }
                }
                else if (property is EffectBase)
                {
                    if (!effects.ContainsKey(id))
                    {
                        added = true;
                        effects[id] = property as EffectBase;
                    }
                }
                else if (property is FallbackValues)
                {
                    if (!fallbackValues.ContainsKey(id))
                    {
                        added = true;
                        fallbackValues[id] = property as FallbackValues;
                    }
                }
                else if (property is DatasourceBase)
                {
                    if (!datasources.ContainsKey(id))
                    {
                        added = true;
                        datasources[id] = property as DatasourceBase;
                    }
                }
            }
            else if (property is ManagerBase)
            {
                if (!managers.ContainsKey(id))
                {
                    bool managerExist = false;

                    Type managerType = property.GetType();
                    foreach (ManagerBase manager in managers.Values)
                    {
                        if (manager.GetType() == managerType)
                        {
                            managerExist = true;
                            errorMsg = "You can only have one '"+ managerType.Name+ "' instance per Scene.";
                            break;
                        }
                    }

                    if (!managerExist)
                    {
                        added = true;
                        managers[id] = property as ManagerBase;
                    }
                }
            }

            if (added)
            {
                AddedEvent?.Invoke(property);
            }
            else
            {
#if UNITY_EDITOR
                if (property is Editor.SceneCamera)
                    property = null;
#endif
                if (property is IPersistent || property is ManagerBase)
                {
                    UnityEngine.Object unityObject = property as UnityEngine.Object;
                    if (property is PersistentMonoBehaviour)
                        unityObject = (property as PersistentMonoBehaviour).gameObject;

                    if (string.IsNullOrEmpty(errorMsg))
                    {
                        //Problem: Sometimes performing Undo will recreate an Object with an already existing id
                        //Fix: If the Object already exsists we Dispose it
                        errorMsg = "'" + property + "' will be destroyed: ID already exist";
                    }

                    Debug.LogError(errorMsg);

                    property.ResetId();
                    DisposeManager.Dispose(unityObject, DisposeContext.Programmatically_Destroy);
                }
            }

            return added;
        }

        private static string GetMutlipleInstanceErrorMsg(IProperty property)
        {
            return GetMultipleInstanceErrorMsg(property.GetType().Name);
        }

        public static string GetMultipleInstanceErrorMsg(string instanceName)
        {
            return "Multiple " + instanceName + " is not supported!";
        }

        public bool Remove(SerializableGuid id, IProperty property)
        {
            bool removed = false;
            if (property is TransformBase)
            {
                if (transforms.Remove(id))
                    removed = true;
            }
            else if (property is IPersistent)
            {
                if (property is PersistentMonoBehaviour)
                {
                    if (persistentMonoBehaviours.Remove(id))
                    {
                        if (property is VisualObject)
                        {
                            visualObjects.Remove(id);
                            if (property is TerrainGridMeshObject)
                                terrainGridMeshObjects.Remove(id);
                        }
                        else if (property is AstroObject)
                        {
                            if (astroObjects.Remove(id))
                            {
                                if (property is Star)
                                    stars.Remove(property as Star);
                            }
                        }
                        else if (property is StarSystem)
                        {
                            StarSystem starSystem = property as StarSystem;
                            starSystems.Remove(starSystem);
                        }
                        else if (property is Camera)
                        {
                            Camera camera = property as Camera;
                            int index = cameras.IndexOf(camera);
                            cameras.RemoveAt(index);
                            camerasInstanceIds.RemoveAt(index);
                        }

                        removed = true;
                    }
                }
                else if (property is PersistentScriptableObject)
                {
                    if (persistentScriptableObjects.Remove(id))
                        removed = true;
                }
            }
            else if (property is Script) 
            {
                if (property is AnimatorBase)
                {
                    if (animators.Remove(id))
                        removed = true;
                }
                else if (property is ControllerBase)
                {
                    if (controllers.Remove(id))
                        removed = true;
                }
                else if (property is GeneratorBase)
                {
                    if (generators.Remove(id))
                        removed = true;
                }
                else if (property is ReferenceBase)
                {
                    if (references.Remove(id))
                        removed = true;
                }
                else if (property is EffectBase)
                {
                    if (effects.Remove(id))
                        removed = true;
                }
                else if (property is FallbackValues)
                {
                    if (fallbackValues.Remove(id))
                        removed = true;
                }
                else if (property is DatasourceBase)
                {
                    if (datasources.Remove(id))
                        removed = true;
                }
            }
            else if (property is ManagerBase)
            {
                if (managers.Remove(id))
                    removed = true;
            }

            if (removed)
                RemovedEvent?.Invoke(property);
            
            return removed;
        }

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <typeparam name="T">The type of instance to create.</typeparam>
        /// <param name="parent">The parent <see cref="UnityEngine.Transform"/> under which the instance should be created.</param>
        /// <param name="json">Values to initialize the instance with.</param>
        /// <param name="propertyModifiers">A list of <see cref="DepictionEngine.PropertyModifier"/>'s used to modify properties that cannot be initialized through json.</param>
        /// <param name="initializingContext"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <param name="isFallbackValues">If true a <see cref="DepictionEngine.FallbackValues"/> will be created and the instance type will be passed to the <see cref="DepictionEngine.FallbackValues.SetFallbackJsonFromType"/> function.</param>
        /// <returns>The newly created instance.</returns>
        public T CreateInstance<T>(Transform parent = null, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, InitializationContext initializingContext = InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false, bool isFallbackValues = false) where T : IDisposable
        {
            return (T)CreateInstance(typeof(T), parent, json, propertyModifiers, initializingContext, setParentAndAlign, moveToView, isFallbackValues);
        }

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="type">The type of instance to create.</param>
        /// <param name="parent">The parent <see cref="UnityEngine.Transform"/> under which the instance should be created.</param>
        /// <param name="json">Values to initialize the instance with.</param>
        /// <param name="propertyModifiers">A list of <see cref="DepictionEngine.PropertyModifier"/>'s used to modify properties that cannot be initialized through json.</param>
        /// <param name="initializingContext"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <param name="isFallbackValues">If true a <see cref="DepictionEngine.FallbackValues"/> will be created and the instance type will be passed to the <see cref="DepictionEngine.FallbackValues.SetFallbackJsonFromType"/> function.</param>
        /// <returns>The newly created instance.</returns>
        public IDisposable CreateInstance(Type type, Transform parent = null, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, InitializationContext initializingContext = InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false, bool isFallbackValues = false)
        {
            if (type == null)
                return null;

            if (initializingContext == InitializationContext.Existing)
                initializingContext = InitializationContext.Programmatically;

            bool isMonoBehaviourType = type.IsSubclassOf(typeof(MonoBehaviour));

            if (json != null)
            {
                if (json is JSONString)
                {
                    string name = json;
                    json = new JSONObject
                    {
                        [nameof(IPersistent.name)] = name
                    };
                }
                if (json[nameof(Object.transform)] != null && !string.IsNullOrEmpty(json[nameof(Object.transform)][nameof(TransformBase.parent)]))
                {
                    //If Parent does not exist do not create the Object
                    TransformBase parentTransform = GetTransform(SerializableGuid.Parse(json[nameof(Object.transform)][nameof(TransformBase.parent)]));
                    json[nameof(Object.transform)].Remove(nameof(TransformBase.parent));
                    if (parentTransform != Disposable.NULL)
                        parent = parentTransform.transform;
                }
            }

            IDisposable disposable = null;

            if (initializingContext != InitializationContext.Editor && initializingContext != InitializationContext.Editor_Duplicate)
            {
                PoolManager poolManager = PoolManager.Instance();
                if (poolManager != Disposable.NULL)
                    disposable = poolManager.GetFromPool(type);
            }

            if (disposable is null)
            {
                if (isMonoBehaviourType)
                {
                    GameObject go = new ();
                    InitializeGameObject(go, parent, setParentAndAlign, moveToView);

#if UNITY_EDITOR
                    Editor.UndoManager.RegisterCreatedObjectUndo(go, initializingContext);
#endif

                    disposable = go.AddSafeComponent(type, initializingContext, json, propertyModifiers, isFallbackValues) as IDisposable;
                 }
                else if (typeof(IDisposable).IsAssignableFrom(type))
                {
                    InitializingContext(() =>
                    {
                        if (typeof(IScriptableBehaviour).IsAssignableFrom(type))
                            disposable = ScriptableObject.CreateInstance(type) as IScriptableBehaviour;
                        else
                            disposable = Activator.CreateInstance(type) as IDisposable;

#if UNITY_EDITOR
                        Editor.UndoManager.RegisterCreatedObjectUndo(disposable as UnityEngine.Object, initializingContext);
#endif

                        disposable.Initialize();

                    }, initializingContext, json, propertyModifiers, isFallbackValues);
                }
            }
            else if (disposable is MonoBehaviourDisposable)
            {
                GameObject go = (disposable as MonoBehaviourDisposable).gameObject;
                InitializeGameObject(go, parent, setParentAndAlign, moveToView);

                MonoBehaviourDisposable[] components = go.GetComponents<MonoBehaviourDisposable>();

                foreach (MonoBehaviourDisposable component in components)
                    component.InhibitExplicitOnEnableDisable();

                go.SetActive(true);
                disposable = go.GetSafeComponent(type, initializingContext, json, propertyModifiers, isFallbackValues) as IDisposable;

                foreach (MonoBehaviourDisposable component in components)
                    component.UninhibitExplicitOnEnableDisable();

                foreach (MonoBehaviourDisposable component in components)
                {
                    if (!Object.ReferenceEquals(disposable, component))
                        component.ExplicitOnEnable();
                }
            }
            else
                Initialize(disposable, initializingContext, json, propertyModifiers, isFallbackValues);

            if (!disposable.IsDisposing())
            {
                if (disposable is IScriptableBehaviour)
                    (disposable as IScriptableBehaviour).ExplicitOnEnable();
            }

            static void InitializeGameObject(GameObject gameObject, Transform newParent, bool setParentAndAlign, bool moveToView)
            {
                Transform goTransform = gameObject.transform;
#if UNITY_EDITOR
                if (setParentAndAlign)
                    UnityEditor.GameObjectUtility.SetParentAndAlign(gameObject, newParent != null ? newParent.gameObject : null);
#endif
                if (goTransform.parent != newParent)
                    goTransform.SetParent(newParent, false);

#if UNITY_EDITOR
                if (moveToView && goTransform.parent == null && UnityEditor.SceneView.lastActiveSceneView != null)
                    UnityEditor.SceneView.lastActiveSceneView.MoveToView(goTransform);
#endif
            }

            return disposable;
        }

        /// <summary>
        /// Duplicate an existing object.
        /// </summary>
        /// <typeparam name="T">The type of the UnityEngine.Object to duplicate.</typeparam>
        /// <param name="objectToDuplicate">The UnityEngine.Object instance to duplicate.</param>
        /// <param name="initializingContext"></param>
        /// <returns>The duplicated object</returns>
        public static T Duplicate<T>(T objectToDuplicate, InitializationContext initializingContext = InitializationContext.Programmatically) where T : UnityEngine.Object
        {
            T duplicatedObject = null;

            InitializingContext(() =>
            {
                duplicatedObject = !DisposeManager.IsNullOrDisposing(objectToDuplicate) ? Object.Instantiate(objectToDuplicate) : null;

#if UNITY_EDITOR
                Editor.UndoManager.RegisterCreatedObjectUndo(duplicatedObject, initializingContext);
#endif
                if (duplicatedObject is IDisposable)
                    (duplicatedObject as IDisposable).Initialize();
            }, initializingContext);

            return duplicatedObject;
        }

        /// <summary>
        /// Initialize an object.
        /// </summary>
        /// <param name="disposable">The object to initialize.</param>
        /// <param name="initializingContext"></param>
        /// <param name="json">Values to initialize the instance with.</param>
        /// <param name="propertyModifiers">A list of <see cref="DepictionEngine.PropertyModifier"/>'s used to modify properties that cannot be initialized through json.</param>
        /// <param name="isFallbackValues">If true a <see cref="DepictionEngine.FallbackValues"/> will be created and the instance type will be passed to the <see cref="DepictionEngine.FallbackValues.SetFallbackJsonFromType"/> function.</param>
        /// <returns>The object that was initialized.</returns>
        public static IDisposable Initialize(IDisposable disposable, InitializationContext initializingContext = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            InitializingContext(() => { disposable.Initialize(); }, initializingContext, json, propertyModifiers, isFallbackValues);

            return disposable;
        }

        public static void InitializingContext(Action callback, InitializationContext initializingContext, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            InitializationContext lastinitializingContext = InstanceManager.initializingContext;
            JSONNode lastInitializeJSON = InstanceManager.initializeJSON;
            List<PropertyModifier> lastInitializePropertyModifers = InstanceManager.initializePropertyModifiers;
            bool lastIsFallbackValues = InstanceManager.initializeIsFallbackValues;

            InstanceManager.initializingContext = initializingContext;
            InstanceManager.initializeJSON = json;
            InstanceManager.initializePropertyModifiers = propertyModifiers;
            InstanceManager.initializeIsFallbackValues = isFallbackValues;

            callback();
            
            InstanceManager.initializingContext = lastinitializingContext;
            InstanceManager.initializeJSON = lastInitializeJSON;
            InstanceManager.initializePropertyModifiers = lastInitializePropertyModifers;
            InstanceManager.initializeIsFallbackValues = lastIsFallbackValues;
        }
    }
}
