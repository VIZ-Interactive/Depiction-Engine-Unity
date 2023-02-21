// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/MeshRendererVisual/" + nameof(TerrainEdgeMeshRendererVisualNoCollider))]
    public class TerrainEdgeMeshRendererVisualNoCollider : TerrainEdgeMeshRendererVisual
    {
        public override bool SetColliderType(ColliderType value)
        {
            return base.SetColliderType(ColliderType.None);
        }
    }
}