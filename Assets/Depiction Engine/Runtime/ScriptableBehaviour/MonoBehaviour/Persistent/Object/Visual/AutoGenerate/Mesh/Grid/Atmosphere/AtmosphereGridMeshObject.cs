// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using UnityEngine;

namespace DepictionEngine
{
    public class AtmosphereGridMeshObject : TerrainGridMeshObject
    {
#if UNITY_EDITOR
        protected override bool GetShowMaterialProperties()
        {
            return false;
        }
#endif

        public override void Initialized(InitializationContext initializingContext)
        {
            base.Initialized(initializingContext);

            UpdateChildrenMeshRendererVisualActive();
        }

        protected override void ParentGeoAstroObjectPropertyAssignedHandler(IProperty property, string name, object newValue, object oldValue)
        {
            base.ParentGeoAstroObjectPropertyAssignedHandler(property, name, newValue, oldValue);

            if (initialized)
            {
                if (name == nameof(GeoAstroObject.sphericalRatio))
                    UpdateChildrenMeshRendererVisualActive();
            }
        }

        protected override bool CanBeDuplicated()
        {
            return false;
        }

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

        protected override TerrainGeometryType GetDefaultGenerateTerrainGeometry()
        {
            return TerrainGeometryType.Surface;
        }

        protected override MeshRendererVisual.ColliderType GetColliderType()
        {
            return MeshRendererVisual.ColliderType.None;
        }

        protected override bool GetFlipTriangles()
        {
            return true;
        }

        protected override int GetCacheHash(VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            return RenderingManager.DEFAULT_MISSING_CACHE_HASH;
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
                altitudeOffset = (float)((AtmosphereEffect.ATMOPSHERE_ALTITUDE_FACTOR * parentGeoAstroObject.radius) - parentGeoAstroObject.radius);
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            if (visualsChanged)
                UpdateChildrenMeshRendererVisualActive();
        }

        private void UpdateChildrenMeshRendererVisualActive()
        {
            transform.IterateOverChildren<MeshRendererVisual>((meshRendererVisual) =>
            {
                meshRendererVisual.gameObject.SetActive(IsSpherical());

                return true;
            });
        }
    }
}
