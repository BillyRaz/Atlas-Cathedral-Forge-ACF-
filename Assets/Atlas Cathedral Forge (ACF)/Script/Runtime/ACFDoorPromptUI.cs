using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ACFSystem
{
    public class ACFDoorPromptUI : MonoBehaviour
    {
        private static ACFDoorPromptUI instance;

        private Canvas canvas;
        private TextMeshProUGUI promptText;

        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            EnsureInstance().ShowInternal(message);
        }

        public static void Hide()
        {
            if (instance == null)
                return;

            instance.HideInternal();
        }

        private static ACFDoorPromptUI EnsureInstance()
        {
            if (instance != null)
                return instance;

            GameObject root = new GameObject("__ACF_DoorPrompt");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<ACFDoorPromptUI>();
            instance.BuildUi();
            return instance;
        }

        private void BuildUi()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4995;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            GameObject panelObject = new GameObject("PromptPanel");
            panelObject.transform.SetParent(transform, false);

            Image panel = panelObject.AddComponent<Image>();
            panel.color = new Color(0.08f, 0.08f, 0.04f, 0.84f);

            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -92f);
            panelRect.sizeDelta = new Vector2(560f, 56f);

            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(panelObject.transform, false);

            promptText = textObject.AddComponent<TextMeshProUGUI>();
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.fontSize = 28f;
            promptText.color = Color.white;
            promptText.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform textRect = promptText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 8f);
            textRect.offsetMax = new Vector2(-18f, -8f);

            canvas.enabled = false;
        }

        private void ShowInternal(string message)
        {
            if (promptText == null || canvas == null)
                BuildUi();

            promptText.text = message;
            canvas.enabled = true;
        }

        private void HideInternal()
        {
            if (canvas != null)
                canvas.enabled = false;
        }
    }
}
