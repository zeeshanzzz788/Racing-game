#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VelocityRush.AudioSystem;
using VelocityRush.CameraSystem;
using VelocityRush.Cars;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Endless;
using VelocityRush.Input;
using VelocityRush.Polish;
using VelocityRush.Race;
using VelocityRush.TrackSystem;
using VelocityRush.UI;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Creates a runnable, primitive-art prototype so a fresh clone has real scenes, cars, data,
    /// controls and URP wiring. It never imports third-party art; replace generated assets freely.
    /// </summary>
    public static class VelocityRushProjectBootstrapper
    {
        private const string Root = "Assets";
        private const string ResourceData = "Assets/Resources/Data";
        private const string SceneFolder = "Assets/Scenes";
        private static readonly string[] SceneNames =
        {
            "MainMenu", "Garage", "LevelSelect", "DesertCircuit", "CityCircuit", "CoastCircuit", "MountainSprint", "HarborRun", "EndlessRun"
        };

        [MenuItem("Velocity Rush/Create Prototype Content", priority = 1)]
        public static void CreatePrototypeContent()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Exit Play Mode before creating Velocity Rush prototype content.");
                return;
            }

            EnsureFolders();
            UniversalRenderPipelineAsset pipeline = CreateUrpAssets();
            Material roadMaterial = CreateMaterial("Assets/Art/Materials/M_Road.mat", new Color(.07f, .08f, .10f), .15f, .05f);
            Material groundMaterial = CreateMaterial("Assets/Art/Materials/M_Ground.mat", new Color(.25f, .18f, .10f), 0f, .9f);
            GameObject carPrefab = CreateCarPrefab();
            TrackPiece[] modularPieces = CreateModularTrackPiecePrefabs(roadMaterial);
            GameObject coinPrefab = CreateCollectiblePrefab("Coin", new Color(1f, .72f, .05f), CollectibleType.Coin);
            GameObject nitroPrefab = CreateCollectiblePrefab("NitroPickup", new Color(.1f, .8f, 1f), CollectibleType.Nitro);
            GameObject repairPrefab = CreateRepairPowerUpPrefab();
            GameObject hazardPrefab = CreateHazardPrefab();
            CarDefinition[] cars = CreateCars(carPrefab);
            TrackDefinition[] tracks = CreateTracks();
            CreateCampaign(tracks);

            CreateMainMenuScene(tracks[0]);
            CreateGarageScene();
            CreateLevelSelectScene();
            CreateRaceScene("DesertCircuit", new Color(.42f, .26f, .10f), roadMaterial, groundMaterial);
            CreateRaceScene("CityCircuit", new Color(.15f, .19f, .23f), roadMaterial, groundMaterial);
            CreateRaceScene("CoastCircuit", new Color(.12f, .32f, .38f), roadMaterial, groundMaterial);
            CreateRaceScene("MountainSprint", new Color(.22f, .31f, .20f), roadMaterial, groundMaterial);
            CreateRaceScene("HarborRun", new Color(.10f, .18f, .29f), roadMaterial, groundMaterial);
            CreateEndlessScene(modularPieces, coinPrefab, nitroPrefab, repairPrefab, hazardPrefab);
            ConfigureBuildSettings();
            ConfigurePlayerSettings();

            GraphicsSettings.renderPipelineAsset = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Velocity Rush prototype created. Open Assets/Scenes/MainMenu.unity and press Play.");
            EditorUtility.DisplayDialog("Velocity Rush", "Prototype content and Build Settings were created.\n\nOpen MainMenu and press Play. See Docs/SETUP.md before making a mobile build.", "OK");
        }

        [MenuItem("Velocity Rush/Open Setup Guide", priority = 20)]
        private static void OpenSetupGuide()
        {
            Object guide = AssetDatabase.LoadAssetAtPath<Object>("Docs/SETUP.md");
            if (guide != null) AssetDatabase.OpenAsset(guide);
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Resources/Data/Cars", "Assets/Resources/Data/Tracks", "Assets/Resources/Data/Campaign",
                "Assets/Resources/Prefabs", "Assets/Resources/Prefabs/TrackPieces", "Assets/Settings/Rendering", "Assets/Art/Materials", "Assets/Prefabs/Cars", "Assets/Prefabs/TrackPieces"
            };
            foreach (string folder in folders) EnsureFolder(folder);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static UniversalRenderPipelineAsset CreateUrpAssets()
        {
            const string rendererPath = "Assets/Settings/Rendering/VelocityRush_Renderer.asset";
            const string pipelinePath = "Assets/Settings/Rendering/VelocityRush_URP.asset";
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            SerializedObject serialized = new SerializedObject(pipeline);
            SerializedProperty renderers = serialized.FindProperty("m_RendererDataList");
            if (renderers != null)
            {
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }
            SerializedProperty defaultRenderer = serialized.FindProperty("m_DefaultRendererIndex");
            if (defaultRenderer != null) defaultRenderer.intValue = 0;
            SerializedProperty msaa = serialized.FindProperty("m_MSAA");
            if (msaa != null) msaa.intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static VolumeProfile CreateMobilePolishProfile()
        {
            const string path = "Assets/Settings/Rendering/VP_MobilePolish.asset";
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.18f);
            bloom.threshold.Override(1.15f);
            bloom.scatter.Override(.55f);
            if (!profile.TryGet(out ColorAdjustments colorAdjustments)) colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.postExposure.Override(0f);
            colorAdjustments.contrast.Override(4f);
            colorAdjustments.saturation.Override(3f);
            if (!profile.TryGet(out MotionBlur motionBlur)) motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.intensity.Override(.08f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static Material CreateSkyboxMaterial()
        {
            const string path = "Assets/Art/Materials/M_Skybox_Procedural.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null) return null;
            material = new Material(shader);
            if (material.HasProperty("_Exposure")) material.SetFloat("_Exposure", 1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateMaterial(string path, Color color, float metallic, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateCarPrefab()
        {
            const string path = "Assets/Prefabs/Cars/PF_PrototypeCar.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("PrototypeCar");
            root.AddComponent<Rigidbody>();
            BoxCollider bodyCollider = root.AddComponent<BoxCollider>();
            bodyCollider.center = new Vector3(0f, .65f, 0f);
            bodyCollider.size = new Vector3(1.7f, .7f, 3.4f);
            root.AddComponent<CarController>();
            root.AddComponent<CarVisualPolish>();
            root.AddComponent<CarCinematicPolish>();
            root.AddComponent<CarAudioController>();
            root.AddComponent<CarEffectsController>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "BodyVisual";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, .66f, 0f);
            body.transform.localScale = new Vector3(1.65f, .62f, 3.25f);
            Object.DestroyImmediate(body.GetComponent<BoxCollider>());

            GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "CabinVisual";
            cabin.transform.SetParent(root.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 1.12f, -.15f);
            cabin.transform.localScale = new Vector3(1.3f, .45f, 1.55f);
            Object.DestroyImmediate(cabin.GetComponent<BoxCollider>());

            CreateWheel(root.transform, "Wheel_FL", new Vector3(-.83f, .37f, 1.18f));
            CreateWheel(root.transform, "Wheel_FR", new Vector3(.83f, .37f, 1.18f));
            CreateWheel(root.transform, "Wheel_RL", new Vector3(-.83f, .37f, -1.18f));
            CreateWheel(root.transform, "Wheel_RR", new Vector3(.83f, .37f, -1.18f));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateWheel(Transform parent, string name, Vector3 localPosition)
        {
            GameObject wheelRoot = new GameObject(name);
            wheelRoot.transform.SetParent(parent, false);
            wheelRoot.transform.localPosition = localPosition;
            WheelCollider collider = wheelRoot.AddComponent<WheelCollider>();
            collider.radius = .36f;
            collider.mass = 20f;
            collider.suspensionDistance = .16f;
            JointSpring suspension = collider.suspensionSpring;
            suspension.spring = 36000f;
            suspension.damper = 4500f;
            collider.suspensionSpring = suspension;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(wheelRoot.transform, false);
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            visual.transform.localScale = new Vector3(.68f, .18f, .68f);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
        }

        private static TrackPiece[] CreateModularTrackPiecePrefabs(Material roadMaterial)
        {
            return new[]
            {
                CreateTrackPiecePrefab("Straight", TrackPieceType.Straight, roadMaterial, 0f, 6f, false),
                CreateTrackPiecePrefab("LeftCurve", TrackPieceType.LeftCurve, roadMaterial, .08f, 2.1f, false),
                CreateTrackPiecePrefab("RightCurve", TrackPieceType.RightCurve, roadMaterial, .08f, 2.1f, false),
                CreateTrackPiecePrefab("HillUp", TrackPieceType.HillUp, roadMaterial, .25f, 1.2f, false),
                CreateTrackPiecePrefab("HillDown", TrackPieceType.HillDown, roadMaterial, .35f, 1.1f, false),
                CreateTrackPiecePrefab("Jump", TrackPieceType.Jump, roadMaterial, .48f, .65f, true),
                CreateTrackPiecePrefab("Chicane", TrackPieceType.Chicane, roadMaterial, .65f, .8f, true)
            };
        }

        private static TrackPiece CreateTrackPiecePrefab(string name, TrackPieceType type, Material roadMaterial,
            float minimumDifficulty, float selectionWeight, bool special)
        {
            string path = "Assets/Resources/Prefabs/TrackPieces/PF_Track_" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<TrackPiece>();

            GameObject root = new GameObject("Track_" + name);
            root.layer = LayerMask.NameToLayer("Track");
            TrackPiece piece = root.AddComponent<TrackPiece>();
            Transform entry = CreateLocalMarker(root.transform, "Entry", Vector3.zero, Quaternion.identity);
            Vector3 exitPosition = new Vector3(0f, 0f, 72f);
            Quaternion exitRotation = Quaternion.identity;
            Vector3[] aiMarkers;

            switch (type)
            {
                case TrackPieceType.LeftCurve:
                    CreateRoadBlock(root.transform, "RoadA", new Vector3(0f, 0f, 21f), new Vector3(12f, .16f, 42f), Quaternion.identity, roadMaterial);
                    CreateRoadBlock(root.transform, "RoadB", new Vector3(-21f, 0f, 42f), new Vector3(42f, .16f, 12f), Quaternion.identity, roadMaterial);
                    exitPosition = new Vector3(-42f, 0f, 42f);
                    exitRotation = Quaternion.Euler(0f, -90f, 0f);
                    aiMarkers = new[] { new Vector3(0f, .25f, 16f), new Vector3(-3f, .25f, 35f), new Vector3(-18f, .25f, 42f), new Vector3(-36f, .25f, 42f) };
                    break;
                case TrackPieceType.RightCurve:
                    CreateRoadBlock(root.transform, "RoadA", new Vector3(0f, 0f, 21f), new Vector3(12f, .16f, 42f), Quaternion.identity, roadMaterial);
                    CreateRoadBlock(root.transform, "RoadB", new Vector3(21f, 0f, 42f), new Vector3(42f, .16f, 12f), Quaternion.identity, roadMaterial);
                    exitPosition = new Vector3(42f, 0f, 42f);
                    exitRotation = Quaternion.Euler(0f, 90f, 0f);
                    aiMarkers = new[] { new Vector3(0f, .25f, 16f), new Vector3(3f, .25f, 35f), new Vector3(18f, .25f, 42f), new Vector3(36f, .25f, 42f) };
                    break;
                case TrackPieceType.HillUp:
                    CreateRoadBlock(root.transform, "HillRoad", new Vector3(0f, 5f, 30f), new Vector3(12f, .16f, 61f), Quaternion.Euler(-9.5f, 0f, 0f), roadMaterial);
                    exitPosition = new Vector3(0f, 10f, 60f);
                    exitRotation = Quaternion.Euler(-9.5f, 0f, 0f);
                    aiMarkers = new[] { new Vector3(0f, 1.8f, 15f), new Vector3(0f, 4.3f, 30f), new Vector3(0f, 6.8f, 45f) };
                    break;
                case TrackPieceType.HillDown:
                    CreateRoadBlock(root.transform, "HillRoad", new Vector3(0f, -5f, 30f), new Vector3(12f, .16f, 61f), Quaternion.Euler(9.5f, 0f, 0f), roadMaterial);
                    exitPosition = new Vector3(0f, -10f, 60f);
                    exitRotation = Quaternion.Euler(9.5f, 0f, 0f);
                    aiMarkers = new[] { new Vector3(0f, -1.8f, 15f), new Vector3(0f, -4.3f, 30f), new Vector3(0f, -6.8f, 45f) };
                    break;
                case TrackPieceType.Jump:
                    CreateRoadBlock(root.transform, "Ramp", new Vector3(0f, 3.5f, 15f), new Vector3(12f, .16f, 31f), Quaternion.Euler(-13f, 0f, 0f), roadMaterial);
                    CreateRoadBlock(root.transform, "Landing", new Vector3(0f, 3.5f, 55f), new Vector3(12f, .16f, 31f), Quaternion.Euler(13f, 0f, 0f), roadMaterial);
                    exitPosition = new Vector3(0f, 0f, 70f);
                    aiMarkers = new[] { new Vector3(0f, 1.2f, 12f), new Vector3(0f, 6.5f, 28f), new Vector3(0f, 5.5f, 48f), new Vector3(0f, .3f, 66f) };
                    break;
                case TrackPieceType.Chicane:
                    CreateRoadBlock(root.transform, "RoadA", new Vector3(-5f, 0f, 18f), new Vector3(12f, .16f, 38f), Quaternion.Euler(0f, -15f, 0f), roadMaterial);
                    CreateRoadBlock(root.transform, "RoadB", new Vector3(5f, 0f, 54f), new Vector3(12f, .16f, 38f), Quaternion.Euler(0f, 15f, 0f), roadMaterial);
                    exitPosition = new Vector3(0f, 0f, 72f);
                    aiMarkers = new[] { new Vector3(-4f, .25f, 15f), new Vector3(-8f, .25f, 30f), new Vector3(6f, .25f, 49f), new Vector3(3f, .25f, 65f) };
                    break;
                default:
                    CreateRoadBlock(root.transform, "Road", new Vector3(0f, 0f, 36f), new Vector3(12f, .16f, 72f), Quaternion.identity, roadMaterial);
                    aiMarkers = new[] { new Vector3(0f, .25f, 18f), new Vector3(0f, .25f, 42f), new Vector3(0f, .25f, 66f) };
                    break;
            }

            Transform exit = CreateLocalMarker(root.transform, "Exit", exitPosition, exitRotation);
            Transform[] ai = CreateLocalMarkers(root.transform, "AI", aiMarkers);
            Transform[] collectibles = CreateLocalMarkers(root.transform, "Collectible", new[]
            {
                new Vector3(-3.2f, 1f, 18f), new Vector3(0f, 1f, 36f), new Vector3(3.2f, 1f, 54f), new Vector3(0f, 1f, 66f)
            });
            Transform[] powerUps = CreateLocalMarkers(root.transform, "PowerUp", new[] { new Vector3(0f, 1f, 48f) });
            Transform[] obstacles = CreateLocalMarkers(root.transform, "Obstacle", new[]
            {
                new Vector3(-3f, .55f, 30f), new Vector3(3f, .55f, 58f)
            });

            CreateRoadBlock(root.transform, "BarrierLeft", new Vector3(-6.65f, .55f, 36f), new Vector3(.25f, 1f, 72f), Quaternion.identity, null);
            CreateRoadBlock(root.transform, "BarrierRight", new Vector3(6.65f, .55f, 36f), new Vector3(.25f, 1f, 72f), Quaternion.identity, null);
            SetObject(piece, "entryAnchor", entry); SetObject(piece, "exitAnchor", exit);
            SetFloat(piece, "nominalLength", Mathf.Max(50f, exitPosition.magnitude));
            SetEnum(piece, "pieceType", (int)type); SetFloat(piece, "minimumDifficulty", minimumDifficulty);
            SetFloat(piece, "selectionWeight", selectionWeight); SetBool(piece, "specialPiece", special);
            SetArray(piece, "aiWaypoints", ai); SetArray(piece, "collectibleSlots", collectibles);
            SetArray(piece, "powerUpSlots", powerUps); SetArray(piece, "obstacleSlots", obstacles);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<TrackPiece>();
        }

        private static Transform CreateLocalMarker(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = localRotation;
            return marker.transform;
        }

        private static Transform[] CreateLocalMarkers(Transform parent, string prefix, Vector3[] positions)
        {
            Transform[] markers = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                markers[i] = CreateLocalMarker(parent, prefix + "_" + i, positions[i], Quaternion.identity);
            return markers;
        }

        private static GameObject CreateRoadBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale, Quaternion localRotation, Material material)
        {
            GameObject block = CreateBlock(parent, name, localPosition, scale, material);
            block.transform.localRotation = localRotation;
            block.layer = LayerMask.NameToLayer("Track");
            return block;
        }

        private static GameObject CreateRepairPowerUpPrefab()
        {
            const string path = "Assets/Resources/Prefabs/PF_RepairPickup.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "RepairPickup";
            root.transform.localScale = Vector3.one * .7f;
            SphereCollider collider = root.GetComponent<SphereCollider>(); collider.isTrigger = true; collider.radius = .9f;
            root.AddComponent<PowerUpPickup>();
            root.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Assets/Art/Materials/M_RepairPickup.mat", new Color(.18f, 1f, .42f), .4f, .75f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateEndlessSegmentPrefab(Material roadMaterial, Material groundMaterial)
        {
            const string path = "Assets/Resources/Prefabs/PF_EndlessSegment.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = new GameObject("EndlessSegment");
            CreateBlock(root.transform, "Ground", new Vector3(0f, -.35f, 50f), new Vector3(40f, .5f, 100f), groundMaterial);
            CreateBlock(root.transform, "Road", new Vector3(0f, 0f, 50f), new Vector3(12f, .16f, 100f), roadMaterial);
            CreateBlock(root.transform, "BarrierLeft", new Vector3(-6.7f, .55f, 50f), new Vector3(.25f, 1f, 100f), null);
            CreateBlock(root.transform, "BarrierRight", new Vector3(6.7f, .55f, 50f), new Vector3(.25f, 1f, 100f), null);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCollectiblePrefab(string name, Color color, CollectibleType type)
        {
            string path = "Assets/Resources/Prefabs/PF_" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = GameObject.CreatePrimitive(type == CollectibleType.Coin ? PrimitiveType.Cylinder : PrimitiveType.Capsule);
            root.name = name;
            root.transform.localScale = type == CollectibleType.Coin ? new Vector3(.55f, .08f, .55f) : new Vector3(.4f, .65f, .4f);
            Object.DestroyImmediate(root.GetComponent<Collider>());
            SphereCollider trigger = root.AddComponent<SphereCollider>();
            trigger.radius = .7f;
            trigger.isTrigger = true;
            Collectible collectible = root.AddComponent<Collectible>();
            SetEnum(collectible, "type", (int)type);
            Material material = CreateMaterial("Assets/Art/Materials/M_" + name + ".mat", color, .5f, .8f);
            root.GetComponent<Renderer>().sharedMaterial = material;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateHazardPrefab()
        {
            const string path = "Assets/Resources/Prefabs/PF_Obstacle.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Obstacle";
            root.transform.localScale = new Vector3(1.1f, 1f, 1.1f);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            root.AddComponent<Hazard>();
            root.GetComponent<Renderer>().sharedMaterial = CreateMaterial("Assets/Art/Materials/M_Obstacle.mat", new Color(1f, .26f, .04f), .15f, .4f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static CarDefinition[] CreateCars(GameObject prefab)
        {
            string[] ids = { "street_rookie", "sand_viper", "neon_gt", "apex_x", "storm_r", "phantom_rs" };
            string[] names = { "Street Rookie", "Sand Viper", "Neon GT", "Apex X", "Storm R", "Phantom RS" };
            Color[] colors = { new Color(.95f, .12f, .10f), new Color(1f, .55f, .05f), new Color(.05f, .85f, 1f), new Color(.78f, .12f, 1f), new Color(.25f, .9f, .4f), new Color(.95f, .95f, 1f) };
            float[] speed = { 165f, 178f, 195f, 215f, 232f, 255f };
            float[] torque = { 2100f, 2450f, 2600f, 3000f, 3300f, 3700f };
            float[] handling = { .95f, 1.12f, 1.02f, 1.18f, .92f, 1.08f };
            int[] costs = { 0, 750, 1400, 2600, 4200, 7000 };
            CarDefinition[] result = new CarDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                string path = ResourceData + "/Cars/Car_" + ids[i] + ".asset";
                CarDefinition car = AssetDatabase.LoadAssetAtPath<CarDefinition>(path);
                if (car == null)
                {
                    car = ScriptableObject.CreateInstance<CarDefinition>();
                    AssetDatabase.CreateAsset(car, path);
                }
                car.id = ids[i]; car.displayName = names[i]; car.description = "A mobile-tuned " + names[i] + " built for Velocity Rush.";
                car.prefab = prefab; car.bodyColor = colors[i]; car.unlockedByDefault = i == 0; car.unlockCost = costs[i];
                car.driveLayout = i == 1 ? DriveLayout.RearWheelDrive : DriveLayout.AllWheelDrive;
                car.topSpeedKph = speed[i]; car.maxReverseKph = 36f; car.motorTorque = torque[i]; car.brakeTorque = 4300f + i * 120f;
                car.frontBrakeBias = .64f; car.handling = handling[i]; car.grip = .95f + i * .06f; car.mass = 1180f + i * 35f;
                car.suspensionTravel = .18f; car.suspensionSpring = 36000f; car.suspensionDamper = 4500f;
                car.driftRearGrip = .58f; car.driftForwardGrip = .72f;
                car.nitroCapacity = 3.8f + i * .15f; car.nitroForce = 1250f + i * 95f; car.nitroDrainPerSecond = 1f;
                car.nitroCooldown = 1.25f; car.nitroRechargePerSecond = .35f; car.maxPerformanceLossAtFullDamage = .35f; car.collisionDamageMultiplier = 1f;
                car.maxUpgradeLevel = 5; car.engineUpgradeBaseCost = 180 + i * 20; car.handlingUpgradeBaseCost = 160 + i * 20; car.nitroUpgradeBaseCost = 200 + i * 20;
                car.engineBonusPerLevel = .045f; car.handlingBonusPerLevel = .035f; car.nitroBonusPerLevel = .06f;
                if (car.torqueBySpeed == null || car.torqueBySpeed.length == 0)
                    car.torqueBySpeed = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(.35f, .92f), new Keyframe(.8f, .55f), new Keyframe(1f, 0f));
                EditorUtility.SetDirty(car); result[i] = car;
            }
            return result;
        }

        private static TrackDefinition[] CreateTracks()
        {
            string[] ids = { "desert_circuit", "city_circuit", "coast_circuit", "mountain_sprint", "harbor_run" };
            string[] names = { "Desert Circuit", "Neon City Circuit", "Coastline Circuit", "Mountain Sprint", "Harbor Run" };
            string[] scenes = { "DesertCircuit", "CityCircuit", "CoastCircuit", "MountainSprint", "HarborRun" };
            TrackDefinition[] result = new TrackDefinition[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                string path = ResourceData + "/Tracks/Track_" + ids[i] + ".asset";
                TrackDefinition track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(path);
                if (track == null) { track = ScriptableObject.CreateInstance<TrackDefinition>(); AssetDatabase.CreateAsset(track, path); }
                track.id = ids[i]; track.displayName = names[i]; track.sceneName = scenes[i]; track.defaultLaps = 3; track.recommendedDifficulty = i + 1; track.supportsEndless = i == 0;
                track.description = "A low-poly mobile circuit with baked environment lighting.";
                EditorUtility.SetDirty(track); result[i] = track;
            }
            return result;
        }

        private static void CreateCampaign(TrackDefinition[] tracks)
        {
            for (int i = 1; i <= 10; i++)
            {
                string path = ResourceData + "/Campaign/Level_" + i.ToString("00") + ".asset";
                CampaignLevelDefinition level = AssetDatabase.LoadAssetAtPath<CampaignLevelDefinition>(path);
                if (level == null) { level = ScriptableObject.CreateInstance<CampaignLevelDefinition>(); AssetDatabase.CreateAsset(level, path); }
                level.levelNumber = i; level.track = tracks[(i - 1) % tracks.Length]; level.laps = i < 4 ? 1 : i < 8 ? 2 : 3;
                level.aiOpponents = Mathf.Clamp(2 + i / 2, 2, 5); level.targetTimeSeconds = 105f - i * 3.5f; level.coinReward = 90 + i * 40;
                level.starsRequiredToUnlock = Mathf.Max(0, (i - 1) * 2); EditorUtility.SetDirty(level);
            }
        }

        private static void CreateMainMenuScene(TrackDefinition defaultTrack)
        {
            string path = SceneFolder + "/MainMenu.unity";
            if (!CanOverwrite(path)) return;
            NewScene(new Color(.025f, .04f, .08f));
            CreatePersistentSystems();
            Canvas canvas = CreateCanvas("MainMenuCanvas");
            MainMenuController controller = new GameObject("MainMenuController").AddComponent<MainMenuController>();
            SetObject(controller, "defaultTrack", defaultTrack);
            CreateText(canvas.transform, "Title", "VELOCITY RUSH", new Vector2(.5f, .78f), new Vector2(720f, 110f), 68, new Color(.15f, .85f, 1f), TextAnchor.MiddleCenter);
            CreateText(canvas.transform, "Subtitle", "MOBILE STREET RACING", new Vector2(.5f, .695f), new Vector2(600f, 42f), 20, Color.white, TextAnchor.MiddleCenter);
            Button play = CreateButton(canvas.transform, "Play", "PLAY", new Vector2(.5f, .57f)); UnityEventTools.AddPersistentListener(play.onClick, controller.Play);
            Button modes = CreateButton(canvas.transform, "Modes", "MODES", new Vector2(.5f, .47f)); UnityEventTools.AddPersistentListener(modes.onClick, controller.ToggleModes);
            Button garage = CreateButton(canvas.transform, "Garage", "GARAGE", new Vector2(.5f, .37f)); UnityEventTools.AddPersistentListener(garage.onClick, controller.OpenGarage);
            Button settings = CreateButton(canvas.transform, "Settings", "SETTINGS", new Vector2(.5f, .27f)); UnityEventTools.AddPersistentListener(settings.onClick, controller.ToggleSettings);
            Button quit = CreateButton(canvas.transform, "Quit", "QUIT", new Vector2(.5f, .17f)); UnityEventTools.AddPersistentListener(quit.onClick, controller.Quit);

            GameObject modesPanel = CreatePanel(canvas.transform, "ModesPanel", new Vector2(.79f, .52f), new Vector2(300f, 330f));
            CreateText(modesPanel.transform, "Header", "RACE MODES", new Vector2(.5f, .87f), new Vector2(270f, 38f), 24, Color.white, TextAnchor.MiddleCenter);
            Button quick = CreateButton(modesPanel.transform, "Quick", "QUICK RACE", new Vector2(.5f, .65f), new Vector2(240f, 48f)); UnityEventTools.AddPersistentListener(quick.onClick, controller.PlayQuickRace);
            Button endless = CreateButton(modesPanel.transform, "Endless", "ENDLESS", new Vector2(.5f, .48f), new Vector2(240f, 48f)); UnityEventTools.AddPersistentListener(endless.onClick, controller.PlayEndless);
            Button time = CreateButton(modesPanel.transform, "TimeTrial", "TIME TRIAL", new Vector2(.5f, .31f), new Vector2(240f, 48f)); UnityEventTools.AddPersistentListener(time.onClick, controller.PlayTimeTrial);
            Button campaign = CreateButton(modesPanel.transform, "Campaign", "CAMPAIGN", new Vector2(.5f, .14f), new Vector2(240f, 48f)); UnityEventTools.AddPersistentListener(campaign.onClick, controller.OpenCampaign);
            modesPanel.SetActive(false);

            GameObject settingsPanel = CreatePanel(canvas.transform, "SettingsPanel", new Vector2(.20f, .5f), new Vector2(300f, 250f));
            CreateText(settingsPanel.transform, "Header", "SETTINGS", new Vector2(.5f, .78f), new Vector2(260f, 44f), 24, Color.white, TextAnchor.MiddleCenter);
            CreateText(settingsPanel.transform, "Info", "Music, SFX, quality and tilt settings\nare exposed by SettingsController.", new Vector2(.5f, .47f), new Vector2(260f, 100f), 17, Color.white, TextAnchor.MiddleCenter);
            settingsPanel.SetActive(false);
            SetObject(controller, "modesPanel", modesPanel); SetObject(controller, "settingsPanel", settingsPanel);
            SaveScene(path);
        }

        private static void CreateGarageScene()
        {
            string path = SceneFolder + "/Garage.unity";
            if (!CanOverwrite(path)) return;
            NewScene(new Color(.035f, .04f, .055f));
            CreatePersistentSystems();
            Canvas canvas = CreateCanvas("GarageCanvas");
            GarageController controller = new GameObject("GarageController").AddComponent<GarageController>();
            CreateText(canvas.transform, "Title", "GARAGE", new Vector2(.5f, .86f), new Vector2(550f, 80f), 52, new Color(.15f, .85f, 1f), TextAnchor.MiddleCenter);
            Text name = CreateText(canvas.transform, "CarName", "CAR", new Vector2(.5f, .68f), new Vector2(500f, 60f), 36, Color.white, TextAnchor.MiddleCenter);
            Text desc = CreateText(canvas.transform, "Description", "Choose your ride.", new Vector2(.5f, .61f), new Vector2(650f, 60f), 18, Color.white, TextAnchor.MiddleCenter);
            Text coins = CreateText(canvas.transform, "Coins", "0", new Vector2(.88f, .92f), new Vector2(180f, 46f), 24, new Color(1f, .75f, .1f), TextAnchor.MiddleCenter);
            Slider speed = CreateStatBar(canvas.transform, "Speed", "SPEED", .48f);
            Slider acceleration = CreateStatBar(canvas.transform, "Acceleration", "ACCEL", .41f);
            Slider handling = CreateStatBar(canvas.transform, "Handling", "HANDLING", .34f);
            Button previous = CreateButton(canvas.transform, "Previous", "<", new Vector2(.18f, .65f), new Vector2(90f, 70f)); UnityEventTools.AddPersistentListener(previous.onClick, controller.Previous);
            Button next = CreateButton(canvas.transform, "Next", ">", new Vector2(.82f, .65f), new Vector2(90f, 70f)); UnityEventTools.AddPersistentListener(next.onClick, controller.Next);
            Button action = CreateButton(canvas.transform, "Action", "SELECT / UNLOCK", new Vector2(.5f, .20f), new Vector2(280f, 58f)); UnityEventTools.AddPersistentListener(action.onClick, controller.SelectOrUnlock);
            Button back = CreateButton(canvas.transform, "Back", "BACK", new Vector2(.5f, .11f), new Vector2(180f, 46f)); UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
            SetObject(controller, "carName", name); SetObject(controller, "description", desc); SetObject(controller, "coinsText", coins); SetObject(controller, "actionText", action.GetComponentInChildren<Text>());
            SetObject(controller, "speedBar", speed); SetObject(controller, "accelerationBar", acceleration); SetObject(controller, "handlingBar", handling);
            SaveScene(path);
        }

        private static void CreateLevelSelectScene()
        {
            string path = SceneFolder + "/LevelSelect.unity";
            if (!CanOverwrite(path)) return;
            NewScene(new Color(.025f, .05f, .08f));
            CreatePersistentSystems();
            Canvas canvas = CreateCanvas("LevelSelectCanvas");
            LevelSelectController controller = new GameObject("LevelSelectController").AddComponent<LevelSelectController>();
            CreateText(canvas.transform, "Title", "CAMPAIGN", new Vector2(.5f, .86f), new Vector2(600f, 70f), 50, new Color(.15f, .85f, 1f), TextAnchor.MiddleCenter);
            for (int i = 0; i < 10; i++)
            {
                int level = i + 1;
                float x = .24f + (i % 5) * .13f;
                float y = .61f - (i / 5) * .18f;
                Button button = CreateButton(canvas.transform, "Level" + level, level.ToString("00"), new Vector2(x, y), new Vector2(95f, 62f));
                UnityEventTools.AddIntPersistentListener(button.onClick, controller.PlayLevel, level);
            }
            Button back = CreateButton(canvas.transform, "Back", "BACK", new Vector2(.5f, .12f), new Vector2(180f, 46f)); UnityEventTools.AddPersistentListener(back.onClick, controller.Back);
            SaveScene(path);
        }

        private static void CreateRaceScene(string sceneName, Color ambient, Material roadMaterial, Material groundMaterial)
        {
            string path = SceneFolder + "/" + sceneName + ".unity";
            if (!CanOverwrite(path)) return;
            NewScene(ambient);
            CreateLighting(ambient);
            GameObject track = new GameObject("Track"); track.layer = LayerMask.NameToLayer("Track");
            CreateBlock(track.transform, "Ground", new Vector3(0f, -.4f, 0f), new Vector3(130f, .6f, 180f), groundMaterial);
            CreateBlock(track.transform, "SouthRoad", new Vector3(0f, 0f, -60f), new Vector3(104f, .15f, 14f), roadMaterial);
            CreateBlock(track.transform, "EastRoad", new Vector3(45f, 0f, 0f), new Vector3(14f, .15f, 134f), roadMaterial);
            CreateBlock(track.transform, "NorthRoad", new Vector3(0f, 0f, 60f), new Vector3(104f, .15f, 14f), roadMaterial);
            CreateBlock(track.transform, "WestRoad", new Vector3(-45f, 0f, 0f), new Vector3(14f, .15f, 134f), roadMaterial);
            CreateStaticCircuitObstacles(track.transform, sceneName);

            GameObject raceRoot = new GameObject("RaceManager");
            RaceManager race = raceRoot.AddComponent<RaceManager>();
            WaypointCircuit circuit = raceRoot.AddComponent<WaypointCircuit>();
            Transform[] points = CreateCircuitWaypoints(raceRoot.transform);
            SetArray(circuit, "waypoints", points);
            Transform playerStart = CreateTransform(raceRoot.transform, "PlayerStart", new Vector3(-13f, .4f, -60f), Quaternion.Euler(0f, 90f, 0f));
            Transform[] opponentStarts = new Transform[4];
            for (int i = 0; i < opponentStarts.Length; i++)
                opponentStarts[i] = CreateTransform(raceRoot.transform, "OpponentStart_" + (i + 1), new Vector3(-20f - i * 5f, .4f, -57f), Quaternion.Euler(0f, 90f, 0f));
            Checkpoint[] checkpoints = CreateCheckpoints(raceRoot.transform);
            SetObject(race, "playerStart", playerStart); SetArray(race, "opponentStarts", opponentStarts); SetArray(race, "orderedCheckpoints", checkpoints); SetObject(race, "waypoints", circuit);

            CreateRaceCamera();
            CreateMinimapCamera();
            CreateHud();
            SaveScene(path);
        }

        private static void CreateStaticCircuitObstacles(Transform parent, string sceneName)
        {
            Vector3[] positions;
            switch (sceneName)
            {
                case "MountainSprint": positions = new[] { new Vector3(26f, .65f, -60f), new Vector3(45f, .65f, 24f), new Vector3(-22f, .65f, 60f) }; break;
                case "HarborRun": positions = new[] { new Vector3(12f, .65f, -60f), new Vector3(45f, .65f, -20f), new Vector3(-45f, .65f, 28f) }; break;
                case "CityCircuit": positions = new[] { new Vector3(28f, .65f, -60f), new Vector3(45f, .65f, 14f) }; break;
                case "CoastCircuit": positions = new[] { new Vector3(-22f, .65f, -60f), new Vector3(45f, .65f, 38f) }; break;
                default: positions = new[] { new Vector3(31f, .65f, -60f) }; break;
            }
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Obstacle.mat");
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = "TrackObstacle_" + i;
                obstacle.transform.SetParent(parent);
                obstacle.transform.position = positions[i];
                obstacle.transform.localScale = new Vector3(1.2f, 1.25f, 1.2f);
                obstacle.layer = LayerMask.NameToLayer("Hazard");
                BoxCollider collider = obstacle.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                obstacle.AddComponent<Hazard>();
                if (material != null) obstacle.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static Transform[] CreateCircuitWaypoints(Transform parent)
        {
            Vector3[] positions = { new Vector3(0f, .2f, -60f), new Vector3(40f, .2f, -60f), new Vector3(45f, .2f, -10f), new Vector3(45f, .2f, 55f), new Vector3(0f, .2f, 60f), new Vector3(-40f, .2f, 60f), new Vector3(-45f, .2f, 10f), new Vector3(-45f, .2f, -55f) };
            Transform[] result = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++) result[i] = CreateTransform(parent, "Waypoint_" + i, positions[i], Quaternion.identity);
            return result;
        }

        private static Checkpoint[] CreateCheckpoints(Transform parent)
        {
            Vector3[] positions = { new Vector3(0f, 1.5f, -60f), new Vector3(45f, 1.5f, 0f), new Vector3(0f, 1.5f, 60f), new Vector3(-45f, 1.5f, 0f) };
            Vector3[] sizes = { new Vector3(2f, 3f, 14f), new Vector3(14f, 3f, 2f), new Vector3(2f, 3f, 14f), new Vector3(14f, 3f, 2f) };
            Checkpoint[] result = new Checkpoint[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject checkpoint = new GameObject(i == 0 ? "FinishLine" : "Checkpoint_" + i);
                checkpoint.transform.SetParent(parent); checkpoint.transform.position = positions[i];
                BoxCollider trigger = checkpoint.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = sizes[i];
                Checkpoint script = checkpoint.AddComponent<Checkpoint>(); SetInt(script, "checkpointIndex", i); result[i] = script;
            }
            return result;
        }

        private static void CreateEndlessScene(TrackPiece[] modularPieces, GameObject coin, GameObject nitro, GameObject repair, GameObject hazard)
        {
            string path = SceneFolder + "/EndlessRun.unity";
            if (!CanOverwrite(path)) return;
            NewScene(new Color(.09f, .035f, .12f));
            CreateLighting(new Color(.09f, .035f, .12f));
            GameObject root = new GameObject("EndlessRunManager");
            EndlessRunManager run = root.AddComponent<EndlessRunManager>();
            Transform start = CreateTransform(root.transform, "PlayerStart", new Vector3(0f, .4f, 5f), Quaternion.identity);
            GameObject managerObject = new GameObject("TrackManager");
            TrackManager trackManager = managerObject.AddComponent<TrackManager>();
            SetArray(trackManager, "modularPiecePrefabs", modularPieces);
            SetArray(trackManager, "collectiblePrefabs", new[] { coin, nitro });
            SetArray(trackManager, "powerUpPrefabs", new[] { repair });
            SetArray(trackManager, "obstaclePrefabs", new[] { hazard });
            SetObject(run, "playerStart", start); SetObject(run, "trackManager", trackManager);
            CreateRaceCamera();
            CreateMinimapCamera();
            CreateHud();
            SaveScene(path);
        }

        private static void CreatePersistentSystems()
        {
            GameObject systems = new GameObject("PersistentSystems");
            systems.AddComponent<GameManager>();
            systems.AddComponent<InputManager>();
            AudioSource musicSource = systems.AddComponent<AudioSource>();
            musicSource.loop = true;
            AudioManager audioManager = systems.AddComponent<AudioManager>();
            SetObject(audioManager, "musicSource", musicSource);
            systems.AddComponent<MusicLoopController>();
            systems.AddComponent<MobileGraphicsController>();
            systems.AddComponent<CinematicTimeController>();
        }

        private static void CreateLighting(Color ambient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient * .75f;
            Material skybox = CreateSkyboxMaterial();
            if (skybox != null) RenderSettings.skybox = skybox;
            GameObject lightObject = new GameObject("Sun (Mixed)");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.05f; sun.shadows = LightShadows.Soft;
            // One mixed sun supplies vehicle lighting; static geometry receives baked lighting after Bake.
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            GameObject volumeObject = new GameObject("Mobile Polish Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = CreateMobilePolishProfile();

            GameObject reflectionObject = new GameObject("Car Reflection Probe (Baked)");
            ReflectionProbe probe = reflectionObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.resolution = 128;
            probe.size = new Vector3(150f, 45f, 150f);
            probe.center = new Vector3(0f, 10f, 0f);

            GameObject weatherObject = new GameObject("Time Of Day & Weather");
            TimeOfDayWeatherController weather = weatherObject.AddComponent<TimeOfDayWeatherController>();
            SetObject(weather, "sun", sun);
            SetObject(weather, "skyboxMaterial", skybox);
        }

        private static void CreateRaceCamera()
        {
            GameObject cameraObject = new GameObject("RaceCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 5f, -10f);
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraObject.AddComponent<RaceCameraController>();
            cameraObject.AddComponent<CameraPolishController>();
        }

        private static void CreateMinimapCamera()
        {
            GameObject cameraObject = new GameObject("MinimapCamera");
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 78f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.02f, .04f, .07f, 1f);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.depth = -2;
            camera.targetTexture = GetMinimapRenderTexture();
            cameraObject.AddComponent<MinimapCameraController>();
        }

        private static RenderTexture GetMinimapRenderTexture()
        {
            const string path = "Assets/Settings/Rendering/RT_Minimap.renderTexture";
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            if (texture != null) return texture;
            texture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                name = "RT_Minimap",
                useMipMap = false,
                autoGenerateMips = false
            };
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static void CreateHud()
        {
            Canvas canvas = CreateCanvas("HUD");
            UIManager ui = canvas.gameObject.AddComponent<UIManager>();
            Text speed = CreateText(canvas.transform, "Speed", "0\n<size=42>KM/H</size>", new Vector2(.12f, .16f), new Vector2(210f, 115f), 52, Color.white, TextAnchor.MiddleCenter);
            Text timer = CreateText(canvas.transform, "Timer", "00:00.00", new Vector2(.5f, .93f), new Vector2(280f, 55f), 32, Color.white, TextAnchor.MiddleCenter);
            Text score = CreateText(canvas.transform, "Score", "0", new Vector2(.86f, .93f), new Vector2(190f, 48f), 28, new Color(1f, .78f, .1f), TextAnchor.MiddleCenter);
            Text lap = CreateText(canvas.transform, "Lap", "LAP 1/3", new Vector2(.14f, .93f), new Vector2(190f, 48f), 26, Color.white, TextAnchor.MiddleCenter);
            RawImage minimap = CreateMinimapWidget(canvas.transform);
            Text countdown = CreateText(canvas.transform, "Countdown", "", new Vector2(.5f, .55f), new Vector2(430f, 160f), 90, Color.white, TextAnchor.MiddleCenter);
            Slider nitro = CreateSlider(canvas.transform, "Nitro", new Vector2(.5f, .10f), new Vector2(260f, 22f));
            GameObject result = CreatePanel(canvas.transform, "ResultPanel", new Vector2(.5f, .5f), new Vector2(420f, 250f));
            Text resultTitle = CreateText(result.transform, "ResultTitle", "FINISH!", new Vector2(.5f, .65f), new Vector2(370f, 70f), 44, Color.white, TextAnchor.MiddleCenter);
            Text resultTime = CreateText(result.transform, "ResultTime", "00:00.00", new Vector2(.5f, .43f), new Vector2(300f, 50f), 28, Color.white, TextAnchor.MiddleCenter);
            Button menu = CreateButton(result.transform, "Menu", "MAIN MENU", new Vector2(.5f, .18f), new Vector2(210f, 44f)); UnityEventTools.AddPersistentListener(menu.onClick, ui.QuitToMenu);
            result.SetActive(false);
            CreateMobileControls(canvas.transform);
            SetObject(ui, "speedText", speed); SetObject(ui, "timerText", timer); SetObject(ui, "scoreText", score); SetObject(ui, "lapText", lap); SetObject(ui, "countdownText", countdown);
            SetObject(ui, "nitroSlider", nitro); SetObject(ui, "resultPanel", result); SetObject(ui, "resultTitleText", resultTitle); SetObject(ui, "resultTimeText", resultTime);
        }

        private static RawImage CreateMinimapWidget(Transform parent)
        {
            GameObject root = new GameObject("Minimap", typeof(RectTransform), typeof(RawImage));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.89f, .75f); rect.anchorMax = new Vector2(.89f, .75f); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(175f, 175f);
            RawImage image = root.GetComponent<RawImage>(); image.texture = GetMinimapRenderTexture(); image.color = Color.white;
            return image;
        }

        private static void CreateMobileControls(Transform canvas)
        {
            GameObject wheel = CreatePanel(canvas, "SteeringWheel", new Vector2(.14f, .25f), new Vector2(145f, 145f));
            wheel.GetComponent<Image>().color = new Color(.1f, .65f, .9f, .28f);
            wheel.AddComponent<TouchSteeringWheel>();
            CreateMobileButton(canvas, "Brake", "BRAKE", new Vector2(.76f, .19f), MobileInputAction.Brake, new Color(.85f, .2f, .15f, .65f));
            CreateMobileButton(canvas, "Throttle", "GO", new Vector2(.88f, .25f), MobileInputAction.Accelerate, new Color(.12f, .8f, .35f, .65f));
            CreateMobileButton(canvas, "Nitro", "N2O", new Vector2(.79f, .37f), MobileInputAction.Nitro, new Color(.12f, .55f, 1f, .65f));
        }

        private static void CreateMobileButton(Transform parent, string name, string label, Vector2 anchor, MobileInputAction action, Color color)
        {
            GameObject root = CreatePanel(parent, name, anchor, new Vector2(100f, 70f));
            root.GetComponent<Image>().color = color;
            MobileButton button = root.AddComponent<MobileButton>(); SetEnum(button, "action", (int)action);
            CreateText(root.transform, "Label", label, new Vector2(.5f, .5f), new Vector2(90f, 55f), 20, Color.white, TextAnchor.MiddleCenter);
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = .5f;
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                eventSystem.transform.SetParent(root.transform, false);
            }
            return canvas;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            root.GetComponent<Image>().color = new Color(.025f, .06f, .1f, .88f);
            return root;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2? sizeOverride = null)
        {
            GameObject root = CreatePanel(parent, name, anchor, sizeOverride ?? new Vector2(240f, 56f));
            Image image = root.GetComponent<Image>(); image.color = new Color(.08f, .42f, .64f, .92f);
            Button button = root.AddComponent<Button>(); button.targetGraphic = image;
            CreateText(root.transform, "Label", label, new Vector2(.5f, .5f), root.GetComponent<RectTransform>().sizeDelta - new Vector2(10f, 4f), 22, Color.white, TextAnchor.MiddleCenter);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string content, Vector2 anchor, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Text));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>(); rect.anchorMin = anchor; rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
            Text text = root.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); text.text = content; text.fontSize = fontSize; text.color = color; text.alignment = alignment; text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            GameObject root = CreatePanel(parent, name, anchor, size);
            Object.DestroyImmediate(root.GetComponent<Image>());
            Slider slider = root.AddComponent<Slider>();
            GameObject background = CreatePanel(root.transform, "Background", new Vector2(.5f, .5f), size); background.GetComponent<Image>().color = new Color(.05f, .1f, .15f, .8f);
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform)); fillArea.transform.SetParent(root.transform, false); RectTransform areaRect = fillArea.GetComponent<RectTransform>(); areaRect.anchorMin = Vector2.zero; areaRect.anchorMax = Vector2.one; areaRect.offsetMin = new Vector2(2f, 2f); areaRect.offsetMax = new Vector2(-2f, -2f);
            GameObject fill = CreatePanel(fillArea.transform, "Fill", new Vector2(.5f, .5f), size); RectTransform fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero; fill.GetComponent<Image>().color = new Color(.1f, .7f, 1f, .95f);
            slider.fillRect = fillRect; slider.targetGraphic = fill.GetComponent<Image>(); slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            return slider;
        }

        private static Slider CreateStatBar(Transform parent, string name, string label, float y)
        {
            CreateText(parent, name + "Label", label, new Vector2(.35f, y), new Vector2(150f, 28f), 18, Color.white, TextAnchor.MiddleRight);
            return CreateSlider(parent, name + "Bar", new Vector2(.59f, y), new Vector2(330f, 18f));
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube); block.name = name; block.transform.SetParent(parent); block.transform.localPosition = position; block.transform.localScale = scale;
            if (material != null) block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static Transform CreateTransform(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            GameObject root = new GameObject(name); root.transform.SetParent(parent); root.transform.SetPositionAndRotation(position, rotation); return root.transform;
        }

        private static bool CanOverwrite(string path)
        {
            if (!System.IO.File.Exists(path)) return true;
            return EditorUtility.DisplayDialog("Velocity Rush", "Replace existing " + System.IO.Path.GetFileName(path) + "?\nChoose Cancel to preserve this scene.", "Replace", "Keep Existing");
        }

        private static void NewScene(Color background)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientLight = background;
        }

        private static void SaveScene(string path) => EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            foreach (string scene in SceneNames) scenes.Add(new EditorBuildSettingsScene(SceneFolder + "/" + scene + ".unity", true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.productName = "Velocity Rush";
            PlayerSettings.companyName = "Your Studio";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.yourstudio.velocityrush");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.yourstudio.velocityrush");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            Application.targetFrameRate = 60;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) { Debug.LogError("Missing serialized property " + propertyName + " on " + target.name); return; }
            property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void SetArray(Object target, string propertyName, Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) { Debug.LogError("Missing serialized property " + propertyName + " on " + target.name); return; }
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return; property.intValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return; property.floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target); SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return; property.boolValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string propertyName, int value) => SetInt(target, propertyName, value);
    }
}
#endif
