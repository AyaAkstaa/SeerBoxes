using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class Chest : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {
    public int index;
    public Image body;
    public Text label;
    public Action<int> OnClicked;

    // Настройки анимации
    [Header("Animation Settings")]
    public float hoverScale = 1.1f;
    public float correctScale = 1.0f;
    public float wrongScale = 0.8f;
    public float animationDuration = 0.2f;
    public float wrongAnimationDuration = 0.3f; // Отдельная скорость для неправильного выбора
    public bool hideAfterWrong = true; // Скрывать ли объект после неправильного выбора
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Добавляем задержку для предотвращения спама звуков
    private float lastHoverTime = 0f;
    private float hoverCooldown = 0.2f;
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private bool isInitialized = false;
    private bool isInteractable = true;
    private CanvasGroup canvasGroup; // Для управления прозрачностью

    void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;
        
        // Сохраняем оригинальный размер
        originalScale = transform.localScale;
        correctScale = 1.0f;
        
        // Гарантируем, что у нас есть компонент Image
        if (body == null)
        {
            body = GetComponent<Image>();
            if (body == null)
            {
                body = GetComponentInChildren<Image>();
            }
        }
        
        // Добавляем или получаем CanvasGroup для управления прозрачностью
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        isInitialized = true;
    }

    public virtual void Init(int idx, Action<int> onClicked) {
        Initialize();
        
        index = idx;
        OnClicked = onClicked;
        if (label) label.text = (idx+1).ToString();
        gameObject.SetActive(true);
        isInteractable = true;
        
        // Восстанавливаем видимость и интерактивность
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Сбрасываем масштаб при инициализации
        transform.localScale = originalScale * correctScale;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (!isInteractable) return;
        
        OnClicked?.Invoke(index);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!isInteractable) return;
        
        if (label) label.color = Color.yellow;
        
        if (AudioManager.Instance != null && Time.time - lastHoverTime > hoverCooldown)
        {
            AudioManager.Instance.PlayChestHover();
            lastHoverTime = Time.time;
        }

        // Запускаем анимацию увеличения только если сундук интерактивен
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * hoverScale, animationDuration));
    }
    
    public void OnPointerExit(PointerEventData eventData) {
        if (!isInteractable) return;
        
        if (label) label.color = Color.white;

        // Возвращаем к обычному размеру только если сундук интерактивен
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * correctScale, animationDuration));
    }

    // Метод для правильного выбора
    public void MarkAsCorrect()
    {
        isInteractable = false;
        if (label) label.color = Color.green;
        
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleTo(originalScale * correctScale, animationDuration));
    }

    // Метод для неправильного выбора
    public void MarkAsWrong()
    {
        isInteractable = false;
        if (label) label.color = Color.red;
        
        if (scaleCoroutine != null)
            StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleToWithHide(originalScale * wrongScale, wrongAnimationDuration));
    }

    // Корутина для плавного изменения масштаба
    private IEnumerator ScaleTo(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, scaleCurve.Evaluate(t));
            yield return null;
        }

        transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    // Новая корутина для уменьшения с последующим скрытием
    private IEnumerator ScaleToWithHide(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float time = 0f;

        // Анимация уменьшения масштаба
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, scaleCurve.Evaluate(t));
            
            // Если включено скрытие, добавляем плавное исчезновение
            if (hideAfterWrong && canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            }
            
            yield return null;
        }

        transform.localScale = targetScale;
        
        // Скрываем объект после завершения анимации
        if (hideAfterWrong)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        
        scaleCoroutine = null;
    }

    // Метод для принудительного сброса анимации
    public void ResetAnimation()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        
        transform.localScale = originalScale * correctScale;
        isInteractable = true;
        
        if (label) label.color = Color.white;
        
        // Восстанавливаем видимость
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }

    public void SetLabelText(string s) { if (label) label.text = s; }
    public void SetImage(Sprite sp) { /* for ImageChest override or set child image */ }
    public void SetColor(Color c) { if (body) body.color = c; }
    public void SetAlpha(float a) { var col = body.color; col.a = a; body.color = col; }
}