using System.Collections.Generic;
using UnityEngine;

public class MazeManager : MonoBehaviour
{
	public int cellsWidth = 10;
	public int cellsHeight = 10;
	public bool[,] isPath;
	public Vector2Int Start;
	public Vector2Int End;
	public GameObject wallPrefab;
	public GameObject floorPrefab;
	public Transform mazeParent;
	public float cellSize = 1f;
	public bool buildVisuals = true;
	public GameObject playerPrefab;
	public Transform playerParent;

	public int GridWidth => cellsWidth * 2 + 1;
	public int GridHeight => cellsHeight * 2 + 1;

	public void GenerateMaze(int widthCells, int heightCells)
	{
		cellsWidth = Mathf.Max(1, widthCells);
		cellsHeight = Mathf.Max(1, heightCells);
		int gw = GridWidth;
		int gh = GridHeight;
		isPath = new bool[gw, gh];
		for (int x = 0; x < gw; x++)
		{
			for (int y = 0; y < gh; y++)
			{
				isPath[x, y] = false;
			}
		}

		System.Random rng = new System.Random();
		Vector2Int startCell = new Vector2Int(1, 1);
		Stack<Vector2Int> stack = new Stack<Vector2Int>();
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
		stack.Push(startCell);
		visited.Add(startCell);
		isPath[startCell.x, startCell.y] = true;

		Vector2Int[] dirs = new Vector2Int[] { new Vector2Int(2, 0), new Vector2Int(-2, 0), new Vector2Int(0, 2), new Vector2Int(0, -2) };

		while (stack.Count > 0)
		{
			Vector2Int current = stack.Pop();
			List<Vector2Int> neighbors = new List<Vector2Int>();
			foreach (var d in dirs)
			{
				Vector2Int n = new Vector2Int(current.x + d.x, current.y + d.y);
				if (n.x > 0 && n.x < gw - 1 && n.y > 0 && n.y < gh - 1 && !visited.Contains(n))
				{
					neighbors.Add(n);
				}
			}

			if (neighbors.Count > 0)
			{
				stack.Push(current);
				int idx = rng.Next(neighbors.Count);
				Vector2Int chosen = neighbors[idx];
				visited.Add(chosen);
				isPath[chosen.x, chosen.y] = true;
				Vector2Int between = new Vector2Int((current.x + chosen.x) / 2, (current.y + chosen.y) / 2);
				isPath[between.x, between.y] = true;
				stack.Push(chosen);
			}
		}

		Start = new Vector2Int(1, 1);
		// choose random start and end among path tiles to vary each run
		List<Vector2Int> pathCells = new List<Vector2Int>();
		for (int x = 0; x < gw; x++)
		{
			for (int y = 0; y < gh; y++)
			{
				if (isPath[x, y]) pathCells.Add(new Vector2Int(x, y));
			}
		}
		System.Random rng2 = new System.Random();
		if (pathCells.Count >= 2)
		{
			Start = pathCells[rng2.Next(pathCells.Count)];
			// pick End with some distance from Start
			Vector2Int candidate;
			int attempts = 0;
			do
			{
				candidate = pathCells[rng2.Next(pathCells.Count)];
				attempts++;
			} while ((Mathf.Abs(candidate.x - Start.x) + Mathf.Abs(candidate.y - Start.y) < (gw + gh) / 4) && attempts < 200);
			End = candidate;
		}

		if (buildVisuals) BuildVisuals();

		// spawn player prefab at Start if provided
		if (playerPrefab != null)
		{
			if (playerParent == null)
			{
				GameObject pp = GameObject.Find("Players");
				if (pp == null) pp = new GameObject("Players");
				playerParent = pp.transform;
			}
			Vector3 worldPos = new Vector3(Start.x * cellSize, 0.5f * cellSize, Start.y * cellSize);
			GameObject p = Instantiate(playerPrefab, worldPos, Quaternion.identity, playerParent);
			var pc = p.GetComponent<PlayerController>();
			if (pc != null)
			{
				pc.Initialize(this, FindObjectOfType<ResultEvaluator>(), Start);
			}
		}
	}

	public bool IsWalkable(Vector2Int gridPos)
	{
		if (isPath == null) return false;
		if (gridPos.x < 0 || gridPos.x >= GridWidth || gridPos.y < 0 || gridPos.y >= GridHeight) return false;
		return isPath[gridPos.x, gridPos.y];
	}

	public void ClearVisuals()
	{
		if (mazeParent == null) return;
		for (int i = mazeParent.childCount - 1; i >= 0; i--)
		{
			DestroyImmediate(mazeParent.GetChild(i).gameObject);
		}
	}

	public void BuildVisuals()
	{
		if (!buildVisuals) return;
		if (mazeParent == null)
		{
			GameObject mp = GameObject.Find("MazeVisuals");
			if (mp == null) mp = new GameObject("MazeVisuals");
			mazeParent = mp.transform;
		}
		ClearVisuals();
		int gw = GridWidth;
		int gh = GridHeight;
		for (int x = 0; x < gw; x++)
		{
			for (int y = 0; y < gh; y++)
			{
				Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize);
				if (isPath[x, y])
				{
					if (floorPrefab != null)
					{
						var f = Instantiate(floorPrefab, pos, Quaternion.identity, mazeParent);
						f.transform.localScale = Vector3.one * cellSize;
					}
				}
				else
				{
					if (wallPrefab != null)
					{
						var w = Instantiate(wallPrefab, pos + new Vector3(0f, cellSize * 0.5f, 0f), Quaternion.identity, mazeParent);
						w.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
					}
				}
			}
		}
		// optional: mark start/end visually
		if (floorPrefab != null)
		{
			Vector3 sPos = new Vector3(Start.x * cellSize, 0f, Start.y * cellSize);
			var sObj = Instantiate(floorPrefab, sPos, Quaternion.identity, mazeParent);
			sObj.name = "Start";
			Vector3 ePos = new Vector3(End.x * cellSize, 0f, End.y * cellSize);
			var eObj = Instantiate(floorPrefab, ePos, Quaternion.identity, mazeParent);
			eObj.name = "End";
		}
	}

	public List<Vector2Int> GetBFSPath(Vector2Int start, Vector2Int end)
	{
		if (isPath == null) return null;
		int gw = GridWidth;
		int gh = GridHeight;
		bool[,] visited = new bool[gw, gh];
		Queue<Vector2Int> q = new Queue<Vector2Int>();
		Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
		q.Enqueue(start);
		visited[start.x, start.y] = true;
		Vector2Int[] moves = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };

		while (q.Count > 0)
		{
			Vector2Int cur = q.Dequeue();
			if (cur == end)
			{
				List<Vector2Int> path = new List<Vector2Int>();
				Vector2Int p = cur;
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
				Vector2Int nb = new Vector2Int(cur.x + m.x, cur.y + m.y);
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
		if (isPath == null) return null;
		int gw = GridWidth;
		int gh = GridHeight;
		bool[,] visited = new bool[gw, gh];
		Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();
		List<Vector2Int> path = new List<Vector2Int>();
		bool found = DFSRec(start, end, visited, parent);
		if (!found) return null;
		Vector2Int cur = end;
		while (!cur.Equals(start))
		{
			path.Add(cur);
			cur = parent[cur];
		}
		path.Add(start);
		path.Reverse();
		return path;

		bool DFSRec(Vector2Int node, Vector2Int target, bool[,] vis, Dictionary<Vector2Int, Vector2Int> par)
		{
			vis[node.x, node.y] = true;
			if (node.Equals(target)) return true;
			Vector2Int[] moves = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
			List<Vector2Int> order = new List<Vector2Int>(moves);
			for (int i = 0; i < order.Count; i++)
			{
				Vector2Int nb = new Vector2Int(node.x + order[i].x, node.y + order[i].y);
				if (nb.x >= 0 && nb.x < gw && nb.y >= 0 && nb.y < gh && !vis[nb.x, nb.y] && isPath[nb.x, nb.y])
				{
					par[nb] = node;
					if (DFSRec(nb, target, vis, par)) return true;
				}
			}
			return false;
		}
	}

	public List<Vector2Int> GetDFSVisitedOrder(Vector2Int start, Vector2Int end)
	{
		if (isPath == null) return null;
		int gw = GridWidth;
		int gh = GridHeight;
		bool[,] visited = new bool[gw, gh];
		List<Vector2Int> visitedOrder = new List<Vector2Int>();
		bool found = false;
		DFSVisit(start);
		return visitedOrder;

		void DFSVisit(Vector2Int node)
		{
			if (found) return;
			visited[node.x, node.y] = true;
			visitedOrder.Add(node);
			if (node.Equals(end))
			{
				found = true;
				return;
			}
			Vector2Int[] moves = new Vector2Int[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
			for (int i = 0; i < moves.Length; i++)
			{
				Vector2Int nb = new Vector2Int(node.x + moves[i].x, node.y + moves[i].y);
				if (nb.x >= 0 && nb.x < gw && nb.y >= 0 && nb.y < gh && !visited[nb.x, nb.y] && isPath[nb.x, nb.y])
				{
					DFSVisit(nb);
					if (found) return;
					visitedOrder.Add(node);
				}
			}
		}
	}
}
