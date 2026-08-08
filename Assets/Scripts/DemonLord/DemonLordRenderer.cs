using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemonLordRenderer : MonoBehaviour
{
    [SerializeField] private Sprite spriteDemonLord;

    private DemonLordManager demonLordManager;
    private Dictionary<int, GameObject> captives = new Dictionary<int, GameObject>();
    private Transform viewsParent;
    private GameObject demonLordView;

    private void Start()
    {
        demonLordManager = GetComponent<DemonLordManager>() ?? FindObjectOfType<DemonLordManager>();
        if (demonLordManager == null)
        {
            Debug.LogError("[DemonLordRenderer] DemonLordManager not found in scene.");
            return;
        }

        var parentGO = new GameObject("DemonLordViews");
        viewsParent = parentGO.transform;

        CreateDemonLordView();
        Debug.Log("[DemonLordRenderer] Initialized.");
    }

    public void CreateDemonLordView()
    {
        if (demonLordView != null || demonLordManager == null) return;

        Vector2Int pos = demonLordManager.GetPosition();
        demonLordView = CreateView("DemonLord", pos);
    }

    public void MoveDemonLordViewTo(Vector2Int gridPos)
    {
        if (demonLordView == null)
            CreateDemonLordView();

        if (demonLordView != null)
            demonLordView.transform.position = GridToWorld(gridPos);
    }

    public void AttachCaptiveDemonLord(int heroId, Vector2Int gridPos)
    {
        if (captives.ContainsKey(heroId)) return;

        if (demonLordView != null)
        {
            Destroy(demonLordView);
            demonLordView = null;
        }

        GameObject captive = CreateView("CaptiveDemonLord", gridPos);
        captives[heroId] = captive;
        Debug.Log($"[DemonLordRenderer] Captive DemonLord attached to Hero {heroId}.");
    }

    public IEnumerator SmoothMoveCaptive(int heroId, Vector2Int toGrid, float duration)
    {
        if (!captives.TryGetValue(heroId, out var go)) yield break;

        Vector3 startPos = go.transform.position;
        Vector3 endPos = GridToWorld(toGrid);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            go.transform.position = Vector3.Lerp(startPos, endPos, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        go.transform.position = endPos;
    }

    public bool DropCaptiveDemonLord(int heroId, Vector2Int gridPos)
    {
        if (!captives.TryGetValue(heroId, out GameObject captive)) return false;

        captives.Remove(heroId);
        if (captive != null) Destroy(captive);

        if (demonLordView == null)
            demonLordView = CreateView("DemonLord", gridPos);
        else
            demonLordView.transform.position = GridToWorld(gridPos);

        Debug.Log($"[DemonLordRenderer] DemonLord view dropped at ({gridPos.x},{gridPos.y}).");
        return true;
    }

    private GameObject CreateView(string objectName, Vector2Int gridPos)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(viewsParent, false);
        go.transform.position = GridToWorld(gridPos);
        go.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteDemonLord;
        sr.color = spriteDemonLord != null ? Color.white : new Color(1f, 0.7f, 0.7f, 1f);
        sr.sortingOrder = 1;
        return go;
    }

    private static Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, -0.15f);
    }
}
