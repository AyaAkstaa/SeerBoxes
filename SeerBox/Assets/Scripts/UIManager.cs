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

    [Header("Win Panel Elements")]
    public TextMeshProUGUI winText;
    public Button restartButton;

    [Header("Dialogue Texts")]
    public string[] startLines = {
        "Добро пожаловать в игру!",
        "Ищите сокровища...",
    };

    public string[] winRestartLines = {
        "Захотел пройти игру еще раз?"
    };

    public string[] loseLines = {
        "Ой не получилось пройти("
    };

    [Header("Win Text")]
    public string winMessage = "Ты победил!!!";

    [Header("Level Dialogues")]
    public string[] level1Lines = { "Уровень 1", "Выберите правильный сундук" };
    public string[] level2Lines = { "Уровень 2", "Следуйте карте" };
    public string[] level3Lines = { "Уровень 3", "Решите математическую задачу" };
    public string[] level4Lines = { "Уровень 4", "Щщщщщ....." };
    public string[] level5Lines = { "Уровень 5", "Куда ведут эти стрелки?" };
    public string[] level6Lines = { "Уровень 6", "Что-то тут не так" };
    public string[] level7Lines = { "Уровень 7", "Что за пятна на листочке?" };
    public string[] level8Lines = { "Уровень 8", "Ты думал все так легко?" };

    [Header("Level Panel Settings")]
    public float levelPanelHideDelay = 2f;
    public float minDisplayTime = 3f;

    private Dialogue startDialogue;
    private Dialogue levelDialogue;
    private Dialogue winDialogue;
    private LevelManager levelManager;
    private Coroutine hideLevelPanelCoroutine;
    private float levelPanelShowTime;

    void Start()
    {
        winPanel.SetActive(false);
        levelManager = FindObjectOfType<LevelManager>();
        
        // Настраиваем кнопку рестарта
        if (restartButton != null)
            restartButton.onClick.AddListener(OnWinRestartClicked);
            
        // Добавляем обработчик звука на кнопку рестарта
        if (restartButton != null && restartButton.GetComponent<ButtonSoundHandler>() == null)
        {
            restartButton.gameObject.AddComponent<ButtonSoundHandler>();
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
                yield return StartCoroutine(StartStartDialogue());
            }
        }

        // Настраиваем панель уровня
        if (levelPanel != null)
        {
            levelDialogue = levelPanel.GetComponent<Dialogue>();
            levelPanel.SetActive(false);
            
            levelDialogue.OnDialogueComplete += OnLevelDialogueComplete;
        }
    }

    IEnumerator StartStartDialogue()
    {
        yield return null;
        startDialogue.StartDialogue();
    }

    void OnStartDialogueComplete()
    {
        StartGame();
    }

    void StartGame()
    {
        startPanel.SetActive(false);
        levelManager.StartLevel(1);
    }

    void OnWinRestartClicked()
    {
        // Звук теперь воспроизводится через ButtonSoundHandler
        winPanel.SetActive(false);
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
        if (hideLevelPanelCoroutine != null)
        {
            StopCoroutine(hideLevelPanelCoroutine);
            hideLevelPanelCoroutine = null;
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
        else
        {
            if (winText != null)
                winText.text = winMessage;
        }
    }

    public void ShowLoseScreen()
    {
        if (hideLevelPanelCoroutine != null)
        {
            StopCoroutine(hideLevelPanelCoroutine);
            hideLevelPanelCoroutine = null;
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