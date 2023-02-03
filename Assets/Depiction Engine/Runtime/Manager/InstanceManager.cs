﻿// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// Singleton managing instances.
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Manager/" + nameof(InstanceManager))]
    [RequireComponent(typeof(SceneManager))]
    public class InstanceManager : ManagerBase
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
        /// <b><see cref="Existing_Or_Editor_UndoRedo"/>:</b> <br/>
        /// The initialization was triggered by a loading scene or was triggered in the editor as a result of an undo or redo action. <br/><br/>
        /// <b><see cref="Editor_Duplicate"/>:</b> <br/>
        /// The initialization was triggered in the editor and the object is a duplicate. Duplication can come from a copy paste / duplicate menu item or draging droping of a component. <br/><br/>
        /// <b><see cref="Reset"/>:</b> <br/>
        /// The object properties are reseted to their default values.
        /// </summary> 
        public enum InitializationContext
        {
            Unknown,
            Programmatically,
            Programmatically_Duplicate,
            Editor,
            Existing_Or_Editor_UndoRedo,
            Editor_Duplicate,
            Reset
        };

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
        private static InitializationContext _initializingState = InitializationContext.Editor;
        public static InitializationContext initializingState
        {
            get { return _initializingState; }
            set
            {
                if (_initializingState == value)
                    return;
                _initializingState = value;
            }
        }
        [ThreadStatic]
        public static JSONNode initializeJSON;
        [ThreadStatic]
        public static List<PropertyModifier> initializePropertyModifiers;
        [ThreadStatic]
        public static bool initializeIsFallbackValues;
        [ThreadStatic]
        public static bool inhibitExplicitAwake = false;

        public static Action<IProperty> AddedEvent;

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
        /// Returns the number of <see cref="TransformDouble"/>.
        /// </summary>
        public int transformsCount { get { return transforms.Count; } }
        private TransformDictionary transforms
        {
            get 
            {
                if (_transforms == null)
                    _transforms = new TransformDictionary();
                return _transforms;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="PersistentMonoBehaviour"/>.
        /// </summary>
        public int persistentMonoBehavioursCount { get { return persistentMonoBehaviours.Count; } }
        private PersistentMonoBehaviourDictionary persistentMonoBehaviours
        {
            get
            {
                if (_persistentMonoBehaviours == null)
                    _persistentMonoBehaviours = new PersistentMonoBehaviourDictionary();
                return _persistentMonoBehaviours;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="PersistentScriptableObject"/>.
        /// </summary>
        public int persistentScriptableObjectsCount { get { return persistentScriptableObjects.Count; } }
        private PersistentScriptableObjectDictionary persistentScriptableObjects
        {
            get
            {
                if (_persistentScriptableObjects == null)
                    _persistentScriptableObjects = new PersistentScriptableObjectDictionary();
                return _persistentScriptableObjects;
            }
        }

        /// <summary>
        /// Returns the number of <see cref="TerrainGridMeshObject"/>.
        /// </summary>
        public int terrainGridMeshObjectsCount { get { return terrainGridMeshObjects.Count; } }
        private TerrainGridMeshObjectDictionary terrainGridMeshObjects
        {
            get 
            {
                if (_terrainGridMeshObjects == null)
                    _terrainGridMeshObjects = new TerrainGridMeshObjectDictionary();
                return _terrainGridMeshObjects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="AstroObject"/>.
        /// </summary>
        public int astroObjectsCount { get { return astroObjects.Count; } }
        private AstroObjectDictionary astroObjects
        {
            get 
            {
                if (_astroObjects == null)
                    _astroObjects = new AstroObjectDictionary();
                return _astroObjects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="StarSystem"/>.
        /// </summary>
        public int starSystemsCount { get { return starSystems.Count; } }
        private List<StarSystem> starSystems
        {
            get 
            {
                if (_starSystems == null)
                    _starSystems = new List<StarSystem>();
                return _starSystems; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="Star"/>.
        /// </summary>
        public int starsCount { get { return stars.Count; } }
        private List<Star> stars
        {
            get 
            {
                if (_stars == null)
                    _stars = new List<Star>();
                return _stars; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="VisualObject"/>.
        /// </summary>
        public int visualObjectsCount { get { return visualObjects.Count; } }
        private VisualObjectDictionary visualObjects
        {
            get 
            {
                if (_visualObjects == null)
                    _visualObjects = new VisualObjectDictionary();
                return _visualObjects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="AnimatorBase"/>.
        /// </summary>
        public int animatorsCount { get { return animators.Count; } }
        private AnimatorDictionary animators
        {
            get 
            {
                if (_animators == null)
                    _animators = new AnimatorDictionary();
                return _animators; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="ControllerBase"/>.
        /// </summary>
        public int controllersCount { get { return controllers.Count; } }
        private ControllerDictionary controllers
        {
            get 
            {
                if (_controllers == null)
                    _controllers = new ControllerDictionary();
                return _controllers; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="GeneratorBase"/>.
        /// </summary>
        public int generatorsCount { get { return generators.Count; } }
        private GeneratorDictionary generators
        {
            get 
            {
                if (_generators == null)
                    _generators = new GeneratorDictionary();
                return _generators; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="ReferenceBase"/>.
        /// </summary>
        public int referencesCount { get { return references.Count; } }
        private ReferenceDictionary references
        {
            get 
            {
                if (_references == null)
                    _references = new ReferenceDictionary();
                return _references; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="EffectBase"/>.
        /// </summary>
        public int effectsCount { get { return effects.Count; } }
        private EffectDictionary effects
        {
            get 
            {
                if (_effects == null)
                    _effects = new EffectDictionary();
                return _effects; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="FallbackValues"/>.
        /// </summary>
        public int fallbackValuesCount { get { return fallbackValues.Count; } }
        private FallbackValuesDictionary fallbackValues
        {
            get 
            {
                if (_fallbackValues == null)
                    _fallbackValues = new FallbackValuesDictionary();
                return _fallbackValues; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="DatasourceBase"/>.
        /// </summary>
        public int datasourcesCount { get { return datasources.Count; } }
        private DatasourceDictionary datasources
        {
            get 
            {
                if (_datasources == null)
                    _datasources = new DatasourceDictionary();
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
                if (_camerasInstanceIds == null)
                    _camerasInstanceIds = new List<int>();
                return _camerasInstanceIds; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="Camera"/>.
        /// </summary>
        public int camerasCount { get { return cameras.Count; } }
        private List<Camera> cameras
        {
            get 
            {
                if (_cameras == null)
                    _cameras = new List<Camera>();
                return _cameras; 
            }
        }

        /// <summary>
        /// Returns the number of <see cref="ManagerBase"/>.
        /// </summary>
        public int managersCount { get { return managers.Count; } }
        private ManagerDictionary managers
        {
            get 
            {
                if (_managers == null)
                    _managers = new ManagerDictionary();
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
        /// Returns the <see cref="Star"/> if one exists.
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
                                camerasInstanceIds.Add(camera.GetInstanceID());
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
                    added = true;
                    managers[id] = property as ManagerBase;
                }
            }

            if (added)
            {
                if (AddedEvent != null)
                    AddedEvent(property);
            }
            else
            {
#if UNITY_EDITOR
                if (property is Editor.SceneCamera)
                    property = null;
#endif
                if (property is IPersistent)
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
                    DisposeManager.Dispose(unityObject, DisposeManager.DestroyContext.Programmatically);
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
            {
                if (RemovedEvent != null)
                    RemovedEvent(property);
            }

            return removed;
        }

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="initializingState"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <param name="isFallbackValues">If true a <see cref="FallbackValues"/> will be created and the instance type will be passed to the <see cref="FallbackValues.SetFallbackJsonFromType"/> function.</param>
        /// <returns></returns>
        public T CreateInstance<T>(Transform parent = null, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, InitializationContext initializingState = InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false, bool isFallbackValues = false) where T : IDisposable
        {
            return (T)CreateInstance(typeof(T), parent, json, propertyModifiers, initializingState, setParentAndAlign, moveToView, isFallbackValues);
        }

        /// <summary>
        /// Create an instance.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parent"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="initializingState"></param>
        /// <param name="setParentAndAlign">Sets the parent and gives the child the same layer and position (Editor Only).</param>
        /// <param name="moveToView">Instantiates the GameObject at the scene pivot  (Editor Only).</param>
        /// <param name="isFallbackValues">If true a <see cref="FallbackValues"/> will be created and the instance type will be passed to the <see cref="FallbackValues.SetFallbackJsonFromType"/> function.</param>
        /// <returns></returns>
        public IDisposable CreateInstance(Type type, Transform parent = null, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, InitializationContext initializingState = InitializationContext.Programmatically, bool setParentAndAlign = false, bool moveToView = false, bool isFallbackValues = false)
        {
            if (type == null)
                return null;

            if (initializingState == InitializationContext.Existing_Or_Editor_UndoRedo)
                initializingState = InitializationContext.Editor;

            bool isMonoBehaviourType = type.IsSubclassOf(typeof(MonoBehaviour));
#if UNITY_EDITOR
            if (isMonoBehaviourType)
                Editor.UndoManager.PreGetMonoBehaviourInstance();
#endif

            if (json != null)
            {
                if (json is JSONString)
                {
                    string name = json;
                    json = new JSONObject();
                    json[nameof(IPersistent.name)] = name;
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

            if (initializingState != InitializationContext.Editor && initializingState != InitializationContext.Editor_Duplicate && initializingState != InitializationContext.Existing_Or_Editor_UndoRedo)
            {
                PoolManager poolManager = PoolManager.Instance();
                if (poolManager != Disposable.NULL)
                    disposable = poolManager.GetFromPool(type);
            }

            if (Object.ReferenceEquals(disposable, null))
            {
                if (isMonoBehaviourType)
                {
                    GameObject go = new GameObject();
                    InitializeGameObject(go, parent, setParentAndAlign, moveToView);

#if UNITY_EDITOR
                    Editor.UndoManager.RegisterCreatedObjectUndo(go, initializingState);
#endif

                    disposable = go.AddSafeComponent(type, initializingState, json, propertyModifiers, isFallbackValues) as IDisposable;
                }
                else if (typeof(IDisposable).IsAssignableFrom(type))
                {
                    InitializingState(() =>
                    {
                        if (typeof(IScriptableBehaviour).IsAssignableFrom(type))
                            InhibitExplicitAwake(() => { disposable = ScriptableObject.CreateInstance(type) as IScriptableBehaviour; }, true);
                        else
                            disposable = Activator.CreateInstance(type) as IDisposable;

#if UNITY_EDITOR
                        Editor.UndoManager.RegisterCreatedObjectUndo(disposable as UnityEngine.Object, initializingState);
#endif

                        if (disposable is IScriptableBehaviour)
                            (disposable as IScriptableBehaviour).ExplicitAwake();
                        disposable.Initialize();

                    }, initializingState, json, propertyModifiers, isFallbackValues);
                }
            }
            else if (disposable is MonoBehaviourBase)
            {
                GameObject go = (disposable as MonoBehaviourBase).gameObject;
                InitializeGameObject(go, parent, setParentAndAlign, moveToView);

                MonoBehaviourBase[] components = go.GetComponents<MonoBehaviourBase>();

                foreach (MonoBehaviourBase component in components)
                    component.InhibitExplicitOnEnableDisable();

                go.SetActive(true);
                disposable = go.GetSafeComponent(type, initializingState, json, propertyModifiers, isFallbackValues) as IDisposable;

                foreach (MonoBehaviourBase component in components)
                    component.UninhibitExplicitOnEnableDisable();

                foreach (MonoBehaviourBase component in components)
                {
                    if (!Object.ReferenceEquals(disposable, component))
                        component.ExplicitOnEnable();
                }
            }
            else
                Initialize(disposable, initializingState, json, propertyModifiers, isFallbackValues);

            if (!disposable.IsDisposing())
            {
                if (disposable is IScriptableBehaviour)
                    (disposable as IScriptableBehaviour).ExplicitOnEnable();
            }

#if UNITY_EDITOR
            if (isMonoBehaviourType)
                Editor.UndoManager.PostGetMonoBehaviourInstance();
#endif

            void InitializeGameObject(GameObject gameObject, Transform newParent, bool setParentAndAlign, bool moveToView)
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
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToDuplicate"></param>
        /// <param name="initializationState"></param>
        /// <returns>The duplicated object</returns>
        public static T Duplicate<T>(T objectToDuplicate, InitializationContext initializationState) where T : UnityEngine.Object
        {
            T duplicatedObject = null;

            InitializingState(() =>
            {
                duplicatedObject = !DisposeManager.IsNullOrDisposing(objectToDuplicate) ? Object.Instantiate(objectToDuplicate) : null;

#if UNITY_EDITOR
                Editor.UndoManager.RegisterCreatedObjectUndo(duplicatedObject, initializationState);
#endif
                if (duplicatedObject is IDisposable)
                {
                    if (duplicatedObject is IScriptableBehaviour)
                        (duplicatedObject as IScriptableBehaviour).ExplicitAwake();
                    (duplicatedObject as IDisposable).Initialize();
                }
            }, initializationState);

            return duplicatedObject;
        }

        /// <summary>
        /// Initialize an object.
        /// </summary>
        /// <param name="disposable"></param>
        /// <param name="initializationState"></param>
        /// <param name="json"></param>
        /// <param name="propertyModifiers"></param>
        /// <param name="isFallbackValues">If true a <see cref="FallbackValues"/> will be created and the instance type will be passed to the <see cref="FallbackValues.SetFallbackJsonFromType"/> function.</param>
        /// <returns></returns>
        public static IDisposable Initialize(IDisposable disposable, InitializationContext initializationState = InitializationContext.Programmatically, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            InitializingState(() =>
            {
                if (disposable is IScriptableBehaviour)
                    (disposable as IScriptableBehaviour).ExplicitAwake();
                disposable.Initialize();
            }, initializationState, json, propertyModifiers, isFallbackValues);

            return disposable;
        }

        public static void InitializingState(Action callback, InitializationContext initializingState, JSONNode json = null, List<PropertyModifier> propertyModifiers = null, bool isFallbackValues = false)
        {
            InitializationContext lastInitializingState = InstanceManager.initializingState;
            JSONNode lastInitializeJSON = InstanceManager.initializeJSON;
            List<PropertyModifier> lastInitializePropertyModifers = InstanceManager.initializePropertyModifiers;
            bool lastIsFallbackValues = InstanceManager.initializeIsFallbackValues;

            InstanceManager.initializingState = initializingState;
            InstanceManager.initializeJSON = json;
            InstanceManager.initializePropertyModifiers = propertyModifiers;
            InstanceManager.initializeIsFallbackValues = isFallbackValues;

            callback();
            
            InstanceManager.initializingState = lastInitializingState;
            InstanceManager.initializeJSON = lastInitializeJSON;
            InstanceManager.initializePropertyModifiers = lastInitializePropertyModifers;
            InstanceManager.initializeIsFallbackValues = lastIsFallbackValues;
        }

        private static void InhibitExplicitAwake(Action callback, bool inhibitExplicitAwake)
        {
            bool lastInhibitExplicitAwake = InstanceManager.inhibitExplicitAwake;
            
            InstanceManager.inhibitExplicitAwake = inhibitExplicitAwake;
            
            callback();
            
            InstanceManager.inhibitExplicitAwake = lastInhibitExplicitAwake;
        }
    }
}