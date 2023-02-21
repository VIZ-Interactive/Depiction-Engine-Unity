// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;
using UnityEngine.Rendering;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/MeshRendererVisual/" + nameof(UIMeshRendererVisual))]
    public class UIMeshRendererVisual : MeshRendererVisual
    {
        protected override ShadowCastingMode GetDefaultShadowCastingMode()
        {
            return ShadowCastingMode.Off;
        }

        protected override bool GetDefaultReceiveShadows()
        {
            return false;
        }
    }
}
