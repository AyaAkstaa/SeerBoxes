using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startPanel;
    public GameObject levelPanel;
    public GameObject winPanel;

    [Header("UI Elements")]
    public RectTransform hintImage;
    public TextMeshProUGUI winText;
    public Button restartButton;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public float slideDistance = 500f;

    [Header("Dialogue Texts")]
    public string[] startLines = { "Добро пожаловать в игру!", "Ищите сокровища..." };
    public string[] winRestartLines = { "Захотел пройти игру еще раз?" };
    public string[] loseLines = { "Ой не получилось пройти(" };
    public string winMessage = "Ты победил!!!";

    [Header("Level Dialogues")]
    public string[] level1Lines = { "Уровень 1", "Выберите правильный сундук" };
    public string[] level2Lines = { "Уровень 2", "Следуйте карте" };
    public string[] level3Lines = { "Уровень 3", "Решите математическую задачу" };
    public string[] level4Lines = { "Уровень 4", "Щщщщщ....." };
    public string[] level5Lines = { "Уровень 5", "Куда ведут эти стрелки?" };
    public string[] level6Lines = { "Уровень 6", "Что-то тут не так" };
    public string[] level7Lines = { "Уровень 7", "Что за пятна на листочке?" };
    public string[] level8Lines = { "Уровень 8", "Продолжи последовательность" };
    public string[] level9Lines = { "Уровень 9", "Найди ближайшее число" };
    public string[] level10Lines = { "Уровень 10", "Ты думал все так легко?" };

    [Header("Level Panel Settings")]
    public float levelPanelHideDelay = 2f;
    public float minDisplayTime = 3f;

    private Dialogue startDialogue;
    private Dialogue levelDialogue;
    private Dialogue winDialogue;
    private LevelManager levelManager;
    private Coroutine hideLevelPanelCoroutine;
    private float levelPanelShowTime;
    private Vector2 hintImageOriginalPos;

    void Start()
    {
        // Сохраняем оригинальную позицию hintImage
        if (hintImage != null)
        {
            hintImageOriginalPos = hintImage.anchoredPosition;
        }

        winPanel.SetActive(false);
        levelManager = FindFirstObjectByType<LevelManager>();
        
        // Настраиваем кнопку рестарта
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnWinRestartClicked);
            if (restartButton.GetComponent<ButtonSoundHandler>() == null)
            {
                restartButton.gameObject.AddComponent<ButtonSoundHandler>();
            }
        }
            
        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        // Настраиваем winDialogue
        if (winPanel != null)
        {
            winDialogue = winPanel.GetComponent<Dialogue>();
            if (winDialogue != null)
            {
                winDialogue.lines = new string[] { winMessage };
            }
        }

        // Запускаем стартовый диалог
        if (startPanel != null)
        {
            startDialogue = startPanel.GetComponent<Dialogue>();
            if (startDialogue != null)
            {
                startDialogue.lines = startLines;
                startDialogue.OnDialogueComplete += OnStartDialogueComplete;
                startPanel.SetActive(true);
                startDialogue.StartDialogue();
            }
        }

        // Настраиваем панель уровня
        if (levelPanel != null)
        {
            levelDialogue = levelPanel.GetComponent<Dialogue>();
            levelPanel.SetActive(false);
            levelDialogue.OnDialogueComplete += OnLevelDialogueComplete;
        }

        yield return null;
    }

    void OnStartDialogueComplete()
    {
        StartCoroutine(TransitionFromStartToFirstLevel());
    }

    void OnWinRestartClicked()
    {
        StartCoroutine(RestartGameRoutine());
    }

    IEnumerator RestartGameRoutine()
    {
        winPanel.SetActive(false);
        
        // Прячем изображение подсказки
        if (hintImage != null)
        {
            yield return StartCoroutine(SlideAnimation(hintImageOriginalPos + new Vector2(slideDistance, 0)));
        }
        
        startPanel.SetActive(true);
        startDialogue.SetLines(winRestartLines);
    }

    public void ShowLevelPanel(int levelNumber)
    {
        if (levelPanel == null || levelDialogue == null) return;

        if (hideLevelPanelCoroutine != null)
        {
            StopCoroutine(hideLevelPanelCoroutine);
            hideLevelPanelCoroutine = null;
        }

        string[] lines = GetLevelDialogue(levelNumber);
        levelPanel.SetActive(true);
        levelDialogue.SetLines(lines);
        
        levelPanelShowTime = Time.time;
    }

    private IEnumerator HideLevelPanelWithDelay()
    {
        float timeSinceShow = Time.time - levelPanelShowTime;
        float remainingMinTime = Mathf.Max(0, minDisplayTime - timeSinceShow);
        
        yield return new WaitForSeconds(remainingMinTime + levelPanelHideDelay);
        
        if (levelPanel != null)
        {
            levelPanel.SetActive(false);
        }
        
        hideLevelPanelCoroutine = null;
    }

    private void OnLevelDialogueComplete()
    {
        hideLevelPanelCoroutine = StartCoroutine(HideLevelPanelWithDelay());
    }

    public void ShowWinPanel()
    {
        StartCoroutine(ShowWinPanelRoutine());
    }

    IEnumerator ShowWinPanelRoutine()
    {
        if (hideLevelPanelCoroutine != null)
        {
            StopCoroutine(hideLevelPanelCoroutine);
            hideLevelPanelCoroutine = null;
        }
        
        // Прячем изображение подсказки
        if (hintImage != null)
        {
            yield return StartCoroutine(SlideAnimation(hintImageOriginalPos + new Vector2(slideDistance, 0)));
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWinSound();
        }
        
        startPanel.SetActive(false);
        levelPanel.SetActive(false);
        winPanel.SetActive(true);
        
        if (winDialogue != null)
        {
            winDialogue.lines = new string[] { winMessage };
            winDialogue.StartDialogue();
        }
        else if (winText != null)
        {
            winText.text = winMessage;
        }
    }

    public void ShowLoseScreen()
    {
        StartCoroutine(ShowLoseScreenRoutine());
    }

    IEnumerator ShowLoseScreenRoutine()
    {
        if (hideLevelPanelCoroutine != null)
        {
            StopCoroutine(hideLevelPanelCoroutine);
            hideLevelPanelCoroutine = null;
        }
        
        // Прячем изображение подсказки
        if (hintImage != null)
        {
            yield return StartCoroutine(SlideAnimation(hintImageOriginalPos + new Vector2(slideDistance, 0)));
        }
        
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLoseSound();
        }
        
        winPanel.SetActive(false);
        levelPanel.SetActive(false);
        startPanel.SetActive(true);
        startDialogue.SetLines(loseLines);
    }

    // Основные методы анимации переходов
    public IEnumerator TransitionToNextLevel(int nextLevel)
    {
        if (hintImage == null) yield break;

        // 1. Сдвигаем вправо
        yield return StartCoroutine(SlideAnimation(hintImageOriginalPos + new Vector2(slideDistance, 0)));
        
        // 2. LevelManager делает сброс и генерацию нового уровня
        yield return StartCoroutine(levelManager.ResetAndGenerateLevel(nextLevel));
        
        // 3. Возвращаем на место с анимацией
        yield return StartCoroutine(SlideAnimation(hintImageOriginalPos));
    }

    IEnumerator TransitionFromStartToFirstLevel()
    {
        // Скрываем стартовую панель
        startPanel.SetActive(false);

        // Сначала убираем hint image вправо
        if (hintImage != null)
        {
            yield return StartCoroutine(SlideAnimation(hintImageOriginalPos + new Vector2(slideDistance, 0)));
        }

        // Затем запускаем первый уровень с анимацией
        yield return TransitionToNextLevel(1);
    }

    // Универсальная корутина для анимации движения
    private IEnumerator SlideAnimation(Vector2 targetPos)
    {
        Vector2 startPos = hintImage.anchoredPosition;
        float elapsed = 0f;
        
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            hintImage.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        hintImage.anchoredPosition = targetPos;
    }

    private string[] GetLevelDialogue(int levelNumber)
    {
        return levelNumber switch
        {
            1 => level1Lines,
            2 => level2Lines,
            3 => level3Lines,
            4 => level4Lines,
            5 => level5Lines,
            6 => level6Lines,
            7 => level7Lines,
            8 => level8Lines,
            9 => level9Lines,
            10 => level10Lines,
            _ => new string[] { $"Уровень {levelNumber}", "Найдите сокровище" }
        };
    }

    void OnDestroy()
    {
        if (startDialogue != null)
            startDialogue.OnDialogueComplete -= OnStartDialogueComplete;
            
        if (levelDialogue != null)
            levelDialogue.OnDialogueComplete -= OnLevelDialogueComplete;
    }
}