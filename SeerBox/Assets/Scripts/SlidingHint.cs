using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class SlidingHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Placement")]
    public bool slideFromLeft = true;
    [Tooltip("Сколько пикселей выглядывает край, когда спрятан")]
    public float peekAmount = 48f;

    [Header("Timing")]
    [Tooltip("Время появления (сек)")]
    public float showDuration = 0.45f;
    [Tooltip("Время скрытия (сек)")]
    public float hideDuration = 0.55f;
    [Tooltip("Задержка перед скрытием (сек) — полезно, чтобы не прятать мгновенно при выходе курсора)")]
    public float hideDelay = 0.08f;

    [Header("Easing / Behaviour")]
    [Tooltip("Если true — используем SmoothDamp (spring-like), иначе используем easing curves")]
    public bool useSpring = false;
    [Tooltip("Жёсткость пружины (меньше = мягче), только для useSpring")]
    public float springSmoothTime = 0.12f;
    [Tooltip("Максимальная 'прыгающая' составляющая при показе (scale). 1 = без подпрыгивания.")]
    public float bounceScale = 1.06f;
    [Tooltip("Multiplier для overshoot в EaseOutBack (только для show)")]
    public float overshoot = 1.05f;

    [Header("Custom curves (optional)")]
    public AnimationCurve showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve hideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Interaction")]
    public RectTransform hoverZone; // optional
    public bool toggleOnClick = false;

    // internals
    RectTransform rect;
    CanvasGroup cg;
    Vector2 shownAnchoredPos;
    Vector2 hiddenAnchoredPos;
    Coroutine anim;
    Vector3 velocity = Vector3.zero; // for SmoothDamp

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();

        // compute positions
        shownAnchoredPos = rect.anchoredPosition;
        float width = Mathf.Max(1f, Mathf.Abs(rect.rect.width));
        float hideOffset = Mathf.Max(width - peekAmount, Mathf.Abs(peekAmount));
        Vector2 dir = slideFromLeft ? Vector2.left : Vector2.right;
        hiddenAnchoredPos = shownAnchoredPos + dir * hideOffset;

        // start hidden
        rect.anchoredPosition = hiddenAnchoredPos;
        rect.localScale = Vector3.one;
        cg.alpha = 1f;

        // hover zone ensure raycast
        if (hoverZone != null) {
            var g = hoverZone.GetComponent<UnityEngine.UI.Graphic>();
            if (g == null) {
                var img = hoverZone.gameObject.AddComponent<Image>();
                img.color = new Color(1f,1f,1f,0f);
            }
        }

        // set reasonable default curves if user didn't change them
        // showCurve default: easeOutBack approximation
        if (showCurve == null || showCurve.keys.Length == 0) {
            showCurve = new AnimationCurve(new Keyframe(0,0), new Keyframe(0.6f, 1.08f), new Keyframe(1,1f));
            showCurve.SmoothTangents(0, 0); showCurve.SmoothTangents(1,0);
        }
        // hideCurve default: smooth in-out cubic
        if (hideCurve == null || hideCurve.keys.Length == 0) {
            hideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    #region Interface handlers
    public void OnPointerEnter(PointerEventData eventData) { Show(); }
    public void OnPointerExit(PointerEventData eventData) { if (!toggleOnClick) Hide(); }
    public void OnPointerClick(PointerEventData eventData) {
        if (toggleOnClick) {
            bool isShown = Vector2.Distance(rect.anchoredPosition, shownAnchoredPos) < 2f;
            if (isShown) Hide(); else Show();
        }
    }
    #endregion

    public void Show()
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(Animate(true));
    }

    public void Hide()
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(DelayedHide());
    }

    IEnumerator DelayedHide() {
        if (hideDelay > 0f) yield return new WaitForSecondsRealtime(hideDelay);
        anim = StartCoroutine(Animate(false));
    }

    IEnumerator Animate(bool show)
    {
        if (useSpring)
        {
            // SmoothDamp style (spring-like)
            float duration = show ? showDuration : hideDuration;
            float elapsed = 0f;
            Vector3 startPos = rect.anchoredPosition;
            Vector3 targetPos = (show ? (Vector3)shownAnchoredPos : (Vector3)hiddenAnchoredPos);
            Vector3 startScale = rect.localScale;
            Vector3 targetScale = show ? Vector3.one * bounceScale : Vector3.one;

            velocity = Vector3.zero;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                // smooth damp toward target with time factor
                rect.anchoredPosition = Vector3.SmoothDamp(rect.anchoredPosition, targetPos, ref velocity, springSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                rect.localScale = Vector3.SmoothDamp(rect.localScale, targetScale, ref velocity, springSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                yield return null;
            }
            rect.anchoredPosition = targetPos;
            rect.localScale = Vector3.one;
            yield break;
        }

        // Easing curves approach
        float durationCurve = show ? showDuration : hideDuration;
        float t = 0f;
        Vector2 start = rect.anchoredPosition;
        Vector2 target = show ? shownAnchoredPos : hiddenAnchoredPos;
        Vector3 startS = rect.localScale;
        Vector3 targetS = show ? Vector3.one * bounceScale : Vector3.one;

        // choose chosen curve
        AnimationCurve curve = show ? showCurve : hideCurve;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, durationCurve);
            float eval = Mathf.Clamp01(curve.Evaluate(t));
            // If show, we may want to scale the eval to account for overshoot value (curve's keyframes can encode overshoot)
            rect.anchoredPosition = Vector2.LerpUnclamped(start, target, eval);
            // scale using a softer ease to avoid abrupt pop when hiding
            rect.localScale = Vector3.LerpUnclamped(startS, targetS, eval);
            yield return null;
        }

        rect.anchoredPosition = target;
        rect.localScale = Vector3.one;
    }

    // Optional API: programmatic attach to hoverZone events (if used)
    void OnEnable()
    {
        if (hoverZone != null) {
            AddEventTrigger(hoverZone.gameObject, EventTriggerType.PointerEnter, (d) => Show());
            AddEventTrigger(hoverZone.gameObject, EventTriggerType.PointerExit, (d) => { if (!toggleOnClick) Hide(); });
            if (toggleOnClick) AddEventTrigger(hoverZone.gameObject, EventTriggerType.PointerClick, (d) => {
                bool isShown = Vector2.Distance(rect.anchoredPosition, shownAnchoredPos) < 2f;
                if (isShown) Hide(); else Show();
            });
        }
    }

    void AddEventTrigger(GameObject target, EventTriggerType type, System.Action<BaseEventData> callback)
    {
        var trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener((data) => callback(data));
        trigger.triggers.Add(entry);
    }
}
