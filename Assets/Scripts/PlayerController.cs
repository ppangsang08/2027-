using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public MazeManager mazeManager;
    public ResultEvaluator resultEvaluator;
    public float cellSize = 1f;
    public Vector2Int currentGridPos;
    public List<Vector2Int> userPath = new List<Vector2Int>();

    void Start()
    {
        // no auto-generate here; initialization occurs via MazeManager.Initialize
    }

    public void Initialize(MazeManager mgr, ResultEvaluator evaluator, Vector2Int startPos)
    {
        mazeManager = mgr;
        resultEvaluator = evaluator;
        currentGridPos = startPos;
        transform.position = GridToWorld(currentGridPos);
        userPath = new List<Vector2Int>() { currentGridPos };
    }

    void Update()
    {
        if (mazeManager == null) return;
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) dir = new Vector2Int(0, 1);
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) dir = new Vector2Int(0, -1);
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) dir = new Vector2Int(-1, 0);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) dir = new Vector2Int(1, 0);
        if (dir != Vector2Int.zero)
        {
            Vector2Int next = new Vector2Int(currentGridPos.x + dir.x, currentGridPos.y + dir.y);
            if (mazeManager.IsWalkable(next))
            {
                currentGridPos = next;
                transform.position = GridToWorld(currentGridPos);
                if (userPath.Count == 0 || userPath[userPath.Count - 1] != currentGridPos)
                {
                    userPath.Add(currentGridPos);
                }
                if (currentGridPos == mazeManager.End)
                {
                    StartCoroutine(OnReachedEnd());
                }
            }
        }
    }

    IEnumerator OnReachedEnd()
    {
        yield return null;
        List<Vector2Int> bfs = mazeManager.GetBFSPath(mazeManager.Start, mazeManager.End);
        List<Vector2Int> dfs = mazeManager.GetDFSPath(mazeManager.Start, mazeManager.End);
        if (resultEvaluator != null)
        {
            resultEvaluator.EvaluateAndShow(userPath, dfs, bfs);
        }
    }

    Vector3 GridToWorld(Vector2Int gpos)
    {
        float size = (mazeManager != null) ? mazeManager.cellSize : cellSize;
        return new Vector3(gpos.x * size, 0.5f, gpos.y * size);
    }
}
