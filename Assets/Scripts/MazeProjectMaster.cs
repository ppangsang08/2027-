using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class MazeProjectMaster : MonoBehaviour
{
    // Maze settings
    public int width = 15;
    public int height = 15;
    public float cellSize = 1f;

    // inspector controls
    public bool useSeed = false;
    public int seed = 12345;
    public bool useFixedStartEnd = false;
    public Vector2Int fixedStart = new Vector2Int(1, 1);
    public Vector2Int fixedEnd = new Vector2Int(13, 13);
    public bool useTMP = true;
    public bool enableShadows = false;
    public Material wallMaterial;
    public Material floorMaterial;
    public Material playerMaterial;
    public bool animatePaths = true;
    public float pathAnimateDelay = 0.03f;

    // runtime data
    bool[,] isPath;
    Vector2Int startPos;
    Vector2Int endPos;

    // parents for organization
    Transform mazeRoot;
    Transform wallsRoot;
    Transform floorsRoot;
    Transform playerRoot;

    // player
    GameObject playerGO;
    Vector2Int playerGrid;
    List<Vector2Int> userPath = new List<Vector2Int>();

    // UI
    Canvas canvas;
    Text resultText;
    object resultTextTMPObj;
    PropertyInfo tmpTextProp;

    // visualization
    LineRenderer userLine, bfsLine, dfsLine;

    void Start()
    {
        InitializeAll();
    }

    void InitializeAll()
    {
        // clamp size to odd numbers for maze algorithm
        if (width < 5) width = 5;
        if (height < 5) height = 5;
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        // create parents
        mazeRoot = new GameObject("MazeRoot").transform;
        wallsRoot = new GameObject("Walls").transform; wallsRoot.parent = mazeRoot;
        floorsRoot = new GameObject("Floors").transform; floorsRoot.parent = mazeRoot;
        playerRoot = new GameObject("Players").transform; playerRoot.parent = mazeRoot;

        // generate maze data
        GenerateMaze();

        // build visuals
        BuildVisuals();

        // setup camera
        SetupCamera();

        // create player
        SpawnPlayer();

        // create UI
        CreateUI();
    }

    void GenerateMaze()
    {
        int gw = width;
        int gh = height;
        isPath = new bool[gw, gh];
        for (int x = 0; x < gw; x++) for (int y = 0; y < gh; y++) isPath[x, y] = false;

        System.Random rng = useSeed ? new System.Random(seed) : new System.Random();
        // recursive backtracker on grid where we step by 2 for corridors
        Vector2Int startCell = new Vector2Int(1, 1);
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        stack.Push(startCell);
        visited.Add(startCell);
        isPath[startCell.x, startCell.y] = true;

        Vector2Int[] dirs = new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(-2, 0), new Vector2Int(0, 2), new Vector2Int(0, -2) };

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Pop();
            List<Vector2Int> neigh = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                Vector2Int n = new Vector2Int(cur.x + d.x, cur.y + d.y);
                if (n.x > 0 && n.x < gw - 1 && n.y > 0 && n.y < gh - 1 && !visited.Contains(n)) neigh.Add(n);
            }
            if (neigh.Count > 0)
            {
                stack.Push(cur);
                Vector2Int chosen = neigh[rng.Next(neigh.Count)];
                visited.Add(chosen);
                isPath[chosen.x, chosen.y] = true;
                Vector2Int between = new Vector2Int((cur.x + chosen.x) / 2, (cur.y + chosen.y) / 2);
                isPath[between.x, between.y] = true;
                stack.Push(chosen);
            }
        }

        // collect path cells and choose random start/end with distance
        List<Vector2Int> pathCells = new List<Vector2Int>();
        for (int x = 0; x < gw; x++) for (int y = 0; y < gh; y++) if (isPath[x, y]) pathCells.Add(new Vector2Int(x, y));
        System.Random rng2 = useSeed ? new System.Random(seed + 1) : new System.Random();
        if (pathCells.Count >= 2)
        {
            // choose start/end, or use fixed positions if requested
            if (useFixedStartEnd)
            {
                startPos = fixedStart; endPos = fixedEnd;
                if (startPos.x < 0 || startPos.x >= gw || startPos.y < 0 || startPos.y >= gh || !isPath[startPos.x, startPos.y]) startPos = pathCells[rng2.Next(pathCells.Count)];
                if (endPos.x < 0 || endPos.x >= gw || endPos.y < 0 || endPos.y >= gh || !isPath[endPos.x, endPos.y]) endPos = pathCells[rng2.Next(pathCells.Count)];
            }
            else
            {
                startPos = pathCells[rng2.Next(pathCells.Count)];
                Vector2Int cand; int attempts = 0;
                do
                {
                    cand = pathCells[rng2.Next(pathCells.Count)]; attempts++;
                } while ((Mathf.Abs(cand.x - startPos.x) + Mathf.Abs(cand.y - startPos.y) < (gw + gh) / 4) && attempts < 300);
                endPos = cand;
            }
        }
        else
        {
            startPos = new Vector2Int(1, 1);
            endPos = new Vector2Int(gw - 2, gh - 2);
            if (!isPath[endPos.x, endPos.y]) isPath[endPos.x, endPos.y] = true;
        }
    }

    void BuildVisuals()
    {
        int gw = width;
        int gh = height;
        for (int x = 0; x < gw; x++)
        {
            for (int y = 0; y < gh; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize);
                if (isPath[x, y])
                {
                    // floor
                    GameObject f = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    f.transform.position = pos + new Vector3(0f, -0.49f * cellSize, 0f);
                    f.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    f.transform.localScale = Vector3.one * cellSize;
                    f.transform.parent = floorsRoot;
                    var mr = f.GetComponent<MeshRenderer>(); if (mr)
                    {
                        if (floorMaterial != null) mr.material = floorMaterial; else mr.material.color = Color.white;
                        mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        mr.receiveShadows = enableShadows;
                    }
                }
                else
                {
                    // wall
                    GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    w.transform.position = pos + new Vector3(0f, 0.5f * cellSize, 0f);
                    w.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                    w.transform.parent = wallsRoot;
                    var mr = w.GetComponent<MeshRenderer>(); if (mr)
                    {
                        if (wallMaterial != null) mr.material = wallMaterial; else mr.material.color = Color.gray;
                        mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        mr.receiveShadows = enableShadows;
                    }
                }
            }
        }

        // mark start/end with colored quads
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Quad);
        s.name = "StartMarker";
        s.transform.position = new Vector3(startPos.x * cellSize, -0.48f * cellSize, startPos.y * cellSize);
        s.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        s.transform.localScale = Vector3.one * cellSize * 0.8f;
        s.transform.parent = floorsRoot;
        var smr = s.GetComponent<MeshRenderer>(); if (smr)
        {
            if (floorMaterial != null) smr.material = floorMaterial; else smr.material.color = Color.green;
            smr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            smr.receiveShadows = enableShadows;
        }

        GameObject e = GameObject.CreatePrimitive(PrimitiveType.Quad);
        e.name = "EndMarker";
        e.transform.position = new Vector3(endPos.x * cellSize, -0.48f * cellSize, endPos.y * cellSize);
        e.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        e.transform.localScale = Vector3.one * cellSize * 0.8f;
        e.transform.parent = floorsRoot;
        var emr = e.GetComponent<MeshRenderer>(); if (emr)
        {
            if (floorMaterial != null) emr.material = floorMaterial; else emr.material.color = Color.blue;
            emr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            emr.receiveShadows = enableShadows;
        }
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject cgo = new GameObject("Main Camera");
            cam = cgo.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.orthographic = true;
        float mapWidth = width * cellSize;
        float mapHeight = height * cellSize;
        cam.transform.position = new Vector3((mapWidth - cellSize) / 2f, 10f, (mapHeight - cellSize) / 2f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.orthographicSize = Mathf.Max(mapWidth, mapHeight) / 2f + 1f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    void SpawnPlayer()
    {
        playerGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerGO.name = "Player";
        playerGO.transform.parent = playerRoot;
        playerGO.transform.localScale = Vector3.one * (cellSize * 0.6f);
        var mr = playerGO.GetComponent<MeshRenderer>(); if (mr)
        {
            if (playerMaterial != null) mr.material = playerMaterial; else mr.material.color = Color.red;
            mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = enableShadows;
        }
        // remove collider physics to control movement deterministically
        var col = playerGO.GetComponent<Collider>(); if (col) Destroy(col);

        playerGrid = startPos;
        playerGO.transform.position = GridToWorld(playerGrid) + Vector3.up * (0.5f * cellSize);
        userPath.Clear(); userPath.Add(playerGrid);
    }

    Vector3 GridToWorld(Vector2Int g)
    {
        return new Vector3(g.x * cellSize, 0f, g.y * cellSize);
    }

    void Update()
    {
        HandleMovementInput();
    }

    void HandleMovementInput()
    {
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = new Vector2Int(0, 1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = new Vector2Int(0, -1);
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = new Vector2Int(1, 0);
        if (dir != Vector2Int.zero)
        {
            Vector2Int next = playerGrid + dir;
            if (IsWalkable(next))
            {
                playerGrid = next;
                playerGO.transform.position = GridToWorld(playerGrid) + Vector3.up * (0.5f * cellSize);
                if (userPath.Count == 0 || userPath[userPath.Count - 1] != playerGrid) userPath.Add(playerGrid);
                if (playerGrid == endPos)
                {
                    OnReachedEnd();
                }
            }
        }
    }

    bool IsWalkable(Vector2Int g)
    {
        if (g.x < 0 || g.x >= width || g.y < 0 || g.y >= height) return false;
        return isPath[g.x, g.y];
    }

    void OnReachedEnd()
    {
        // compute BFS and DFS paths
        List<Vector2Int> bfs = GetBFSPath(startPos, endPos);
        List<Vector2Int> dfs = GetDFSPath(startPos, endPos);
        float pctBFS = (bfs == null) ? 0f : ComputeLcsPercentage(userPath, bfs);
        float pctDFS = (dfs == null) ? 0f : ComputeLcsPercentage(userPath, dfs);
        string txt = $"유저 vs DFS: {pctDFS:F1}%  /  유저 vs BFS: {pctBFS:F1}%";
        SetResultText(txt);

        // draw/animate paths
        ClearLines();
        StartDrawLine(ref userLine, userPath, Color.yellow);
        StartDrawLine(ref dfsLine, dfs, Color.red);
        StartDrawLine(ref bfsLine, bfs, Color.cyan);
    }

    void ClearLines()
    {
        if (userLine != null) Destroy(userLine.gameObject);
        if (dfsLine != null) Destroy(dfsLine.gameObject);
        if (bfsLine != null) Destroy(bfsLine.gameObject);
        userLine = dfsLine = bfsLine = null;
    }

    void StartDrawLine(ref LineRenderer lr, List<Vector2Int> path, Color color)
    {
        if (path == null || path.Count == 0) return;
        if (lr == null)
        {
            GameObject go = new GameObject("Line"); go.transform.parent = mazeRoot;
            lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.widthMultiplier = cellSize * 0.12f;
            lr.numCapVertices = 4;
        }
        lr.startColor = lr.endColor = color;
        if (animatePaths)
        {
            StartCoroutine(AnimateLine(lr, path));
        }
        else
        {
            lr.positionCount = path.Count;
            for (int i = 0; i < path.Count; i++) lr.SetPosition(i, GridToWorld(path[i]) + Vector3.up * (0.1f * cellSize));
        }
    }

    IEnumerator AnimateLine(LineRenderer lr, List<Vector2Int> path)
    {
        lr.positionCount = 0;
        for (int i = 0; i < path.Count; i++)
        {
            lr.positionCount = i + 1;
            lr.SetPosition(i, GridToWorld(path[i]) + Vector3.up * (0.1f * cellSize));
            yield return new WaitForSeconds(pathAnimateDelay);
        }
    }

    List<Vector2Int> GetBFSPath(Vector2Int start, Vector2Int end)
    {
        int gw = width, gh = height;
        bool[,] visited = new bool[gw, gh];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        q.Enqueue(start); visited[start.x, start.y] = true;
        Vector2Int[] moves = new Vector2Int[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == end)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                var p = cur; while (!p.Equals(start)) { path.Add(p); p = parent[p]; }
                path.Add(start); path.Reverse(); return path;
            }
            foreach (var m in moves)
            {
                var nb = new Vector2Int(cur.x + m.x, cur.y + m.y);
                if (nb.x >= 0 && nb.x < gw && nb.y >= 0 && nb.y < gh && !visited[nb.x, nb.y] && isPath[nb.x, nb.y])
                {
                    visited[nb.x, nb.y] = true; parent[nb] = cur; q.Enqueue(nb);
                }
            }
        }
        return null;
    }

    List<Vector2Int> GetDFSPath(Vector2Int start, Vector2Int end)
    {
        int gw = width, gh = height;
        bool[,] visited = new bool[gw, gh];
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        bool found = false;
        System.Action<Vector2Int> dfs = null;
        Vector2Int[] moves = new Vector2Int[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };
        dfs = (node) =>
        {
            if (found) return;
            visited[node.x, node.y] = true;
            if (node == end) { found = true; return; }
            foreach (var m in moves)
            {
                var nb = new Vector2Int(node.x + m.x, node.y + m.y);
                if (nb.x >= 0 && nb.x < gw && nb.y >= 0 && nb.y < gh && !visited[nb.x, nb.y] && isPath[nb.x, nb.y])
                {
                    parent[nb] = node;
                    dfs(nb);
                    if (found) return;
                }
            }
        };
        dfs(start);
        if (!found) return null;
        List<Vector2Int> path = new List<Vector2Int>();
        var cur = end; while (!cur.Equals(start)) { path.Add(cur); cur = parent[cur]; }
        path.Add(start); path.Reverse(); return path;
    }

    float ComputeLcsPercentage(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null || a.Count == 0 || b.Count == 0) return 0f;
        int l = LcsLength(a, b);
        int denom = Mathf.Max(a.Count, b.Count);
        if (denom == 0) return 0f;
        return (float)l / denom * 100f;
    }

    int LcsLength(List<Vector2Int> a, List<Vector2Int> b)
    {
        int n = a.Count, m = b.Count;
        int[,] dp = new int[n + 1, m + 1];
        for (int i = 1; i <= n; i++) for (int j = 1; j <= m; j++) dp[i, j] = (a[i - 1].Equals(b[j - 1])) ? dp[i - 1, j - 1] + 1 : Mathf.Max(dp[i - 1, j], dp[i, j - 1]);
        return dp[n, m];
    }

    void CreateUI()
    {
        GameObject cgo = new GameObject("Canvas");
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cgo.AddComponent<CanvasScaler>();
        cgo.AddComponent<GraphicRaycaster>();

        GameObject textGO = new GameObject("ResultText"); textGO.transform.parent = cgo.transform;
        // Try TextMeshPro if available
        resultText = null; resultTextTMPObj = null; tmpTextProp = null;
        if (useTMP)
        {
            var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                var comp = textGO.AddComponent(tmpType);
                resultTextTMPObj = comp;
                tmpTextProp = tmpType.GetProperty("text");
                tmpTextProp.SetValue(resultTextTMPObj, "Move with WASD or arrow keys");
                var rect = textGO.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0.1f, 0.9f); rect.anchorMax = new Vector2(0.9f, 1f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            }
        }
        if (resultTextTMPObj == null)
        {
            resultText = textGO.AddComponent<Text>();
            resultText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            resultText.fontSize = 24; resultText.alignment = TextAnchor.UpperCenter;
            RectTransform rt = resultText.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0.1f, 0.9f); rt.anchorMax = new Vector2(0.9f, 1f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; resultText.text = "Move with WASD or arrow keys";
        }
    }

    void SetResultText(string txt)
    {
        if (resultText != null) resultText.text = txt;
        else if (resultTextTMPObj != null && tmpTextProp != null) tmpTextProp.SetValue(resultTextTMPObj, txt);
        else Debug.Log(txt);
    }

    void DrawLine(ref LineRenderer lr, List<Vector2Int> path, Color color)
    {
        if (path == null || path.Count == 0) return;
        if (lr == null)
        {
            GameObject go = new GameObject("Line"); go.transform.parent = mazeRoot;
            lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.widthMultiplier = cellSize * 0.15f;
            lr.positionCount = 0;
            lr.numCapVertices = 4;
        }
        lr.startColor = lr.endColor = color;
        lr.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++) lr.SetPosition(i, GridToWorld(path[i]) + Vector3.up * (0.1f * cellSize));
    }
}
