using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    public int width = 45;
    public int height = 45;
    public bool useSeed = false;
    public int seed = 12345;
    public Vector2Int startPos = new Vector2Int(1, 1);
    public Vector2Int endPos = new Vector2Int(43, 43);
    public bool[,] isPath;
    public List<Vector2Int> bfsPath;
    public List<Vector2Int> dfsPath;

    public void Setup(int width, int height, bool useSeed, int seed)
    {
        this.width = width;
        this.height = height;
        this.useSeed = useSeed;
        this.seed = seed;
        startPos = new Vector2Int(1, 1);
        endPos = new Vector2Int(width - 2, height - 2);
    }

    public void GenerateMaze()
    {
        int gw = width;
        int gh = height;
        isPath = new bool[gw, gh];
        for (int x = 0; x < gw; x++)
            for (int y = 0; y < gh; y++)
                isPath[x, y] = false;

        System.Random rng = useSeed ? new System.Random(seed) : new System.Random();
        Vector2Int startCell = new Vector2Int(1, 1);
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        stack.Push(startCell);
        visited.Add(startCell);
        isPath[startCell.x, startCell.y] = true;

        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(2, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(0, 2),
            new Vector2Int(0, -2)
        };

        while (stack.Count > 0)
        {
            Vector2Int cur = stack.Pop();
            List<Vector2Int> neighbours = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                Vector2Int n = new Vector2Int(cur.x + d.x, cur.y + d.y);
                if (n.x > 0 && n.x < gw - 1 && n.y > 0 && n.y < gh - 1 && !visited.Contains(n))
                    neighbours.Add(n);
            }

            if (neighbours.Count > 0)
            {
                stack.Push(cur);
                Vector2Int chosen = neighbours[rng.Next(neighbours.Count)];
                visited.Add(chosen);
                isPath[chosen.x, chosen.y] = true;
                Vector2Int between = new Vector2Int((cur.x + chosen.x) / 2, (cur.y + chosen.y) / 2);
                isPath[between.x, between.y] = true;
                stack.Push(chosen);
            }
        }

        // Fixed start position: bottom-left
        startPos = new Vector2Int(1, gh - 2);
        // Fixed end position: top-right
        endPos = new Vector2Int(gw - 2, 1);
        isPath[startPos.x, startPos.y] = true;
        isPath[endPos.x, endPos.y] = true;
        CarveConnectionToEnd(startPos, gw, gh);
        CarveConnectionToEnd(endPos, gw, gh);
        AddRandomLoops(gw, gh, rng, Mathf.Max(30, (gw + gh) / 5));

        bfsPath = GetBFSPath(startPos, endPos);
        dfsPath = GetDFSPath(startPos, endPos);
        int retries = 0;
        while (PathsAreEqual(bfsPath, dfsPath) && retries < 5)
        {
            AddRandomLoops(gw, gh, rng, Mathf.Max(20, (gw + gh) / 6));
            bfsPath = GetBFSPath(startPos, endPos);
            dfsPath = GetDFSPath(startPos, endPos);
            retries++;
        }
    }

    bool PathsAreEqual(List<Vector2Int> path1, List<Vector2Int> path2)
    {
        if (path1 == null || path2 == null) return false;
        if (path1.Count != path2.Count) return false;
        for (int i = 0; i < path1.Count; i++)
        {
            if (path1[i] != path2[i]) return false;
        }
        return true;
    }

    bool IsWithinGrid(Vector2Int g, int gw, int gh)
    {
        return g.x >= 0 && g.x < gw && g.y >= 0 && g.y < gh;
    }

    void CarveConnectionToEnd(Vector2Int endCandidate, int gw, int gh)
    {
        if (IsWithinGrid(endCandidate, gw, gh))
            isPath[endCandidate.x, endCandidate.y] = true;

        Vector2Int current = endCandidate;
        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        for (int radius = 1; radius < Mathf.Max(gw, gh); radius++)
        {
            foreach (var d in dirs)
            {
                Vector2Int probe = current + d * radius;
                if (IsWithinGrid(probe, gw, gh) && isPath[probe.x, probe.y])
                {
                    Vector2Int step = current;
                    while (step != probe)
                    {
                        int stepX = probe.x > step.x ? 1 : (probe.x < step.x ? -1 : 0);
                        int stepY = probe.y > step.y ? 1 : (probe.y < step.y ? -1 : 0);
                        step += new Vector2Int(stepX, stepY);
                        isPath[step.x, step.y] = true;
                    }
                    return;
                }
            }
        }
    }

    void AddRandomLoops(int gw, int gh, System.Random rng, int loopCount)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 1; x < gw - 1; x++)
        {
            for (int y = 1; y < gh - 1; y++)
            {
                if (isPath[x, y]) continue;
                bool left = isPath[x - 1, y];
                bool right = isPath[x + 1, y];
                bool up = isPath[x, y + 1];
                bool down = isPath[x, y - 1];
                if ((left && right && !up && !down) || (up && down && !left && !right))
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        for (int i = 0; i < loopCount && candidates.Count > 0; i++)
        {
            int index = rng.Next(candidates.Count);
            Vector2Int choice = candidates[index];
            isPath[choice.x, choice.y] = true;
            candidates.RemoveAt(index);
        }
    }

    public bool IsWalkable(Vector2Int g)
    {
        if (g.x < 0 || g.x >= width || g.y < 0 || g.y >= height) return false;
        return isPath[g.x, g.y];
    }

    public List<Vector2Int> GetBFSPath(Vector2Int start, Vector2Int end)
    {
        int gw = width, gh = height;
        bool[,] visited = new bool[gw, gh];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        q.Enqueue(start);
        visited[start.x, start.y] = true;

        // BFS explores right, left, up, down
        Vector2Int[] moves = new Vector2Int[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == end)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                var p = cur;
                while (!p.Equals(start))
                {
                    path.Add(p);
                    p = parent[p];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            foreach (var m in moves)
            {
                var nb = new Vector2Int(cur.x + m.x, cur.y + m.y);
                if (nb.x >= 0 && nb.x < gw && nb.y >= 0 && nb.y < gh && !visited[nb.x, nb.y] && isPath[nb.x, nb.y])
                {
                    visited[nb.x, nb.y] = true;
                    parent[nb] = cur;
                    q.Enqueue(nb);
                }
            }
        }

        return null;
    }

    public List<Vector2Int> GetDFSPath(Vector2Int start, Vector2Int end)
    {
        int gw = width, gh = height;
        bool[,] visited = new bool[gw, gh];
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
        bool found = false;
        System.Action<Vector2Int> dfs = null;

        // DFS explores in completely opposite direction: up, down, right, left
        Vector2Int[] moves = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0)
        };

        dfs = (node) =>
        {
            if (found) return;
            visited[node.x, node.y] = true;
            if (node == end)
            {
                found = true;
                return;
            }

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
        var cur = end;
        while (!cur.Equals(start))
        {
            path.Add(cur);
            cur = parent[cur];
        }
        path.Add(start);
        path.Reverse();
        return path;
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
                dp[i, j] = a[i - 1].Equals(b[j - 1]) ? dp[i - 1, j - 1] + 1 : Mathf.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        return dp[n, m];
    }
}
