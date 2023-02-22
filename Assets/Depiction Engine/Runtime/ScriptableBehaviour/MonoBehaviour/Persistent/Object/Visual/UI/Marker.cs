// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace DepictionEngine
{
    [AddComponentMenu(SceneManager.NAMESPACE + "/Object/UI/" + nameof(Marker))]
    public class Marker : UIBase
    {
        /// <summary>
        /// The different types of marker icons.
        /// </summary>
        public enum Icon
        {
            ABSEILING,
            ACCOUNTING,
            AIRPORT,
            AMUSEMENTPARK,
            AQUARIUM,
            ARCHERY,
            ARTGALLERY,
            ASSISTIVELISTENINGSYSTEM,
            ATM,
            AUDIODESCRIPTION,
            BAKERY,
            BANK,
            BAR,
            BASEBALL,
            BEAUTYSALON,
            BICYCLESTORE,
            BICYCLING,
            BOATRAMP,
            BOATTOUR,
            BOATING,
            BOOKSTORE,
            BOWLINGALLEY,
            BRAILLE,
            BUSSTATION,
            CAFE,
            CAMPGROUND,
            CANOE,
            CARDEALER,
            CARRENTAL,
            CARREPAIR,
            CARWASH,
            CASINO,
            CEMETERY,
            CHAIRLIFT,
            CHURCH,
            CIRCLE,
            CITYHALL,
            CLIMBING,
            CLOSEDCAPTIONING,
            CLOTHINGSTORE,
            COMPASS,
            CONVENIENCESTORE,
            COURTHOUSE,
            CROSSCOUNTRYSKIING,
            CROSSHAIRS,
            DENTIST,
            DEPARTMENTSTORE,
            DIVING,
            DOCTOR,
            ELECTRICIAN,
            ELECTRONICSSTORE,
            EMBASSY,
            EXPAND,
            FEMALE,
            FINANCE,
            FIRESTATION,
            FISHCLEANING,
            FISHINGPIER,
            FISHING,
            FLORIST,
            FOOD,
            FULLSCREEN,
            FUNERALHOME,
            FURNITURESTORE,
            GASSTATION,
            GENERALCONTRACTOR,
            GOLF,
            GROCERYORSUPERMARKET,
            GYM,
            HAIRCARE,
            HANGGLIDING,
            HARDWARESTORE,
            HEALTH,
            HINDUTEMPLE,
            HORSERIDING,
            HOSPITAL,
            ICEFISHING,
            ICESKATING,
            INLINESKATING,
            INSURANCEAGENCY,
            JETSKIING,
            JEWELRYSTORE,
            KAYAKING,
            LAUNDRY,
            LAWYER,
            LIBRARY,
            LIQUORSTORE,
            LOCALGOVERNMENT,
            LOCATIONARROW,
            LOCKSMITH,
            LODGING,
            LOWVISIONACCESS,
            MALE,
            UNIVERSEPIN,
            MARINA,
            MOSQUE,
            MOTOBIKETRAIL,
            MOVIERENTAL,
            MOVIETHEATER,
            MOVINGCOMPANY,
            MUSEUM,
            NATURALFEATURE,
            NIGHTCLUB,
            OPENCAPTIONING,
            PAINTER,
            PARK,
            PARKING,
            PETSTORE,
            PHARMACY,
            PHYSIOTHERAPIST,
            PLACEOFWORSHIP,
            PLAYGROUND,
            PLUMBER,
            POINTOFINTEREST,
            POLICE,
            POLITICAL,
            POSTBOX,
            POSTOFFICE,
            POSTALCODEPREFIX,
            POSTALCODE,
            RAFTING,
            REALESTATEAGENCY,
            RESTAURANT,
            ROOFINGCONTRACTOR,
            ROUTEPIN,
            ROUTE,
            RVPARK,
            SAILING,
            SCHOOL,
            SCUBADIVING,
            SEARCH,
            SHIELD,
            SHOPPINGMALL,
            SIGNLANGUAGE,
            SKATEBOARDING,
            SKIJUMPING,
            SKIING,
            SLEDDING,
            SNOWSHOEING,
            SNOW,
            SNOWBOARDING,
            SNOWMOBILE,
            SPA,
            SQUAREPIN,
            SQUAREROUNDED,
            SQUARE,
            STADIUM,
            STORAGE,
            STORE,
            SUBWAYSTATION,
            SURFING,
            SWIMMING,
            SYNAGOGUE,
            TAXISTAND,
            TENNIS,
            TOILET,
            TRAILWALKING,
            TRAINSTATION,
            TRANSITSTATION,
            TRAVELAGENCY,
            UNISEX,
            UNIVERSITY,
            VETERINARYCARE,
            VIEWING,
            VOLUMECONTROLTELEPHONE,
            WALKING,
            WATERSKIING,
            WHALEWATCHING,
            WHEELCHAIR,
            WINDSURFING,
            ZOO,
            ZOOMINALT,
            ZOOMIN,
            ZOOMOUTALT,
            ZOOMOUT
        }

        private static readonly string[] ICONS_UNICODE = new string[]
        {
            "\ue800", "\ue801", "\ue802", "\ue803", "\ue804", "\ue805", "\ue806", "\ue807", "\ue808", "\ue809", "\ue80a", "\ue80b", "\ue80c", "\ue80d", "\ue80e", "\ue80f", "\ue810", "\ue811",
            "\ue812", "\ue813", "\ue814", "\ue815", "\ue816", "\ue817", "\ue818", "\ue819", "\ue81a", "\ue81b", "\ue81c", "\ue81d", "\ue81e", "\ue81f", "\ue820", "\ue821", "\ue822", "\ue823",
            "\ue824", "\ue825", "\ue826", "\ue827", "\ue828", "\ue829", "\ue82a", "\ue82b", "\ue82c", "\ue82d", "\ue82e", "\ue82f", "\ue830", "\ue831", "\ue832", "\ue833", "\ue834", "\ue835",
            "\ue836", "\ue837", "\ue838", "\ue839", "\ue83a", "\ue83b", "\ue83c", "\ue83d", "\ue83e", "\ue83f", "\ue840", "\ue841", "\ue842", "\ue843", "\ue844", "\ue845", "\ue846", "\ue847",
            "\ue848", "\ue849", "\ue84a", "\ue84b", "\ue84c", "\ue84d", "\ue84e", "\ue84f", "\ue850", "\ue851", "\ue852", "\ue853", "\ue854", "\ue855", "\ue856", "\ue857", "\ue858", "\ue859",
            "\ue85a", "\ue85b", "\ue85c", "\ue85d", "\ue85e", "\ue85f", "\ue860", "\ue861", "\ue862", "\ue863", "\ue864", "\ue865", "\ue866", "\ue867", "\ue868", "\ue869", "\ue86a", "\ue86b",
            "\ue86c", "\ue86d", "\ue86e", "\ue86f", "\ue870", "\ue871", "\ue872", "\ue873", "\ue874", "\ue875", "\ue876", "\ue877", "\ue878", "\ue879", "\ue87a", "\ue87b", "\ue87c", "\ue87d",
            "\ue87e", "\ue87f", "\ue880", "\ue881", "\ue882", "\ue883", "\ue884", "\ue885", "\ue886", "\ue887", "\ue888", "\ue889", "\ue88a", "\ue88b", "\ue88c", "\ue88d", "\ue88e", "\ue88f",
            "\ue890", "\ue891", "\ue892", "\ue893", "\ue894", "\ue895", "\ue896", "\ue897", "\ue898", "\ue899", "\ue89a", "\ue89b", "\ue89c", "\ue89d", "\ue89e", "\ue89f", "\ue8a0", "\ue8a1",
            "\ue8a2", "\ue8a3", "\ue8a4", "\ue8a5", "\ue8a6", "\ue8a7", "\ue8a8", "\ue8a9", "\ue8aa", "\ue8ab", "\ue8ac", "\ue8ad", "\ue8ae"
        };

        private static readonly Vector2 BADGE_SCALE = new Vector2(22.0f, 32.0f);

        private const string BADGE_MESH_NAME = "BadgeMeshRendererVisual";
        private const string LINE_MESH_NAME = "LineMeshMeshRendererVisual";
        private const string SHADOW_MESH_NAME = "ShadowMeshMeshRendererVisual";

        public const string MARKER_ICON_FONT_NAME = "MarkerIcons"; 

        [BeginFoldout("Marker")]
        [SerializeField, Tooltip("The Marker tint.")]
        private Color _color;
        [SerializeField, Tooltip("The distance between the Marker position and the badge.")]
        private float _badgeOffset;
        [SerializeField, Tooltip("The icon to display in the badge.")]
        private Icon _icon;
        [SerializeField, Tooltip("General purpose metadata associated with the marker."), EndFoldout]
        private string _additionalData;

        [SerializeField, HideInInspector]
        private Material _markerBadgeMaterial;
        [SerializeField, HideInInspector]
        private Material _markerLineMaterial;
        [SerializeField, HideInInspector]
        private Material _markerShadowMaterial;

        [SerializeField, HideInInspector]
        private Mesh _badgeMesh;

        private Color _currentColor;

        private SerializableGuid _buildingId;
        private Building _building;
        private static IdLoadScope _buildingLoadScope;

        public override void Recycle()
        {
            base.Recycle();

            _currentColor = Color.clear;

            _buildingId = SerializableGuid.Empty;
            _building = null;
            _buildingLoadScope = null;
        }

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            if (initializingState == InstanceManager.InitializationContext.Editor_Duplicate || initializingState == InstanceManager.InitializationContext.Programmatically_Duplicate)
            {
                _markerBadgeMaterial = null;
                _markerLineMaterial = null;
                _markerShadowMaterial = null;
            }

            InitValue(value => color = value, new Color(188.0f / 255.0f, 22.0f / 255.0f, 22.0f / 255.0f), initializingState);
            InitValue(value => badgeOffset = value, 10.0f, initializingState);
            InitValue(value => icon = value, Icon.CIRCLE, initializingState);
            InitValue(value => additionalData = value, "", initializingState);
        }

#if UNITY_EDITOR
        protected override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();

            //UndoManager.RecordObject does not work with UnityMesh so when we detect an Undo/Redo we push the UVs back onto the UnityMesh in case an icon change was part of the Undo/Redo
            UpdateUILabelMeshUVs();
        }
#endif

        protected override bool UpdateAllDelegates()
        {
            if (base.UpdateAllDelegates())
            {
                RemoveBuildingDelegates();
                if (!IsDisposing())
                    AddBuildingDelegates();

                return true;
            }
            return false;
        }

        protected override void InstanceAddedHandler(IProperty property)
        {
            base.InstanceAddedHandler(property);

            if (property is Building && buildingId == property.id)
                building = property as Building;
        }

        protected void RemoveBuildingDelegates()
        {
            if (!Object.ReferenceEquals(building, null))
                building.DisposeEvent -= BuildingDisposeHandler;
        }

        private void AddBuildingDelegates()
        {
            if (building != Disposable.NULL)
                building.DisposeEvent += BuildingDisposeHandler;
        }

        private void BuildingDisposeHandler(IDisposable disposable)
        {
            building = null;
        }

        public override void OnMouseClickedHit(RaycastHitDouble hit)
        {
            base.OnMouseClickedHit(hit);

            if (buildingId != null && buildingId != SerializableGuid.Empty && parentGeoAstroObject != Disposable.NULL)
            {
                parentGeoAstroObject.transform.IterateOverChildrenObject<DatasourceRoot>((datasourceRoot) =>
                {
                    if (datasourceRoot.generators.Count > 0 && datasourceRoot.generators[0] is IdLoader)
                    {
                        IdLoader idLoader = datasourceRoot.generators[0] as IdLoader;

                        bool hasBuildingFallbackValues = false;

                        idLoader.IterateOverFallbackValues<Building>((buildingFallbackValues) =>
                        {
                            hasBuildingFallbackValues = true;
                            return false;
                        });

                        if (hasBuildingFallbackValues)
                        {
                            Dispose(_buildingLoadScope);

                            idLoader.GetLoadScope(out _buildingLoadScope, buildingId, false, true);

                            return false;
                        }
                    }
                    return true;
                });
            }
        }

        private SerializableGuid buildingId
        {
            get { return _buildingId; }
            set { _buildingId = value; }
        }

        private Building building
        {
            get { return _building; }
            set 
            {
                if (Object.ReferenceEquals(_building, value))
                    return;
                RemoveBuildingDelegates();
                _building = value;
                AddBuildingDelegates();
            }
        }

        /// <summary>
        /// The Marker tint.
        /// </summary>
        [Json]
        public Color color
        {
            get { return _color; }
            set { SetValue(nameof(color), value, ref _color); }
        }

#if UNITY_EDITOR
        protected UnityEngine.Object[] GetUIVisualTransformsAdditionalRecordObjects()
        {
            List<UnityEngine.Object> transforms = new List<UnityEngine.Object>();

            transform.IterateOverChildren<UIVisual>((uiVisual) =>
            {
                foreach (MeshRendererVisual meshRendererVisual in uiVisual.children)
                    transforms.Add(meshRendererVisual.transform);
          
                return true;
            });

            return transforms.ToArray();
        }
#endif

        /// <summary>
        /// The distance between the Marker position and the badge.
        /// </summary>
        [Json]
#if UNITY_EDITOR
        [RecordAdditionalObjects(nameof(GetUIVisualTransformsAdditionalRecordObjects))]
#endif
        public float badgeOffset
        {
            get { return _badgeOffset; }
            set
            {
                SetValue(nameof(badgeOffset), value < 0.0f ? 0.0f : value, ref _badgeOffset, (newValue, oldValue) =>
                {
                    UpdateUILabelTransform();
                });
            }
        }

        /// <summary>
        /// The icon to display in the badge.
        /// </summary>
        [Json]
        public Icon icon
        {
            get { return _icon; }
            set 
            { 
                SetValue(nameof(icon), value, ref _icon, (newValue, oldValue) => 
                {
                    UpdateUILabelMeshUVs();
                }); 
            }
        }

        /// <summary>
        /// General purpose metadata associated with the marker.
        /// </summary>
        [Json]
        public string additionalData
        {
            get { return _additionalData; }
            set { SetValue(nameof(additionalData), value, ref _additionalData); }
        }

        private Color currentColor
        {
            get { return _currentColor; }
            set { SetValue(nameof(currentColor), value, ref _currentColor); }
        }

        private CharacterInfo characterInfo
        {
            get 
            {
                renderingManager.GetCharacterInfoFromFontName(Convert.ToChar(ICONS_UNICODE[(int)icon]), out CharacterInfo characterInfo, MARKER_ICON_FONT_NAME);
                return characterInfo;
            }
        }

        public override float GetCurrentAlpha()
        {
            return building != Disposable.NULL ? 0.0f : currentColor.a * base.GetCurrentAlpha();
        }

        protected override void InitializeMaterial(MeshRendererVisual meshRendererVisual, Material material = null)
        {
            string shaderBasePath = RenderingManager.SHADER_BASE_PATH;

            if (meshRendererVisual.name == BADGE_MESH_NAME)
                material = UpdateMaterial(ref _markerBadgeMaterial, shaderBasePath + "UI/Marker/MarkerBadge");
            else if (meshRendererVisual.name == LINE_MESH_NAME)
                material = UpdateMaterial(ref _markerLineMaterial, shaderBasePath + "UI/UIColor");
            else if (meshRendererVisual.name == SHADOW_MESH_NAME)
                material = UpdateMaterial(ref _markerShadowMaterial, shaderBasePath + "UI/Marker/MarkerShadow");

            base.InitializeMaterial(meshRendererVisual, material);
        }

        protected override UIVisual CreateUIVisual(Type visualType, bool useCollider)
        {
            UIVisual uiVisual = base.CreateUIVisual(visualType, useCollider);

            Mesh quadMesh = renderingManager.quadMesh;
            if (_badgeMesh == Disposable.NULL)
                _badgeMesh = Duplicate(quadMesh);
            CreateMeshRendererVisual(useCollider ? typeof(UIMeshRendererVisualBoxCollider) : typeof(UIMeshRendererVisualNoCollider), BADGE_MESH_NAME, uiVisual.transform).sharedMesh = _badgeMesh.unityMesh;
            CreateMeshRendererVisual(typeof(UIMeshRendererVisualNoCollider), LINE_MESH_NAME, uiVisual.transform).sharedMesh = quadMesh.unityMesh;
            CreateMeshRendererVisual(typeof(UIMeshRendererVisualNoCollider), SHADOW_MESH_NAME, uiVisual.transform).sharedMesh = quadMesh.unityMesh;

            return uiVisual;
        }

        protected override void UpdateVisualProperties()
        {
            base.UpdateVisualProperties();
           
            Color.RGBToHSV(color, out float h, out float s, out float v);
            
            s = GetMouseDown() ? s * 0.3f : GetMouseOver() ? 1.0f : s;
            
            currentColor = Color.HSVToRGB(h, s, v);
        }

        protected override void ApplyPropertiesToVisual(bool visualsChanged, VisualObjectVisualDirtyFlags meshRendererVisualDirtyFlags)
        {
            base.ApplyPropertiesToVisual(visualsChanged, meshRendererVisualDirtyFlags);

            if (visualsChanged)
            {
                UpdateUILabelTransform();
                UpdateUILabelMeshUVs();
            }

            transform.IterateOverChildren<UIVisual>((uiVisual) =>
            {
                foreach (MeshRendererVisual meshRendererVisual in uiVisual.children)
                {
                    if (meshRendererVisual.name == BADGE_MESH_NAME)
                    {
                        UpdateMeshRendererVisualCollider(meshRendererVisual, visualsChanged);
                        InitializeMaterial(meshRendererVisual);
                    }
                    else if (meshRendererVisual.name == LINE_MESH_NAME)
                        InitializeMaterial(meshRendererVisual);
                    else if (meshRendererVisual.name == SHADOW_MESH_NAME)
                        InitializeMaterial(meshRendererVisual);
                }

                return true;
            });
        }

        private void UpdateUILabelTransform()
        {
            if (initialized)
            {
                transform.IterateOverChildren<UIVisual>((uiVisual) =>
                {
                    foreach (MeshRendererVisual meshRendererVisual in uiVisual.children)
                    {
                        if (meshRendererVisual.name == BADGE_MESH_NAME)
                        {
                            meshRendererVisual.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                            meshRendererVisual.transform.localScale = new Vector3(BADGE_SCALE.x, 0.0f, BADGE_SCALE.y);
                            meshRendererVisual.transform.localPosition = new Vector3(0.0f, badgeOffset + meshRendererVisual.transform.localScale.z / 2.0f, 0.0f);
                        }
                        else if (meshRendererVisual.name == LINE_MESH_NAME)
                        {
                            meshRendererVisual.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                            meshRendererVisual.transform.localScale = new Vector3(1.0f, 0.0f, badgeOffset);
                            meshRendererVisual.transform.localPosition = new Vector3(0.0f, badgeOffset / 2.0f, 0.0f);
                        }
                        else if (meshRendererVisual.name == SHADOW_MESH_NAME)
                        {
                            meshRendererVisual.transform.localRotation = Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                            meshRendererVisual.transform.localScale = new Vector3(25.0f, 0.0f, 10.0f);
                            meshRendererVisual.transform.localPosition = new Vector3(0.0f, 0.0f, 0.5f);
                        }
                    }

                    return true;
                });
            }
        }

        private void UpdateUILabelMeshUVs()
        {
            if (initialized)
            {
                if (_badgeMesh != Disposable.NULL)
                {
                    CharacterInfo characterInfo = this.characterInfo;

                    Vector2 scale = new Vector2(0.35f * BADGE_SCALE.y / BADGE_SCALE.x, 0.35f);
                    if (characterInfo.glyphWidth != 0 && characterInfo.glyphHeight != 0)
                    {
                        if (characterInfo.glyphWidth > characterInfo.glyphHeight)
                            scale.y *= (float)characterInfo.glyphHeight / (float)characterInfo.glyphWidth;
                        else
                            scale.x *= (float)characterInfo.glyphWidth / (float)characterInfo.glyphHeight;
                    }

                    Vector2 bottomLeft = new Vector2(Mathf.Min(Mathf.Min(characterInfo.uvBottomLeft.x, characterInfo.uvBottomRight.x), Mathf.Min(characterInfo.uvTopLeft.x, characterInfo.uvTopRight.x)), Mathf.Min(Mathf.Min(characterInfo.uvBottomLeft.y, characterInfo.uvBottomRight.y), Mathf.Min(characterInfo.uvTopLeft.y, characterInfo.uvTopRight.y)));
                    Vector2 topRight = new Vector2(Mathf.Max(Mathf.Max(characterInfo.uvBottomLeft.x, characterInfo.uvBottomRight.x), Mathf.Max(characterInfo.uvTopLeft.x, characterInfo.uvTopRight.x)), Mathf.Max(Mathf.Max(characterInfo.uvBottomLeft.y, characterInfo.uvBottomRight.y), Mathf.Max(characterInfo.uvTopLeft.y, characterInfo.uvTopRight.y)));

                    Vector2 size = new Vector2(topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
                    Vector2 halfSize = size / 2.0f;
                    Vector2 center = new Vector2(bottomLeft.x + halfSize.x, bottomLeft.y + halfSize.y);

                    Vector2[] matrix = new Vector2[] { characterInfo.uvBottomLeft - center, characterInfo.uvTopLeft - center, characterInfo.uvTopRight - center, characterInfo.uvBottomRight - center };
                    Vector2 vec;
                    for (int i = 0; i < matrix.Length; i++)
                    {
                        vec = matrix[i];
                        vec = new Vector2(vec.x > 0.0f ? 1.0f : -1.0f, vec.y > 0.0f ? 1.0f : -1.0f);
                        matrix[i] = vec;
                    }

                    bool flipped = characterInfo.uvBottomLeft.x != characterInfo.uvBottomRight.x;

                    Vector2 offset = new Vector2(0.0f, 0.155f);
                    offset = Rotate(flipped, offset, true);
                    offset = new Vector2(offset.x * size.x, offset.y * size.y);
                    scale = Rotate(flipped, scale);

                    int vertexCount = 4;
                    List<Vector4> uv2 = new List<Vector4>(vertexCount);
                    List<Vector4> uv3 = new List<Vector4>(vertexCount);

                    for (int i = 0; i < vertexCount; ++i)
                    {
                        uv2.Add(center + (((halfSize * matrix[i]) + offset) / scale));
                        uv3.Add(new Vector4(bottomLeft.x, bottomLeft.y, topRight.x, topRight.y));
                    }

                    _badgeMesh.SetUVs(uv2, 2);
                    _badgeMesh.SetUVs(uv3, 3);
                }
            }
        }

        private Vector2 Rotate(bool flip, Vector2 vec, bool invert = false)
        {
            return flip ? new Vector2(invert ? -vec.x : vec.x, vec.y) : new Vector2(invert ? -vec.y : vec.y, vec.x);
        }

        private static Texture2D _shadowTexture;
        private Texture2D GetShadowTexture()
        {
            if (_shadowTexture == null)
                _shadowTexture = Resources.Load<Texture2D>("Texture/Marker/Shadow");
            return _shadowTexture;
        }

        private static Texture2D _badgeLuminosityTexture;
        private Texture2D GetBadgeLuminosityTextureShadowTexture()
        {
            if (_badgeLuminosityTexture == null)
                _badgeLuminosityTexture = Resources.Load<Texture2D>("Texture/Marker/Marker");
            return _badgeLuminosityTexture;
        }

        protected override void ApplyPropertiesToMaterial(MeshRenderer meshRenderer, Material material, MaterialPropertyBlock materialPropertyBlock, double cameraAtmosphereAltitudeRatio, Camera camera, GeoAstroObject closestGeoAstroObject, Star star)
        {
            base.ApplyPropertiesToMaterial(meshRenderer, material, materialPropertyBlock, cameraAtmosphereAltitudeRatio, camera, closestGeoAstroObject, star);

            ApplyFontsToMaterials(material, materialPropertyBlock);

            if (meshRenderer.name == SHADOW_MESH_NAME)
                SetTextureToMaterial("_AlphaMap", GetShadowTexture(), material, materialPropertyBlock);
            if (meshRenderer.name == BADGE_MESH_NAME)
            {
                SetTextureToMaterial("_LuminosityMap", GetBadgeLuminosityTextureShadowTexture(), material, materialPropertyBlock);
                SetColorToMaterial(currentColor, material, materialPropertyBlock);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Dispose(_markerBadgeMaterial);
            Dispose(_markerLineMaterial);
            Dispose(_markerShadowMaterial);
        }
    }
}