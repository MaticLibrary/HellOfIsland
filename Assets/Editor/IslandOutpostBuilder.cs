using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class IslandOutpostBuilder
{
    private const string RootPath = "Assets/IslandOutpost";
    private const string MaterialPath = RootPath + "/Materials";
    private const string AnimationPath = RootPath + "/Animations";
    private const string AudioPath = RootPath + "/Audio";
    private const string PrefabPath = RootPath + "/IslandOutpost.prefab";
    private const string ControllerPath = AnimationPath + "/Outpost.controller";
    private const string SoundPath = AudioPath + "/door_open.wav";

    [MenuItem("Tools/Island/Rebuild Outpost")]
    private static void Rebuild()
    {
        GameObject existing = GameObject.Find("Island Outpost");
        if (existing == null)
        {
            Build();
            return;
        }

        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode before rebuilding the outpost.");
            return;
        }

        Transform oldShy = existing.transform.Find("Coconut Shy");
        if (oldShy != null)
            UnityEngine.Object.DestroyImmediate(oldShy.gameObject);

        BuildCoconutShy(existing.transform);

        AudioClip doorSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundPath);
        if (doorSound == null)
        {
            Debug.LogError("Could not load the outpost door sound.");
            return;
        }

        AttachPlayerScript(doorSound);
        PrefabUtility.SaveAsPrefabAsset(existing, PrefabPath);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = existing;
        Debug.Log("Rebuilt the Coconut Shy without moving the existing Island Outpost or Player.", existing);
    }

    [MenuItem("Tools/Island/Build Outpost")]
    private static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play mode before building the outpost.");
            return;
        }

        GameObject existing = GameObject.Find("Island Outpost");
        if (existing != null)
        {
            GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing);
            if (source != null && AssetDatabase.GetAssetPath(source) == PrefabPath)
            {
                Selection.activeGameObject = existing;
                Debug.LogWarning("Island Outpost already exists in the active scene; it was left unchanged.", existing);
                return;
            }
            throw new InvalidOperationException("An object named 'Island Outpost' already exists. Rename it before building.");
        }

        EnsureFolders();
        EnsureTag("playerDoor");
        EnsureTag("coconut");

        Material wood = CreateMaterial("Wood", new Color(0.34f, 0.17f, 0.085f), 0.12f);
        Material lightWood = CreateMaterial("Sunbleached Wood", new Color(0.58f, 0.36f, 0.19f), 0.14f);
        Material darkWood = CreateMaterial("Dark Wood", new Color(0.19f, 0.105f, 0.06f), 0.1f);
        Material roof = CreateMaterial("Deep Teal Roof", new Color(0.075f, 0.20f, 0.22f), 0.34f);
        Material roofRib = CreateMaterial("Roof Ribs", new Color(0.12f, 0.31f, 0.31f), 0.28f);
        Material stone = CreateMaterial("Warm Stone", new Color(0.36f, 0.34f, 0.28f), 0.08f);
        Material glass = CreateMaterial("Sea Glass", new Color(0.075f, 0.25f, 0.27f), 0.52f);
        Material brass = CreateMaterial("Aged Brass", new Color(0.65f, 0.43f, 0.17f), 0.48f);
        Material lantern = CreateMaterial("Lantern Glow", new Color(1f, 0.55f, 0.16f), 0.2f, true);
        Material sign = CreateMaterial("Sign Paint", new Color(0.84f, 0.74f, 0.54f), 0.2f);

        AnimationClip idleClip = CreateClip("idle", 0f, 0f);
        AnimationClip openClip = CreateClip("dooropen", 0f, -104f);
        AnimationClip shutClip = CreateClip("doorshut", -104f, 0f);
        AnimatorController controller = CreateController(idleClip, openClip, shutClip);
        CreateDoorSound();
        AssetDatabase.Refresh();
        AudioClip doorSound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundPath);
        if (doorSound == null)
            throw new InvalidOperationException("Could not import the generated door sound.");

        GameObject hut = new GameObject("Island Outpost");
        hut.transform.position = Vector3.zero;
        hut.transform.rotation = Quaternion.identity;

        BuildPlatform(hut.transform, stone, darkWood, lightWood);
        BuildWalls(hut.transform, wood, lightWood, darkWood, glass, brass);
        BuildRoof(hut.transform, roof, roofRib, darkWood, lightWood);
        BuildEntry(hut.transform, lightWood, darkWood, brass, lantern, sign, roof, controller);

        BuildCoconutShy(hut.transform);

        Animator animator = hut.GetComponent<Animator>();
        if (animator == null)
            animator = hut.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject savedOutpost = PrefabUtility.SaveAsPrefabAsset(hut, PrefabPath);
        if (savedOutpost == null)
            throw new InvalidOperationException("Unity could not save the Island Outpost prefab.");
        PositionOutpost(hut.transform);
        AttachPlayerScript(doorSound);

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = hut;
        Debug.Log("Built and saved the Island Outpost prefab, door animations, sound, and scene instance.", hut);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "IslandOutpost");
        CreateFolderIfMissing(RootPath, "Materials");
        CreateFolderIfMissing(RootPath, "Animations");
        CreateFolderIfMissing(RootPath, "Audio");
    }

    private static void CreateFolderIfMissing(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folder);
    }

    private static Component AddRuntimeComponent(GameObject target, string typeName)
    {
        System.Type runtimeType = System.Type.GetType(typeName + ", Assembly-CSharp");
        if (runtimeType == null)
            throw new InvalidOperationException("Could not load runtime component: " + typeName);
        return target.AddComponent(runtimeType);
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, bool emission = false)
    {
        string path = MaterialPath + "/" + name.Replace(' ', '_') + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("Could not find a supported material shader.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (emission)
        {
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 2.2f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureTag(string tag)
    {
        UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject settings = new SerializedObject(asset);
        SerializedProperty tags = settings.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                return;
        }

        int index = tags.arraySize;
        tags.InsertArrayElementAtIndex(index);
        tags.GetArrayElementAtIndex(index).stringValue = tag;
        settings.ApplyModifiedProperties();
    }

    private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3? rotation = null)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        if (rotation.HasValue)
            part.transform.localEulerAngles = rotation.Value;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    private static void BuildPlatform(Transform root, Material stone, Material darkWood, Material lightWood)
    {
        GameObject foundation = CreateBox(root, "Stone Foundation", new Vector3(0f, 0.12f, 0f), new Vector3(4.35f, 0.24f, 3.72f), stone);
        BoxCollider foundationCollider = foundation.AddComponent<BoxCollider>();
        foundationCollider.size = new Vector3(1f, 1f, 1f);

        CreateBox(root, "Raised Timber Floor", new Vector3(0f, 0.38f, 0f), new Vector3(4.1f, 0.16f, 3.55f), darkWood);
        for (int i = 0; i < 11; i++)
        {
            float x = -1.8f + i * 0.36f;
            CreateBox(root, "Floor Board", new Vector3(x, 0.47f, 0f), new Vector3(0.33f, 0.035f, 3.42f), i % 3 == 0 ? lightWood : darkWood);
        }

        CreateBox(root, "Porch Step Lower", new Vector3(0f, 0.09f, 2.26f), new Vector3(1.9f, 0.18f, 0.44f), stone);
        CreateBox(root, "Porch Step Upper", new Vector3(0f, 0.24f, 1.98f), new Vector3(1.75f, 0.18f, 0.4f), lightWood);
        for (int i = 0; i < 5; i++)
        {
            float x = -0.82f + i * 0.41f;
            CreateBox(root, "Porch Deck Board", new Vector3(x, 0.48f, 2.03f), new Vector3(0.38f, 0.04f, 0.76f), i % 2 == 0 ? lightWood : darkWood);
        }
    }

    private static void BuildWalls(Transform root, Material wood, Material lightWood, Material darkWood, Material glass, Material brass)
    {
        const float wallBottom = 0.52f;
        const float wallTop = 2.83f;
        const float boardHeight = 0.205f;
        int rows = Mathf.CeilToInt((wallTop - wallBottom) / boardHeight);

        for (int row = 0; row < rows; row++)
        {
            float y = wallBottom + row * boardHeight + boardHeight * 0.5f;
            Material plank = row % 4 == 0 ? lightWood : wood;
            CreateBox(root, "Rear Wall Board", new Vector3(0f, y, -1.64f), new Vector3(3.72f, boardHeight - 0.018f, 0.14f), plank);
            CreateBox(root, "Left Wall Board", new Vector3(-1.79f, y, 0f), new Vector3(0.14f, boardHeight - 0.018f, 3.34f), plank);
            CreateBox(root, "Right Wall Board", new Vector3(1.79f, y, 0f), new Vector3(0.14f, boardHeight - 0.018f, 3.34f), plank);
            CreateBox(root, "Front Wall Board Left", new Vector3(-1.28f, y, 1.64f), new Vector3(1.16f, boardHeight - 0.018f, 0.14f), plank);
            CreateBox(root, "Front Wall Board Right", new Vector3(1.28f, y, 1.64f), new Vector3(1.16f, boardHeight - 0.018f, 0.14f), plank);
        }

        BoxCollider backCollider = CreateBox(root, "Rear Wall Collision", new Vector3(0f, 1.67f, -1.64f), new Vector3(3.72f, 2.3f, 0.18f), wood).AddComponent<BoxCollider>();
        BoxCollider leftCollider = CreateBox(root, "Left Wall Collision", new Vector3(-1.79f, 1.67f, 0f), new Vector3(0.18f, 2.3f, 3.34f), wood).AddComponent<BoxCollider>();
        BoxCollider rightCollider = CreateBox(root, "Right Wall Collision", new Vector3(1.79f, 1.67f, 0f), new Vector3(0.18f, 2.3f, 3.34f), wood).AddComponent<BoxCollider>();
        BoxCollider frontLeftCollider = CreateBox(root, "Front Left Wall Collision", new Vector3(-1.28f, 1.67f, 1.64f), new Vector3(1.16f, 2.3f, 0.18f), wood).AddComponent<BoxCollider>();
        BoxCollider frontRightCollider = CreateBox(root, "Front Right Wall Collision", new Vector3(1.28f, 1.67f, 1.64f), new Vector3(1.16f, 2.3f, 0.18f), wood).AddComponent<BoxCollider>();
        backCollider.center = Vector3.zero;
        leftCollider.center = Vector3.zero;
        rightCollider.center = Vector3.zero;
        frontLeftCollider.center = Vector3.zero;
        frontRightCollider.center = Vector3.zero;

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 1.9f;
            float depth = side * 0.075f;
            CreateBox(root, "Sea Glass Window", new Vector3(x, 1.82f, 0.15f), new Vector3(0.055f, 0.66f, 0.78f), glass);
            CreateBox(root, "Window Upper Frame", new Vector3(x + depth, 2.18f, 0.15f), new Vector3(0.11f, 0.075f, 0.92f), lightWood);
            CreateBox(root, "Window Lower Frame", new Vector3(x + depth, 1.46f, 0.15f), new Vector3(0.11f, 0.075f, 0.92f), lightWood);
            CreateBox(root, "Window Side Frame", new Vector3(x + depth, 1.82f, -0.28f), new Vector3(0.11f, 0.78f, 0.075f), lightWood);
            CreateBox(root, "Window Side Frame", new Vector3(x + depth, 1.82f, 0.58f), new Vector3(0.11f, 0.78f, 0.075f), lightWood);
            CreateBox(root, "Window Crossbar", new Vector3(x + depth, 1.82f, 0.15f), new Vector3(0.12f, 0.055f, 0.78f), brass);
            CreateBox(root, "Window Sill", new Vector3(x + side * 0.11f, 1.39f, 0.15f), new Vector3(0.26f, 0.1f, 1.02f), darkWood);
        }

        for (int side = -1; side <= 1; side += 2)
        for (int front = -1; front <= 1; front += 2)
            CreateBox(root, "Corner Timber Post", new Vector3(side * 1.82f, 1.7f, front * 1.64f), new Vector3(0.24f, 2.5f, 0.24f), darkWood);
    }

    private static void BuildRoof(Transform root, Material roof, Material roofRib, Material darkWood, Material lightWood)
    {
        CreateBox(root, "Left Roof Panel", new Vector3(-1.03f, 3.34f, 0f), new Vector3(2.52f, 0.16f, 4.32f), roof, new Vector3(0f, 0f, 22f));
        CreateBox(root, "Right Roof Panel", new Vector3(1.03f, 3.34f, 0f), new Vector3(2.52f, 0.16f, 4.32f), roof, new Vector3(0f, 0f, -22f));
        CreateBox(root, "Roof Ridge Cap", new Vector3(0f, 3.79f, 0f), new Vector3(0.28f, 0.22f, 4.52f), lightWood);
        CreateBox(root, "Front Roof Fascia Left", new Vector3(-1.1f, 3.32f, 2.12f), new Vector3(2.55f, 0.19f, 0.16f), darkWood, new Vector3(0f, 0f, 22f));
        CreateBox(root, "Front Roof Fascia Right", new Vector3(1.1f, 3.32f, 2.12f), new Vector3(2.55f, 0.19f, 0.16f), darkWood, new Vector3(0f, 0f, -22f));
        CreateBox(root, "Rear Roof Fascia Left", new Vector3(-1.1f, 3.32f, -2.12f), new Vector3(2.55f, 0.19f, 0.16f), darkWood, new Vector3(0f, 0f, 22f));
        CreateBox(root, "Rear Roof Fascia Right", new Vector3(1.1f, 3.32f, -2.12f), new Vector3(2.55f, 0.19f, 0.16f), darkWood, new Vector3(0f, 0f, -22f));

        for (int side = -1; side <= 1; side += 2)
        for (int rib = 0; rib < 5; rib++)
        {
            float distance = 0.24f + rib * 0.47f;
            float x = side * distance;
            float y = 3.79f - distance * Mathf.Tan(22f * Mathf.Deg2Rad) + 0.09f;
            CreateBox(root, "Roof Standing Seam", new Vector3(x, y, 0f), new Vector3(0.055f, 0.045f, 4.25f), roofRib, new Vector3(0f, 0f, -side * 22f));
        }

        CreateGable(root, "Front Gable", 1.68f, darkWood, lightWood);
        CreateGable(root, "Rear Gable", -1.68f, darkWood, lightWood);
    }

    private static void CreateGable(Transform root, string name, float z, Material backing, Material trim)
    {
        GameObject gable = new GameObject(name);
        gable.transform.SetParent(root, false);
        Mesh mesh = new Mesh { name = name + " Mesh" };
        mesh.vertices = new[]
        {
            new Vector3(-1.78f, 2.82f, z), new Vector3(1.78f, 2.82f, z), new Vector3(0f, 3.69f, z)
        };
        mesh.triangles = z > 0f ? new[] { 0, 1, 2 } : new[] { 2, 1, 0 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        gable.AddComponent<MeshFilter>().sharedMesh = mesh;
        gable.AddComponent<MeshRenderer>().sharedMaterial = backing;
        float fasciaLength = Mathf.Sqrt(1.78f * 1.78f + 0.87f * 0.87f);
        float angle = Mathf.Atan2(0.87f, 1.78f) * Mathf.Rad2Deg;
        CreateBox(root, name + " Left Trim", new Vector3(-0.89f, 3.255f, z + (z > 0f ? 0.04f : -0.04f)), new Vector3(fasciaLength, 0.11f, 0.09f), trim, new Vector3(0f, 0f, -angle));
        CreateBox(root, name + " Right Trim", new Vector3(0.89f, 3.255f, z + (z > 0f ? 0.04f : -0.04f)), new Vector3(fasciaLength, 0.11f, 0.09f), trim, new Vector3(0f, 0f, angle));
    }

    private static void BuildEntry(Transform root, Material lightWood, Material darkWood, Material brass, Material lantern, Material sign, Material roof, AnimatorController controller)
    {
        CreateBox(root, "Door Frame Left", new Vector3(-0.69f, 1.6f, 1.76f), new Vector3(0.16f, 2.32f, 0.2f), lightWood);
        CreateBox(root, "Door Frame Right", new Vector3(0.69f, 1.6f, 1.76f), new Vector3(0.16f, 2.32f, 0.2f), lightWood);
        CreateBox(root, "Door Header", new Vector3(0f, 2.77f, 1.76f), new Vector3(1.5f, 0.19f, 0.23f), darkWood);

        GameObject pivot = new GameObject("DoorPivot");
        pivot.transform.SetParent(root, false);
        pivot.transform.localPosition = new Vector3(-0.61f, 0.53f, 1.79f);
        GameObject door = new GameObject("Door");
        door.transform.SetParent(pivot.transform, false);
        door.transform.localPosition = new Vector3(0.61f, 1.05f, 0f);
        door.tag = "playerDoor";
        BoxCollider doorCollider = door.AddComponent<BoxCollider>();
        doorCollider.size = new Vector3(1.18f, 2.08f, 0.16f);
        Rigidbody body = door.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        AudioSource audio = door.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = null;
        audio.spatialBlend = 1f;
        audio.volume = 0.72f;
        audio.minDistance = 2.5f;
        audio.maxDistance = 18f;
        audio.rolloffMode = AudioRolloffMode.Logarithmic;
        CreateBox(door.transform, "Door Panel", Vector3.zero, new Vector3(1.18f, 2.08f, 0.16f), lightWood);

        for (int i = 0; i < 5; i++)
        {
            float y = 0.28f + i * 0.385f;
            CreateBox(door.transform, "Door Plank Inlay", new Vector3(-0.42f + i * 0.205f, y - 1.05f, -0.085f), new Vector3(0.035f, 1.8f, 0.02f), darkWood);
        }
        CreateBox(door.transform, "Door Cross Brace", new Vector3(0f, -0.36f, -0.11f), new Vector3(1.02f, 0.12f, 0.035f), darkWood);
        CreateBox(door.transform, "Door Cross Brace", new Vector3(0f, 0.34f, -0.11f), new Vector3(1.02f, 0.12f, 0.035f), darkWood);
        CreateBox(door.transform, "Door Handle Backplate", new Vector3(0.37f, 0.03f, -0.115f), new Vector3(0.14f, 0.22f, 0.035f), brass);
        CreateBox(door.transform, "Door Handle", new Vector3(0.32f, 0.03f, -0.15f), new Vector3(0.13f, 0.045f, 0.06f), brass);
        for (int hinge = 0; hinge < 3; hinge++)
        {
            float y = -0.72f + hinge * 0.72f;
            CreateBox(door.transform, "Door Hinge", new Vector3(-0.54f, y, -0.105f), new Vector3(0.18f, 0.07f, 0.04f), brass);
        }

        Animator animator = root.GetComponent<Animator>();
        if (animator == null)
            animator = root.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        CreateBox(root, "Lantern Backplate", new Vector3(0.96f, 2.32f, 1.78f), new Vector3(0.24f, 0.42f, 0.12f), darkWood);
        CreateBox(root, "Lantern Glass", new Vector3(0.96f, 2.31f, 1.88f), new Vector3(0.16f, 0.2f, 0.08f), lantern);
        CreateBox(root, "Lantern Cap", new Vector3(0.96f, 2.45f, 1.88f), new Vector3(0.28f, 0.06f, 0.17f), brass);
        GameObject lanternLight = new GameObject("Lantern Light");
        lanternLight.transform.SetParent(root, false);
        lanternLight.transform.localPosition = new Vector3(0.96f, 2.31f, 2.04f);
        Light point = lanternLight.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = new Color(1f, 0.57f, 0.25f);
        point.intensity = 1.15f;
        point.range = 5.5f;
        point.shadows = LightShadows.None;

        GameObject plaque = CreateBox(root, "Outpost Plaque", new Vector3(0f, 2.37f, 1.84f), new Vector3(0.88f, 0.34f, 0.08f), darkWood);
        TextMesh label = new GameObject("Outpost Sign").AddComponent<TextMesh>();
        label.transform.SetParent(plaque.transform, false);
        label.transform.localPosition = new Vector3(0f, 0f, 0.56f);
        label.transform.localScale = Vector3.one;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 60;
        label.characterSize = 0.1f;
        label.color = new Color(0.83f, 0.71f, 0.48f);
        label.text = "OUTPOST 07";
        label.GetComponent<MeshRenderer>().sharedMaterial = sign;

        CreateBox(root, "Porch Canopy", new Vector3(0f, 2.91f, 2.12f), new Vector3(1.75f, 0.12f, 0.95f), roof);
        CreateBox(root, "Porch Canopy Support Left", new Vector3(-0.79f, 2.44f, 2.42f), new Vector3(0.09f, 0.88f, 0.09f), darkWood);
        CreateBox(root, "Porch Canopy Support Right", new Vector3(0.79f, 2.44f, 2.42f), new Vector3(0.09f, 0.88f, 0.09f), darkWood);
    }

    private static AnimationClip CreateClip(string name, float closedAngle, float openAngle)
    {
        string path = AnimationPath + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name, frameRate = 30f };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.frameRate = 30f;
        clip.wrapMode = WrapMode.ClampForever;
        float duration = name == "idle" ? 0.05f : 0.85f;
        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, closedAngle, 0f, 0f),
            new Keyframe(duration, openAngle, 0f, 0f));
        clip.SetCurve("DoorPivot", typeof(Transform), "localEulerAnglesRaw.y", curve);
        AnimationUtility.SetAnimationClipSettings(clip, new AnimationClipSettings { loopTime = false, stopTime = duration });
        clip.EnsureQuaternionContinuity();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(AnimationClip idleClip, AnimationClip openClip, AnimationClip shutClip)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("DoorOpen", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states)
            machine.RemoveState(child.state);

        AnimatorState idle = machine.AddState("idle");
        AnimatorState open = machine.AddState("dooropen");
        AnimatorState shut = machine.AddState("doorshut");
        idle.motion = idleClip;
        open.motion = openClip;
        shut.motion = shutClip;
        machine.defaultState = idle;
        AddDoorTransition(idle, open, true);
        AddDoorTransition(open, shut, false);
        AddDoorTransition(shut, open, true);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddDoorTransition(AnimatorState from, AnimatorState to, bool doorOpen)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0f;
        transition.AddCondition(doorOpen ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "DoorOpen");
    }

    private static void CreateDoorSound()
    {
        if (File.Exists(SoundPath))
            return;

        const int sampleRate = 44100;
        const float duration = 1.15f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        byte[] wav = new byte[44 + sampleCount * 2];
        using (MemoryStream stream = new MemoryStream(wav))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + sampleCount * 2);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(sampleCount * 2);

            uint noise = 0x7a31d52fu;
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float progress = time / duration;
                float envelope = Mathf.Sin(Mathf.PI * progress) * Mathf.Exp(-0.38f * progress);
                float sweep = Mathf.Lerp(185f, 92f, progress);
                float creak = Mathf.Sin(2f * Mathf.PI * sweep * time) * 0.56f
                    + Mathf.Sin(2f * Mathf.PI * (sweep * 2.13f) * time) * 0.2f
                    + Mathf.Sin(2f * Mathf.PI * 327f * time) * 0.08f;
                noise ^= noise << 13;
                noise ^= noise >> 17;
                noise ^= noise << 5;
                float friction = ((noise & 0xffff) / 32767.5f - 1f) * 0.12f;
                float knockEnvelope = Mathf.Exp(-Mathf.Max(0f, time - 0.96f) * 42f) * (time >= 0.96f ? 1f : 0f);
                float knock = Mathf.Sin(2f * Mathf.PI * 74f * time) * knockEnvelope * 0.45f;
                float sample = Mathf.Clamp((creak + friction) * envelope + knock, -1f, 1f);
                writer.Write((short)Mathf.RoundToInt(sample * 20000f));
            }
        }
        File.WriteAllBytes(SoundPath, wav);
    }

    private static void PositionOutpost(Transform hut)
    {
        GameObject player = GameObject.Find("Player");
        GameObject island = GameObject.Find("Island");
        Terrain terrain = island != null ? island.GetComponent<Terrain>() : null;
        if (player == null || terrain == null)
            throw new InvalidOperationException("The active scene needs a Player and a Terrain to place the outpost.");

        Vector3 playerPosition = player.transform.position;
        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 terrainOrigin = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 bestPosition = playerPosition + forward * 10f;
        float bestScore = float.PositiveInfinity;
        float bestGround = terrain.SampleHeight(bestPosition) + terrainOrigin.y;

        for (float distance = 8f; distance <= 20f; distance += 2f)
        for (float lateral = -10f; lateral <= 10f; lateral += 2f)
        {
            Vector3 candidate = playerPosition + forward * distance + right * lateral;
            if (candidate.x < terrainOrigin.x + 3f || candidate.z < terrainOrigin.z + 3f
                || candidate.x > terrainOrigin.x + terrainSize.x - 3f || candidate.z > terrainOrigin.z + terrainSize.z - 3f)
                continue;

            float minHeight = float.PositiveInfinity;
            float maxHeight = float.NegativeInfinity;
            for (int x = -2; x <= 2; x += 2)
            for (int z = -2; z <= 2; z += 2)
            {
                Vector3 sample = candidate + new Vector3(x, 0f, z);
                float height = terrain.SampleHeight(sample) + terrainOrigin.y;
                minHeight = Mathf.Min(minHeight, height);
                maxHeight = Mathf.Max(maxHeight, height);
            }

            float unevenness = maxHeight - minHeight;
            float score = distance * 0.15f + Mathf.Abs(lateral) * 0.2f + unevenness * 5f;
            if (unevenness <= 1.2f && score < bestScore)
            {
                bestScore = score;
                bestPosition = candidate;
                bestGround = terrain.SampleHeight(candidate) + terrainOrigin.y;
            }
        }

        bestPosition.y = bestGround;
        Vector3 toPlayer = Vector3.ProjectOnPlane(playerPosition - bestPosition, Vector3.up).normalized;
        hut.SetPositionAndRotation(bestPosition, Quaternion.LookRotation(toPlayer, Vector3.up));
    }

    private static void BuildCoconutShy(Transform outpost)
    {
        Material wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "/Wood.mat");
        Material lightWood = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "/Sunbleached_Wood.mat");
        Material roof = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "/Deep_Teal_Roof.mat");
        if (wood == null || lightWood == null || roof == null)
            throw new InvalidOperationException("Could not load outpost materials for the coconut shy.");

        GameObject shy = new GameObject("Coconut Shy");
        shy.transform.SetParent(outpost, false);
        shy.transform.localPosition = new Vector3(-5.1f, 0f, 1.1f);
        shy.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        CreateBox(shy.transform, "Rear Panel", new Vector3(0f, 1.7f, -1.35f),
            new Vector3(4.1f, 3.4f, 0.16f), wood);
        CreateBox(shy.transform, "Left Post", new Vector3(-2.05f, 1.7f, -0.55f),
            new Vector3(0.22f, 3.4f, 1.8f), lightWood);
        CreateBox(shy.transform, "Right Post", new Vector3(2.05f, 1.7f, -0.55f),
            new Vector3(0.22f, 3.4f, 1.8f), lightWood);
        CreateBox(shy.transform, "Canopy", new Vector3(0f, 3.5f, -0.55f),
            new Vector3(4.4f, 0.24f, 2.2f), roof);
        CreateBox(shy.transform, "Sign", new Vector3(0f, 3.05f, -0.55f),
            new Vector3(2.8f, 0.55f, 0.12f), lightWood);

        GameObject mat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mat.name = "Throw Mat";
        mat.transform.SetParent(shy.transform, false);
        mat.transform.localPosition = new Vector3(0f, 0.08f, 1.85f);
        mat.transform.localScale = new Vector3(3.2f, 0.08f, 2.4f);
        MeshRenderer matRenderer = mat.GetComponent<MeshRenderer>();
        matRenderer.sharedMaterial = lightWood;

        BoxCollider throwZone = mat.AddComponent<BoxCollider>();
        throwZone.isTrigger = true;
        throwZone.center = new Vector3(0f, 10f, 0f);
        throwZone.size = new Vector3(1f, 20f, 1f);
        Component trigger = AddRuntimeComponent(mat, "ThrowTrigger");
        SerializedObject triggerSerialized = new SerializedObject(trigger);

        GameObject balloonParent = new GameObject("Balloons");
        balloonParent.transform.SetParent(shy.transform, false);
        Component minigame = AddRuntimeComponent(balloonParent, "BalloonMinigame");
        SerializedObject minigameSerialized = new SerializedObject(minigame);
        minigameSerialized.FindProperty("targetHits").intValue = 10;
        minigameSerialized.ApplyModifiedProperties();
        triggerSerialized.FindProperty("minigame").objectReferenceValue = minigame;
        triggerSerialized.ApplyModifiedProperties();

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Component[] targets = new Component[9];
        for (int row = 0; row < 3; row++)
        for (int column = 0; column < 3; column++)
        {
            int index = row * 3 + column;
            GameObject balloon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            balloon.name = "Balloon " + (index + 1);
            balloon.transform.SetParent(balloonParent.transform, false);
            balloon.transform.localPosition = new Vector3((column - 1) * 0.75f, 2.15f + (2 - row) * 0.75f, -1.23f);
            balloon.transform.localScale = Vector3.one * 0.38f;
            balloon.SetActive(false);

            Collider balloonCollider = balloon.GetComponent<Collider>();
            balloonCollider.isTrigger = false;
            Rigidbody balloonBody = balloon.AddComponent<Rigidbody>();
            balloonBody.isKinematic = true;

            Renderer balloonRenderer = balloon.GetComponent<Renderer>();
            Material balloonMaterial = new Material(shader);
            balloonMaterial.color = new Color(
                0.35f + column * 0.25f,
                0.25f + row * 0.22f,
                0.72f - row * 0.16f);
            balloonRenderer.sharedMaterial = balloonMaterial;

            Component target = AddRuntimeComponent(balloon, "BalloonTarget");
            SerializedObject targetSerialized = new SerializedObject(target);
            targetSerialized.FindProperty("balloonIndex").intValue = index;
            targetSerialized.FindProperty("minigame").objectReferenceValue = minigame;
            targetSerialized.ApplyModifiedProperties();
            targets[index] = target;
        }

        SerializedObject minigameTargets = new SerializedObject(minigame);
        SerializedProperty balloonTargets = minigameTargets.FindProperty("balloons");
        if (balloonTargets != null)
        {
            balloonTargets.arraySize = targets.Length;
            for (int i = 0; i < targets.Length; i++)
                balloonTargets.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
        }
        minigameTargets.ApplyModifiedProperties();

        GameObject reward = CreateRewardModel(shy.transform);
        reward.name = "Minigame Reward";
        AddRuntimeComponent(reward, "RewardDisplay");
        reward.SetActive(false);
        reward.SetActive(false);
    }

    private static GameObject CreateRewardModel(Transform parent)
    {
        GameObject reward = new GameObject("Reward Battery");
        reward.transform.SetParent(parent, false);
        reward.transform.localPosition = new Vector3(0f, 0.75f, 1.15f);
        reward.transform.localRotation = Quaternion.identity;
        reward.transform.localScale = Vector3.one * 0.55f;

        Material powerCase = AssetDatabase.LoadAssetAtPath<Material>("Assets/PowerCells/Materials/PowerCell_Case.mat");
        Material powerTerminal = AssetDatabase.LoadAssetAtPath<Material>("Assets/PowerCells/Materials/PowerCell_Terminals.mat");
        GameObject caseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        caseObject.name = "Battery";
        caseObject.transform.SetParent(reward.transform, false);
        caseObject.transform.localScale = new Vector3(1.2f, 0.7f, 0.6f);
        if (powerCase != null)
            caseObject.GetComponent<Renderer>().sharedMaterial = powerCase;
        UnityEngine.Object.DestroyImmediate(caseObject.GetComponent<Collider>());

        for (int i = -1; i <= 1; i += 2)
        {
            GameObject terminal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            terminal.name = "Terminal";
            terminal.transform.SetParent(reward.transform, false);
            terminal.transform.localPosition = new Vector3(i * 0.33f, 0.43f, 0f);
            terminal.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
            if (powerTerminal != null)
                terminal.GetComponent<Renderer>().sharedMaterial = powerTerminal;
            UnityEngine.Object.DestroyImmediate(terminal.GetComponent<Collider>());
        }

        return reward;
    }

    private static void AttachPlayerScript(AudioClip sound)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            throw new InvalidOperationException("Could not find the Player object.");

        Camera playerCamera = player.GetComponentInChildren<Camera>();
        if (playerCamera == null)
            throw new InvalidOperationException("Could not find a Camera under the Player object.");

        Transform launcherTransform = playerCamera.transform.Find("Launcher");
        if (launcherTransform == null)
        {
            GameObject launcher = new GameObject("Launcher");
            launcherTransform = launcher.transform;
            launcherTransform.SetParent(playerCamera.transform, false);
            launcherTransform.localPosition = new Vector3(0.5f, -0.3f, 1f);
        }

        Rigidbody coconutPrefab = CreateCoconutPrefab();
        AudioSource launcherAudio = launcherTransform.GetComponent<AudioSource>();
        if (launcherAudio == null)
            launcherAudio = launcherTransform.gameObject.AddComponent<AudioSource>();
        launcherAudio.playOnAwake = false;

        Component thrower = launcherTransform.GetComponent("CoconutThrower");
        if (thrower == null)
            thrower = AddRuntimeComponent(launcherTransform.gameObject, "CoconutThrower");
        SerializedObject throwerSerialized = new SerializedObject(thrower);
        throwerSerialized.FindProperty("coconutPrefab").objectReferenceValue = coconutPrefab;
        throwerSerialized.FindProperty("throwSpeed").floatValue = 30f;
        throwerSerialized.FindProperty("throwCooldown").floatValue = 0.25f;
        throwerSerialized.ApplyModifiedProperties();

        PlayerCollisions collisions = player.GetComponent<PlayerCollisions>();
        if (collisions == null)
            collisions = player.AddComponent<PlayerCollisions>();
        SerializedObject serialized = new SerializedObject(collisions);
        serialized.FindProperty("doorOpenSound").objectReferenceValue = sound;
        serialized.FindProperty("playerCamera").objectReferenceValue = playerCamera.transform;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(player);
    }

    private static Rigidbody CreateCoconutPrefab()
    {
        CreateFolderIfMissing("Assets", "Prefabs");
        const string prefabPath = "Assets/Prefabs/Coconut.prefab";
        Rigidbody existing = AssetDatabase.LoadAssetAtPath<Rigidbody>(prefabPath);
        if (existing != null)
            return existing;

        GameObject coconut = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coconut.name = "Coconut";
        coconut.tag = "coconut";
        coconut.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = new Color(0.42f, 0.22f, 0.09f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0f);
            coconut.GetComponent<Renderer>().sharedMaterial = material;
        }

        Rigidbody body = coconut.AddComponent<Rigidbody>();
        body.mass = 0.35f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        System.Type tidyType = System.Type.GetType("CoconutTidyObject, Assembly-CSharp");
        if (tidyType == null)
            throw new InvalidOperationException("Could not load CoconutTidyObject.");
        coconut.AddComponent(tidyType);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(coconut, prefabPath);
        UnityEngine.Object.DestroyImmediate(coconut);
        return prefab != null ? AssetDatabase.LoadAssetAtPath<Rigidbody>(prefabPath) : null;
    }
}
