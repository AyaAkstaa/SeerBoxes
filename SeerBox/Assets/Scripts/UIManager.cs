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

    private Dialogue startDialogue;
    private Dialogue levelDialogue;
    private Dialogue winDialogue;
    private LevelManager levelManager;

    void Start()
    {
        winPanel.SetActive(false);
        levelManager = FindObjectOfType<LevelManager>();
        
        // Настраиваем кнопку рестарта
        if (restartButton != null)
            restartButton.onClick.AddListener(OnWinRestartClicked);
            
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
                // Создаем массив с одним элементом - winMessage
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
        winPanel.SetActive(false);
        startPanel.SetActive(true);
        startDialogue.SetLines(winRestartLines);
    }

    public void ShowLevelPanel(int levelNumber)
    {
        if (levelPanel == null || levelDialogue == null) return;

        string[] lines = GetLevelDialogue(levelNumber);
        levelPanel.SetActive(true);
        levelDialogue.SetLines(lines);
    }

    public void ShowWinPanel()
    {
        startPanel.SetActive(false);
        levelPanel.SetActive(false);
        winPanel.SetActive(true);
        
        // Запускаем анимацию текста победы
        if (winDialogue != null)
        {
            // Обновляем текст на случай, если он изменился в инспекторе
            winDialogue.lines = new string[] { winMessage };
            winDialogue.StartDialogue();
        }
        else
        {
            // Если нет Dialogue компонента, просто устанавливаем текст
            if (winText != null)
                winText.text = winMessage;
        }
    }

    public void ShowLoseScreen()
    {
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
    }
}