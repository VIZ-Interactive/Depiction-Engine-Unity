// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// 
    /// </summary>
    public class Level : Object
    {
        [BeginFoldout("Children")]
        [SerializeField, Tooltip("When enabled the children objects will be hidden when the level is closed.")]
        private bool _showInteriorOnOpenOnly;
        [SerializeField, Tooltip("When enabled the level will trigger '"+nameof(LoaderBase.LoadAll)+ "' on open and '"+nameof(LoaderBase.DisposeAllLoadScopes)+"' on close."), EndFoldout]
        private bool _manageLoaders;

#if UNITY_EDITOR
        [BeginFoldout("Interior")]
        [SerializeField, Button(nameof(OpenBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Hide the roof of the current level and hide any subsequent levels to expose its interior.")]
        private bool _openLevel;
        [SerializeField, Button(nameof(CloseBtn)), ConditionalShow(nameof(IsNotFallbackValues)), Tooltip("Unhide the roof of the current level and show all subsequent levels."), EndFoldout]
        private bool _closeLevel;

        private void OpenBtn()
        {
            SceneManager.UserContext(() => { Open(); });
        }

        private void CloseBtn()
        {
            SceneManager.UserContext(() => { Close(); });
        } 
#endif

        private float _groundY;

        public override void Recycle()
        {
            base.Recycle();

            _groundY = default;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            InitValue(value => showInteriorOnOpenOnly = value, true, initializingContext);
            InitValue(value => manageLoaders = value, true, initializingContext);
        }

        protected override bool LateInitialize(InitializationContext initializingContext)
        {
            if (base.LateInitialize(initializingContext))
            {
                if (interior == Disposable.NULL)
                    CreateChild<Interior>(nameof(Interior));
            }
            return false;
        }

        public override bool IsPhysicsObject()
        {
            return false;
        }

        protected override bool TransformObjectCallback(LocalPositionParam localPositionParam, LocalRotationParam localRotationParam, LocalScaleParam localScaleParam, Camera camera = null)
        {
            if (base.TransformObjectCallback(localPositionParam, localRotationParam, localScaleParam, camera))
            {
                if (localPositionParam.changed)
                {
                    Vector3Double localPosition = new(0.0d, groundY, 0.0d);
                    if (!localPositionParam.isGeoCoordinate)
                        localPositionParam.SetValue(localPosition);
                    else
                        localPositionParam.SetValue(transform.parentGeoAstroObject.GetGeoCoordinateFromPoint(transform.parent != Disposable.NULL ? transform.parent.TransformPoint(localPosition) : localPosition));
                }
                if (localRotationParam.changed)
                    localRotationParam.SetValue(QuaternionDouble.identity);
                if (localScaleParam.changed)
                    localScaleParam.SetValue(Vector3Double.one);

                return true;
            }
            return false;
        }

        public float groundY
        {
            get => _groundY;
            set 
            {
                SetValue(nameof(groundY), value, ref _groundY, (newValue, oldValue) => 
                {
                    ForceUpdateTransform(true);
                });
            }
        }

        public float GetHeight()
        {
            float height = 0.0f;

            transform.IterateOverChildrenObject<LevelMeshObject>((levelMeshObject) =>
            {
                height = Mathf.Max(height, levelMeshObject.GetHeight());
                return true;
            });

            return height;
        }
     
        public bool IsOpened()
        {
            bool isOpened = true;

            transform.IterateOverChildrenObject<LevelMeshObject>((levelMeshObject) =>
            {
                if (!levelMeshObject.IsOpened())
                {
                    isOpened = false;
                    return false;
                }
                return true;
            });

            return isOpened;
        }

        public void Opened()
        {
            transform.IterateOverChildrenObject<LevelMeshObject>((levelMeshObject) =>
            {
                levelMeshObject.Opened();
                return true;
            });

            UpdateInteriorObjectActive();

            if (manageLoaders)
            {
                IterateOverGenerators<LoaderBase>((loader) =>
                {
                    loader.LoadAll();
                    return true;
                });
            }
        }

        public void Closed()
        {
            transform.IterateOverChildrenObject<LevelMeshObject>((levelMeshObject) => 
            {
                levelMeshObject.Closed();
                return true;
            });

            UpdateInteriorObjectActive();

            if (manageLoaders)
            {
                IterateOverGenerators<LoaderBase>((loader) =>
                {
                    DisposeContext disposeContext = DisposeContext.Programmatically_Pool;
#if UNITY_EDITOR
                    if (SceneManager.IsUserChangeContext())
                        disposeContext = DisposeContext.Editor_Destroy;
#endif

                    loader.DisposeAllLoadScopes(disposeContext);

                    return true;
                });
            }
        }

        public bool GetLevelMeshObjectInitialized()
        {
            bool levelMeshObjectsInitialized = false;

            transform.IterateOverChildrenObject<LevelMeshObject>((levelMeshObject) =>
            {
                if (!levelMeshObject.GetMeshRenderersInitialized())
                {
                    levelMeshObjectsInitialized = false;
                    return false;
                }
                else
                {
                    levelMeshObjectsInitialized = true;
                    return true;
                }
            });

            return levelMeshObjectsInitialized;
        }

        public Interior interior
        {
            get
            {
                Interior firstInterior = null;

                transform.IterateOverChildrenObject<Interior>((interior) =>
                {
                    firstInterior = interior;
                    return false;
                });

                return firstInterior;
            }
        }

        /// <summary>
        /// When enabled the children objects will be hidden when the level is closed.
        /// </summary>
        [Json]
        public bool showInteriorOnOpenOnly
        {
            get => _showInteriorOnOpenOnly;
            set 
            { 
                SetValue(nameof(showInteriorOnOpenOnly), value, ref _showInteriorOnOpenOnly, (newValue, oldValue) => 
                {
                    UpdateInteriorObjectActive();
                }); 
            }
        }

        /// <summary>
        /// When enabled the level will trigger <see cref="DepictionEngine.LoaderBase.LoadAll"/> on open and <see cref="DepictionEngine.LoaderBase.DisposeAllLoadScopes"/> on close.
        /// </summary>
        [Json]
        public bool manageLoaders
        {
            get => _manageLoaders;
            set { SetValue(nameof(manageLoaders), value, ref _manageLoaders); }
        }

        /// <summary>
        /// Hide the roof of the current level and hide any subsequent levels to expose its interior.
        /// </summary>
        public void Open()
        {
            Building building = transform.parentObject as Building;
            if (building != Disposable.NULL)
                building.currentLevel = this;
        }

        /// <summary>
        /// Unhide the roof of the current level and show all subsequent levels.
        /// </summary>
        public void Close()
        {
            Building building = transform.parentObject as Building;
            if (building != Disposable.NULL)
            {
                if (Object.ReferenceEquals(building.currentLevel, this))
                    building.currentLevel = null;
            }
        }

        public override bool PreHierarchicalUpdate()
        {
            if (base.PreHierarchicalUpdate())
            {
                UpdateInteriorObjectActive();

                return true;
            }
            return false;
        }

        private void UpdateInteriorObjectActive()
        {
            if (initialized)
            {
                Interior interior = this.interior;
                if (interior != Disposable.NULL)
                    interior.gameObjectActiveSelf = !showInteriorOnOpenOnly || IsOpened();
            }
        }
    }
}