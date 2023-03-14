// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class AtmosphereGridMeshObject : TerrainGridMeshObject
    {
        protected override string GetDefaultShaderPath()
        {
            return RenderingManager.SHADER_BASE_PATH + "AtmosphereGrid";
        }

        protected override float GetSphericalRatio()
        {
            return 1.0f;
        }

        protected override float GetDefaultPopupDuration()
        {
            return 0.0f;
        }

        protected override bool GetDefaultUseCollider()
        {
            return false;
        }

        protected override bool GetDefaultCastShadow()
        {
            return false;
        }

        protected override bool GetDefaultReceiveShadows()
        {
            return false;
        }

        protected override float GetDefaultEdgeDepth()
        {
            return 0.0f;
        }

        protected override MeshRendererVisual.ColliderType GetColliderType()
        {
            return MeshRendererVisual.ColliderType.None;
        }

        protected override bool GetFlipTriangles()
        {
            return true;
        }

        protected override int GetCacheHash(MeshRendererVisualModifier meshRendererVisualModifier)
        {
            return DEFAULT_MISSING_CACHE_HASH;
        }

        protected override float GetAtmosphereAlpha(float atmosphereAlpha, float atmosphereAltitudeRatio)
        {
            return Mathf.Lerp(atmosphereAlpha * 0.7f, base.GetAtmosphereAlpha(atmosphereAlpha, atmosphereAltitudeRatio), atmosphereAltitudeRatio);
        }

        protected override float GetSunBrightness(float sunBrightness, float atmosphereAltitudeRatio)
        {
            return Mathf.Lerp(sunBrightness * 0.4f, base.GetAtmosphereAlpha(sunBrightness, atmosphereAltitudeRatio), atmosphereAltitudeRatio);
        }

        protected override void UpdateAltitudeOffset()
        {
            base.UpdateAltitudeOffset();

            if (parentGeoAstroObject != Disposable.NULL)
            {
                double parentGeoAstroObjectRadius = parentGeoAstroObject.radius;
                altitudeOffset = (AtmosphereEffect.ATMOPSHERE_ALTITUDE_FACTOR * parentGeoAstroObjectRadius) - parentGeoAstroObjectRadius;
            }
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                if (visualsChanged || PropertyDirty(nameof(sphericalRatio)))
                    meshRendererVisual.gameObject.SetActive(IsSpherical());

                return true;
            });
        }
    }
}
