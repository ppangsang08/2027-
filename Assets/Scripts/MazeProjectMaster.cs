using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(MazeGenerator))]
[RequireComponent(typeof(MazeVisualizer))]
[RequireComponent(typeof(MazePlayerController))]
[RequireComponent(typeof(MazeUIController))]
public class MazeProjectMaster : MonoBehaviour
{
    public int width = 45;
    public int height = 45;
    public float cellSize = 1f;
    public bool useSeed = false;
    public int seed = 12345;
    public string resultSceneName = "Finish";
    public bool disableOldCameraOnResult = true;
    public float resultSceneFadeTime = 0.5f;
    public bool enableShadows = false;
    public Material wallMaterial;
    public Material floorMaterial;
    public Material playerMaterial;
    public Vector2 timerAnchorMin = new Vector2(0.1f, 0.95f);
    public Vector2 timerAnchorMax = new Vector2(0.9f, 1f);
    public int sessionDurationSeconds = 300;
    public bool animatePaths = true;
    public float pathAnimateDelay = 0.03f;

    MazeGenerator generator;
    MazeVisualizer visualizer;
    MazePlayerController playerController;
    MazeUIController uiController;

    bool sessionActive = false;
    bool isFinished = false;
    float sessionTimeRemaining = 0f;
    float timeUsed = 0f;

    void Awake()
    {
        generator = GetComponent<MazeGenerator>();
        visualizer = GetComponent<MazeVisualizer>();
        playerController = GetComponent<MazePlayerController>();
        uiController = GetComponent<MazeUIController>();
    }

    void Start()
    {
        InitializeAll();
    }

    void InitializeAll()
    {
        if (width < 5) width = 5;
        if (height < 5) height = 5;
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        generator.Setup(width, height, useSeed, seed);
        generator.GenerateMaze();

        visualizer.Setup(wallMaterial, floorMaterial, enableShadows);
        visualizer.BuildVisuals(generator, cellSize);
        visualizer.SetupCamera(width, height, cellSize);

        GameObject playerGO = visualizer.SpawnPlayer(generator.startPos, cellSize, playerMaterial, enableShadows);
        playerController.Setup(generator, playerGO, cellSize);
        playerController.OnReachedEnd += HandlePlayerReachedEnd;

        uiController.Setup(this, generator, playerController, timerAnchorMin, timerAnchorMax, pathAnimateDelay);
        uiController.SetTimerText(sessionDurationSeconds);

        sessionActive = true;
        isFinished = false;
        sessionTimeRemaining = sessionDurationSeconds;
        timeUsed = 0f;
    }

    void Update()
    {
        if (!sessionActive || isFinished) return;
        UpdateSessionTimer();
    }

    void UpdateSessionTimer()
    {
        sessionTimeRemaining -= Time.deltaTime;
        timeUsed = sessionDurationSeconds - sessionTimeRemaining;
        uiController.SetTimerText(sessionTimeRemaining);
        if (sessionTimeRemaining <= 0f)
        {
            sessionTimeRemaining = 0f;
            FinishSession(false);
        }
    }

    void HandlePlayerReachedEnd(bool reachedGoal)
    {
        FinishSession(reachedGoal);
    }

    void FinishSession(bool reachedGoal)
    {
        if (isFinished) return;
        isFinished = true;
        sessionActive = false;
        playerController.sessionActive = false;

        float pctDFS = generator.ComputeLcsPercentage(playerController.userPath, generator.dfsPath);
        float pctBFS = generator.ComputeLcsPercentage(playerController.userPath, generator.bfsPath);

        float usedSec = sessionDurationSeconds - sessionTimeRemaining;
        string score = $"{(reachedGoal ? "CLEAR!" : "TIME UP")}\nUser vs DFS: {pctDFS:F1}%\nUser vs BFS: {pctBFS:F1}%\nSteps: {playerController.userPath.Count}\nTime: {usedSec:F1}s";
        StartCoroutine(TransitionToResultScene(score, reachedGoal, generator.bfsPath, generator.dfsPath));
    }

    IEnumerator TransitionToResultScene(string scoreText, bool reachedGoal, List<Vector2Int> bfs, List<Vector2Int> dfs)
    {
        yield return new WaitForSeconds(resultSceneFadeTime);
        
        // Disable game UI canvas immediately
        uiController.DestroyCanvas();
        yield return new WaitForEndOfFrame();
        
        // Create and activate result scene
        var resultScene = SceneManager.CreateScene(resultSceneName);
        SceneManager.SetActiveScene(resultScene);
        
        // Create result UI in the new scene
        GameObject resultUIGO = new GameObject("ResultUIContainer");
        SceneManager.MoveGameObjectToScene(resultUIGO, resultScene);
        
        // Make the UI controller create result scene UI
        uiController.CreateResultSceneUI(scoreText, reachedGoal, bfs, dfs);
        
        // Move maze and camera to result scene
        if (visualizer != null && visualizer.MazeRoot != null)
        {
            SceneManager.MoveGameObjectToScene(visualizer.MazeRoot.gameObject, resultScene);
        }
        if (Camera.main != null)
        {
            SceneManager.MoveGameObjectToScene(Camera.main.gameObject, resultScene);
        }
    }

    void CreateResultSceneUI(string scoreText, bool reachedGoal, List<Vector2Int> bfs, List<Vector2Int> dfs)
    {
        // Delegated to uiController
    }
}
