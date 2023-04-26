// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DepictionEngine
{
    public class AutoGenerateVisualObject : VisualObject
    {
        [BeginFoldout("Visual")]
        [SerializeField, Tooltip("When enabled the visuals will be instantiated automatically when the object is created or when the dependencies change.")]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowAutoInstantiate))]
#endif
        private bool _autoInstantiate;
        [SerializeField, Tooltip("When enabled the child 'Visual' will not be saved as part of the Scene."), EndFoldout]
#if UNITY_EDITOR
        [ConditionalShow(nameof(GetShowDontSaveVisualsToScene))]
#endif
        private bool _dontSaveVisualsToScene;

        [BeginFoldout("Popup")]
        [SerializeField, Tooltip("How the object should fade in. Alpha Popup requires an instanced Material to work.")]
        private PopupType _popupType;
        [SerializeField, Tooltip("The duration of the fade in animation."), EndFoldout]
        private float _popupDuration;

        [SerializeField, HideInInspector]
        private VisualObjectVisualDirtyFlags _meshRendererVisualDirtyFlags;

        private Tween _popupTween;
        private bool _popup;
        private float _popupT;

        private bool _visualsChanged;

#if UNITY_EDITOR
        protected override bool GetShowUpdateAllChildMeshRenderers()
        {
            return false;
        }

        protected virtual bool GetShowAutoInstantiate()
        {
            return false;
        }

        protected virtual bool GetShowDontSaveVisualsToScene()
        {
            return true;
        }
#endif

        public override void Recycle()
        {
            base.Recycle();

            if (_meshRendererVisualDirtyFlags != null)
                _meshRendererVisualDirtyFlags.Recycle();

            _popup = default;
            _popupT = default;
        }

        protected override void InitializeFields(InitializationContext initializingContext)
        {
            base.InitializeFields(initializingContext);

            if (initializingContext == InitializationContext.Existing)
            {
#if UNITY_EDITOR
                RecoverMeshRendererVisualDirtyFlags();
#endif
            }

            bool duplicating = initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate;

            if (duplicating)
            {
                meshRendererVisualDirtyFlags = InstanceManager.Duplicate(meshRendererVisualDirtyFlags, initializingContext);
                meshRendererVisualDirtyFlags.Recreate();
            }
            else if (meshRendererVisualDirtyFlags == null)
            {
                meshRendererVisualDirtyFlags = ScriptableObject.CreateInstance(GetMeshRendererVisualDirtyFlagType()) as VisualObjectVisualDirtyFlags;
#if UNITY_EDITOR
                Editor.UndoManager.QueueRegisterCreatedObjectUndo(meshRendererVisualDirtyFlags, initializingContext);
#endif
            }

            if (!GetMeshRenderersInitialized())
                SetMeshRendererVisualsAllDirty();
        }

        public void SetMeshRendererVisualsAllDirty()
        {
            if (meshRendererVisualDirtyFlags != null)
                meshRendererVisualDirtyFlags.AllDirty();
        }

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            //Dont Popup if the object was duplicated or already exists
            popup = initializingContext == InitializationContext.Editor || initializingContext == InitializationContext.Programmatically;

            if (!popup)
                DetectCompromisedPopup();
        }

#if UNITY_EDITOR
        public override bool AfterAssemblyReload()
        {
            if (base.AfterAssemblyReload())
            {
                DetectCompromisedPopup();

                return true;
            }
            return false;
        }
#endif

        private void DetectCompromisedPopup()
        {
            if (popupT != 0.0f && popupT != 1.0f)
                popup = true;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => autoInstantiate = value, true, initializingContext);
            InitValue(value => dontSaveVisualsToScene = value, GetDefaultDontSaveVisualsToScene(), initializingContext);
            InitValue(value => popupType = value, GetDefaultPopupType(), initializingContext);
            InitValue(value => popupDuration = value, GetDefaultPopupDuration(), initializingContext);
        }

#if UNITY_EDITOR
        protected override bool UpdateUndoRedoSerializedFields()
        {
            if (base.UpdateUndoRedoSerializedFields())
            {
                RecoverMeshRendererVisualDirtyFlags();

                return true;
            }
            return false;
        }

        private void RecoverMeshRendererVisualDirtyFlags()
        {
            SerializationUtility.RecoverLostReferencedObject(ref _meshRendererVisualDirtyFlags);
            if (_meshRendererVisualDirtyFlags != null)
                _meshRendererVisualDirtyFlags.UndoRedoPerformed();
        }
#endif

        /// <summary>
        /// When enabled the visuals will be instantiated automatically when the object is created or when the dependencies change.
        /// </summary>
        [Json]
        public bool autoInstantiate
        {
            get { return _autoInstantiate; }
            set
            {
                SetValue(nameof(autoInstantiate), value, ref _autoInstantiate, (newValue, oldValue) =>
                {
                    if (initialized && newValue)
                        meshRendererVisualDirtyFlags.Recreate();
                });
            }
        }

        /// <summary>
        /// When enabled the child <see cref="DepictionEngine.Visual"/> will not be saved as part of the Scene.
        /// </summary>
        [Json]
        public bool dontSaveVisualsToScene
        {
            get { return _dontSaveVisualsToScene; }
            set
            {
                SetValue(nameof(dontSaveVisualsToScene), value, ref _dontSaveVisualsToScene, (newValue, oldValue) =>
                {
#if UNITY_EDITOR
                    if (initialized)
                        SceneManager.MarkSceneDirty();
#endif
                    DontSaveVisualsToSceneChanged(newValue, oldValue);
                });
            }
        }

        /// <summary>
        /// How the object should fade in. Alpha Popup requires an instanced Material to work.
        /// </summary>
        [Json]
        public PopupType popupType
        {
            get { return _popupType; }
            set
            {
#if UNITY_EDITOR
                //Demo the effect if the change was done in the inspector and was not the result of "Paste Component Values".
                if (SceneManager.GetIsUserChangeContext() && SceneManager.sceneExecutionState != SceneManager.ExecutionState.PastingComponentValues)
                    popup = true;
#endif
                SetValue(nameof(popupType), value, ref _popupType);
            }
        }

        /// <summary>
        /// The duration of the fade in animation.
        /// </summary>
        [Json]
        public float popupDuration
        {
            get => _popupDuration;
            set { SetValue(nameof(popupDuration), value, ref _popupDuration); }
        }

        public Tween popupTween
        {
            get => _popupTween;
            set
            {
                if (Object.ReferenceEquals(_popupTween, value))
                    return;

                DisposeManager.Dispose(_popupTween);

                _popupTween = value;
            }
        }

        protected bool popup
        {
            get => _popup;
            set => _popup = value;
        }

        protected float GetPopupT(PopupType popupType)
        {
            return popupType == this.popupType && popupTween != Disposable.NULL ? popupT : 1.0f;
        }

        protected float popupT
        {
            get => _popupT;
            set => SetPopupT(value);
        }

        protected virtual bool SetPopupT(float value)
        {
            return SetValue(nameof(popupT), value, ref _popupT);
        }

        public override float GetCurrentAlpha()
        {
            return base.GetCurrentAlpha() * GetPopupT(PopupType.Alpha);
        }

        protected virtual Type GetMeshRendererVisualDirtyFlagType()
        {
            return typeof(VisualObjectVisualDirtyFlags);
        }

        protected override Type GetChildType()
        {
            return typeof(Visual);
        }

        protected VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags
        {
            get => _meshRendererVisualDirtyFlags;
            private set => _meshRendererVisualDirtyFlags = value;
        }

        protected override void Saving(Scene scene, string path)
        {
            base.Saving(scene, path);

            if (GetDontSaveVisualsToScene())
            {
                transform.IterateOverChildrenGameObject(gameObject, (childGO) =>
                {
                    childGO.hideFlags |= HideFlags.DontSave;
                }, true);
            }
        }

        protected override void Saved(Scene scene)
        {
            base.Saved(scene);

            if (GetDontSaveVisualsToScene())
            {
                transform.IterateOverChildrenGameObject(gameObject, (childGO) =>
                {
                    childGO.hideFlags &= ~HideFlags.DontSave;
                }, true);
            }
        }

        protected bool GetDontSaveVisualsToScene()
        {
            bool dontSaveVisualsToScene = this.dontSaveVisualsToScene;

            if (!dontSaveVisualsToScene)
            {
                IterateOverAssetReferences((asset, assetReference, required) =>
                {
                    if (required && asset != Disposable.NULL && asset.dontSaveToScene)
                    {
                        dontSaveVisualsToScene = true;
                        return false;
                    }
                    return true;
                });
            }

            return dontSaveVisualsToScene;
        }

        protected void UpdateMeshRendererVisualCollider(MeshRendererVisual meshRendererVisual, VisualObjectVisualDirtyFlags visualObjectVisualDirtyFlags, bool convexCollider, bool visualsChanged, bool convexColliderChanged = true)
        {
            UpdateMeshRendererVisualCollider(meshRendererVisual, visualObjectVisualDirtyFlags.colliderType, convexCollider, visualsChanged, visualObjectVisualDirtyFlags.colliderTypeDirty, convexColliderChanged);
            visualObjectVisualDirtyFlags.ResetColliderDirty();
        }

        protected void UpdateMeshRendererVisualCollider(MeshRendererVisual meshRendererVisual, MeshRendererVisual.ColliderType colliderType, bool convexCollider, bool visualsChanged, bool colliderTypeChanged = true, bool convexColliderChanged = true)
        {
            bool colliderChanged = false;
            if (visualsChanged || colliderTypeChanged)
            {
                if (meshRendererVisual.SetColliderType(colliderType))
                    colliderChanged = true;
            }

            if (visualsChanged || colliderChanged || convexColliderChanged)
            {
                if (meshRendererVisual.GetCollider() != null && meshRendererVisual.GetCollider() is MeshCollider)
                {
                    MeshCollider meshCollider = meshRendererVisual.GetCollider() as MeshCollider;
                    if (meshCollider.convex != convexCollider)
                        meshCollider.convex = convexCollider;
                }
            }
        }

        protected virtual void UpdateVisualProperties()
        {

        }

        protected override void TransformChildAddedHandler(TransformBase transform, PropertyMonoBehaviour child)
        {
            base.TransformChildAddedHandler(transform, child);

            if (child is Visual)
                _visualsChanged = true;
        }

        public override void PreHierarchicalUpdateBeforeChildrenAndSiblings()
        {
            base.PreHierarchicalUpdateBeforeChildrenAndSiblings();
            
            UpdateVisualProperties();

            UpdateMeshRendererDirtyFlags(meshRendererVisualDirtyFlags);

            if (meshRendererVisualDirtyFlags.disposeAllVisuals)
                DisposeAllChildren();

            if (autoInstantiate && AssetsReady())
            {
                UpdateMeshRendererVisuals(meshRendererVisualDirtyFlags);
                meshRendererVisualDirtyFlags.ResetDirty();
            }

            if (GetMeshRenderersInitialized())
            {
                if (popup && !IsFullyInitialized())
                    popup = false;

                if (popup)
                {
                    popup = false;
                    if (_popupDuration != 0.0f)
                    {
                        switch (_popupType)
                        {
                            case PopupType.Alpha:
                                popupTween = tweenManager.To(0.0f, alpha, popupDuration, (value) => { popupT = value; }, null, () =>
                                {
                                    popupTween = null;
                                });
                                break;
                            case PopupType.Scale:
                                popupTween = tweenManager.To(0.0f, 1.0f, popupDuration, (value) => { popupT = value; }, null, () =>
                                {
                                    popupTween = null;
                                }, EasingType.ElasticEaseOut);
                                break;
                        }
                    }
                }

                ApplyPropertiesToVisual(_visualsChanged, meshRendererVisualDirtyFlags);
                _visualsChanged = false;
            }
        }

        protected bool AssetsReady()
        {
            bool assetsLoaded = true;

            IterateOverAssetReferences((asset, assetReference, required) =>
            {
                if (required)
                {
                    if (asset == Disposable.NULL)
                    {
                        assetsLoaded = false;
                        return false;
                    }
                }
                else
                {
                    if (assetReference != Disposable.NULL && !assetReference.IsLoaded())
                    {
                        assetsLoaded = false;
                        return false;
                    }
                }

                return true;
            });

            return assetsLoaded;
        }

        protected virtual void UpdateMeshRendererDirtyFlags(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {

        }

        protected override bool DisposeAllChildren(DisposeContext disposeContext = DisposeContext.Programmatically_Pool)
        {
            if (base.DisposeAllChildren())
            {
                meshRendererVisualDirtyFlags.ResetDisposeAllVisuals();

                return true;
            }
            return false;
        }

        protected virtual void UpdateMeshRendererVisuals(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            
        }

        public bool GetMeshRenderersInitialized()
        {
            return gameObject.transform.childCount > 0;
        }

        protected virtual void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {

        }

        public override bool OnDispose(DisposeContext disposeContext)
        {
            if (base.OnDispose(disposeContext))
            {
                popupTween = null;

                if (disposeContext != DisposeContext.Programmatically_Pool)
                    DisposeManager.Dispose(_meshRendererVisualDirtyFlags, disposeContext);

                //We dispose the visuals manualy in case only the Component is disposed and not the entire GameObject and its children
                //The delay is required during a Dispose of the Object since we do not know wether it is only the Component being disposed or the entire GameObject. 
                //If the Entire GameObject is being disposed in the Editor then some Destroy Undo operations will be registered by the Editor automaticaly. By the time the delayed dispose will be performed the children will already be Destroyed and we will not register additional Undo Operations
                DisposeAllChildren(disposeContext);

                return true;
            }
            return false;
        }
    }
}
