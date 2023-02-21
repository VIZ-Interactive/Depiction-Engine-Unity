// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/MeshRendererVisual/" + nameof(TerrainEdgeMeshRendererVisual))]
    public class TerrainEdgeMeshRendererVisual : MeshRendererVisualNoCollider
    {
        private static int _terrainEdgeMeshRendererVisualLayer;
        protected override int GetLayer()
        {
            if (_terrainEdgeMeshRendererVisualLayer == 0)
                _terrainEdgeMeshRendererVisualLayer = LayerUtility.GetLayer(typeof(TerrainEdgeMeshRendererVisual).Name);
            return _terrainEdgeMeshRendererVisualLayer;
        }
    }
}