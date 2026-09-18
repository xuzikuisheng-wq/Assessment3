using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class AssessmentValidation
{
    static readonly List<string> results = new List<string>();
    static readonly Dictionary<string, HashSet<int>> seen = new Dictionary<string, HashSet<int>>();
    static int errors; static float lastTime = -1; static Vector3 lastPosition;
    static bool captured, musicChecked, introChecked, mapChecked;
    const string StageKey = "AssessmentValidation.Stage";
    static AssessmentValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += State;
        Application.logMessageReceived += Log;
    }
    static void Log(string message, string stack, LogType type)
    {
        if (SessionState.GetInt(StageKey, 0) == 0) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        { errors++; results.Add("ERROR: " + message + "\n" + stack); }
    }
    static void Check(bool value, string name)
    { results.Add((value ? "PASS: " : "FAIL: ") + name); if (!value) errors++; }
    [MenuItem("Assessment 3/Validate saved scenes and Play mode")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory("ValidationResults"); results.Clear(); errors = 0;
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity");
            var g = UnityEngine.Object.FindAnyObjectByType<LevelGenerator>();
            Check(g && g.manualLevel && g.levelCamera && g.tilePrefabs.Skip(1).All(x => x), "Generator references assigned");
            Check(g.tilePrefabs.Skip(1).All(x => x.GetComponent<SpriteRenderer>().sprite), "Every tile prefab has a sprite reference");
            Check(g.manualLevel.GetComponentsInChildren<SpriteRenderer>().All(x => x.sprite), "Every saved map tile has a sprite reference");
            Check(g.manualLevel.transform.childCount == 4, "Four saved manual quadrants before Play");
            Check(g.manualLevel.GetComponentsInChildren<SpriteRenderer>().Length == 660, "660 nonempty cells in saved manual map");
            var whole = LevelGenerator.Mirror(g.levelMap); var masks = TileConnections.Solve(whole);
            Check(whole.GetLength(0) == 29 && whole.GetLength(1) == 28, "Default 29 by 28 mirror dimensions");
            var table = File.ReadAllLines("Assets/Editor/ManualPlacement.csv").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim('\uFEFF').Split(',').Select(int.Parse).ToArray());
            Check(table.All(v => g.levelMap[v[0], v[1]] == v[2] && Mathf.Abs(Mathf.DeltaAngle(TileConnections.Angle(v[2], masks[v[0] * 28 + v[1]]), v[5])) < .01f), "Generated orientation matches every manual table entry");
            int[,] small = {{1,2,2,2},{2,5,5,5},{2,5,5,5},{2,5,5,5}};
            var mirrored = LevelGenerator.Mirror(small); TileConnections.Solve(mirrored);
            Check(mirrored.GetLength(0) == 7 && mirrored.GetLength(1) == 8, "Alternative 4 by 4 quadrant solves as 7 by 8");
            bool rejected = false; try { TileConnections.Solve(new int[,] {{0,0,0},{0,1,0},{0,0,0}}); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Isolated corner rejected");
            Check(AssetDatabase.FindAssets("t:AnimationClip", new[]{"Assets/Assessment3Materials/Animations"}).Length == 46, "46 animation clips");
            Check(AssetDatabase.FindAssets("t:AnimatorController", new[]{"Assets/Assessment3Materials/Animators"}).Length == 6, "Six animator controllers");
            Check(AssetDatabase.FindAssets("t:AudioClip", new[]{"Assets/Assessment3Materials/Audio Clips"}).Length == 11, "Eleven audio clips");
            foreach (var id in AssetDatabase.FindAssets("t:AnimationClip", new[]{"Assets/Assessment3Materials/Animations"}))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(id));
                var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                Check(binding.Length == 1 && AnimationUtility.GetObjectReferenceCurve(clip, binding[0]).Select(k => k.value).Distinct().Count() >= 2, "Distinct animation frames: " + clip.name);
            }
            Save("StaticChecks.txt");
            File.WriteAllText("ValidationResults/StaticErrorCount.txt", errors.ToString());
            SessionState.SetInt(StageKey, 1); EditorApplication.isPlaying = true;
        }
        catch (Exception e) { Check(false, e.ToString()); Save("StaticChecks.txt"); SessionState.SetInt(StageKey, 0); }
    }
    static void State(PlayModeStateChange state)
    {
        int stage = SessionState.GetInt(StageKey, 0);
        if (state == PlayModeStateChange.EnteredPlayMode && stage > 0)
        {
            results.Clear(); seen.Clear(); errors = 0; lastTime = -1; captured = musicChecked = introChecked = mapChecked = false;
            Application.runInBackground = true;
        }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (stage == 2)
        {
            results.Clear(); results.AddRange(File.ReadAllLines("ValidationResults/MainRuntimeChecks.txt"));
            errors = int.Parse(File.ReadAllText("ValidationResults/MainErrorCount.txt"));
            var manual = GameObject.Find("ManualLevel01");
            Check(manual && !GameObject.Find("GeneratedLevel"), "Manual map restored after stopping Play");
            Save("MainRuntimeChecks.txt");
            File.WriteAllText("ValidationResults/MainErrorCount.txt", errors.ToString());
            SessionState.SetInt(StageKey, 3);
            EditorApplication.delayCall += () => { EditorSceneManager.OpenScene("Assets/Scenes/AnimationLab.unity"); EditorApplication.isPlaying = true; };
        }
        else if (stage == 4)
        {
            SessionState.SetInt(StageKey, 0);
            EditorSceneManager.OpenScene("Assets/Scenes/RecreatedLevel.unity");
            int total = int.Parse(File.ReadAllText("ValidationResults/AnimationErrorCount.txt")) + int.Parse(File.ReadAllText("ValidationResults/StaticErrorCount.txt")) + int.Parse(File.ReadAllText("ValidationResults/MainErrorCount.txt"));
            File.WriteAllText("ValidationResults/Result.txt", "Unity " + Application.unityVersion + "\nCompleted: " + DateTime.Now.ToString("O") + "\nError count: " + total + "\n" + (total == 0 ? "PASS" : "FAIL") + "\nAudio playback state checked; listening still requires a human.\n");
            Debug.Log("ASSESSMENT3_VALIDATION_FINISHED: " + total + " errors");
        }
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        int stage = SessionState.GetInt(StageKey, 0); if (stage != 1 && stage != 3) return;
        try
        {
            float t = Time.time;
            foreach (var animator in UnityEngine.Object.FindObjectsByType<Animator>())
            {
                if (animator.name != "PacStudent" && !animator.name.StartsWith("Drone")) continue;
                if (!seen.ContainsKey(animator.name)) seen[animator.name] = new HashSet<int>();
                seen[animator.name].Add(animator.GetCurrentAnimatorStateInfo(0).shortNameHash);
            }
            if (stage == 1)
            {
                var music = GameObject.Find("MusicPlayer").GetComponent<AudioSource>();
                if (!introChecked && t > .1f && t < 1) { Check(music.isPlaying && music.clip.name == "BGM_Intro" && !music.loop, "Intro starts once"); introChecked = true; }
                if (!musicChecked && t > 3.3f) { Check(music.isPlaying && music.loop && music.clip.name == "BGM_Normal", "Normal music loops after intro"); musicChecked = true; }
                if (!mapChecked && t > .5f)
                {
                    Check(!GameObject.Find("ManualLevel01") && GameObject.Find("GeneratedLevel"), "Runtime map replaces manual map");
                    Check(GameObject.Find("GeneratedLevel").GetComponentsInChildren<SpriteRenderer>().Length == 660, "Generated 660 cells"); mapChecked = true;
                }
                var player = GameObject.Find("PacStudent");
                // Evaluate absolute route position to include low-frame-rate corner crossings.
                if (t > .2f)
                {
                    float distance = t * 3 % 18;
                    var m = player.GetComponent<PacStudentMovement>(); int seg = 0;
                    while (distance >= Vector3.Distance(m.corners[seg], m.corners[(seg+1)%4])) { distance -= Vector3.Distance(m.corners[seg], m.corners[(seg+1)%4]); seg = (seg+1)%4; }
                    var expected = PacStudentMovement.Sample(m.corners[seg], m.corners[(seg+1)%4], distance);
                    if (Vector3.Distance(player.transform.position, expected) > .35f) { Check(false, "Route position mismatch at " + t); }
                    lastTime = t; lastPosition = player.transform.position;
                }
                if (!captured && t > 4) { Capture("MainPreview.png"); captured = true; }
                if (t > 35)
                {
                    Check(introChecked && musicChecked && mapChecked, "All timed main checks executed");
                    Check(player.GetComponent<AudioSource>().isPlaying, "Movement sound plays");
                    Check(seen.ContainsKey("PacStudent") && seen["PacStudent"].Count == 4, "Four movement directions observed");
                    Check(music.isPlaying && music.loop && music.clip.name == "BGM_Normal", "Normal music continues beyond a complete loop");
                    Save("MainRuntimeChecks.txt"); File.WriteAllText("ValidationResults/MainErrorCount.txt", errors.ToString());
                    SessionState.SetInt(StageKey, 2); EditorApplication.isPlaying = false;
                }
            }
            else
            {
                if (!captured && t > 1) { Capture("AnimationLabPreview.png"); captured = true; }
                if (t > 28)
                {
                    Check(!GameObject.Find("PacStudent").GetComponent<PacStudentMovement>(), "Lab has no movement component");
                    Check(seen.ContainsKey("PacStudent") && seen["PacStudent"].Count == 5, "Five player showcase states observed");
                    for (int i=1;i<=4;i++) Check(seen.ContainsKey("Drone0"+i) && seen["Drone0"+i].Count == 10, "All ten states observed for Drone0"+i);
                    Save("AnimationRuntimeChecks.txt"); File.WriteAllText("ValidationResults/AnimationErrorCount.txt", errors.ToString());
                    SessionState.SetInt(StageKey, 4); EditorApplication.isPlaying = false;
                }
            }
        }
        catch (Exception e) { Check(false, e.ToString()); Save("RuntimeFailure.txt"); SessionState.SetInt(StageKey, 0); EditorApplication.isPlaying = false; }
    }
    static void Save(string name) { File.WriteAllLines("ValidationResults/" + name, results.Concat(new[]{"Error count: " + errors})); }
    static void Capture(string name)
    {
        var camera = Camera.main; var target = new RenderTexture(1280, 900, 24); var previous = camera.targetTexture; var active = RenderTexture.active;
        camera.targetTexture = target; if (camera.GetComponent<FitLevelCamera>()) camera.GetComponent<FitLevelCamera>().Apply();
        camera.Render(); RenderTexture.active = target;
        var image = new Texture2D(1280, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0,0,1280,900),0,0); image.Apply();
        File.WriteAllBytes("ValidationResults/" + name, image.EncodeToPNG());
        camera.targetTexture = previous; RenderTexture.active = active; UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
}
