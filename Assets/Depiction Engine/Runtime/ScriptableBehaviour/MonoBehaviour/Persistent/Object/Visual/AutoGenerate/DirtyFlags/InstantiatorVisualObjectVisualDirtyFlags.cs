// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class InstantiatorVisualObjectVisualDirtyFlags : VisualObjectVisualDirtyFlags
    {
        [SerializeField]
        private AssetBundle _assetBundle;

        public override void Recycle()
        {
            base.Recycle();

            _assetBundle = default;
        }

#if UNITY_EDITOR
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            SerializationUtility.RecoverLostReferencedObject(ref _assetBundle);
        }
#endif

        public AssetBundle assetBundle
        {
            get => _assetBundle;
            set
            {
                if (_assetBundle == value)
                    return;

                _assetBundle = value;

                AssetBundleChanged();
            }
        }

        public void AssetBundleChanged()
        {
            Recreate();
        }
    }
}