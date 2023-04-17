// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/Script/Reference/" + nameof(AssetReference))]
    public class AssetReference : ReferenceBase
    {
        public AssetBase asset
        {
            get => data as AssetBase;
        }
    }
}
