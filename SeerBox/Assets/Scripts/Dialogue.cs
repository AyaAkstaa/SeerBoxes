using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class Dialogue : MonoBehaviour
{
    public TextMeshProUGUI textTMP;
    public string[] lines;
    public float textSpeed = 0.05f;
    public float timeBetweenLines = 0.5f;

    [Header("Typewriter Sound Settings")]
    public bool enableTypewriterSound = true;
    [Range(0, 1)] public float soundChance = 0.7f; // Вероятность воспроизведения звука для каждого символа
    public float minSoundDelay = 0.03f; // Минимальная задержка между звуками

    private int index;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private bool skipRequested = false;
    private bool isComplete = false;
    private float lastSoundTime = 0f;

    // Событие завершения диалога
    public System.Action OnDialogueComplete;

    void Start()
    {
        if (textTMP != null)
        {
            textTMP.text = string.Empty;
        }
        
        // Запускаем диалог только если есть строки и объект активен
        if (lines != null && lines.Length > 0 && gameObject.activeInHierarchy)
        {
            StartDialogue();
        }
    }

    void Update()
    {
        // Проверяем ввод только если объект активен и есть строки для отображения
        if (!gameObject.activeInHierarchy || lines == null || lines.Length == 0) 
            return;

        bool inputPressed = false;
        
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputPressed = true;
        }
        
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            inputPressed = true;
        }
        
        if (inputPressed)
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        if (isTyping)
        {
            skipRequested = true;
        }
        else if (!isComplete)
        {
            NextLine();
        }
    }

    public void StartDialogue()
    {
        // Проверяем, активен ли GameObject перед запуском корутины
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("Cannot start dialogue on inactive GameObject");
            return;
        }

        if (lines == null || lines.Length == 0) 
        {
            CompleteDialogue();
            return;
        }
        
        index = 0;
        if (textTMP != null)
        {
            textTMP.text = string.Empty;
        }
        isTyping = false;
        skipRequested = false;
        isComplete = false;
        lastSoundTime = 0f;
        
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
            
        typingCoroutine = StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        if (lines == null || lines.Length == 0 || textTMP == null) 
        {
            CompleteDialogue();
            yield break;
        }

        isTyping = true;
        textTMP.text = string.Empty;
        string currentLine = lines[index];
        
        for (int i = 0; i < currentLine.Length; i++)
        {
            if (skipRequested)
            {
                textTMP.text = currentLine;
                skipRequested = false;
                break;
            }
            
            textTMP.text += currentLine[i];

            // Воспроизводим звук печатной машинки с вероятностью и задержкой
            if (enableTypewriterSound && ShouldPlaySound(currentLine[i]))
            {
                PlayTypewriterSound();
            }
            
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
        
        // Пауза после завершения строки
        yield return new WaitForSeconds(timeBetweenLines);
        
        // Автоматически переходим к следующей строке
        NextLine();
    }
    
    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeLine());
        }
        else
        {
            // Диалог завершен
            CompleteDialogue();
        }
    }

    void CompleteDialogue()
    {
        isComplete = true;
        OnDialogueComplete?.Invoke();
    }

    // Метод для принудительной установки новых строк
    public void SetLines(string[] newLines)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        lines = newLines;
        index = 0;
        isTyping = false;
        skipRequested = false;
        isComplete = false;
        lastSoundTime = 0f;
        
        if (textTMP != null)
        {
            textTMP.text = string.Empty;
        }
        
        // Запускаем диалог только если GameObject активен
        if (gameObject.activeInHierarchy)
        {
            StartDialogue();
        }
        else
        {
            Debug.LogWarning("Cannot start dialogue on inactive GameObject");
        }
    }

    // Метод для проверки, завершен ли диалог
    public bool IsDialogueComplete()
    {
        return isComplete;
    }

    // Метод для принудительного завершения диалога
    public void ForceComplete()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (textTMP != null && lines != null && lines.Length > 0 && index < lines.Length)
        {
            textTMP.text = lines[index];
        }

        CompleteDialogue();
    }

    // Определяем, нужно ли воспроизводить звук для данного символа
    private bool ShouldPlaySound(char character)
    {
        // Не воспроизводим звук для пробелов и некоторых знаков препинания
        if (char.IsWhiteSpace(character) || character == ' ' || character == '\n')
            return false;

        // Проверяем вероятность
        if (Random.Range(0f, 1f) > soundChance)
            return false;

        // Проверяем задержку между звуками
        if (Time.time - lastSoundTime < minSoundDelay)
            return false;

        return true;
    }

    // Воспроизведение звука печатной машинки
    private void PlayTypewriterSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTypewriterSound();
            lastSoundTime = Time.time;
        }
    }
}