using UnityEngine;
using UnityEngine.UI;

public class MVPResultUI : MonoBehaviour
{
    private MVPGameManager gameManager;
    private Text resultText;

    private void Start()
    {
        gameManager = FindObjectOfType<MVPGameManager>();
        if (gameManager == null)
            Debug.LogError("[MVPResultUI] MVPGameManager not found.");

        BuildUI();
    }

private void BuildUI()
    {
        var canvasGO = new GameObject("ResultCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        var textGO = new GameObject("ResultText");
        textGO.transform.SetParent(canvasGO.transform, false);

        resultText = textGO.AddComponent<Text>();
        resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultText.fontSize = 64;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.color = Color.white;
        resultText.text = "";

        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        resultText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (gameManager == null || resultText == null) return;

        var state = gameManager.GetCurrentState();
        if (state == GameState.Victory)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = "VICTORY";
            resultText.color = Color.yellow;
        }
        else if (state == GameState.Defeat)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = "DEFEAT";
            resultText.color = new Color(1f, 0.3f, 0.3f);
        }
        else
        {
            resultText.gameObject.SetActive(false);
        }
    }
}
