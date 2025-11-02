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

    [Header("Start Panel Texts")]
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

    private Dialogue startDialogue;
    private Dialogue levelDialogue;
    private LevelManager levelManager;
    private bool isStartSequenceActive = true;

    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        
        // Инициализация стартовой панели
        if (startPanel != null)
        {
            startDialogue = startPanel.GetComponent<Dialogue>();
            if (startDialogue != null)
            {
                startDialogue.lines = startLines;
                startDialogue.OnDialogueComplete += OnStartDialogueComplete;
                startPanel.SetActive(true);
                
                StartCoroutine(StartStartDialogue());
            }
            else
            {
                Debug.LogError("Start panel doesn't have Dialogue component!");
                StartGame();
            }
        }
        else
        {
            StartGame();
        }

        // Инициализация панели уровня
        if (levelPanel != null)
        {
            levelDialogue = levelPanel.GetComponent<Dialogue>();
            if (levelDialogue != null)
            {
                levelDialogue.OnDialogueComplete += OnLevelDialogueComplete;
            }
            levelPanel.SetActive(false);
        }

        // Инициализация панели победы
        if (winPanel != null)
        {
            // Находим элементы WinPanel
            if (winText == null)
                winText = winPanel.GetComponentInChildren<TextMeshProUGUI>();
            
            if (restartButton == null)
                restartButton = winPanel.GetComponentInChildren<Button>();
            
            // Настраиваем кнопку рестарта
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnWinRestartClicked);
            }
            
            // Устанавливаем текст победы
            if (winText != null)
            {
                winText.text = "Ты победил!!!";
            }
            
            winPanel.SetActive(false);
        }
    }

    IEnumerator StartStartDialogue()
    {
        yield return null;
        if (startDialogue != null)
        {
            startDialogue.StartDialogue();
        }
    }

    void StartGame()
    {
        if (startPanel != null)
            startPanel.SetActive(false);
            
        if (levelManager != null)
            levelManager.StartLevel(1);
    }

    void OnStartDialogueComplete()
    {
        isStartSequenceActive = false;
        StartGame();
    }

    void OnLevelDialogueComplete()
    {
        if (levelPanel != null)
            levelPanel.SetActive(false);
    }

    void OnWinRestartClicked()
    {
        // Возвращаемся к стартовой панели с другим текстом
        ShowStartPanelWithWinRestart();
    }

    public void ShowLevelPanel(int levelNumber)
    {
        if (levelPanel != null && levelDialogue != null)
        {
            string[] currentLevelLines = {
                $"Уровень {levelNumber}",
                GetLevelHint(levelNumber)
            };
            
            levelPanel.SetActive(true);
            StartCoroutine(SetLevelDialogueLines(currentLevelLines));
        }
    }

    IEnumerator SetLevelDialogueLines(string[] lines)
    {
        yield return null;
        if (levelDialogue != null)
        {
            levelDialogue.SetLines(lines);
        }
    }

    public void ShowWinPanel()
    {
        if (winPanel != null)
        {
            Debug.Log("Showing Win Panel");
            
            // Скрываем другие панели
            if (levelPanel != null) 
            {
                levelPanel.SetActive(false);
                Debug.Log("Level panel hidden");
            }
            if (startPanel != null) 
            {
                startPanel.SetActive(false);
                Debug.Log("Start panel hidden");
            }
            
            // Показываем панель победы
            winPanel.SetActive(true);
            Debug.Log("Win panel activated");
            
            // Убеждаемся, что текст установлен
            if (winText != null)
            {
                winText.text = "Ты победил!!!";
                Debug.Log("Win text set to: " + winText.text);
            }
        }
        else
        {
            Debug.LogError("WinPanel is not assigned in UIManager!");
        }
    }

    public void ShowLoseScreen()
    {
        // Показываем стартовую панель с текстом проигрыша
        ShowStartPanelWithLoseText();
    }

    void ShowStartPanelWithWinRestart()
    {
        if (startPanel != null && startDialogue != null)
        {
            // Скрываем winPanel
            if (winPanel != null)
                winPanel.SetActive(false);
                
            // Показываем стартовую панель
            startPanel.SetActive(true);
            
            // Устанавливаем текст для повторного прохождения
            startDialogue.SetLines(winRestartLines);
        }
    }

    void ShowStartPanelWithLoseText()
    {
        if (startPanel != null && startDialogue != null)
        {
            // Скрываем другие панели
            if (levelPanel != null) levelPanel.SetActive(false);
            if (winPanel != null) winPanel.SetActive(false);
            
            // Показываем стартовую панель
            startPanel.SetActive(true);
            
            // Устанавливаем текст проигрыша
            startDialogue.SetLines(loseLines);
        }
    }

    private string GetLevelHint(int level)
    {
        switch (level)
        {
            case 1: return "Выберите правильный сундук";
            case 2: return "Следуйте карте";
            case 3: return "Решите математическую задачу";
            case 4: return "Щщщщщ.....";
            case 5: return "Куда ведут эти стрелки?";
            case 6: return "Что-то тут не так";
            case 7: return "Что за пятна на листочке?";
            case 8: return "Ты думал все так легко? Вот и гадай теперь где здесь правильный сундук...";
            default: return "Найдите сокровище";
        }
    }

    void OnDestroy()
    {
        if (startDialogue != null)
            startDialogue.OnDialogueComplete -= OnStartDialogueComplete;
        
        if (levelDialogue != null)
            levelDialogue.OnDialogueComplete -= OnLevelDialogueComplete;
    }
}