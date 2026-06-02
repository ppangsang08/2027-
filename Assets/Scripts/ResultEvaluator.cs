using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultEvaluator : MonoBehaviour
{
    public TMP_Text resultTextTMP;
    public Text resultTextUI;
    public MazeManager mazeManager;
    public Material userPathMaterial;
    public Material dfsPathMaterial;
    public Material bfsPathMaterial;
    public float lineWidth = 0.1f;

    LineRenderer userLine;
    LineRenderer dfsLine;
    LineRenderer bfsLine;

    void Awake()
    {
        if (mazeManager == null) mazeManager = FindObjectOfType<MazeManager>();
    }

    public void EvaluateAndShow(List<Vector2Int> userPath, List<Vector2Int> dfsPath, List<Vector2Int> bfsPath)
    {
        string result = "";
        if (dfsPath == null) result += "DFS: No path\n";
        else result += "DFS match: " + ComputeLcsPercentage(userPath, dfsPath).ToString("F1") + "%\n";
        if (bfsPath == null) result += "BFS: No path\n";
        else result += "BFS match: " + ComputeLcsPercentage(userPath, bfsPath).ToString("F1") + "%\n";
        if (resultTextTMP != null) resultTextTMP.text = result;
        else if (resultTextUI != null) resultTextUI.text = result;
        else Debug.Log(result);

        ClearLines();
        DrawPath(userPath, ref userLine, userPathMaterial);
        DrawPath(dfsPath, ref dfsLine, dfsPathMaterial);
        DrawPath(bfsPath, ref bfsLine, bfsPathMaterial);
    }

    public float ComputeLcsPercentage(List<Vector2Int> a, List<Vector2Int> b)
    {
        if (a == null || b == null || a.Count == 0 || b.Count == 0) return 0f;
        int l = LcsLength(a, b);
        int denom = Mathf.Max(a.Count, b.Count);
        if (denom == 0) return 0f;
        return (float)l / denom * 100f;
    }

    int LcsLength(List<Vector2Int> a, List<Vector2Int> b)
    {
        int n = a.Count;
        int m = b.Count;
        int[,] dp = new int[n + 1, m + 1];
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                if (a[i - 1].Equals(b[j - 1])) dp[i, j] = dp[i - 1, j - 1] + 1;
                else dp[i, j] = Mathf.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }
        return dp[n, m];
    }

    void DrawPath(List<Vector2Int> path, ref LineRenderer lr, Material mat)
    {
        if (path == null || path.Count == 0) return;
        if (lr == null)
        {
            GameObject go = new GameObject("LineRenderer");
            go.transform.parent = this.transform;
            lr = go.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.widthMultiplier = lineWidth;
            lr.positionCount = 0;
            lr.useWorldSpace = true;
            lr.numCapVertices = 4;
        }
        lr.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = GridToWorld(path[i]);
            lr.SetPosition(i, p + Vector3.up * 0.05f);
        }
    }

    Vector3 GridToWorld(Vector2Int g)
    {
        float size = (mazeManager != null) ? mazeManager.cellSize : 1f;
        return new Vector3(g.x * size, 0f, g.y * size);
    }

    void ClearLines()
    {
        if (userLine != null) Destroy(userLine.gameObject);
        if (dfsLine != null) Destroy(dfsLine.gameObject);
        if (bfsLine != null) Destroy(bfsLine.gameObject);
        userLine = dfsLine = bfsLine = null;
    }
}
