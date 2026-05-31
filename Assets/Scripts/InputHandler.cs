using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private DigActionHandler digActionHandler;

    private LevelConfig levelConfig;
    private GridManager gridManager;
    private DemonLordManager demonLordManager;
    private DemonLordRenderer demonLordRenderer;
    private Vector3 lastDragWorldPosition;
    private bool isDraggingCamera;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        levelConfig = GetComponent<LevelConfig>() ?? FindObjectOfType<LevelConfig>();
        gridManager = GetComponent<GridManager>() ?? FindObjectOfType<GridManager>();
        demonLordManager = GetComponent<DemonLordManager>() ?? FindObjectOfType<DemonLordManager>();
        demonLordRenderer = GetComponent<DemonLordRenderer>() ?? FindObjectOfType<DemonLordRenderer>();
        if (digActionHandler == null) digActionHandler = GetComponent<DigActionHandler>() ?? FindObjectOfType<DigActionHandler>();

        if (mainCamera == null)
            Debug.LogError("[InputHandler] Main Camera not found.");
        if (levelConfig == null)
            Debug.LogError("[InputHandler] LevelConfig not found.");
        if (gridManager == null)
            Debug.LogError("[InputHandler] GridManager not found.");
        if (demonLordManager == null)
            Debug.LogError("[InputHandler] DemonLordManager not found.");
        if (demonLordRenderer == null)
            Debug.LogError("[InputHandler] DemonLordRenderer not found.");
        if (digActionHandler == null)
            Debug.LogError("[InputHandler] DigActionHandler not found.");

        ApplyInitialCameraView();
    }

    private void Update()
    {
        HandleCameraDrag();

        if (!Input.GetMouseButtonDown(0)) return;
        if (isDraggingCamera) return;

        Vector3 worldPos = GetMouseWorldPosition();
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.y);

        if (TryHandleDemonLordPlacement(x, y)) return;

        digActionHandler.HandlePrimaryClick(x, y);
    }

    private bool TryHandleDemonLordPlacement(int x, int y)
    {
        if (demonLordManager == null || !demonLordManager.IsWaitingForPlacement) return false;

        var gridPos = new Vector2Int(x, y);
        if (demonLordManager.TryPlaceAt(gridPos, gridManager))
        {
            if (demonLordRenderer != null)
                demonLordRenderer.MoveDemonLordViewTo(gridPos);
            return true;
        }

        Debug.Log($"[InputHandler] DemonLord placement rejected at ({x},{y}). Select an Empty cell.");
        return true;
    }

    private void ApplyInitialCameraView()
    {
        if (mainCamera == null || levelConfig == null) return;

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = levelConfig.CameraViewRows * 0.5f;

        Vector2 center = levelConfig.CameraStartCenter;
        mainCamera.transform.position = new Vector3(center.x, center.y, mainCamera.transform.position.z);
    }

    private void HandleCameraDrag()
    {
        if (mainCamera == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            isDraggingCamera = true;
            lastDragWorldPosition = GetMouseWorldPosition();
            return;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isDraggingCamera = false;
            return;
        }

        if (!Input.GetMouseButton(1) || !isDraggingCamera) return;

        Vector3 currentWorldPosition = GetMouseWorldPosition();
        Vector3 delta = lastDragWorldPosition - currentWorldPosition;
        mainCamera.transform.position += new Vector3(delta.x, delta.y, 0f);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }
}
