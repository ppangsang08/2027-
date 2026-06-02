using System.Collections.Generic;
using UnityEngine;

public class MazePlayerController : MonoBehaviour
{
    public MazeGenerator maze;
    public GameObject playerGO;
    public Vector2Int playerGrid;
    public List<Vector2Int> userPath = new List<Vector2Int>();
    public bool sessionActive = false;
    public bool isFinished = false;
    public System.Action<bool> OnReachedEnd;

    float cellSize;

    public void Setup(MazeGenerator maze, GameObject playerGO, float cellSize)
    {
        this.maze = maze;
        this.playerGO = playerGO;
        this.cellSize = cellSize;
        playerGrid = maze.startPos;
        userPath.Clear();
        userPath.Add(playerGrid);
        isFinished = false;
        sessionActive = true;
        UpdatePlayerPosition();
    }

    void Update()
    {
        if (!sessionActive || isFinished) return;
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
            MovePlayerUntilBlocked(dir);
        }
    }

    void MovePlayerUntilBlocked(Vector2Int dir)
    {
        bool moved = false;
        Vector2Int previous = playerGrid;

        while (true)
        {
            Vector2Int next = previous + dir;
            if (!IsWalkable(next)) break;

            playerGrid = next;
            UpdatePlayerPosition();
            if (userPath.Count == 0 || userPath[userPath.Count - 1] != playerGrid)
                userPath.Add(playerGrid);

            moved = true;
            if (playerGrid == maze.endPos)
            {
                isFinished = true;
                OnReachedEnd?.Invoke(true);
                return;
            }

            int available = 0;
            bool straightAhead = false;
            Vector2Int[] moves = new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            foreach (var m in moves)
            {
                Vector2Int neighbor = playerGrid + m;
                if (neighbor == previous) continue;
                if (IsWalkable(neighbor))
                {
                    available++;
                    if (m == dir) straightAhead = true;
                }
            }

            if (available == 0 || available > 1 || !straightAhead)
            {
                break;
            }

            previous = playerGrid;
        }
    }

    bool IsWalkable(Vector2Int g)
    {
        return maze != null && maze.IsWalkable(g);
    }

    void UpdatePlayerPosition()
    {
        if (playerGO == null) return;
        playerGO.transform.position = new Vector3(playerGrid.x * cellSize, 0.5f * cellSize, playerGrid.y * cellSize);
    }
}
