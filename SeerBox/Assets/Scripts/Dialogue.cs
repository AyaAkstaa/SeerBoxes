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

    private int index;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private bool skipRequested = false;

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
        else
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
            OnDialogueComplete?.Invoke();
            return;
        }
        
        index = 0;
        if (textTMP != null)
        {
            textTMP.text = string.Empty;
        }
        isTyping = false;
        skipRequested = false;
        
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
            
        typingCoroutine = StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        if (lines == null || lines.Length == 0 || textTMP == null) 
        {
            OnDialogueComplete?.Invoke();
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
            // Диалог завершен - ВЫЗЫВАЕМ СОБЫТИЕ
            OnDialogueComplete?.Invoke();
        }
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
}