// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// RESTful web services based datasource
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Datasource/" + nameof(RestDatasource))]
    public class RestDatasource : DatasourceBase
    {
        private static readonly string[] COPYRIGHTED_DATASOURCE_LIST = { };

        [BeginFoldout("BaseAddress")]
        [SerializeField, Tooltip("The consistent part or the root of your website's address.")]
        private string _baseAddress;
        [SerializeField, Tooltip("Another variation of the '"+nameof(baseAddress)+"' used randomly for load balancing. Set to blank to ignore.")]
        private string _baseAddress2;
        [SerializeField, Tooltip("Another variation of the '"+nameof(baseAddress)+"' used randomly for load balancing. Set to blank to ignore.")]
        private string _baseAddress3;
        [SerializeField, Tooltip("Another variation of the '"+nameof(baseAddress)+"' used randomly for load balancing. Set to blank to ignore."), EndFoldout]
        private string _baseAddress4;

        [BeginFoldout("Endpoint")]
        [SerializeField, Tooltip("The endpoint used to send Save (Create / Update) operation to a web service.")]
        private string _saveEndpoint;
        [SerializeField, Tooltip("The endpoint used to send Synchronize operation to a web service.")]
        private string _synchronizeEndpoint;
        [SerializeField, Tooltip("The endpoint used to send Delete operation to a web service."), EndFoldout]
        private string _deleteEndpoint;

        [SerializeField, HideInInspector]
        private bool _containsCopyrightedMaterial;

        [SerializeField, HideInInspector]
        private List<string> _baseAddresses;

        protected override void InitializeSerializedFields(InitializationContext initializingContext)
        {
            base.InitializeSerializedFields(initializingContext);

            //Mapbox-Streets : https://api.mapbox.com/styles/v1/mapbox/streets-v11/tiles/{0}/{1}/{2}?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA
            //Earth Satellite : https://api.mapbox.com/v4/mapbox.satellite/{0}/{1}/{2}@2x.jpg90?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA
            //Earth Elevation : https://api.mapbox.com/v4/mapbox.terrain-rgb/{0}/{1}/{2}.pngraw?access_token=pk.eyJ1IjoibWFwYm94IiwiYSI6ImNpejY4M29iazA2Z2gycXA4N2pmbDZmangifQ.-g_vE53SD2WrJ6tFX7QHmA
            //Mars Satellite : https://astro.arcgis.com/arcgis/rest/services/OnMars/MDIM/MapServer/tile/{0}/{2}/{1}
            //Mars Elevation : https://astro.arcgis.com/arcgis/rest/services/OnMars/MColorDEM/MapServer/tile/{0}/{2}/{1}
            //Moon Satellite (Hillshade) : https://tiles.arcgis.com/tiles/RS8mqPfEEjgYh6uG/arcgis/rest/services/MoonHillshade/MapServer/tile/{0}/{2}/{1}
            //Moon Satellite : https://tiles.arcgis.com/tiles/WQ9KVmV6xGGMnCiQ/arcgis/rest/services/Moon_Basemap_Tile0to9/MapServer/tile/{0}/{2}/{1}
            //Vertical Exageration 4x
            //Moon Elevation : https://tiles.arcgis.com/tiles/WQ9KVmV6xGGMnCiQ/arcgis/rest/services/Moon_Elevation_Surface/ImageServer/tile/{0}/{2}/{1}?blankTile=false 

            InitValue(value => baseAddress = value, "", initializingContext);
            InitValue(value => baseAddress2 = value, "", initializingContext);
            InitValue(value => baseAddress3 = value, "", initializingContext);
            InitValue(value => baseAddress4 = value, "", initializingContext);
            InitValue(value => saveEndpoint = value, "", initializingContext);
            InitValue(value => synchronizeEndpoint = value, "", initializingContext);
            InitValue(value => deleteEndpoint = value, "", initializingContext);
        }

        public override string GetDatasourceName()
        {
            return base.GetDatasourceName() + baseAddress;
        }

        protected string GetSaveEndpoint()
        {
            return saveEndpoint;
        }

        protected string GetSynchronizeEndpoint()
        {
            return synchronizeEndpoint;
        }

        protected string GetDeleteEndpoint()
        {
            return deleteEndpoint;
        }

        /// <summary>
        /// The consistent part or the root of your website's address.
        /// </summary>
        [Json]
        public string baseAddress
        {
            get { return _baseAddress; }
            set
            {
                SetValue(nameof(baseAddress), ValidateString(value), ref _baseAddress, (newValue, oldValue) =>
                {
                    UpdateBaseAdresses();
                });
            }
        }

        /// <summary>
        /// Another variation of the <see cref="DepictionEngine.RestDatasource.baseAddress"/> used randomly for load balancing. Set to blank to ignore.
        /// </summary>
        [Json]
        public string baseAddress2
        {
            get { return _baseAddress2; }
            set
            {
                SetValue(nameof(baseAddress2), ValidateString(value), ref _baseAddress2, (newValue, oldValue) =>
                {
                    UpdateBaseAdresses();
                });
            }
        }

        /// <summary>
        /// Another variation of the <see cref="DepictionEngine.RestDatasource.baseAddress"/> used randomly for load balancing. Set to blank to ignore.
        /// </summary>
        [Json]
        public string baseAddress3
        {
            get { return _baseAddress3; }
            set
            {
                SetValue(nameof(baseAddress3), ValidateString(value), ref _baseAddress3, (newValue, oldValue) =>
                {
                    UpdateBaseAdresses();
                });
            }
        }

        /// <summary>
        /// Another variation of the <see cref="DepictionEngine.RestDatasource.baseAddress"/> used randomly for load balancing. Set to blank to ignore.
        /// </summary>
        [Json]
        public string baseAddress4
        {
            get { return _baseAddress4; }
            set
            {
                SetValue(nameof(baseAddress4), ValidateString(value), ref _baseAddress4, (newValue, oldValue) =>
                {
                    UpdateBaseAdresses();
                });
            }
        }

        /// <summary>
        /// The endpoint used to send Save (Create / Update) operation to a web service.
        /// </summary>
        [Json]
        public string saveEndpoint
        {
            get { return _saveEndpoint; }
            set
            {
                SetValue(nameof(saveEndpoint), ValidateString(value), ref _saveEndpoint, (newValue, oldValue) =>
                {
                    datasource.supportsSave = !string.IsNullOrEmpty(newValue);
                    EndpointsChanged();
                });
            }
        }

        /// <summary>
        /// The endpoint used to send Synchronize operation to a web service.
        /// </summary>
        [Json]
        public string synchronizeEndpoint
        {
            get { return _synchronizeEndpoint; }
            set
            {
                SetValue(nameof(synchronizeEndpoint), ValidateString(value), ref _synchronizeEndpoint, (newValue, oldValue) =>
                {
                    datasource.supportsSynchronize = !string.IsNullOrEmpty(newValue);
                    EndpointsChanged();
                });
            }
        }

        /// <summary>
        /// The endpoint used to send Delete operation to a web service.
        /// </summary>
        [Json]
        public string deleteEndpoint
        {
            get { return _deleteEndpoint; }
            set
            {
                SetValue(nameof(deleteEndpoint), ValidateString(value), ref _deleteEndpoint, (newValue, oldValue) =>
                {
                    datasource.supportsDelete = !string.IsNullOrEmpty(newValue);
                    EndpointsChanged();
                });
            }
        }

        public static string ValidateString(string value)
        {
            return value.Trim();
        }

        private void EndpointsChanged()
        {
            
        }

        private void UpdateBaseAdresses()
        {
            if (_baseAddresses == null)
                _baseAddresses = new List<string>();
            else
                _baseAddresses.Clear();
            if (!string.IsNullOrEmpty(baseAddress))
                _baseAddresses.Add(baseAddress);
            if (!string.IsNullOrEmpty(baseAddress2))
                _baseAddresses.Add(baseAddress2);
            if (!string.IsNullOrEmpty(baseAddress3))
                _baseAddresses.Add(baseAddress3);
            if (!string.IsNullOrEmpty(baseAddress4))
                _baseAddresses.Add(baseAddress4);

            UpdateContainsCopyrightedMaterial();
        }

        private void UpdateContainsCopyrightedMaterial()
        {
            _containsCopyrightedMaterial = false;
            foreach (string baseAddress in _baseAddresses)
            {
                if (!string.IsNullOrEmpty(baseAddress))
                {
                    foreach (string copyrightDatasource in COPYRIGHTED_DATASOURCE_LIST)
                    {
                        if (baseAddress.ToLower().Contains(copyrightDatasource.ToLower()))
                        {
                            _containsCopyrightedMaterial = true;
                            return;
                        }
                    }
                }
            }
        }

        public bool containsCopyrightedMaterial
        {
            get { return _containsCopyrightedMaterial; }
        }

        protected override DatasourceOperationBase CreateLoadDatasourceOperation(LoadScope loadScope)
        {
            LoadWebRequestDatasourceOperation loadWebRequestDatasourceOperation = CreateWebRequestDatasourceOperation<LoadWebRequestDatasourceOperation>(loadScope.loadEndpoint, null, loadScope.depth, loadScope.GetURLParams(), loadScope.timeout, loadScope.headers);

            loadWebRequestDatasourceOperation.Init(loadScope.dataType, loadScope.GetLoadScopeFallbackValuesJson(), loadScope.GetPersistentFallbackValuesJson());

            return loadWebRequestDatasourceOperation;
        }

        protected override DatasourceOperationBase CreateSaveDatasourceOperation(Dictionary<Guid, Datasource.PersistenceOperationData> savePersistenceOperationDatas)
        {
            JSONNode bodyData = savePersistenceOperationDatas.Count > 1 ? new JSONArray() : null;

            foreach (Datasource.PersistenceOperationData persistenceOperationData in savePersistenceOperationDatas.Values)
            {
                if (bodyData is JSONArray)
                    bodyData.Add(persistenceOperationData.data);
                else
                    bodyData = persistenceOperationData.data;
            }

            return CreateWebRequestDatasourceOperation<SaveWebRequestDatasourceOperation>(saveEndpoint, JSONNodeToByteArray(bodyData));
        }

        protected override DatasourceOperationBase CreateSynchronizeDatasourceOperation(Dictionary<Guid, Datasource.PersistenceOperationData> synchronizePersistenceOperationDatas)
        {
            JSONNode bodyData = synchronizePersistenceOperationDatas.Count > 1 ? new JSONArray() : null;

            foreach (Datasource.PersistenceOperationData persistenceOperationData in synchronizePersistenceOperationDatas.Values)
            {
                if (bodyData is JSONArray)
                    bodyData.Add(persistenceOperationData.persistent.id.ToString());
                else
                    bodyData = persistenceOperationData.persistent.id.ToString();
            }

            return CreateWebRequestDatasourceOperation<SynchronizeWebRequestDatasourceOperation>(synchronizeEndpoint, JSONNodeToByteArray(bodyData));
        }

        protected override DatasourceOperationBase CreateDeleteDatasourceOperation(Dictionary<Guid, Datasource.PersistenceOperationData> deletePersistenceOperationDatas)
        {
            JSONNode bodyData = deletePersistenceOperationDatas.Count > 1 ? new JSONArray() : null;

            foreach (Datasource.PersistenceOperationData persistenceOperationData in deletePersistenceOperationDatas.Values)
            {
                if (bodyData is JSONArray)
                    bodyData.Add(persistenceOperationData.persistent.id.ToString());
                else
                    bodyData = persistenceOperationData.persistent.id.ToString();
            }

            return CreateWebRequestDatasourceOperation<DeleteWebRequestDatasourceOperation>(deleteEndpoint, JSONNodeToByteArray(bodyData));
        }

        private byte[] JSONNodeToByteArray(JSONNode jsonNode)
        {
            if (JsonUtility.FromJson(out string jsonStr, jsonNode))
                return Encoding.ASCII.GetBytes(jsonStr);
            return new byte[0];
        }

        protected T CreateWebRequestDatasourceOperation<T>(string endpoint, byte[] bodyData = null, int depth = 0, object[] urlParams = null, int timeout = 60, List<string> headers = null) where T : WebRequestDatasourceOperationBase
        {
            T webRequestDatasourceOperation = base.CreateDatasourceOperation<T>();

            if (depth != 0)
                endpoint += "?depth=" + depth;

            if (urlParams != null && urlParams.Length > 0)
            {
                try
                {
                    endpoint = string.Format(endpoint, urlParams);
                }
                catch (FormatException)
                {
                    Debug.LogWarning("Endpoint mismatch between the number of URL Parameters sent and the number expected");
                }
            }

            webRequestDatasourceOperation.Init(GetDatasourceName() + endpoint, timeout, headers, bodyData);

            return webRequestDatasourceOperation;
        }
    }
}
