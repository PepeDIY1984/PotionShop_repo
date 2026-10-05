#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PotionMortarSceneBuilder
{
    private const string ScenePath = "Assets/PotionMortarPrototype/Scenes/PotionMortarDemo.unity";
    private const string MaterialsPath = "Assets/PotionMortarPrototype/Materials";

    [MenuItem("Potion Mortar/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        Directory.CreateDirectory(MaterialsPath);
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PopulateScene();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Saved the static potion mortar scene to " + ScenePath);
    }

    private static void PopulateScene()
    {
        var root = new GameObject("Potion Mortar Demo");
        PotionMortarDemo demo = root.AddComponent<PotionMortarDemo>();

        BuildCamera();
        BuildLighting();
        BuildWorkbench(demo);
        BuildIngredients();

    }

    private static void BuildCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.transform.position = new Vector3(0, 6.5f, -8.8f);
        cameraObject.transform.LookAt(new Vector3(0, 0.65f, 0.15f));
        camera.fieldOfView = 43;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.105f, 0.16f);
    }

    private static void BuildLighting()
    {
        var lightObject = new GameObject("Key Light");
        lightObject.transform.rotation = Quaternion.Euler(48, -28, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
    }

    private static void BuildWorkbench(PotionMortarDemo demo)
    {
        Vector3 mortarCenter = new Vector3(-1.2f, 0.65f, 0.2f);
        Vector3 cauldronCenter = new Vector3(1.35f, 0.65f, 0.2f);
        GameObject mortar = new GameObject("Mortero");
        mortar.transform.position = mortarCenter;
        demo.mortarCenter = mortar.transform;

        MakePrimitive("Table", PrimitiveType.Cube, new Vector3(0, -0.18f, 0.1f), new Vector3(7.2f, 0.35f, 5.4f), new Color(0.26f, 0.16f, 0.12f));
        MakePrimitive("Mortero · base", PrimitiveType.Cylinder, new Vector3(0f, -0.4f, 0f), new Vector3(1.35f, 0.5f, 1.35f), new Color(0.43f, 0.39f, 0.48f), mortar.transform);
        MakePrimitive("Mortero · cuenco", PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.2f, 0.72f, 1.2f), new Color(0.55f, 0.51f, 0.6f), mortar.transform);
        MakePrimitive("Mortero · interior", PrimitiveType.Cylinder, new Vector3(0f, 0.31f, 0f), new Vector3(0.96f, 0.14f, 0.96f), new Color(0.16f, 0.14f, 0.21f), mortar.transform);
        MakeMixture("Mezcla del mortero", new Vector3(0f, 0.41f, 0f), new Vector3(0.76f, 0.05f, 0.76f), mortar.transform);
        MakePrimitive("Mano del mortero", PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0f), new Vector3(0.25f, 0.72f, 0.25f), new Color(0.48f, 0.29f, 0.18f), mortar.transform);

        MakePrimitive("Caldero · base", PrimitiveType.Cylinder, new Vector3(cauldronCenter.x, 0.25f, cauldronCenter.z), new Vector3(2.35f, 0.5f, 2.35f), new Color(0.23f, 0.28f, 0.32f));
        MakePrimitive("Caldero", PrimitiveType.Cylinder, cauldronCenter, new Vector3(2.2f, 0.72f, 2.2f), new Color(0.30f, 0.36f, 0.39f));
        MakePrimitive("Caldero · interior", PrimitiveType.Cylinder, new Vector3(cauldronCenter.x, 0.96f, cauldronCenter.z), new Vector3(1.88f, 0.14f, 1.88f), new Color(0.12f, 0.16f, 0.18f));
        MakeMixture("Mezcla del caldero", new Vector3(cauldronCenter.x, 1.06f, cauldronCenter.z), new Vector3(1.55f, 0.05f, 1.55f));

        GameObject ladle = new GameObject("Cucharón");
        ladle.transform.position = new Vector3(cauldronCenter.x, 1.65f, cauldronCenter.z);
        MakePrimitive("Cucharón · mango", PrimitiveType.Capsule, new Vector3(0f, 0.13f, 0f), new Vector3(0.1f, 0.43f, 0.1f), new Color(0.7f, 0.48f, 0.24f), ladle.transform);
        MakePrimitive("Cucharón · cazo", PrimitiveType.Sphere, new Vector3(0f, -0.22f, 0f), new Vector3(0.38f, 0.12f, 0.34f), new Color(0.68f, 0.72f, 0.76f), ladle.transform);

        MakePrimitive("Estante de ingredientes", PrimitiveType.Cube, new Vector3(0, 0.12f, -1.8f), new Vector3(5.8f, 0.16f, 0.72f), new Color(0.38f, 0.24f, 0.16f));
        MakePrimitive("Fondo", PrimitiveType.Cube, new Vector3(0, -1.1f, 2.7f), new Vector3(7.2f, 2f, 0.25f), new Color(0.21f, 0.16f, 0.23f));
    }

    private static void BuildIngredients()
    {
        float[] positions = { -1.8f, -0.6f, 0.6f };
        string[] names = { "Raíz lunar", "Seta azul", "Pétalo ígneo" };
        Color[] colors = {
            new Color(0.48f, 0.72f, 0.42f), new Color(0.28f, 0.55f, 0.92f), new Color(0.95f, 0.34f, 0.12f)
        };

        for (int i = 0; i < names.Length; i++)
        {
            Vector3 position = new Vector3(positions[i], 0.48f, -1.78f);
            GameObject ingredient = MakePrimitive("Ingrediente · " + names[i], PrimitiveType.Sphere, position, Vector3.one * 0.38f, colors[i]);
            ingredient.transform.position = position;
        }
    }

    private static void MakeMixture(string name, Vector3 position, Vector3 scale, Transform parent = null)
    {
        GameObject mixture = MakePrimitive(name, PrimitiveType.Cylinder, position, scale, new Color(0.35f, 0.55f, 0.45f), parent);
        Collider collider = mixture.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        mixture.SetActive(false);
    }

    private static GameObject MakePrimitive(string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color, Transform parent = null)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
        }
        else go.transform.position = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = GetMaterial(name, color);
        return go;
    }

    private static Material GetMaterial(string objectName, Color color)
    {
        string fileName = objectName.Replace(' ', '_').Replace('·', '_');
        string path = MaterialsPath + "/" + fileName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        bool transparentBowl = objectName == "Mortero · cuenco" || objectName == "Caldero";
        if (transparentBowl)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            color.a = 0.48f;
        }
        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }
}
#endif
