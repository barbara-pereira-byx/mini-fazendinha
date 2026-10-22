using System;
using System.Collections.Generic;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmV3ResourceSetup
    {
        const string WaterPath = "Assets/MiniFazendinha/Settings/Inventory/Resources/Water.asset";
        const string AnimalDefs = "Assets/MiniFazendinha/Settings/V2/Animals/";
        const string FoodModel = "Assets/ThirdParty/VoxelFarm/FBX/Environment/TVS_VoxelFarm_FoodTrough.fbx";
        const string WaterModel = "Assets/ThirdParty/VoxelFarm/FBX/Environment/TVS_VoxelFarm_WaterTrough.fbx";
        const string MaterialFolder = "Assets/MiniFazendinha/Materials/Animals";

        [MenuItem("MiniFazendinha/V3.1/Configure animal resources")]
        [MenuItem("MiniFazendinha/V3.1/Configure animal resources")]
        public static void Configure()
        {
            try { ConfigureImpl(); }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("Assets/MiniFazendinha/Settings/V3_ResourceError.txt", ex.ToString());
                throw;
            }
        }

        static void ConfigureImpl()
        {
            var wheat = AssetDatabase.LoadAssetAtPath<CropDefinition>("Assets/MiniFazendinha/Settings/Crops/Wheat.asset");
            var corn = AssetDatabase.LoadAssetAtPath<CropDefinition>("Assets/MiniFazendinha/Settings/Crops/Corn.asset");
            var pumpkin = AssetDatabase.LoadAssetAtPath<CropDefinition>("Assets/MiniFazendinha/Settings/Crops/Pumpkin.asset");
            if (wheat == null || corn == null || pumpkin == null) throw new InvalidOperationException("Expected wheat, corn, and pumpkin CropDefinitions.");
            EnsureFolder("Assets/MiniFazendinha/Settings/Inventory/Resources");
            var water = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(WaterPath);
            if (water == null)
            {
                water = ScriptableObject.CreateInstance<InventoryItemDefinition>();
                AssetDatabase.CreateAsset(water, WaterPath);
            }
            Set(water, "identifier", "water");
            Set(water, "displayName", "Água");

            ConfigureDefinition("Cow", wheat, water);
            ConfigureDefinition("Chicken", corn, water);
            ConfigureDefinition("Sheep", wheat, water);
            ConfigureDefinition("Pig", pumpkin, water);
            ConfigureDefinition("Duck", corn, water);

            ConfigurePalettes();
            var scene = SceneManager.GetActiveScene();
            var wellObject = GameObject.Find("Poco");
            if (wellObject == null) throw new InvalidOperationException("Pandazole well object 'Poco' was not found.");
            var well = wellObject.GetComponent<WaterWellController>() ?? Undo.AddComponent<WaterWellController>(wellObject);
            Set(well, "waterItem", water);
            Set(well, "productionAmount", 10);
            Set(well, "rechargeSeconds", 30f);
            Set(well, "startsReady", true);

            var animalsRoot = GameObject.Find("06_Animais");
            var inventory = UnityEngine.Object.FindAnyObjectByType<FarmInventory>();
            if (animalsRoot == null || inventory == null) throw new InvalidOperationException("Animal root or FarmInventory missing.");
            AddInitialItem(inventory, water, 0);

            var care = new Dictionary<string, FeedingPoint>();
            care["Wheat"] = FindOrCreatePoint(animalsRoot.transform, "Comedouro", FeedingPointKind.Food, wheat, FoodModel, new Vector3(14.2f, .05f, -10.5f));
            care["Corn"] = FindOrCreatePoint(animalsRoot.transform, "Comedouro_Milho", FeedingPointKind.Food, corn, FoodModel, new Vector3(16.1f, .05f, -10.5f));
            care["Pumpkin"] = FindOrCreatePoint(animalsRoot.transform, "Comedouro_Abobora", FeedingPointKind.Food, pumpkin, FoodModel, new Vector3(16.1f, .05f, -6.5f));
            care["Water"] = FindOrCreatePoint(animalsRoot.transform, "Bebedouro", FeedingPointKind.Water, water, WaterModel, new Vector3(12.1f, .05f, -10.5f));

            foreach (var animal in animalsRoot.GetComponentsInChildren<FarmAnimalController>(true))
            {
                string name = animal.name;
                string food = name.StartsWith("Vaca", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Ovelha", StringComparison.OrdinalIgnoreCase) ? "Wheat" :
                    name.StartsWith("Porco", StringComparison.OrdinalIgnoreCase) ? "Pumpkin" : "Corn";
                Set(animal, "feedingPoints", new[] { care[food], care["Water"] });
                EditorUtility.SetDirty(animal);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MiniFazendinha V3.1] Animal food and water linked to crop inventory; well supplies 10 water every 30 seconds.");
        }

        static void ConfigureDefinition(string species, InventoryItemDefinition feed, InventoryItemDefinition water)
        {
            string path = AnimalDefs + species + ".asset";
            var definition = AssetDatabase.LoadAssetAtPath<AnimalDefinition>(path);
            if (definition == null) throw new InvalidOperationException("Animal definition not found: " + path);
            Set(definition, "feedItem", feed);
            Set(definition, "waterItem", water);
            EditorUtility.SetDirty(definition);
        }

        static FeedingPoint FindOrCreatePoint(Transform root, string name, FeedingPointKind kind,
            InventoryItemDefinition item, string modelPath, Vector3 position)
        {
            var pointObject = root.Find(name)?.gameObject;
            if (pointObject == null)
            {
                pointObject = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(pointObject, "Create animal care point");
                pointObject.transform.SetParent(root, false);
                pointObject.transform.position = position;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null) throw new InvalidOperationException("Care point model missing: " + modelPath);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Visual";
                visual.transform.SetParent(pointObject.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * .5f;
            }
            pointObject.transform.position = position;
            var rendererMaterial = GetPropMaterial(kind == FeedingPointKind.Food ? "FoodTrough" : "WaterTrough");
            foreach (var renderer in pointObject.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = rendererMaterial;
            var collider = pointObject.GetComponent<BoxCollider>() ?? pointObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, .3f, 0f);
            collider.size = new Vector3(1.2f, .7f, 1.2f);
            var point = pointObject.GetComponent<FeedingPoint>() ?? pointObject.AddComponent<FeedingPoint>();
            Set(point, "kind", kind);
            Set(point, "acceptedItem", item);
            Set(point, "initialUnits", 0);
            Set(point, "capacity", 20);
            return point;
        }

        static void ConfigurePalettes()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            string[] names = { "FoodTrough", "WaterTrough" };
            foreach (var name in names)
            {
                string texturePath = "Assets/ThirdParty/VoxelFarm/Textures/Environment/TVS_VoxelFarm_" + name + "_Texture.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null) throw new InvalidOperationException("Trough palette missing: " + texturePath);
                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer != null && (importer.mipmapEnabled || importer.filterMode != FilterMode.Point || importer.wrapMode != TextureWrapMode.Clamp))
                {
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.SaveAndReimport();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                }
                string path = MaterialFolder + "/VoxelFarm_" + name + "_URP.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(material);
            }
        }

        static Material GetPropMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/VoxelFarm_" + name + "_URP.mat");

        static void AddInitialItem(FarmInventory inventory, InventoryItemDefinition item, int amount)
        {
            var so = new SerializedObject(inventory);
            var array = so.FindProperty("initialItems");
            for (int i = 0; i < array.arraySize; i++)
                if (array.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue == item) return;
            int index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            var row = array.GetArrayElementAtIndex(index);
            row.FindPropertyRelative("item").objectReferenceValue = item;
            row.FindPropertyRelative("quantity").intValue = amount;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inventory);
        }

        static void Set(UnityEngine.Object target, string property, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(property);
            if (p == null) throw new InvalidOperationException("Serialized field not found: " + property);
            if (value is UnityEngine.Object obj) p.objectReferenceValue = obj;
            else if (value is string text) p.stringValue = text;
            else if (value is int integer) p.intValue = integer;
            else if (value is float number) p.floatValue = number;
            else if (value is bool boolean) p.boolValue = boolean;
            else if (value is FeedingPointKind kind) p.enumValueIndex = (int)kind;
            else if (value is FeedingPoint[] points)
            {
                p.arraySize = points.Length;
                for (int i=0; i<points.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
            }
            else throw new InvalidOperationException("Unsupported field value: " + property);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}