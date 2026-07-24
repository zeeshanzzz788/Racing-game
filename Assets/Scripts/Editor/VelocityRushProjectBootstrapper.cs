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
using VelocityRush.Race;
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
            "MainMenu", "Garage", "LevelSelect", "DesertCircuit", "CityCircuit", "CoastCircuit", "EndlessRun"
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
            GameObject segmentPrefab = CreateEndlessSegmentPrefab(roadMaterial, groundMaterial);
            GameObject coinPrefab = CreateCollectiblePrefab("Coin", new Color(1f, .72f, .05f), CollectibleType.Coin);
            GameObject nitroPrefab = CreateCollectiblePrefab("NitroPickup", new Color(.1f, .8f, 1f), CollectibleType.Nitro);
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
            CreateEndlessScene(segmentPrefab, coinPrefab, nitroPrefab, hazardPrefab, groundMaterial);
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
                "Assets/Resources/Prefabs", "Assets/Settings/Rendering", "Assets/Art/Materials", "Assets/Prefabs/Cars"
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
            root.AddComponent<PlayerCarController>();
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
                car.topSpeedKph = speed[i]; car.motorTorque = torque[i]; car.handling = handling[i]; car.grip = .95f + i * .06f;
                car.nitroCapacity = 3.8f + i * .15f; car.nitroForce = 1250f + i * 95f;
                EditorUtility.SetDirty(car); result[i] = car;
            }
            return result;
        }

        private static TrackDefinition[] CreateTracks()
        {
            string[] ids = { "desert_circuit", "city_circuit", "coast_circuit" };
            string[] names = { "Desert Circuit", "Neon City Circuit", "Coastline Circuit" };
            string[] scenes = { "DesertCircuit", "CityCircuit", "CoastCircuit" };
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

        private static void CreateEndlessScene(GameObject segment, GameObject coin, GameObject nitro, GameObject hazard, Material groundMaterial)
        {
            string path = SceneFolder + "/EndlessRun.unity";
            if (!CanOverwrite(path)) return;
            NewScene(new Color(.09f, .035f, .12f));
            CreateLighting(new Color(.09f, .035f, .12f));
            GameObject root = new GameObject("EndlessRunManager");
            EndlessRunManager run = root.AddComponent<EndlessRunManager>();
            Transform start = CreateTransform(root.transform, "PlayerStart", new Vector3(0f, .4f, 5f), Quaternion.identity);
            GameObject generatorObject = new GameObject("EndlessTrackGenerator");
            EndlessTrackGenerator generator = generatorObject.AddComponent<EndlessTrackGenerator>();
            SetArray(generator, "segmentPrefabs", new[] { segment }); SetObject(generator, "coinPrefab", coin); SetObject(generator, "nitroPrefab", nitro); SetObject(generator, "obstaclePrefab", hazard);
            SetObject(run, "playerStart", start); SetObject(run, "trackGenerator", generator);
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
            systems.AddComponent<AudioManager>();
        }

        private static void CreateLighting(Color ambient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient * .75f;
            GameObject lightObject = new GameObject("Sun (Baked)");
            Light sun = lightObject.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.05f; sun.shadows = LightShadows.Soft;
            // One mixed sun supplies vehicle lighting; static geometry receives baked lighting after Bake.
            sun.lightmapBakeType = LightmapBakeType.Mixed;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        private static void CreateRaceCamera()
        {
            GameObject cameraObject = new GameObject("RaceCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 5f, -10f);
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.AddComponent<RaceCameraController>();
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

        private static void SetEnum(Object target, string propertyName, int value) => SetInt(target, propertyName, value);
    }
}
#endif
