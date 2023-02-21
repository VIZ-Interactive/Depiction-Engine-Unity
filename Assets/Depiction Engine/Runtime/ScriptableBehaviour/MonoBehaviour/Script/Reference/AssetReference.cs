// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Reference/" + nameof(AssetReference))]
    public class AssetReference : ReferenceBase
    {
        private AssetBase _asset;

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
                AssetBase oldValue = _asset;
                AssetBase newValue = value;
                if (newValue == oldValue)
                    return;

                _asset = value;
            }
        }

        public override bool OnDispose()
        {
            if (base.OnDispose())
            {
                asset = null;

                return true;
            }
            return false;
        }
    }
}
