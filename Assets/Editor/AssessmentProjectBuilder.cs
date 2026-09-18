using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;


// This automates editor placement; it is not evidence of hand placement by the student.
[InitializeOnLoad]
public static class AssessmentProjectBuilder
{
    const string Root = "Assets/Assessment3Materials/";
    static AssessmentProjectBuilder() { EditorApplication.delayCall += AutoBuild; }
    static void AutoBuild()
    {
        if (!File.Exists("BUILD_REQUESTED") || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += AutoBuild; return; }
        File.Delete("BUILD_REQUESTED");
        try { Build(); AssessmentValidation.Begin(); }
        catch (Exception e) { Directory.CreateDirectory("ValidationResults"); File.WriteAllText("ValidationResults/BuildFailure.txt", e.ToString()); Debug.LogException(e); }
    }
    static T Load<T>(string path) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(Root + path);
        if (!asset) throw new Exception("Missing asset: " + path);
        return asset;
    }
    static GameObject Group(string name, Transform parent = null)
    { var g = new GameObject(name); g.transform.SetParent(parent, false); return g; }
    static Camera Camera(bool fit)
    {
        var go = Group("Main Camera"); go.tag = "MainCamera";
        var c = go.AddComponent<Camera>(); c.orthographic = true; c.orthographicSize = fit ? 16.5f : 5;
        c.backgroundColor = new Color(.015f, .027f, .055f); c.clearFlags = CameraClearFlags.SolidColor;
        go.transform.position = new Vector3(0, 0, -10); go.AddComponent<AudioListener>();
        if (fit) go.AddComponent<FitLevelCamera>();
        return c;
    }
    static GameObject Character(string name, string frame, string controller, Transform parent, Vector3 position)
    {
        var g = Group(name, parent); g.transform.localPosition = position;
        var renderer = g.AddComponent<SpriteRenderer>(); renderer.sprite = Load<Sprite>("Sprites/Frames/" + frame + ".asset"); renderer.sortingOrder = 10;
        if (controller != null) g.AddComponent<Animator>().runtimeAnimatorController = Load<RuntimeAnimatorController>("Animators/" + controller + ".controller");
        return g;
    }
    static GameObject Characters(bool lab)
    {
        var parent = Group("Characters");
        var player = Character("PacStudent", "Player_Walk_Right_0", "PacStudentAnimator", parent.transform, lab ? new Vector3(-4, 2) : new Vector3(-12.5f, 13));
        for (int i = 1; i <= 4; i++) Character("Drone0" + i, "Drone" + i + "_Walk_Right_0", "GhostAnimator_" + i, parent.transform, lab ? new Vector3(i * 2 - 4, 2) : new Vector3(i - 2.5f, 0));
        Character("Bonus", "BonusSaturn_0", null, parent.transform, lab ? new Vector3(0, -1) : new Vector3(-2.5f, 0));
        Character("LifeIcon", "LifeIcon_0", null, parent.transform, lab ? new Vector3(2, -1) : new Vector3(-12, -15.5f));
        if (!lab)
        {
            var a = player.AddComponent<AudioSource>(); a.playOnAwake = false; a.loop = true; a.spatialBlend = 0; a.volume = .12f;
            a.clip = Load<AudioClip>("Audio Clips/SFX/SFX_Move.wav");
            var m = player.AddComponent<PacStudentMovement>(); m.animator = player.GetComponent<Animator>(); m.movingAudio = a;
        }
        return parent;
    }
    [MenuItem("Assessment 3/Build complete project scenes")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play mode first.");
        if (File.Exists("Assets/Scenes/RecreatedLevel.unity")) throw new Exception("Scenes already exist; refusing to overwrite your saved work.");
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Tiles/Tile_1.prefab")) Assessment3MaterialsBuilder.Build();
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("ValidationResults");
        var tiles = new GameObject[9]; for (int i = 1; i <= 8; i++) tiles[i] = Load<GameObject>("Prefabs/Tiles/Tile_" + i + ".prefab");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = Camera(true); Characters(false); Group("UI");
        var audio = Group("Audio"); var music = Group("MusicPlayer", audio.transform);
        var source = music.AddComponent<AudioSource>(); source.playOnAwake = false; source.volume = .25f; source.spatialBlend = 0;
        var intro = music.AddComponent<IntroMusic>(); intro.music = source;
        intro.intro = Load<AudioClip>("Audio Clips/Music/BGM_Intro.wav"); intro.normal = Load<AudioClip>("Audio Clips/Music/BGM_Normal.wav");
        var manual = Group("ManualLevel01");
        var table = File.ReadAllLines("Assets/Editor/ManualPlacement.csv").Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim('\uFEFF').Split(',').Select(int.Parse).ToArray()).ToArray();
        if (table.Length != 166) throw new Exception("Manual placement table must contain 166 tiles.");
        string[] names = { "TopLeft", "TopRight", "BottomLeft", "BottomRight" };
        for (int q = 0; q < 4; q++)
        {
            bool right = q % 2 == 1, bottom = q >= 2;
            var quadrant = Group(names[q], manual.transform);
            quadrant.transform.localPosition = new Vector3(right ? 13.5f : -13.5f, bottom ? -14 : 14);
            quadrant.transform.localScale = new Vector3(right ? -1 : 1, bottom ? -1 : 1, 1);
            for (int r = 0; r < (bottom ? 14 : 15); r++)
            {
                var row = Group("Row_" + r.ToString("00"), quadrant.transform);
                foreach (var cell in table.Where(v => v[0] == r))
                {
                    var tile = (GameObject)PrefabUtility.InstantiatePrefab(tiles[cell[2]], row.transform);
                    tile.name = "Tile_" + r.ToString("00") + "_" + cell[1].ToString("00");
                    tile.transform.localPosition = new Vector3(cell[3], cell[4]);
                    tile.transform.localRotation = Quaternion.Euler(0, 0, cell[5]);
                }
            }
        }
        var systems = Group("Level Systems"); var generator = systems.AddComponent<LevelGenerator>();
        generator.manualLevel = manual; generator.tilePrefabs = tiles; generator.levelCamera = camera;
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/RecreatedLevel.unity");
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera(false); Characters(true);
        var power = (GameObject)PrefabUtility.InstantiatePrefab(tiles[6]); power.name = "PowerPellet"; power.transform.position = new Vector3(-2, -1);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/AnimationLab.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/RecreatedLevel.unity", true), new EditorBuildSettingsScene("Assets/Scenes/AnimationLab.unity", true) };
        PlayerSettings.companyName = "Assessment3"; PlayerSettings.productName = "MyPacStudent";
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 900;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity");
        File.WriteAllText("ValidationResults/BuildCompleted.txt", "Scenes and references saved by Unity " + Application.unityVersion + " at " + DateTime.Now.ToString("O") + "\nPlacement automated from the supplied manual table.\n");
        Debug.Log("ASSESSMENT3_PROJECT_BUILT");
    }
}
