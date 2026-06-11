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
    public int width = 151;
    public int height = 151;
    public float cellSize = 1f;
    public bool useSeed = false;
    public int seed = 12345;
    public int sessionDurationSeconds = 120;
    public string resultSceneName = "Finish";
    public bool disableOldCameraOnResult = true;
    public float resultSceneFadeTime = 0.5f;
    public bool enableShadows = false;
    public Material wallMaterial;
    public Material floorMaterial;
    public Material playerMaterial;
    public Vector2 timerAnchorMin = new Vector2(0.82f, 0.98f);
    public Vector2 timerAnchorMax = new Vector2(0.98f, 1f);
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
        uiController.StartTutorialAnimation();

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
        float pctDfsBfs = generator.ComputeLcsPercentage(generator.dfsPath, generator.bfsPath);

        float usedSec = sessionDurationSeconds - sessionTimeRemaining;
        string score = $"{(reachedGoal ? "CLEAR!" : "TIME UP")}\nUser vs DFS: {pctDFS:F1}%\nUser vs BFS: {pctBFS:F1}%\nDFS vs BFS: {pctDfsBfs:F1}%\nSteps: {playerController.userPath.Count}\nTime: {usedSec:F1}s";
        StartCoroutine(TransitionToResultScene(score, reachedGoal, generator.bfsPath, generator.dfsPath, playerController.userPath));
    }

    IEnumerator TransitionToResultScene(string scoreText, bool reachedGoal, List<Vector2Int> bfs, List<Vector2Int> dfs, List<Vector2Int> userPath)
    {
        // Create transition overlay with sweep animation
        GameObject transitionGO = new GameObject("TransitionOverlay");
        Canvas transitionCanvas = transitionGO.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionGO.AddComponent<CanvasScaler>();
        transitionGO.AddComponent<GraphicRaycaster>();
        
        GameObject panelGO = new GameObject("Panel");
        panelGO.transform.parent = transitionGO.transform;
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 1);
        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        CanvasGroup canvasGroup = panelGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1;
        
        GameObject textGO = new GameObject("TransitionText");
        textGO.transform.parent = panelGO.transform;
        TMP_Text transitionText = textGO.AddComponent<TextMeshProUGUI>();
        transitionText.text = "ANALYZING RESULTS...";
        transitionText.fontSize = 60;
        transitionText.alignment = TextAlignmentOptions.Center;
        transitionText.color = Color.white;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Wait with analyzing message
        yield return new WaitForSeconds(1.5f);
        
        // Sweep animation (slide left while fading out)
        float sweepDuration = 1f;
        float elapsedTime = 0f;
        while (elapsedTime < sweepDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / sweepDuration;
            
            // Slide to left
            panelRect.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(-1920, 0), t);
            canvasGroup.alpha = Mathf.Lerp(1, 0, t);
            
            yield return null;
        }
        
        // Disable game UI canvas
        uiController.DestroyCanvas();
        yield return new WaitForEndOfFrame();
        
        // Create and activate result scene
        var resultScene = SceneManager.CreateScene(resultSceneName);
        SceneManager.SetActiveScene(resultScene);
        
        // Create result UI in the new scene
        GameObject resultUIGO = new GameObject("ResultUIContainer");
        SceneManager.MoveGameObjectToScene(resultUIGO, resultScene);
        
        // Make the UI controller create result scene UI with user path
        uiController.CreateResultSceneUI(scoreText, reachedGoal, bfs, dfs, userPath);
        
        // Move maze and camera to result scene
        if (visualizer != null && visualizer.MazeRoot != null)
        {
            SceneManager.MoveGameObjectToScene(visualizer.MazeRoot.gameObject, resultScene);
        }
        if (Camera.main != null)
        {
            SceneManager.MoveGameObjectToScene(Camera.main.gameObject, resultScene);
        }
        
        // Destroy transition overlay
        Destroy(transitionGO);
    }

    void CreateResultSceneUI(string scoreText, bool reachedGoal, List<Vector2Int> bfs, List<Vector2Int> dfs)
    {
        // Delegated to uiController
    }
}
