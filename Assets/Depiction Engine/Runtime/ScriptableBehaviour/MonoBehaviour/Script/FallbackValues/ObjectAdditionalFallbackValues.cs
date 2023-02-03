// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    public class ObjectAdditionalFallbackValues : OptionalPropertiesBase
    {
        [BeginFoldout("Object Additional Fallback Values")]
        [Layer, Tooltip("The layer the GameObject is in.")]
        public int layer;
        [Tooltip("The tag of this GameObject.")]
        public string tag;
        [Tooltip("When enabled, the 'GameObject' will not be displayed in the hierarchy.")]
        public bool isHiddenInHierarchy;

        [Space(20.0f)]

        [Tooltip("When enabled, a new 'AnimatorBase' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createAnimatorIfMissing;
        [ComponentReference, Tooltip("The Id of the animator.")]
        public SerializableGuid animatorId;
        
        [Space]

        [Tooltip("When enabled, a new 'ControllerBase' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createControllerIfMissing;
        [ComponentReference, Tooltip("The Id of the controller.")]
        public SerializableGuid controllerId;
        
        [Space]

        [Tooltip("When enabled, a new 'GeneratorBase' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createGeneratorIfMissing;
        [ComponentReference, Tooltip("The Id of the generators.")]
        public List<SerializableGuid> generatorsId;
        
        [Space]

        [Tooltip("When enabled, a new 'ReferenceBase' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createReferenceIfMissing;
        [ComponentReference, Tooltip("The Id of the references.")]
        public List<SerializableGuid> referencesId;
        
        [Space]

        [Tooltip("When enabled, a new 'EffectBase' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createEffectIfMissing;
        [ComponentReference, Tooltip("The Id of the effects.")]
        public List<SerializableGuid> effectsId;
        
        [Space]

        [Tooltip("When enabled, a new 'FallbackValues' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createFallbackValuesIfMissing;
        [ComponentReference, Tooltip("The Id of the fallbackValues.")]
        public List<SerializableGuid> fallbackValuesId;
        
        [Space]

        [Tooltip("When enabled, a new 'DatasourceBase' will be created if none is present in the 'IPersistent' returned from the datasource.")]
        public bool createDatasourceIfMissing;
        [ComponentReference, Tooltip("The Id of the datasources."), EndFoldout]
        public List<SerializableGuid> datasourcesId;

        [HideInInspector]
        public AnimatorBase animator;

        [HideInInspector]
        public ControllerBase controller;

        [HideInInspector]
        public List<GeneratorBase> generators;

        [HideInInspector]
        public List<ReferenceBase> references;

        [HideInInspector]
        public List<EffectBase> effects;

        [HideInInspector]
        public List<FallbackValues> fallbackValues;

        [HideInInspector]
        public List<DatasourceBase> datasources;
    }
}
