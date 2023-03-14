// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Reference/" + nameof(AssetReference))]
    public class AssetReference : ReferenceBase
    {
        [SerializeField, HideInInspector]
        private AssetBase _asset;

        public override void Recycle()
        {
            base.Recycle();

            _asset = null;
        }

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            if (initializingContext == InitializationContext.Editor_Duplicate || initializingContext == InitializationContext.Programmatically_Duplicate)
                _asset = default;
        }

        protected override void DataChanged(PersistentScriptableObject newValue, PersistentScriptableObject oldValue)
        {
            base.DataChanged(newValue, oldValue);

            asset = newValue as AssetBase;
        }

        public AssetBase asset
        {
            get { return _asset; }
            private set
            {
                if (_asset == value)
                    return;

                _asset = value;
            }
        }
    }
}
