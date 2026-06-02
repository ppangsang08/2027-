using UnityEngine;
using UnityEngine.Rendering;

public class MazeVisualizer : MonoBehaviour
{
    public Material wallMaterial;
    public Material floorMaterial;
    public bool enableShadows;

    Transform mazeRoot;
    Transform wallsRoot;
    Transform floorsRoot;
    Transform playerRoot;

    public Transform MazeRoot => mazeRoot;

    public void Setup(Material wallMaterial, Material floorMaterial, bool enableShadows)
    {
        this.wallMaterial = wallMaterial;
        this.floorMaterial = floorMaterial;
        this.enableShadows = enableShadows;
    }

    public void BuildVisuals(MazeGenerator generator, float cellSize)
    {
        if (mazeRoot != null)
            Destroy(mazeRoot.gameObject);

        mazeRoot = new GameObject("MazeRoot").transform;
        wallsRoot = new GameObject("Walls").transform;
        wallsRoot.parent = mazeRoot;
        floorsRoot = new GameObject("Floors").transform;
        floorsRoot.parent = mazeRoot;
        playerRoot = new GameObject("Players").transform;
        playerRoot.parent = mazeRoot;

        int gw = generator.width;
        int gh = generator.height;

        for (int x = 0; x < gw; x++)
        {
            for (int y = 0; y < gh; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0f, y * cellSize);
                if (generator.isPath[x, y])
                {
                    GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    floor.transform.position = pos + new Vector3(0f, -0.49f * cellSize, 0f);
                    floor.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    floor.transform.localScale = Vector3.one * cellSize;
                    floor.transform.parent = floorsRoot;
                    var mr = floor.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        if (floorMaterial != null) mr.material = floorMaterial;
                        else mr.material.color = Color.white;
                        mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        mr.receiveShadows = enableShadows;
                    }
                }
                else
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.transform.position = pos + new Vector3(0f, 0.5f * cellSize, 0f);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                    wall.transform.parent = wallsRoot;
                    var mr = wall.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        if (wallMaterial != null) mr.material = wallMaterial;
                        else mr.material.color = Color.gray;
                        mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                        mr.receiveShadows = enableShadows;
                    }
                }
            }
        }

        CreateMarker("StartMarker", generator.startPos, Color.green, cellSize);
        CreateMarker("EndMarker", generator.endPos, Color.blue, cellSize);
    }

    void CreateMarker(string name, Vector2Int position, Color color, float cellSize)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marker.name = name;
        marker.transform.position = new Vector3(position.x * cellSize, -0.48f * cellSize, position.y * cellSize);
        marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        marker.transform.localScale = Vector3.one * cellSize * 0.8f;
        marker.transform.parent = floorsRoot;
        var mr = marker.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (floorMaterial != null) mr.material = floorMaterial;
            else mr.material.color = color;
            mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = enableShadows;
        }
    }

    public GameObject SpawnPlayer(Vector2Int startPos, float cellSize, Material playerMaterial, bool enableShadows)
    {
        GameObject playerGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerGO.name = "Player";
        playerGO.transform.parent = playerRoot;
        playerGO.transform.localScale = Vector3.one * (cellSize * 0.6f);
        var mr = playerGO.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (playerMaterial != null) mr.material = playerMaterial;
            else mr.material.color = Color.red;
            mr.shadowCastingMode = enableShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            mr.receiveShadows = enableShadows;
        }

        var col = playerGO.GetComponent<Collider>();
        if (col != null) Destroy(col);

        playerGO.transform.position = new Vector3(startPos.x * cellSize, 0.5f * cellSize, startPos.y * cellSize);
        return playerGO;
    }

    public void SetupCamera(int width, int height, float cellSize)
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
}
