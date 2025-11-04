using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class Chest : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler 
{
    public int index;
    public Image body;
    public Action<int> OnClicked;

    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float hoverAnimationDuration = 0.1f; // Быстрая анимация наведения
    public float jumpHeight = 80f;
    public float jumpDuration = 0.8f;
    public AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalScale;
    private Vector2 originalPosition;
    private bool isInteractable = true;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Coroutine currentAnimation;

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = transform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        
        // Гарантируем наличие компонентов
        if (body == null)
            body = GetComponent<Image>();
        if (body == null)
            body = GetComponentInChildren<Image>();
            
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public virtual void Init(int idx, Action<int> onClicked) 
    {
        Initialize();
        
        index = idx;
        OnClicked = onClicked;
        
        // Сбрасываем все состояния
        gameObject.SetActive(true);
        isInteractable = true;
        transform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = Quaternion.identity;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Принудительно показываем сундук
        if (body != null)
        {
            body.color = Color.white;
        }
        
        // Останавливаем все анимации
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    public void SetInteractable(bool state)
    {
        isInteractable = state;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = state;
        }
    }

    public void OnPointerClick(PointerEventData eventData) 
    {
        if (!isInteractable) return;
        OnClicked?.Invoke(index);
    }

    public void OnPointerEnter(PointerEventData eventData) 
    {
        if (!isInteractable) return;
        
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayChestHover();
            
        // Используем быструю анимацию для наведения
        StartCoroutine(ScaleTo(originalScale * hoverScale, hoverAnimationDuration));
    }
    
    public void OnPointerExit(PointerEventData eventData) 
    {
        if (!isInteractable) return;

        // Используем быструю анимацию для наведения
        StartCoroutine(ScaleTo(originalScale, hoverAnimationDuration));
    }

    public void MarkAsCorrect()
    {
        isInteractable = false;
        
        // Запускаем анимацию победы для правильного сундука
        StartCoroutine(VictoryAnimation());
    }

    public void MarkAsWrong(Action onComplete = null)
    {
        isInteractable = false;
        
        // Запускаем анимацию прыжка и падения для неправильного сундука
        currentAnimation = StartCoroutine(JumpAndFallAnimation(onComplete));
    }

    public void PlayJumpAndFallAnimation(Action onComplete = null)
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(JumpAndFallAnimation(onComplete));
    }

    // Анимация прыжка и падения для неправильных сундуков
    private IEnumerator JumpAndFallAnimation(Action onComplete = null)
    {
        Vector2 startPosition = originalPosition;
        float elapsed = 0f;

        // Фаза 1: Прыжок вверх
        while (elapsed < jumpDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (jumpDuration * 0.4f);
            float height = jumpCurve.Evaluate(t) * jumpHeight;
            rectTransform.anchoredPosition = startPosition + new Vector2(0, height);
            yield return null;
        }

        Vector2 peakPosition = rectTransform.anchoredPosition;
        elapsed = 0f;

        // Фаза 2: Падение вниз с исчезновением
        while (elapsed < jumpDuration * 0.6f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (jumpDuration * 0.6f);
            
            // Падаем ниже исходной позиции
            float fallDistance = fallCurve.Evaluate(t) * jumpHeight * 1.2f;
            rectTransform.anchoredPosition = peakPosition - new Vector2(0, fallDistance);
            
            // Постепенно исчезаем
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            // Немного уменьшаем масштаб при падении
            transform.localScale = originalScale * (1f - t * 0.3f);

            yield return null;
        }

        // Завершаем анимацию
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        onComplete?.Invoke();
        currentAnimation = null;
    }

    // Анимация победы для правильного сундука
    private IEnumerator VictoryAnimation()
    {
        float elapsed = 0f;
        float duration = 1.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Пульсация масштаба
            float pulse = Mathf.Sin(elapsed * 8f) * 0.1f + 1f;
            transform.localScale = originalScale * pulse;
            
            // Легкое вращение
            float rotation = Mathf.Sin(elapsed * 6f) * 5f;
            rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            
            yield return null;
        }
        
        // Возвращаем к нормальному состоянию
        transform.localScale = originalScale;
        rectTransform.localRotation = Quaternion.identity;
    }

    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    public void ResetAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        StopAllCoroutines();
        
        transform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localRotation = Quaternion.identity;
        isInteractable = true;
        
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public bool HasFallen()
    {
        return canvasGroup != null && canvasGroup.alpha < 0.1f;
    }

    public void SetColor(Color c) { if (body) body.color = c; }
}