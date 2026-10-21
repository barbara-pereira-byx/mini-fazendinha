using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmSceneBuilder
    {
        const string Root = "Assets/MiniFazendinha/";
        const string Bundle = "Assets/Pandazole_Lowpoly_Asset_Bundle/";
        const string Farm = Bundle + "Pandazole Farm Ranch Pack/Prefabs/";
        const string Nature = Bundle + "Pandazole Nature Environment Pack/Prefabs/";
        const string ScenePath = Root + "Scenes/FarmMain.unity";
        static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        static readonly List<GameObject> instances = new List<GameObject>();
        static readonly Dictionary<string, string> sourceHashes = new Dictionary<string, string>();
        static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))); }
        static void GuardOriginal(string path)
        {
            if (File.Exists(path) && !sourceHashes.ContainsKey(path)) sourceHashes[path] = Hash(path);
        }
        [MenuItem("MiniFazendinha/Build FarmMain")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Editor must be ready in Edit Mode.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene changes: build cancelled.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("FarmMain already exists. Refusing to overwrite.");
            string[] required = { Farm+"Bld_FarmerHouse.prefab", Farm+"Bld_Barn_01.prefab", Farm+"Env_FarmLand_03.prefab", Farm+"Env_WoodFence_05.prefab", Nature+"Tree_02_Spring.prefab", Nature+"Bush_04.prefab", Nature+"Flower_23.prefab", Farm+"Env_Well_01.prefab", Farm+"Prop_Berrel_03.prefab", Farm+"Prop_Haystack_01.prefab", Farm+"Prop_WoodenCrates_01.prefab", Farm+"Prop_WateringCan_01.prefab", Farm+"Prop_Wheelbarrow.prefab", Farm+"Env_GrassPlant_06.prefab" };
            foreach (string path in required)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new FileNotFoundException(path);
                foreach (string dep in AssetDatabase.GetDependencies(path, true)) { GuardOriginal(dep); GuardOriginal(dep+".meta"); }
            }
            GuardOriginal("Assets/Scenes/SampleScene.unity"); GuardOriginal("Assets/Scenes/SampleScene.unity.meta");
            foreach (string file in Directory.GetFiles("ProjectSettings")) GuardOriginal(file);
            GuardOriginal("Packages/manifest.json"); GuardOriginal("Packages/packages-lock.json");
            converted.Clear(); instances.Clear();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var farmRoot = new GameObject("MiniFazendinha").transform;
            Transform terrain = Group("01_Terreno_e_Caminhos", farmRoot);
            Transform buildings = Group("02_Construcoes", farmRoot);
            Transform plots = Group("03_Canteiros", farmRoot);
            Transform fences = Group("04_Cercas", farmRoot);
            Transform trees = Group("05_Arvores", farmRoot);
            Transform bushes = Group("06_Arbustos_e_Flores", farmRoot);
            Transform decor = Group("07_Decoracoes", farmRoot);
            Transform crops = Group("08_Vegetacao_Estatica", farmRoot);
            Transform lighting = Group("09_Iluminacao", farmRoot);
            Transform cameras = Group("10_Camera", farmRoot);
            var grass = Material("Grass_Mint", new Color(0.44f,0.68f,0.28f));
            var earth = Material("Earth_Edge", new Color(0.47f,0.32f,0.21f));
            var pathMat = Material("Path_Sand", new Color(0.79f,0.66f,0.43f));
            var meadow = Material("Pasture_Grass", new Color(0.52f,0.73f,0.33f));
            Cube("Terra_Base", terrain, new Vector3(0,-0.48f,0), new Vector3(40,0.55f,40), earth);
            Cube("Terreno_Verde_40x40", terrain, new Vector3(0,-0.12f,0), new Vector3(40,0.24f,40), grass);
            Cube("Caminho_Principal", terrain, new Vector3(-0.5f,0.012f,3), new Vector3(35,0.024f,1.8f), pathMat);
            Cube("Caminho_Horta", terrain, new Vector3(6.7f,0.012f,-7.7f), new Vector3(1.6f,0.024f,19.6f), pathMat);
            Cube("Acesso_Casa", terrain, new Vector3(-10.5f,0.012f,5.8f), new Vector3(1.8f,0.024f,4), pathMat);
            Cube("Acesso_Celeiro", terrain, new Vector3(7,0.012f,4.5f), new Vector3(1.8f,0.024f,2), pathMat);
            Cube("Gramado_Pastagem", terrain, new Vector3(13.5f,0.012f,-6.5f), new Vector3(8.7f,0.024f,14.7f), meadow);
            Place(Farm+"Bld_FarmerHouse.prefab", "Casa_Principal", buildings, -10.5f,11,180);
            Place(Farm+"Bld_Barn_01.prefab", "Celeiro", buildings, 7,11,180);
            for (int row=0;row<3;row++) for (int col=0;col<4;col++)
            {
                float x=-14.7f+col*5.6f, z=-13.6f+row*5.6f;
                var bed=Place(Farm+"Env_FarmLand_03.prefab", "Canteiro_"+(row*4+col+1).ToString("00"), plots, x,z,0);
                if ((row+col)%2==0)
                {
                    float surface=BoundsOf(bed).max.y;
                    for(int a=0;a<3;a++) for(int b=0;b<3;b++)
                        Place(Farm+"Env_GrassPlant_06.prefab", "Muda_Decorativa_"+(row*4+col+1)+"_"+a+b, crops, x+(a-1)*1.2f,z+(b-1)*1.2f, (a+b)*35, surface);
                }
            }
            // Three 3m panels across, five along. Leave one panel open for the pasture entrance.
            for(int i=0;i<3;i++)
            {
                Place(Farm+"Env_WoodFence_05.prefab", "Cerca_Sul_"+i, fences,10.5f+i*3,-14,0,0.025f);
                Place(Farm+"Env_WoodFence_05.prefab", "Cerca_Norte_"+i,fences,10.5f+i*3,1,0,0.025f);
            }
            for(int i=0;i<5;i++)
            {
                Place(Farm+"Env_WoodFence_05.prefab", "Cerca_Leste_"+i,fences,18,-12.5f+i*3,90,0.025f);
                if(i!=3) Place(Farm+"Env_WoodFence_05.prefab", "Cerca_Oeste_"+i,fences,9,-12.5f+i*3,90,0.025f);
            }
            float[,] treePositions={{-17.5f,16.5f},{-17.5f,11},{-4,17.5f},{1,17.5f},{15.5f,16.5f},{17.5f,9},{-18,5},{-18,-18},{-10,-18.5f},{0,-18.5f},{15.8f,-17.5f}};
            for(int i=0;i<treePositions.GetLength(0);i++) Place(Nature+"Tree_02_Spring.prefab","Arvore_"+(i+1).ToString("00"),trees,treePositions[i,0],treePositions[i,1],i*47);
            float[,] shrubPositions={{-15.5f,6.5f},{-14.2f,6.5f},{-6,6.5f},{-4.7f,6.5f},{-17.8f,7.5f},{16.8f,5.4f},{15.5f,5.4f},{-18,-10},{-18,-11.4f},{-5,-18.2f},{-3.6f,-18.2f},{17,-16.2f}};
            for(int i=0;i<shrubPositions.GetLength(0);i++) Place(Nature+"Bush_04.prefab","Arbusto_"+(i+1).ToString("00"),bushes,shrubPositions[i,0],shrubPositions[i,1],i*29);
            for(int i=0;i<16;i++)
            {
                float x=i<8 ? -15.5f+i*1.3f : -15.5f+(i-8)*1.3f;
                float z=i<8 ? 5.3f : 4.6f;
                Place(Nature+"Flower_23.prefab","Flor_"+(i+1).ToString("00"),bushes,x,z,i*61);
            }
            Place(Farm+"Env_Well_01.prefab","Poco",decor,-1,8,20);
            Place(Farm+"Prop_Wheelbarrow.prefab","Carrinho_de_Mao",decor,-2,5,15);
            Place(Farm+"Prop_WateringCan_01.prefab","Regador",decor,-4.1f,1.2f,25);
            Place(Farm+"Prop_Berrel_03.prefab","Barril",decor,-5,10,0);
            Place(Farm+"Prop_WoodenCrates_01.prefab","Caixote",decor,-5,11.6f,15);
            Place(Farm+"Prop_WoodenCrates_01.prefab","Caixote_Celeiro",decor,12.2f,8,5);
            Place(Farm+"Prop_Haystack_01.prefab","Feno_01",decor,15,0,-10,0.025f);
            Place(Farm+"Prop_Haystack_01.prefab","Feno_02",decor,16.2f,0,15,0.025f);
            Place(Farm+"Prop_Haystack_01.prefab","Feno_03",decor,15.8f,-1.5f,75,0.025f);
            var sunObject=new GameObject("Sol_Suave"); sunObject.transform.SetParent(lighting); sunObject.transform.rotation=Quaternion.Euler(48,-35,0);
            var sun=sunObject.AddComponent<Light>(); sun.type=LightType.Directional; sun.color=new Color(1f,0.94f,0.83f); sun.intensity=1.35f; sun.shadows=LightShadows.Soft; sun.shadowStrength=0.48f; sun.shadowBias=0.06f; sun.shadowNormalBias=0.25f;
            RenderSettings.sun=sun; RenderSettings.skybox=null; RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(0.72f,0.82f,0.88f); RenderSettings.ambientEquatorColor=new Color(0.62f,0.70f,0.54f); RenderSettings.ambientGroundColor=new Color(0.43f,0.48f,0.35f); RenderSettings.ambientIntensity=1f; RenderSettings.fog=false;
            var cameraObject=new GameObject("Main Camera"); cameraObject.transform.SetParent(cameras); cameraObject.tag="MainCamera";
            cameraObject.transform.rotation=Quaternion.Euler(60,20,0); cameraObject.transform.position=-cameraObject.transform.forward*32;
            var camera=cameraObject.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=24; camera.nearClipPlane=0.1f; camera.farClipPlane=150; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(0.74f,0.85f,0.79f); camera.allowHDR=true;
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<MiniFazendinha.FarmCameraController>();
            camera.orthographicSize=23;
            var cameraData=cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(); cameraData.renderPostProcessing=false;
            if(SceneView.lastActiveSceneView!=null) { SceneView.lastActiveSceneView.LookAt(Vector3.zero, cameraObject.transform.rotation,32,false,true); SceneView.lastActiveSceneView.Repaint(); }
            AssetDatabase.SaveAssets();
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new IOException("Failed saving FarmMain.");
            Validate();
            Debug.Log("MiniFazendinha: FarmMain saved successfully.");
        }
        static Transform Group(string name,Transform parent) { var g=new GameObject(name);g.transform.SetParent(parent,false);return g.transform; }
        static Material Material(string name,Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0.05f);m.SetFloat("_Metallic",0);AssetDatabase.CreateAsset(m,Root+"Materials/"+name+".mat");return m;
        }
        static void Cube(string name,Transform parent,Vector3 center,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=center;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static Material Compatible(Material original)
        {
            if(original==null || original.shader.name.StartsWith("Universal Render Pipeline/")) return original;
            if(converted.TryGetValue(original,out var found)) return found;
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            string pack=AssetDatabase.GetAssetPath(original).Contains("Nature Environment")?"Nature":"Farm";
            m.name="Pandazole_"+pack+"_URP";
            if(original.HasProperty("_MainTex")) { m.SetTexture("_BaseMap",original.GetTexture("_MainTex"));m.SetTextureScale("_BaseMap",original.GetTextureScale("_MainTex"));m.SetTextureOffset("_BaseMap",original.GetTextureOffset("_MainTex")); }
            m.SetColor("_BaseColor",original.HasProperty("_Color")?original.GetColor("_Color"):Color.white);
            m.SetFloat("_Smoothness",0.06f);m.SetFloat("_Metallic",0);
            if(original.HasProperty("_Cutoff"))m.SetFloat("_Cutoff",original.GetFloat("_Cutoff"));
            if(original.IsKeywordEnabled("_ALPHATEST_ON")) {m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");}
            AssetDatabase.CreateAsset(m,Root+"Materials/"+m.name+".mat");converted[original]=m;return m;
        }
        internal static Bounds BoundsOf(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidOperationException("No renderer: "+go.name);
            var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
        }
        static GameObject Place(string path,string name,Transform parent,float x,float z,float yaw,float ground=0)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new FileNotFoundException(path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);go.name=name;
            go.transform.rotation=Quaternion.Euler(0,yaw,0)*asset.transform.rotation;
            go.transform.position=new Vector3(x,0,z);
            var bounds=BoundsOf(go);go.transform.position+=new Vector3(x-bounds.center.x,ground-bounds.min.y,z-bounds.center.z);
            foreach(var r in go.GetComponentsInChildren<Renderer>()) {r.sharedMaterials=r.sharedMaterials.Select(Compatible).ToArray();r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            instances.Add(go);return go;
        }
        [MenuItem("MiniFazendinha/Finalize Camera View")]
        public static void FinalizeView()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("FarmMain Edit Mode required.");
            var controller=UnityEngine.Object.FindFirstObjectByType<MiniFazendinha.FarmCameraController>();
            Undo.RecordObject(controller,"Finalize farm camera");Undo.RecordObject(controller.transform,"Finalize farm camera");
            var serialized=new SerializedObject(controller);serialized.FindProperty("cameraDistance").floatValue=32;serialized.ApplyModifiedProperties();
            controller.transform.position=controller.FocusPoint-controller.transform.forward*32;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Farm camera positioned inside existing URP shadow distance; global settings unchanged. Pipeline: "+UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
        }
        [MenuItem("MiniFazendinha/Validate FarmMain")]
        public static void Validate()
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new InvalidOperationException("FarmMain must be active.");
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var roots=all.Select(t=>t.gameObject).Where(PrefabUtility.IsAnyPrefabInstanceRoot).ToArray();
            var invalid=all.SelectMany(t=>t.GetComponents<Renderer>()).SelectMany(r=>r.sharedMaterials).Where(m=>m==null||!m.shader.name.StartsWith("Universal Render Pipeline/")).Count();
            var changes=sourceHashes.Where(pair=>!File.Exists(pair.Key)||Hash(pair.Key)!=pair.Value).Select(pair=>pair.Key).ToArray();
            var report=new System.Text.StringBuilder();
            report.AppendLine("Scene: "+scene.path+" | dirty="+scene.isDirty);
            report.AppendLine("Prefab instances: "+roots.Length+" | beds="+all.Count(t=>t.name.StartsWith("Canteiro_"))+" | missingScripts="+missing+" | incompatibleMaterials="+invalid);
            report.AppendLine("Original source/settings integrity: "+(changes.Length==0?"PASS":"FAIL "+string.Join(", ",changes))+" | checked="+sourceHashes.Count);
            foreach(var group in roots.GroupBy(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot).OrderBy(g=>g.Key))report.AppendLine(group.Count()+" x "+group.Key);
            foreach(var obj in roots) {var b=BoundsOf(obj); if(b.min.x < -20.05f||b.max.x>20.05f||b.min.z< -20.05f||b.max.z>20.05f)report.AppendLine("OUTSIDE FARM: "+obj.name+" "+b);}
            File.WriteAllText("Library/MiniFazendinha-Validation.txt",report.ToString());
            if(missing>0||invalid>0||changes.Length>0)throw new InvalidOperationException(report.ToString());
        }
    }
}