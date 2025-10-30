using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private static GameController instance;
    private Camera mainCamera;
    private GameObject selectedObject;
    private Vector3 offset;
    private float yCoord; // Keep Y fixed

    [Header("Scoring")]
    public TMP_Text scoreText;
    public int score;

    [Header("UI")]
    public TMP_Text instructionText;
    public TMP_Text messageText;
    public TMP_Text timerText;

    [Header("Rules")]
    public int winScore = 9;
    public float messageDuration = 2f;
    public float timerDuration = 30f;
    public bool forceTimerDuration = true; // Force la durée du timer à la valeur ci-dessous
    public float forcedTimerDuration = 30f;
    public bool forceWinScore = true; // Active pour forcer la valeur ci-dessous
    public int forcedWinScore = 9;

    private float timeRemaining;
    private bool gameOver;
    [Header("Gameplay")]
    public bool lockInputOnGameOver = false; // si true, bloque le drag quand la partie est finie

    // End overlay UI
    private GameObject endOverlay;
    private GameObject endCard;
    private TMP_Text endTitle;
    private TMP_Text endSubtitle;
    private Button replayButton;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        // Force win score to 9 no matter the Inspector values
        forcedWinScore = 9;
        forceWinScore = true;
        winScore = 9;
        Debug.Log("[GameController] Win score forcé à 9.");

        // Force timer duration to forced value if enabled
        if (forceTimerDuration)
        {
            timerDuration = Mathf.Max(1f, forcedTimerDuration);
            Debug.Log($"[GameController] Timer forcé à {timerDuration}s.");
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        UpdateScoreText();
        SetInstructions();
        ClearMessage();
        EnsureUIAutoSetup();
        EnsureSortableObjectsRigidbodies();
        EnsureStylizedLayout();
        ResetTimer();
    }

    void Update()
    {
        TickTimer();
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartRun();
        }
        if (gameOver && lockInputOnGameOver)
        {
            return;
        }
        // When mouse button is pressed
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                // Check if the object has the allowed tags
                if (hitObj.CompareTag("cube") || hitObj.CompareTag("capsule") || hitObj.CompareTag("sphere"))
                {
                    selectedObject = hitObj;
                    yCoord = selectedObject.transform.position.y; // Save Y position
                    offset = selectedObject.transform.position - GetMouseWorldPos();
                }
            }
        }

        // When mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            selectedObject = null;
        }

        // Drag the selected object
        if (selectedObject != null)
        {
            Vector3 targetPos = GetMouseWorldPos() + offset;
            targetPos.y = yCoord; // Keep Y fixed
            selectedObject.transform.position = targetPos;
        }
    }

    // Convert mouse position to world position
    private Vector3 GetMouseWorldPos()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane xzPlane = new Plane(Vector3.up, Vector3.zero); // XZ plane at y=0
        float distance;
        if (xzPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    public void AddScore(int delta)
    {
        score += delta;
        if (score < 0)
        {
            score = 0;
        }
        UpdateScoreText();
        CheckWinLose();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
    }

    public void HandleCorrect(string kind)
    {
        AddScore(1);
        ShowMessage($"Bien trié: {kind}!", Color.green);
    }

    public void HandleWrong(string expected, string got)
    {
        AddScore(-1);
        ShowMessage($"Mauvais tri: attendu {expected}, reçu {got}", Color.red);
    }

    private void CheckWinLose()
    {
        if (scoreText == null) return;
        if (score >= winScore)
        {
            ShowMessage("Bravo! Vous avez gagné! (R pour rejouer)", Color.yellow);
            gameOver = true;
            ShowEndPanel(true);
        }
    }

    private void SetInstructions()
    {
        if (instructionText != null)
        {
            instructionText.text = "Consignes: Triez chaque forme dans la bonne poubelle.";
            instructionText.color = Color.black;
            instructionText.fontSize = 20;
        }
    }

    private void ClearMessage()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    private void ShowMessage(string text, Color color)
    {
        if (messageText == null) return;
        messageText.color = color;
        messageText.text = text;
        StopAllCoroutines();
        StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        ClearMessage();
    }

    private void ResetTimer()
    {
        timeRemaining = Mathf.Max(0f, timerDuration);
        UpdateTimerText();
        gameOver = false;
    }

    private void RestartRun()
    {
        score = 0;
        UpdateScoreText();
        ClearMessage();
        ResetTimer();
        HideEndPanel();
    }

    private void TickTimer()
    {
        if (gameOver) return;
        if (timeRemaining > 0f)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0f) timeRemaining = 0f;
            UpdateTimerText();
            if (timeRemaining <= 0f)
            {
                ShowMessage("Temps écoulé - Échec (R pour rejouer)", Color.red);
                gameOver = true;
                ShowEndPanel(false);
            }
        }
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"Timer: {seconds}s";
        timerText.color = seconds <= 10 ? new Color(1f, 0.4f, 0.4f) : Color.white;
    }

    private void EnsureUIAutoSetup()
    {
        if (scoreText == null)
        {
            var found = GameObject.Find("ScoreText");
            if (found == null)
            {
                var canvas = EnsureCanvas();
                scoreText = CreateTMPText(canvas.transform, "ScoreText", new Vector2(10, -10), TextAlignmentOptions.Left);
            }
            else
            {
                scoreText = found.GetComponent<TMP_Text>();
            }
            UpdateScoreText();
        }

        if (instructionText == null)
        {
            var found = GameObject.Find("InstructionText");
            if (found == null)
            {
                var canvas = EnsureCanvas();
                instructionText = CreateTMPText(canvas.transform, "InstructionText", new Vector2(10, -40), TextAlignmentOptions.Left);
            }
            else
            {
                instructionText = found.GetComponent<TMP_Text>();
            }
            SetInstructions();
        }

        if (messageText == null)
        {
            var found = GameObject.Find("MessageText");
            if (found == null)
            {
                var canvas = EnsureCanvas();
                messageText = CreateTMPText(canvas.transform, "MessageText", new Vector2(10, -70), TextAlignmentOptions.Left);
            }
            else
            {
                messageText = found.GetComponent<TMP_Text>();
            }
            ClearMessage();
        }

        if (timerText == null)
        {
            var found = GameObject.Find("TimerText");
            if (found == null)
            {
                var canvas = EnsureCanvas();
                timerText = CreateTMPText(canvas.transform, "TimerText", new Vector2(-10, -10), TextAlignmentOptions.Right);
            }
            else
            {
                timerText = found.GetComponent<TMP_Text>();
            }
            UpdateTimerText();
        }
    }

    private GameObject EnsureCanvas()
    {
        var canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        return canvasGo;
    }

    private TMP_Text CreateTMPText(Transform parent, string name, Vector2 anchoredPos, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        bool rightAnchored = align == TextAlignmentOptions.Right || align == TextAlignmentOptions.MidlineRight || align == TextAlignmentOptions.TopRight || align == TextAlignmentOptions.BottomRight;
        rect.anchorMin = new Vector2(rightAnchored ? 1 : 0, 1);
        rect.anchorMax = new Vector2(rightAnchored ? 1 : 0, 1);
        rect.pivot = new Vector2(rightAnchored ? 1 : 0, 1);
        rect.anchoredPosition = anchoredPos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = align;
        tmp.fontSize = 32;
        tmp.text = name;
        tmp.enableWordWrapping = false;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.6f);
        shadow.effectDistance = new Vector2(2, -2);
        return tmp;
    }

    private void EnsureStylizedLayout()
    {
        var canvas = EnsureCanvas();

        // Top bar background
        var topBar = GameObject.Find("TopBar");
        if (topBar == null)
        {
            topBar = new GameObject("TopBar");
            topBar.transform.SetParent(canvas.transform, false);
            var rect = topBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 70);
            var img = topBar.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.16f, 0.8f);
        }

        // Toast/message background centered
        var toast = GameObject.Find("ToastPanel");
        if (toast == null)
        {
            toast = new GameObject("ToastPanel");
            toast.transform.SetParent(canvas.transform, false);
            var rect = toast.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.9f);
            rect.anchorMax = new Vector2(0.5f, 0.9f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800, 60);
            var img = toast.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.35f);
        }

        // Reposition texts into panels if they exist
        if (scoreText != null)
        {
            (scoreText.transform as RectTransform).SetParent(topBar.transform, false);
            (scoreText.transform as RectTransform).anchoredPosition = new Vector2(20, -10);
        }
        if (timerText != null)
        {
            (timerText.transform as RectTransform).SetParent(topBar.transform, false);
            var r = (timerText.transform as RectTransform);
            r.anchorMin = new Vector2(1, 1);
            r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(1, 1);
            r.anchoredPosition = new Vector2(-20, -10);
        }
        if (messageText != null)
        {
            (messageText.transform as RectTransform).SetParent(toast.transform, false);
            (messageText.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (messageText.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (messageText.transform as RectTransform).pivot = new Vector2(0.5f, 0.5f);
            (messageText.transform as RectTransform).anchoredPosition = Vector2.zero;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.fontSize = 30;
        }
        if (instructionText != null)
        {
            var instrPanel = GameObject.Find("BottomInstruction");
            if (instrPanel == null)
            {
                instrPanel = new GameObject("BottomInstruction");
                instrPanel.transform.SetParent(canvas.transform, false);
                var rect = instrPanel.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0, 80);
                var img = instrPanel.AddComponent<Image>();
                img.color = new Color(0.12f, 0.12f, 0.16f, 0.6f);
            }
            (instructionText.transform as RectTransform).SetParent(instrPanel.transform, false);
            (instructionText.transform as RectTransform).anchoredPosition = new Vector2(20, 20);
            instructionText.alignment = TextAlignmentOptions.Left;
            instructionText.fontSize = 26;
        }
        EnsureEndPanel(canvas);
    }

    private void EnsureEndPanel(GameObject canvas)
    {
        if (endOverlay != null) return;

        // Fullscreen overlay
        endOverlay = new GameObject("EndOverlay");
        endOverlay.transform.SetParent(canvas.transform, false);
        var overlayRect = endOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0, 0);
        overlayRect.anchorMax = new Vector2(1, 1);
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        var overlayImg = endOverlay.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.65f);

        // Center card
        endCard = new GameObject("EndCard");
        endCard.transform.SetParent(endOverlay.transform, false);
        var cardRect = endCard.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(520, 280);
        var cardImg = endCard.AddComponent<Image>();
        cardImg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Title
        endTitle = CreateTMPText(endCard.transform, "EndTitle", Vector2.zero, TextAlignmentOptions.Center);
        var tRect = (endTitle.transform as RectTransform);
        tRect.anchorMin = new Vector2(0.5f, 1);
        tRect.anchorMax = new Vector2(0.5f, 1);
        tRect.pivot = new Vector2(0.5f, 1);
        tRect.anchoredPosition = new Vector2(0, -30);
        endTitle.fontSize = 48;

        // Subtitle
        endSubtitle = CreateTMPText(endCard.transform, "EndSubtitle", Vector2.zero, TextAlignmentOptions.Center);
        var sRect = (endSubtitle.transform as RectTransform);
        sRect.anchorMin = new Vector2(0.5f, 0.5f);
        sRect.anchorMax = new Vector2(0.5f, 0.5f);
        sRect.pivot = new Vector2(0.5f, 0.5f);
        sRect.anchoredPosition = new Vector2(0, -10);
        endSubtitle.fontSize = 28;

        // Replay button
        var btnGo = new GameObject("ReplayButton");
        btnGo.transform.SetParent(endCard.transform, false);
        var bRect = btnGo.AddComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0);
        bRect.anchorMax = new Vector2(0.5f, 0);
        bRect.pivot = new Vector2(0.5f, 0);
        bRect.sizeDelta = new Vector2(220, 54);
        bRect.anchoredPosition = new Vector2(0, 24);
        var bImg = btnGo.AddComponent<Image>();
        bImg.color = new Color(0.2f, 0.5f, 1f, 0.95f);
        replayButton = btnGo.AddComponent<Button>();
        replayButton.onClick.RemoveAllListeners();
        replayButton.onClick.AddListener(() => { RestartRun(); });

        var btnLabel = CreateTMPText(btnGo.transform, "ReplayLabel", Vector2.zero, TextAlignmentOptions.Center);
        btnLabel.text = "Rejouer (R)";
        btnLabel.fontSize = 26;

        HideEndPanel();
    }

    private void ShowEndPanel(bool win)
    {
        if (endOverlay == null) return;
        endOverlay.SetActive(true);
        endCard.SetActive(true);
        if (win)
        {
            endTitle.text = "Victoire";
            endTitle.color = new Color(0.4f, 1f, 0.6f);
            endSubtitle.text = "Bravo! Vous avez atteint le score requis.";
        }
        else
        {
            endTitle.text = "Défaite";
            endTitle.color = new Color(1f, 0.45f, 0.45f);
            endSubtitle.text = "Temps écoulé. Réessayez!";
        }
    }

    private void HideEndPanel()
    {
        if (endOverlay == null) return;
        endCard.SetActive(false);
        endOverlay.SetActive(false);
    }

    private void EnsureSortableObjectsRigidbodies()
    {
        EnsureRBForTag("cube");
        EnsureRBForTag("capsule");
        EnsureRBForTag("sphere");
    }

    private void EnsureRBForTag(string tag)
    {
        GameObject[] objs;
        try
        {
            objs = GameObject.FindGameObjectsWithTag(tag);
        }
        catch
        {
            return;
        }
        foreach (var obj in objs)
        {
            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = obj.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
        }
    }
}
