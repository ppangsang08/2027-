using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MazeUIController : MonoBehaviour
{
    Canvas canvas;
    TMP_Text timerText;
    TMP_Text resultText;
    TMP_Text bfsPercentText;
    TMP_Text dfsPercentText;
    TMP_Text pathModeText;

    MazeProjectMaster master;
    MazeGenerator maze;
    MazePlayerController player;
    float cellSize;
    float pathAnimateDelay;
    GameObject pathLineGO;
    LineRenderer pathLine;
    GameObject userPathLineGO;
    LineRenderer userPathLine;
    GameObject overlayPathLineGO;
    LineRenderer overlayPathLine;
    Coroutine tutorialAnimationCoroutine;
    CanvasGroup tutorialCanvasGroup;
    List<Vector2Int> cachedBFSPath;
    List<Vector2Int> cachedDFSPath;
    List<Vector2Int> cachedUserPath;
    GameObject pathFollower;
    GameObject userPathFollower;
    Coroutine playbackCoroutine;

    public void Setup(MazeProjectMaster master, MazeGenerator maze, MazePlayerController player, Vector2 timerAnchorMin, Vector2 timerAnchorMax, float pathAnimateDelay)
    {
        this.master = master;
        this.maze = maze;
        this.player = player;
        this.cellSize = master.cellSize;
        this.pathAnimateDelay = pathAnimateDelay;

        CreateEventSystem();
        CreateCanvas();
        CreateUI(timerAnchorMin, timerAnchorMax);
    }

    void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    void CreateCanvas()
    {
        GameObject cgo = new GameObject("Canvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();
    }

    void CreateUI(Vector2 timerAnchorMin, Vector2 timerAnchorMax)
    {
        GameObject timerGO = new GameObject("TimerText");
        timerGO.transform.parent = canvas.transform;
        timerText = timerGO.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 24;
        timerText.alignment = TextAlignmentOptions.TopRight;
        timerText.color = Color.white;
        RectTransform trt = timerText.GetComponent<RectTransform>();
        trt.anchorMin = timerAnchorMin;
        trt.anchorMax = timerAnchorMax;
        trt.offsetMin = new Vector2(0, -10);
        trt.offsetMax = new Vector2(-10, 0);
        timerText.text = "TIME: 5:00";

        // Create tutorial panel to hold all tutorial UI elements
        GameObject tutorialPanel = new GameObject("TutorialPanel");
        tutorialPanel.transform.parent = canvas.transform;
        tutorialCanvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
        RectTransform tutorialRect = tutorialPanel.AddComponent<RectTransform>();
        tutorialRect.anchorMin = Vector2.zero;
        tutorialRect.anchorMax = Vector2.one;
        tutorialRect.offsetMin = Vector2.zero;
        tutorialRect.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("ResultText");
        textGO.transform.parent = tutorialPanel.transform;
        resultText = textGO.AddComponent<TextMeshProUGUI>();
        resultText.fontSize = 24;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.color = Color.white;
        RectTransform rrt = resultText.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.1f, 0.75f);
        rrt.anchorMax = new Vector2(0.9f, 0.85f);
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;
        resultText.text = "Move with WASD or arrow keys";

        GameObject modeGO = new GameObject("PathModeText");
        modeGO.transform.parent = tutorialPanel.transform;
        pathModeText = modeGO.AddComponent<TextMeshProUGUI>();
        pathModeText.fontSize = 20;
        pathModeText.alignment = TextAlignmentOptions.Center;
        pathModeText.color = Color.white;
        RectTransform mrt = pathModeText.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0.1f, 0.68f);
        mrt.anchorMax = new Vector2(0.9f, 0.75f);
        mrt.offsetMin = Vector2.zero;
        mrt.offsetMax = Vector2.zero;
        pathModeText.text = "Press DFS or BFS to animate path";

        GameObject dfsPercGO = new GameObject("DFSPercentText");
        dfsPercGO.transform.parent = tutorialPanel.transform;
        dfsPercentText = dfsPercGO.AddComponent<TextMeshProUGUI>();
        dfsPercentText.fontSize = 18;
        dfsPercentText.alignment = TextAlignmentOptions.Center;
        dfsPercentText.color = Color.cyan;
        RectTransform dpr = dfsPercentText.GetComponent<RectTransform>();
        dpr.anchorMin = new Vector2(0.1f, 0.62f);
        dpr.anchorMax = new Vector2(0.45f, 0.68f);
        dpr.offsetMin = Vector2.zero;
        dpr.offsetMax = Vector2.zero;
        dfsPercentText.text = "DFS match: N/A";

        GameObject bfsPercGO = new GameObject("BFSPercentText");
        bfsPercGO.transform.parent = tutorialPanel.transform;
        bfsPercentText = bfsPercGO.AddComponent<TextMeshProUGUI>();
        bfsPercentText.fontSize = 18;
        bfsPercentText.alignment = TextAlignmentOptions.Center;
        bfsPercentText.color = Color.cyan;
        RectTransform bpr = bfsPercentText.GetComponent<RectTransform>();
        bpr.anchorMin = new Vector2(0.55f, 0.62f);
        bpr.anchorMax = new Vector2(0.9f, 0.68f);
        bpr.offsetMin = Vector2.zero;
        bpr.offsetMax = Vector2.zero;
        bfsPercentText.text = "BFS match: N/A";
    }

    void CreateButton(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.parent = parent;
        var image = buttonGO.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.5f);
        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        GameObject labelGO = new GameObject("Text");
        labelGO.transform.parent = buttonGO.transform;
        var text = labelGO.AddComponent<TextMeshProUGUI>();
        text.fontSize = 20;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.text = label;
        RectTransform labelRect = text.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    public void SetTimerText(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        if (timerText != null)
            timerText.text = $"TIME: {minutes:D2}:{secs:D2}";
    }

    public void SetResultText(string text)
    {
        if (resultText != null)
            resultText.text = text;
    }

    public void StartTutorialAnimation()
    {
        if (tutorialAnimationCoroutine != null)
            StopCoroutine(tutorialAnimationCoroutine);
        tutorialAnimationCoroutine = StartCoroutine(TutorialFadeOutAnimation());
    }

    IEnumerator TutorialFadeOutAnimation()
    {
        // Display tutorial for 2 seconds
        yield return new WaitForSeconds(2f);

        // Fade out animation: 1 second
        float fadeDuration = 1f;
        float elapsedTime = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = new Vector3(1.1f, 1.1f, 1f);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Fade out alpha
            tutorialCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            
            // Subtle scale up effect
            tutorialCanvasGroup.GetComponent<RectTransform>().localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }

        // Ensure final state
        tutorialCanvasGroup.alpha = 0f;
        tutorialCanvasGroup.GetComponent<RectTransform>().localScale = endScale;
    }

    public void UpdatePercentText(float dfsPct, float bfsPct)
    {
        if (dfsPercentText != null)
            dfsPercentText.text = $"DFS match: {dfsPct:F1}%";
        if (bfsPercentText != null)
            bfsPercentText.text = $"BFS match: {bfsPct:F1}%";
    }

    public void DestroyCanvas()
    {
        if (canvas != null)
        {
            DestroyImmediate(canvas.gameObject);
        }
        canvas = null;
        timerText = null;
        resultText = null;
        pathModeText = null;
        dfsPercentText = null;
        bfsPercentText = null;
    }

    public void CreateResultSceneUI(string scoreText, bool reachedGoal, List<Vector2Int> bfs, List<Vector2Int> dfs, List<Vector2Int> userPath)
    {
        // Cache the paths for button callbacks
        cachedBFSPath = bfs;
        cachedDFSPath = dfs;
        cachedUserPath = userPath;

        Debug.Log($"CreateResultSceneUI called - BFS: {(bfs != null ? bfs.Count : 0)}, DFS: {(dfs != null ? dfs.Count : 0)}, User: {(userPath != null ? userPath.Count : 0)}");

        // Create EventSystem if needed
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        GameObject cgo = new GameObject("ResultCanvas");
        var resultCanvas = cgo.AddComponent<Canvas>();
        resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();

        GameObject titleGO = new GameObject("TitleText");
        titleGO.transform.parent = cgo.transform;
        TMP_Text titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 32;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        RectTransform trt = titleText.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.8f);
        trt.anchorMax = new Vector2(0.9f, 0.95f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        titleText.text = reachedGoal ? "CLEAR!" : "TIME UP";

        GameObject scoreGO = new GameObject("ScoreText");
        scoreGO.transform.parent = cgo.transform;
        TMP_Text scoreDisplay = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreDisplay.fontSize = 24;
        scoreDisplay.alignment = TextAlignmentOptions.Center;
        scoreDisplay.color = Color.yellow;
        RectTransform srt = scoreDisplay.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.1f, 0.45f);
        srt.anchorMax = new Vector2(0.9f, 0.8f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        scoreDisplay.text = scoreText;

        GameObject lineSummary = new GameObject("PathSummary");
        lineSummary.transform.parent = cgo.transform;
        TMP_Text summaryText = lineSummary.AddComponent<TextMeshProUGUI>();
        summaryText.fontSize = 20;
        summaryText.alignment = TextAlignmentOptions.Center;
        summaryText.color = Color.cyan;
        RectTransform lrt = summaryText.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.1f, 0.2f);
        lrt.anchorMax = new Vector2(0.9f, 0.45f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        summaryText.text = "Path comparison summary"
            + "\nDFS length: " + (dfs != null ? dfs.Count.ToString() : "none")
            + "\nBFS length: " + (bfs != null ? bfs.Count.ToString() : "none");

        CreateButton(cgo.transform, "DFSButton", "DFS", new Vector2(0.1f, 0.05f), new Vector2(0.4f, 0.15f), OnShowDFSPath);
        CreateButton(cgo.transform, "BFSButton", "BFS", new Vector2(0.6f, 0.05f), new Vector2(0.9f, 0.15f), OnShowBFSPath);

        // Auto-display user path
        if (cachedUserPath != null && cachedUserPath.Count > 0)
        {
            Debug.Log($"Starting to display user path with {cachedUserPath.Count} points");
            StartCoroutine(AutoDisplayUserPath());
        }
    }

    IEnumerator AutoDisplayUserPath()
    {
        Debug.Log("AutoDisplayUserPath - Waiting for end of frame");
        yield return new WaitForEndOfFrame();
        Debug.Log($"AutoDisplayUserPath - After WaitForEndOfFrame, cachedUserPath: {(cachedUserPath != null ? cachedUserPath.Count : 0)}");
        if (cachedUserPath != null && cachedUserPath.Count > 0)
        {
            Debug.Log("AutoDisplayUserPath - Calling StartUserPathPlayback");
            // Slow animation to see path being drawn
            StartUserPathPlayback(cachedUserPath, new Color(0f, 1f, 0f, 1f), 0.01f);
        }
        else
        {
            Debug.Log("AutoDisplayUserPath - cachedUserPath is null or empty");
        }
    }

    public void OnShowDFSPath()
    {
        Debug.Log("OnShowDFSPath called");
        ClearOverlayPlayback();
        if (cachedDFSPath != null && cachedDFSPath.Count > 0)
        {
            Debug.Log($"Starting DFS playback with {cachedDFSPath.Count} points");
            StartOverlayPathPlayback(cachedDFSPath, Color.cyan, 0.005f);
        }
        else
        {
            Debug.LogWarning("cachedDFSPath is null or empty");
        }
    }

    public void OnShowBFSPath()
    {
        Debug.Log("OnShowBFSPath called");
        ClearOverlayPlayback();
        if (cachedBFSPath != null && cachedBFSPath.Count > 0)
        {
            Debug.Log($"Starting BFS playback with {cachedBFSPath.Count} points");
            StartOverlayPathPlayback(cachedBFSPath, Color.red, 0.005f);
        }
        else
        {
            Debug.LogWarning("cachedBFSPath is null or empty");
        }
    }

    void StartUserPathPlayback(List<Vector2Int> path, Color color, float delay = -1)
    {
        if (path == null || path.Count == 0) return;
        ClearUserPlayback();
        Debug.Log($"StartUserPathPlayback - Drawing {path.Count} path points with color {color}");
        float animDelay = delay >= 0 ? delay : pathAnimateDelay;
        // Use bright lime green for user path
        StartCoroutine(AnimatePath(path, new Color(0f, 1f, 0f, 1f), animDelay, true));
    }

    void StartOverlayPathPlayback(List<Vector2Int> path, Color color, float delay = -1)
    {
        if (path == null || path.Count == 0) return;
        Debug.Log($"StartOverlayPathPlayback - Drawing {path.Count} path points with color {color}");
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }
        float animDelay = delay >= 0 ? delay : pathAnimateDelay;
        playbackCoroutine = StartCoroutine(AnimatePath(path, color, animDelay, false));
    }

    IEnumerator AnimatePath(List<Vector2Int> path, Color color, float delayPerStep, bool isUserPath)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("AnimatePath called with empty path");
            yield break;
        }
        
        Debug.Log($"AnimatePath started - isUserPath: {isUserPath}, path count: {path.Count}, color: {color}");
        
        // Create parent container
        GameObject parentGO;
        LineRenderer lineRenderer;
        
        if (isUserPath)
        {
            if (userPathLine != null) Destroy(userPathLine.gameObject);
            if (userPathFollower != null) Destroy(userPathFollower);
            parentGO = new GameObject("UserPath_Display");
            lineRenderer = parentGO.AddComponent<LineRenderer>();
            userPathLine = lineRenderer;
        }
        else
        {
            if (pathLine != null) Destroy(pathLine.gameObject);
            if (pathFollower != null) Destroy(pathFollower);
            parentGO = new GameObject("OverlayPath_Display");
            lineRenderer = parentGO.AddComponent<LineRenderer>();
            pathLine = lineRenderer;
        }

        // Setup LineRenderer for drawing the entire path
        Material lineMat = new Material(Shader.Find("Standard"));
        lineMat.color = color;
        lineMat.SetFloat("_Glossiness", 0f);
        lineMat.SetVector("_EmissionColor", color * 2f);
        lineMat.EnableKeyword("_EMISSION");
        
        lineRenderer.material = lineMat;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.widthMultiplier = cellSize * 0.5f;  // Very thick line
        lineRenderer.numCapVertices = 20;
        lineRenderer.numCornerVertices = 20;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.positionCount = 0;

        // Create bright follower sphere
        GameObject follower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        follower.name = isUserPath ? "UserPathFollower" : "PathFollower";
        follower.transform.parent = parentGO.transform;
        follower.transform.localScale = Vector3.one * (cellSize * 0.8f);
        
        // Remove collider
        Collider followerCol = follower.GetComponent<Collider>();
        if (followerCol != null) DestroyImmediate(followerCol);
        
        // Set ultra-bright emissive color
        MeshRenderer followerMr = follower.GetComponent<MeshRenderer>();
        if (followerMr != null)
        {
            Material sphereMat = new Material(Shader.Find("Standard"));
            sphereMat.color = color;
            sphereMat.SetFloat("_Glossiness", 0f);
            sphereMat.SetFloat("_Metallic", 1f);
            sphereMat.SetVector("_EmissionColor", color * 3f);
            sphereMat.EnableKeyword("_EMISSION");
            followerMr.material = sphereMat;
            followerMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        if (isUserPath)
            userPathFollower = follower;
        else
            pathFollower = follower;

        // Animate: Draw line as follower moves
        for (int i = 0; i < path.Count; i++)
        {
            // Add new point to line
            lineRenderer.positionCount = i + 1;
            Vector3 worldPos = GridToWorld(path[i]);
            worldPos.y = 0.5f;
            lineRenderer.SetPosition(i, worldPos);
            
            // Move follower to current position
            follower.transform.position = worldPos;
            
            yield return new WaitForSeconds(delayPerStep);
        }
        
        Debug.Log($"AnimatePath - Animation completed with {path.Count} line points");
    }

    Vector3 GridToWorld(Vector2Int position)
    {
        return new Vector3(position.x * cellSize, 0f, position.y * cellSize);
    }

    void ClearOverlayPlayback()
    {
        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);
        if (pathFollower != null) Destroy(pathFollower);
        if (pathLine != null) Destroy(pathLine.gameObject);
        pathFollower = null;
        pathLine = null;
        playbackCoroutine = null;
    }

    void ClearUserPlayback()
    {
        if (userPathFollower != null) Destroy(userPathFollower);
        if (userPathLine != null) Destroy(userPathLine.gameObject);
        userPathFollower = null;
        userPathLine = null;
    }
}
