// Copyright (C) 2023 by VIZ Interactive Media Inc. <contact@vizinteractive.io> | Licensed under MIT license (see LICENSE.md for details)

using System.Runtime.CompilerServices;
using UnityEngine;

namespace DepictionEngine
{
    public class IdLoadScope : LoadScope
    {
        [SerializeField]
        private SerializableGuid _scopeId;

        public override void Recycle()
        {
            base.Recycle();

            _scopeId = SerializableGuid.Empty;
        }

        public IdLoadScope Init(LoaderBase loader, SerializableGuid id)
        {
            Init(loader);

            this.scopeId = id;

            return this;
        }

        public SerializableGuid scopeId
        {
            get { return _scopeId; }
            set { _scopeId = value; }
        }

        public override JSONObject GetLoadScopeFallbackValuesJson()
        {
            JSONObject loadScopeFallbackValuesJson = base.GetLoadScopeFallbackValuesJson();

            loadScopeFallbackValuesJson[nameof(PropertyMonoBehaviour.id)] = scopeId.ToString();

            return loadScopeFallbackValuesJson;
        }

        public override bool IsInScope(IPersistent persistent)
        {
            return persistent.id == scopeId;
        }

        public override object[] GetURLParams()
        {
            return new object[] { scopeId };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string PropertiesToString()
        {
            return "(Id:" + scopeId + ")";
        }
    }
}
