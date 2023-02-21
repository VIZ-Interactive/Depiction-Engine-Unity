// Copyright (C) 2023 by VIZ Interactive Media Inc. https://github.com/VIZ-Interactive | Licensed under MIT license (see LICENSE.md for details)

using System;
using System.Reflection;
using UnityEngine;

namespace DepictionEngine
{
    /// <summary>
    /// File system based datasource
    /// </summary>
    [AddComponentMenu(SceneManager.NAMESPACE + "/Datasource/" + nameof(FileSystemDatasource))]
    public class FileSystemDatasource : DatasourceBase
    {
        [BeginFoldout("Namespace")]
#if UNITY_EDITOR
        [SerializeField, Button(nameof(CreateBtn)), ConditionalShow(nameof(IsNotFallbackValues)), BeginHorizontalGroup(true)]
        private bool _create;
        [SerializeField, Button(nameof(DeleteBtn)), ConditionalShow(nameof(IsNotFallbackValues)), EndHorizontalGroup]
        private bool _delete;
#endif
        [SerializeField, EndFoldout]
        public string _databaseNamespace;

#if UNITY_EDITOR
        private void CreateBtn()
        {
            //Create DB scheme for namespace if it doesn't exist

            //TRANSFORM------------------------------------------------

            //CreateTable(typeof(TransformDouble));

            ////OBJECT---------------------------------------------------

            //CreateTable(typeof(TerrainGridMeshObject));
            //CreateTable(typeof(UnityEngine.Texture));
            //CreateTable(typeof(Elevation));
            //CreateTable(typeof(Object));
            //CreateTable(typeof(GeoAstroObject));
            //CreateTable(typeof(Star));
            //CreateTable(typeof(Building));
            //CreateTable(typeof(LevelMeshObject));
            //CreateTable(typeof(BuildingGridMeshObject));
            //CreateTable(typeof(Marker));
            //CreateTable(typeof(Label));
            //CreateTable(typeof(StarSystem));
            //CreateTable(typeof(Camera));

            ////SCRIPT---------------------------------------------------

            ////FallbackValues
            //CreateTable(typeof(TerrainGridMeshObjectFallbackValues));
            //CreateTable(typeof(TextureFallbackValues));
            //CreateTable(typeof(ElevationFallbackValues));
            //CreateTable(typeof(ObjectFallbackValues));
            //CreateTable(typeof(LevelMeshObjectFallbackValues));
            //CreateTable(typeof(BuildingGridMeshObjectFallbackValues));
            //CreateTable(typeof(MarkerFallbackValues));
            //CreateTable(typeof(LabelFallbackValues));

            ////Datasource
            //CreateTable(typeof(RemoteDatasource));
            //CreateTable(typeof(LocalDatasource));

            ////Effect
            //CreateTable(typeof(AtmosphereEffect));
            //CreateTable(typeof(TerrainSurfaceReflectionEffect));

            ////Loader
            //CreateTable(typeof(Index2DLoader));
            //CreateTable(typeof(IdLoader));
            //CreateTable(typeof(CameraGrid2DLoader));
            //CreateTable(typeof(RectangleGridLoader));
            //CreateTable(typeof(FillGrid2DLoader));

            ////Controller
            //CreateTable(typeof(OrbitController));
            //CreateTable(typeof(TargetController));
            //CreateTable(typeof(CameraController));
            //CreateTable(typeof(GeoCoordinateController));

            ////Animator
            //CreateTable(typeof(TargetControllerAnimator));
            //CreateTable(typeof(StarSystemAnimator));
            //CreateTable(typeof(TransformAnimator));
            //CreateTable(typeof(GeoCoordinateControllerAnimator));
        }

        private void CreateTable(Type type)
        {
            string createCommandText = "CREATE TABLE " + type.Name + " (";

            Action<int, FieldInfo> addFieldDefinition = (key, fieldInfo) =>
            {
                //createCommandText += GetSQLLiteFieldDefinition(key, fieldInfo) + ", ";
            };
            type.GetMethod("IterateOverTypeFieldInfos").Invoke(null, new object[] { addFieldDefinition });

            createCommandText += ");";
        }

        private string GetSQLLiteFieldDefinition(JsonAttribute jsonAttribute, string name, MemberInfo memberInfo)
        {
            Type type = null;

            if (memberInfo is FieldInfo)
                type = (memberInfo as FieldInfo).FieldType;
            else if (memberInfo is PropertyInfo)
                type = (memberInfo as PropertyInfo).PropertyType;

            if (type != null && !string.IsNullOrEmpty(name))
            {
                if (type == typeof(bool))
                    return name + " NUMERIC";
                else if (type == typeof(int) || type == typeof(uint))
                    return name + " INTEGER";
                else if (type == typeof(float) || type == typeof(double))
                    return name + " REAL";
                else if (type == typeof(char))
                    return name + " TEXT";
                else if (type == typeof(string))
                    return name + " TEXT";
                else if (type == typeof(SerializableGuid))
                    return name + " BLOB";
                else if (typeof(IProperty).IsAssignableFrom(type))
                    return name + " BLOB";
                else if (type.IsEnum)
                    return name + " INTEGER";
                else if (type == typeof(Vector2) || type == typeof(Vector2Double))
                    return name + "X REAL " + name + "Y REAL";
                else if (type == typeof(Vector2Int))
                    return name + "X INTEGER " + name + "Y INTEGER";
                else if (type == typeof(Vector3) || type == typeof(Vector3Double))
                    return name + "X REAL " + name + "Y REAL " + name + "Z REAL";
                else if (type == typeof(Vector3Int))
                    return name + "X INTEGER " + name + "Y INTEGER " + name + "Z INTEGER";
                else if (type == typeof(Vector4) || type == typeof(Vector4Double))
                    return name + "X REAL " + name + "Y REAL " + name + "Z REAL " + name + "W REAL";
                else if (type == typeof(GeoCoordinate3) || type == typeof(GeoCoordinate3Double))
                    return name + "Lat REAL " + name + "Long REAL " + name + "Alt REAL";
                else if (type == typeof(GeoCoordinate2) || type == typeof(GeoCoordinate2Double))
                    return name + "Lat REAL " + name + "Long REAL";
                else if (type == typeof(Color))
                    return name + "R REAL " + name + "G REAL " + name + "B REAL";
                else if (type == typeof(Quaternion) || type == typeof(QuaternionDouble))
                    return name + "X REAL " + name + "Y REAL " + name + "Z REAL " + name + "W REAL";
                else if (type == typeof(Grid2DIndex))
                    return name + "IndexX INTEGER " + name + "IndexY INTEGER " + name + "DimensionsX INTEGER " + name + "DimensionsY INTEGER";
                else
                    Debug.LogError("Unsupported FieldType: " + name + ", " + type);
            }

            return null;
        }

        private void DeleteBtn()
        {
            //Delete the DB scheme corresponding to the namespace
        }
#endif

        protected override void InitializeSerializedFields(InstanceManager.InitializationContext initializingState)
        {
            base.InitializeSerializedFields(initializingState);

            string sceneName = gameObject.scene.name;
            InitValue(value => databaseNamespace = value, (!string.IsNullOrEmpty(sceneName) ? sceneName : UnityEngine.Random.Range(0, 1000000)) + "_db", initializingState);
        }

        public override string GetDatasourceName()
        {
            return base.GetDatasourceName() + GetNamespace();
        }

        /// <summary>
        /// Value used to identify which database to use. 
        /// </summary>
        [Json]
        public string databaseNamespace
        {
            get { return _databaseNamespace; }
            set { SetValue(nameof(databaseNamespace), value, ref _databaseNamespace); }
        }

        private string GetNamespace()
        {
            return _databaseNamespace;
        }
    }
}
