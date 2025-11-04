using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public int maxLevel = 10;

    [Header("UI")]
    public RectTransform chestParent;
    public RectTransform chestZoneSize;
    public Image hintImage;
    public Sprite hintSprite;
    public TextMeshProUGUI hintText;

     [Header("Animation Settings")]
    public float jumpDuration = 0.8f; 

    [Header("Fonts")]
    public TMP_FontAsset standartFont;
    public TMP_FontAsset arrowFont;

    [Header("Prefabs")]
    public GameObject chestGenericPrefab;
    public GameObject chestNumberPrefab;
    public GameObject chestImagePrefab;
    public GameObject chestHiddenPrefab;
    public GameObject chestOddPrefab;
    public GameObject chestColorPrefab;

    [Header("Debug")]
    public bool enableDebug = true;

    List<GameObject> spawned = new List<GameObject>();
    int correctIndex = -1;
    int currentLevel = 1;
    private bool isLevelLoading = false;
    private Coroutine levelLoadCoroutine;

    [Header("Coin Settings")]
    public GameObject coinPrefab;
    public float lifeDuration = 0.9f;           // Сколько живёт монетка (сек)
    public float appearScale = 0.6f;            // Начальный масштаб (relative)
    public float peakScale = 1.2f;              // Пиковый масштаб при "появлении"
    public float finalScale = 1.0f;             // Итоговый масштаб после "попа"
    public Vector2 uiMoveOffset = new Vector2(0f, 40f); // Сдвиг в пикселях для UI (за время жизни)
    public Vector3 worldMoveOffset = new Vector3(0f, 0.6f, 0f); // Сдвиг в world-единицах
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // example assets you must assign:
    [Header("Level 2 assets")]
    public Sprite islandHintSprite;
    public Sprite xMarkSprite;

    [Header("Level 4 assets")]
    public RectTransform noChestZoneSize;

    [Header("Level 7 assets")]
    public Sprite colorImage1;
    public Sprite colorImage2;

    private UIManager uiManager;

    private bool isLevelInteractive = true; // Добавляем флаг интерактивности уровня

    class MathTemplate
    {
        public string expr;
        public Func<int, bool> predicate;
        public int correctMin, correctMax;
        public int distractorMin, distractorMax;
        public MathTemplate(string e, Func<int, bool> pred, int cmin, int cmax, int dmin, int dmax)
        {
            expr = e; predicate = pred; correctMin = cmin; correctMax = cmax; distractorMin = dmin; distractorMax = dmax;
        }
    }

    List<MathTemplate> GetMathTemplates()
    {
        return new List<MathTemplate> {
            // 2 + 2 * 2 - x ≤ 6 → 6 - x ≤ 6 → x ≥ 0
            // Правильные числа от 1 до 6
            new MathTemplate("2 + 2 * 2 - ...", x => x <= 6, 1, 6, 7, 50),
            
            // 8 - 3*(6-1) + 1 + x ≥ 3 → -6 + x ≥ 3 → x ≥ 9
            // Но требуются правильные числа от 3 до 12
            new MathTemplate("8 - 3*(6-1) + 1#... ", x => x >= 3 && x <= 12, 3, 12, -20, 2),
            
            // 65 * 4 - 20 + x ≤ 60 → 240 - 20 + x ≤ 60 → 220 + x ≤ 60 → x ≤ -160
            // Исправляем: правильные числа от 51 до 60
            new MathTemplate("65 * 4 - 20#...", x => x >= 51 && x <= 60, 51, 60, 0, 50),
            
            // 496 : 16 + x > 31 → 31 + x > 31 → x > 0
            // Но требуются числа 31 и выше
            new MathTemplate("496 : 16 + ...", x => x >= 31, 31, 200, -20, 30),
            
            // (11 - 8 + 2) * 0 - x ≤ 0 → 0 - x ≤ 0 → -x ≤ 0 → x ≥ 0
            // Но требуются от 0 и отрицательные числа
            new MathTemplate("(11 - 8 + 2) * 0 - ...", x => x <= 0, -20, 0, 1, 80)
        };
    }

    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    void Clear()
    {
        if (levelLoadCoroutine != null)
        {
            StopCoroutine(levelLoadCoroutine);
            levelLoadCoroutine = null;
        }

        // Сбрасываем анимации всех сундуков перед удалением
        foreach (var g in spawned)
        {
            if (g != null)
            {
                var chest = g.GetComponent<Chest>();
                if (chest != null)
                {
                    chest.ResetAnimation();
                }
                Destroy(g);
            }
        }
        spawned.Clear();

        correctIndex = -1;
        isLevelInteractive = true;
    
        // Сбрасываем UI
        if (hintImage != null)
        {
            RectTransform rect = hintImage.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(238, 412);
            hintImage.sprite = null;
            hintImage.color = Color.clear;
        }

        if (hintText != null)
        {
            RectTransform TextRect = hintText.GetComponent<RectTransform>();
            TextRect.sizeDelta = new Vector2(4.84f, 7.35f);
            hintText.text = "";
            hintText.font = standartFont;
            hintText.UpdateFontAsset();
        }

        // Удаляем вспомогательные объекты
        var old = hintImage.transform.Find("HintX");
        if (old) Destroy(old.gameObject);

        // Удаляем все цветные изображения
        var existsColors = GameObject.FindGameObjectsWithTag("ColorImage");
        foreach (GameObject colorImage in existsColors)
        {
            if (colorImage != null)
                Destroy(colorImage);
        }

        isLevelLoading = false;
    }

    public void StartLevel(int lvl)
    {
        if (isLevelLoading) 
        {
            Debug.LogWarning("Загрузка...");
            return;
        }

        isLevelLoading = true;
        
        // Для всех уровней, включая первый, используем анимацию
        StartCoroutine(StartLevelWithAnimation(lvl));
    }

    IEnumerator StartLevelWithAnimation(int lvl)
    {
        // Для всех уровней используем анимацию перехода
        yield return uiManager.TransitionToNextLevel(lvl);
    }

    // Новый метод для сброса и генерации уровня в середине анимации
    public IEnumerator ResetAndGenerateLevel(int lvl)
    {
        Clear();
        currentLevel = lvl;

        Debug.Log($"Рестарт и запуск уровня {lvl}");

        // Показываем панель уровня
        if (uiManager != null)
        {
            uiManager.ShowLevelPanel(lvl);
        }

        // Генерируем уровень немедленно (без задержки)
        GenerateLevelImmediate(lvl);
        
        yield return null;
    }

    void GenerateLevelImmediate(int lvl)
    {
        Debug.Log($"Запуск уровня {lvl}");

        isLevelInteractive = true; // Убеждаемся, что уровень интерактивен

        switch (lvl)
        {
            case 1: GenerateLevel1(); break;
            case 2: GenerateLevel2(); break;
            case 3: GenerateLevel3(); break;
            case 4: GenerateLevel4(); break;
            case 5: GenerateLevel5(); break;
            case 6: GenerateLevel6(); break;
            case 7: GenerateLevel7(); break;
            case 8: GenerateLevel8(); break;
            case 9: GenerateLevel9(); break;
            case 10: GenerateLevel10(); break;
        }

        isLevelLoading = false;
    }

    // helper: spawn grid rows x cols with given prefab, returns list
    List<GameObject> SpawnGrid(int rows, int cols, GameObject prefab)
    {
        List<GameObject> list = new List<GameObject>();
        float spacingX = 2.3f, spacingY = 1.6f; // оставил твои значения; интерпретация ниже
        Vector2 origin = new Vector2(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f);
        int idx = 0;

        // --- Подготовка размеров зоны (UI-пиксели и мировые единицы) ---
        bool haveZone = chestZoneSize != null;
        Vector2 zoneSizeUI = Vector2.zero;      // в пикселях (для UI-элементов)
        Vector2 zoneSizeWorld = Vector2.zero;   // в world units (для SpriteRenderer)

        if (haveZone)
        {
            zoneSizeUI = chestZoneSize.rect.size; // в локальных пикселях RectTransform

            // получим world-ширину/высоту зоны, чтобы масштабировать world-префабы
            Vector3[] corners = new Vector3[4];
            chestZoneSize.GetWorldCorners(corners); // 0 = bottom-left, 2 = top-right
            Vector3 bl = corners[0];
            Vector3 tr = corners[2];
            zoneSizeWorld = new Vector2(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y));
        }

        // --- Получаем образец размеров префаба (по одному экземпляру, чтобы не инстанцировать лишние) ---
        // Создадим временный экземпляр, чтобы измерить его "текущий" визуальный размер (учтём местный scale)
        var sample = Instantiate(prefab);
        sample.transform.SetParent(chestParent, false);
        // сразу отключим видимость временного (чтобы не мелькнул)
        sample.SetActive(false);

        float sampleWidthUI = 0f, sampleHeightUI = 0f;
        float sampleWidthWorld = 0f, sampleHeightWorld = 0f;
        var sampleRect = sample.GetComponent<RectTransform>();
        var sampleSpriteR = sample.GetComponentInChildren<SpriteRenderer>(); // ищем спрайт в children

        if (sampleRect != null)
        {
            // UI prefab: rect.rect даёт "unscaled" размер; учитываем localScale
            Vector2 rectSize = sampleRect.rect.size;
            Vector3 ls = sampleRect.localScale;
            sampleWidthUI = rectSize.x * Mathf.Abs(ls.x);
            sampleHeightUI = rectSize.y * Mathf.Abs(ls.y);
        }
        if (sampleSpriteR != null)
        {
            // World prefab: bounds.size даёт размер в локальных единицах * текущий lossyScale
            Vector3 bsize = sampleSpriteR.bounds.size; // уже с учётом lossy scale
            // bounds.size учитывает мировой масштаб, но возьмём local size via sprite.bounds / transform.localScale
            // для простоты используем bounds.size
            sampleWidthWorld = Mathf.Abs(bsize.x);
            sampleHeightWorld = Mathf.Abs(bsize.y);
        }

        // Удаляем временный образец
        DestroyImmediate(sample);

        // Если оба нулевые — предупредим
        if (sampleWidthUI == 0f && sampleWidthWorld == 0f)
        {
            Debug.LogWarning("SpawnGrid: не удалось определить размер префаба (нет RectTransform и нет SpriteRenderer). Масштабирование пропущено.");
        }

        // --- Вычисляем масштаб, чтобы сетка уместилась в chestZoneSize ---
        float finalScale = 1f;
        if (haveZone)
        {
            // для UI-префабов:
            if (sampleWidthUI > 0f && sampleHeightUI > 0f)
            {
                float gridWidthUI = (cols - 1) * spacingX + sampleWidthUI;
                float gridHeightUI = (rows - 1) * spacingY + sampleHeightUI;
                float sx = zoneSizeUI.x / Mathf.Max(0.0001f, gridWidthUI);
                float sy = zoneSizeUI.y / Mathf.Max(0.0001f, gridHeightUI);
                float scaleUI = Mathf.Min(sx, sy);
                // небольшой запас (чтобы не упёрлось в края)
                scaleUI *= 0.95f;
                finalScale = Mathf.Min(finalScale, scaleUI);
            }
            // для world-префабов:
            if (sampleWidthWorld > 0f && sampleHeightWorld > 0f)
            {
                float gridWidthWorld = (cols - 1) * spacingX + sampleWidthWorld;
                float gridHeightWorld = (rows - 1) * spacingY + sampleHeightWorld;
                float sxw = zoneSizeWorld.x / Mathf.Max(0.0001f, gridWidthWorld);
                float syw = zoneSizeWorld.y / Mathf.Max(0.0001f, gridHeightWorld);
                float scaleWorld = Mathf.Min(sxw, syw);
                scaleWorld *= 0.95f;
                finalScale = Mathf.Min(finalScale, scaleWorld);
            }
            // если всё нулевое — finalScale остается 1
            finalScale = Mathf.Max(0.0001f, finalScale);
        }

        // --- Теперь инстанцируем реальную сетку и применим масштаб + позиционирование ---
        List<GameObject> created = new List<GameObject>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector2 pos = origin + new Vector2(c * spacingX, -r * spacingY);

                var go = Instantiate(prefab);
                go.transform.SetParent(chestParent, false);

                // обработка UI-префаба
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // применяем масштаб (uniform)
                    rect.localScale = Vector3.one * finalScale;

                    // позиционируем в anchoredPosition (при этом pos должен быть в тех же единицах, что spacing)
                    rect.anchoredPosition = pos;

                }
                else
                {
                    // world-объект: изменим localScale и локальную позицию
                    go.transform.localScale = go.transform.localScale * finalScale;
                    go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                }

                // Инициализация Chest (если компонент есть)
                var chest = go.GetComponent<Chest>();
                if (chest != null)
                {
                    chest.Init(idx, OnChestClicked);
                }
                else
                {
                    Debug.LogWarning($"Spawned prefab '{prefab.name}' does not contain Chest component.");
                }

                created.Add(go);
                list.Add(go);
                idx++;
            }
        }

        spawned = list;
        return list;
    }

    void OnChestClicked(int idx)
    {
        if (isLevelLoading || !isLevelInteractive) return;

        Debug.Log("Clicked: " + idx);

        // БЛОКИРУЕМ ВСЕ СУНДУКИ СРАЗУ ПОСЛЕ ПЕРВОГО КЛИКА
        SetAllChestsInteractable(false);
        isLevelInteractive = false;

        Chest clickedChest = null;
        if (idx < spawned.Count && spawned[idx] != null)
        {
            clickedChest = spawned[idx].GetComponent<Chest>();
        }

        if (idx == correctIndex)
        {
            // Воспроизводим звук правильного сундука
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCorrectChest();
            }

            // Помечаем правильный сундук
            if (clickedChest != null)
            {
                clickedChest.MarkAsCorrect();
                SpawnCoinAt(clickedChest.transform);
                clickedChest.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.25f); // зелёный оттенок
            }

            hintText.text = "Correct!";

            // Запускаем анимацию падения для всех неправильных сундуков с задержкой
            StartCoroutine(AnimateWrongChestsFall());
        }
        else
        {
            // Воспроизводим звук неправильного сундука
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWrongChest();
            }

            // Запускаем анимацию только для нажатого неправильного сундука
            if (clickedChest != null)
            {
                clickedChest.MarkAsWrong(() =>
                {
                    // После завершения анимации нажатого сундука
                    // запускаем анимацию для остальных НЕУПАВШИХ сундуков
                    StartCoroutine(AnimateAllChestsFall());
                });
            }
            else
            {
                StartCoroutine(AnimateAllChestsFall());
            }
        }
    }


    // Анимация падения всех сундуков (при неправильном выборе)
    private IEnumerator AnimateAllChestsFall()
    {
        yield return new WaitForSeconds(0.3f); // Небольшая задержка перед началом

        List<Chest> chestsToAnimate = new List<Chest>();
        float maxJumpDuration = 0f;

        // Собираем только те сундуки, которые еще не упали (исключаем уже анимированные)
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null)
            {
                var chest = spawned[i].GetComponent<Chest>();
                if (chest != null)
                {
                    // Проверяем, интерактивен ли еще сундук (не упал ли он уже)
                    var canvasGroup = chest.GetComponent<CanvasGroup>();
                    if (canvasGroup != null && canvasGroup.alpha > 0.1f) // Если сундук еще видим
                    {
                        chestsToAnimate.Add(chest);
                        maxJumpDuration = Mathf.Max(maxJumpDuration, chest.jumpDuration);
                    }
                }
            }
        }

        // Если нет сундуков для анимации, выходим
        if (chestsToAnimate.Count == 0)
        {
            Lose();
            yield break;
        }

        // Запускаем все анимации почти одновременно с небольшими случайными задержками
        foreach (var chest in chestsToAnimate)
        {
            float randomDelay = UnityEngine.Random.Range(0f, 0.1f);
            StartCoroutine(AnimateSingleChestWithDelay(chest, randomDelay));
        }

        // Ждем завершения всех анимаций
        yield return new WaitForSeconds(maxJumpDuration + 0.1f + 0.5f);
        Lose();
    }
    
    // Анимация падения только неправильных сундуков (при правильном выборе)
    private IEnumerator AnimateWrongChestsFall()
    {
        yield return new WaitForSeconds(0.5f); // Пауза чтобы увидеть правильный сундук

        List<Chest> chestsToAnimate = new List<Chest>();
        float maxJumpDuration = 0f;

        // Собираем неправильные сундуки для анимации (только те, что еще не упали)
        for (int i = 0; i < spawned.Count; i++)
        {
            if (i != correctIndex && spawned[i] != null)
            {
                var chest = spawned[i].GetComponent<Chest>();
                if (chest != null)
                {
                    var canvasGroup = chest.GetComponent<CanvasGroup>();
                    if (canvasGroup != null && canvasGroup.alpha > 0.1f) // Если сундук еще видим
                    {
                        chestsToAnimate.Add(chest);
                        maxJumpDuration = Mathf.Max(maxJumpDuration, chest.jumpDuration);
                    }
                }
            }
        }

        // Если нет сундуков для анимации, переходим к следующему уровню
        if (chestsToAnimate.Count == 0)
        {
            NextLevel();
            yield break;
        }

        // Запускаем все анимации почти одновременно с небольшими случайными задержками
        foreach (var chest in chestsToAnimate)
        {
            float randomDelay = UnityEngine.Random.Range(0f, 0.08f);
            StartCoroutine(AnimateSingleChestWithDelay(chest, randomDelay));
        }

        // Ждем завершения всех анимаций
        yield return new WaitForSeconds(maxJumpDuration + 0.1f + 0.5f);
        NextLevel();
    }

    // Вспомогательная корутина для анимации одного сундука с задержкой
    private IEnumerator AnimateSingleChestWithDelay(Chest chest, float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        chest.PlayJumpAndFallAnimation();
    }

    public void SpawnCoinAt(Transform chestTransform)
    {
        if (coinPrefab == null)
        {
            Debug.LogWarning("Coin prefab not set on GameManagerCoinSpawner.");
            return;
        }

        // Определяем, является ли объект UI (имеет RectTransform и находится внутри Canvas)
        RectTransform chestRect = chestTransform as RectTransform;
        Canvas parentCanvas = chestTransform.GetComponentInParent<Canvas>();

        // Проверяем, является ли префаб UI-префабом (имеет RectTransform)
        RectTransform prefabRect = coinPrefab.GetComponent<RectTransform>();

        if (chestRect != null && parentCanvas != null && prefabRect != null)
        {
            // UI-случай: инстанцируем префаб как дочерний элемент сундука, сохраняя локальную позицию/масштаб
            GameObject instance = Instantiate(coinPrefab, chestTransform, false);
            RectTransform rt = instance.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one * appearScale;
            }

            // Попытка найти компонент Image внутри префаба для запуска UI-анимации
            Image img = instance.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.raycastTarget = false;
                StartCoroutine(AnimateCoinUI(img, rt));
            }
            else
            {
                // Если в UI-префабе нет Image — просто уничтожаем через время жизни (защитная логика)
                Destroy(instance, lifeDuration + 0.1f);
            }
        }
        else
        {
            // World-случай: инстанцируем префаб в позиции сундука
            GameObject instance = Instantiate(coinPrefab, chestTransform.position, Quaternion.identity);
            instance.transform.SetParent(null);
            instance.transform.localScale = Vector3.one * appearScale;

            // Попытка найти SpriteRenderer внутри префаба
            SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color col = sr.color;
                col.a = 0f;
                sr.color = col;
                StartCoroutine(AnimateCoinWorld(sr, instance.transform));
            }
            else
            {
                // Если нет SpriteRenderer, проверим на Image (вдруг это UI-префаб, но мы в world)
                Image img = instance.GetComponentInChildren<Image>();
                RectTransform rt = instance.GetComponent<RectTransform>();
                if (img != null && rt != null && chestRect != null)
                {
                    // Переназначим в UI-иерархию сундука и запустим UI-анимацию
                    rt.SetParent(chestTransform, false);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one * appearScale;
                    img.raycastTarget = false;
                    StartCoroutine(AnimateCoinUI(img, rt));
                }
                else
                {
                    // Ничего не найдено — безопасно удалить после времени жизни
                    Destroy(instance, lifeDuration + 0.1f);
                }
            }
        }
    }


    private IEnumerator AnimateCoinUI(Image img, RectTransform rt)
    {
        float t = 0f;
        float half = lifeDuration * 0.5f;

        // стартовые параметры
        Vector3 startScale = Vector3.one * appearScale;
        Vector3 peak = Vector3.one * peakScale;
        Vector3 end = Vector3.one * finalScale;
        Color c = img.color;
        c.a = 0f;
        img.color = c;

        Vector2 startPos = rt.anchoredPosition;
        Vector2 targetPos = startPos + uiMoveOffset;

        // Первый этап — появление до пика (половина времени)
        while (t < half)
        {
            t += Time.deltaTime;
            float tt = Mathf.Clamp01(t / half);
            float s = scaleCurve.Evaluate(tt);
            img.color = new Color(1f, 1f, 1f, alphaCurve.Evaluate(tt));
            rt.localScale = Vector3.Lerp(startScale, peak, s);
            rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, tt);
            yield return null;
        }

        // Второй этап — спад к финишному масштабу с исчезновением
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float tt = Mathf.Clamp01(t / half);
            float s = scaleCurve.Evaluate(tt);
            float alpha = Mathf.Clamp01(1f - alphaCurve.Evaluate(tt)); // плавное исчезание
            img.color = new Color(1f, 1f, 1f, alpha);
            rt.localScale = Vector3.Lerp(peak, end, s);
            rt.anchoredPosition = Vector2.Lerp(targetPos, targetPos + new Vector2(0f, 8f), tt * 0.5f);
            yield return null;
        }

        // Гарантируем финальное состояние и удаляем
        Destroy(rt.gameObject);
    }

    private IEnumerator AnimateCoinWorld(SpriteRenderer sr, Transform tTrans)
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * appearScale;
        Vector3 peak = Vector3.one * peakScale;
        Vector3 end = Vector3.one * finalScale;
        Color startColor = sr.color;
        startColor.a = 0f;
        sr.color = startColor;

        Vector3 startPos = tTrans.position;
        Vector3 targetPos = startPos + worldMoveOffset;

        float half = lifeDuration * 0.5f;

        // появление -> пик
        float time = 0f;
        while (time < half)
        {
            time += Time.deltaTime;
            float tt = Mathf.Clamp01(time / half);
            float s = scaleCurve.Evaluate(tt);
            Color c = sr.color;
            c.a = alphaCurve.Evaluate(tt);
            sr.color = c;
            tTrans.localScale = Vector3.Lerp(startScale, peak, s);
            tTrans.position = Vector3.Lerp(startPos, targetPos, tt * 0.6f);
            yield return null;
        }

        // спад -> исчезновение
        time = 0f;
        while (time < half)
        {
            time += Time.deltaTime;
            float tt = Mathf.Clamp01(time / half);
            float s = scaleCurve.Evaluate(tt);
            float alpha = Mathf.Clamp01(1f - alphaCurve.Evaluate(tt));
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
            tTrans.localScale = Vector3.Lerp(peak, end, s);
            tTrans.position = Vector3.Lerp(targetPos, targetPos + worldMoveOffset * 0.25f, tt);
            yield return null;
        }

        Destroy(tTrans.gameObject);
    }
    


    IEnumerator NextLevelWithDelay()
    {
        yield return new WaitForSeconds(1f);
        NextLevel();
    }

    private void SetAllChestsInteractable(bool interactable)
    {
        foreach (var chestObj in spawned)
        {
            if (chestObj != null)
            {
                var chest = chestObj.GetComponent<Chest>();
                if (chest != null)
                {
                    chest.SetInteractable(interactable);
                    if (interactable)
                    {
                        chest.ResetAnimation();
                    }
                }
            }
        }
    }


    void Lose()
    {
        hintText.text = "Wrong!";
        StartCoroutine(ShowLoseScreenWithDelay());
    }

    IEnumerator ShowLoseScreenWithDelay()
    {
        yield return new WaitForSeconds(2f);
        
        // Очищаем уровень
        Clear();
        
        // Показываем экран проигрыша
        if (uiManager != null)
        {
            uiManager.ShowLoseScreen();
        }
    }

    public void NextLevel()
    {
        if (isLevelLoading) return;

        int nextLevel = currentLevel + 1;
        Debug.Log($"Moving to level {nextLevel}");

        if (nextLevel > maxLevel)
        {
            // все уровни пройдены -> победа
            ShowWin();
        }
        else
        {
            StartLevel(nextLevel);
        }
    }

    void ShowWin()
    {
        // Очищаем уровень
        Clear();
        
        // Показываем панель победы
        if (uiManager != null)
        {
            uiManager.ShowWinPanel();
        }
    }

    // Метод для отладки - перейти на конкретный уровень
    public void DebugLoadLevel(int level)
    {
        if (enableDebug && !isLevelLoading)
        {
            Debug.Log($"Debug: Loading level {level}");
            StartLevel(Mathf.Clamp(level, 1, 10));
        }
    }

    // Метод для отладки - показать текущий уровень
    public void DebugShowCurrentLevel()
    {
        if (enableDebug)
        {
            Debug.Log($"Current level: {currentLevel}, Loading: {isLevelLoading}");
        }
    }

    // ====== LEVEL IMPLEMENTATIONS ======

    // +1) Left/Right training: 2 identical chests
    void GenerateLevel1()
    {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        correctIndex = UnityEngine.Random.Range(0, 2);
        if (correctIndex == 0)
            hintText.text = "Сокровище слева";
        else
            hintText.text = "Сокровище справа";
        var list = SpawnGrid(1, 2, chestGenericPrefab);

    }

    // 2) Island map: 5x5 grid, island image divided into 25 sectors, X at random sector.
    void GenerateLevel2()
    {
        hintImage.sprite = islandHintSprite;
        RectTransform rect = hintImage.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 300);
        hintImage.color = Color.white;
        // spawn 5x5 same chest prefab
        var list = SpawnGrid(5, 5, chestGenericPrefab);
        // choose a random sector 0..24
        int sel = UnityEngine.Random.Range(0, 25);
        correctIndex = sel;
        // draw an overlay 'X' on hintImage programmatically: we can set hintImage UV or place a child X sprite at corresponding sector
        // simplest: place small X GameObject as child of hintImage with anchored position
        PlaceXOnHint(sel, 5, 5);
    }

    void PlaceXOnHint(int index, int cols, int rows)
    {
        // remove old Xs
        var old = hintImage.transform.Find("HintX");
        if (old) Destroy(old.gameObject);
        var go = new GameObject("HintX", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(hintImage.transform, false);
        var img = go.GetComponent<Image>();
        // Use configured xMarkSprite when available, otherwise fall back to a colored placeholder
        if (xMarkSprite != null)
        {
            img.sprite = xMarkSprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        else
        {
            img.color = new Color(1, 0, 0, 0.7f);
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        // compute cell anchored pos
        int col = index % cols;
        int row = index / cols;
        float px = (col + 0.5f) / cols - 0.5f; // normalized centered
        float py = 0.5f - (row + 0.5f) / rows;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(px * hintImage.rectTransform.rect.width, py * hintImage.rectTransform.rect.height);
        // size: use sprite native size if available, otherwise fallback to small square
        if (xMarkSprite != null)
        {
            // sprite.rect is in pixels — clamp to a reasonable UI size so it doesn't dominate
            Vector2 native = new Vector2(xMarkSprite.rect.width, xMarkSprite.rect.height);
            // limit larger dimension to 64 px
            float maxDim = 64f;
            float scale = 1f;
            if (native.x > native.y && native.x > maxDim) scale = maxDim / native.x;
            else if (native.y > native.x && native.y > maxDim) scale = maxDim / native.y;
            else if (native.x > maxDim && native.y > maxDim) scale = maxDim / Mathf.Max(native.x, native.y);
            rt.sizeDelta = native * scale;
        }
        else
        {
            rt.sizeDelta = new Vector2(24, 24);
        }
    }

    void GenerateLevel3()
    {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;

        var list = SpawnGrid(2, 3, chestNumberPrefab);

        // Простая последовательность с разными паттернами
        int patternType = UnityEngine.Random.Range(0, 3);
        List<int> numbers = new List<int>();
        string hint = "";
        int correctNumber = 0;

        switch (patternType)
        {
            case 0: // Арифметическая прогрессия
                int start = UnityEngine.Random.Range(1, 10);
                int step = UnityEngine.Random.Range(1, 4);
                correctNumber = start + 3 * step;
                numbers.Add(correctNumber);
                hint = $"{start}, {start + step}, {start + 2 * step}...";
                break;

            case 1: // Геометрическая прогрессия (упрощенная)
                int geoStart = UnityEngine.Random.Range(1, 5);
                int multiplier = UnityEngine.Random.Range(2, 4);
                correctNumber = geoStart * multiplier * multiplier * multiplier;
                numbers.Add(correctNumber);
                hint = $"{geoStart}, {geoStart * multiplier}, {geoStart * multiplier * multiplier}...";
                break;

            // case 2: // Четные/нечетные
            //     int oddEvenStart = UnityEngine.Random.Range(1, 10);
            //     bool isEvenSequence = UnityEngine.Random.Range(0, 2) == 0;
            //     correctNumber = isEvenSequence ?
            //         (oddEvenStart % 2 == 0 ? oddEvenStart + 6 : oddEvenStart + 7) :
            //         (oddEvenStart % 2 == 1 ? oddEvenStart + 6 : oddEvenStart + 7);
            //     numbers.Add(correctNumber);
            //     string type = isEvenSequence ? "четных" : "нечетных";
            //     hint = $"Продолжи последовательность {type} чисел";
            //     break;
        }

        // Добавляем неправильные варианты
        for (int i = 1; i < list.Count; i++)
        {
            int wrongNumber;
            do
            {
                wrongNumber = correctNumber + UnityEngine.Random.Range(-10, 11);
                if (wrongNumber == correctNumber) wrongNumber += 5;
            } while (numbers.Contains(wrongNumber) || wrongNumber <= 0);

            numbers.Add(wrongNumber);
        }

        // Перемешиваем
        for (int i = 0; i < numbers.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, numbers.Count);
            int temp = numbers[i];
            numbers[i] = numbers[randomIndex];
            numbers[randomIndex] = temp;
        }

        correctIndex = numbers.IndexOf(correctNumber);
        hintText.text = hint;

        // Назначаем числа
        for (int i = 0; i < list.Count; i++)
        {
            var nc = list[i].GetComponent<NumberChest>();
            if (nc != null) nc.SetNumber(numbers[i]);
        }
    }

    void GenerateLevel4()
    {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;

        int target = UnityEngine.Random.Range(20, 80);
        var list = SpawnGrid(2, 3, chestNumberPrefab);

        // Создаем числа, одно из которых ближайшее к target
        List<int> numbers = new List<int>();

        // Правильное число (ближайшее к target)
        int closestNumber = target + UnityEngine.Random.Range(-5, 6);
        // Гарантируем, что не выходим за разумные пределы
        closestNumber = Mathf.Clamp(closestNumber, 1, 100);
        numbers.Add(closestNumber);

        // Неправильные числа (дальше от target)
        for (int i = 1; i < list.Count; i++)
        {
            int wrongNumber;
            do
            {
                int offset = UnityEngine.Random.Range(8, 15);
                wrongNumber = target + (UnityEngine.Random.Range(0, 2) == 0 ? offset : -offset);
                wrongNumber = Mathf.Clamp(wrongNumber, 1, 100);
            } while (numbers.Contains(wrongNumber) || Mathf.Abs(wrongNumber - target) <= Mathf.Abs(closestNumber - target));

            numbers.Add(wrongNumber);
        }

        // Перемешиваем
        for (int i = 0; i < numbers.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, numbers.Count);
            int temp = numbers[i];
            numbers[i] = numbers[randomIndex];
            numbers[randomIndex] = temp;
        }

        correctIndex = numbers.IndexOf(closestNumber);

        hintText.text = $"Что-то по типу {target}";

        // Назначаем числа
        for (int i = 0; i < list.Count; i++)
        {
            var nc = list[i].GetComponent<NumberChest>();
            if (nc != null) nc.SetNumber(numbers[i]);
        }
    }

    // 3) Math: 3x3 numbered chests; hint partial expression implies result <=6 => choose chest with number <=6 (only one such)
    void GenerateLevel5()
    {
        var templates = GetMathTemplates();

        // выбираем случайный шаблон (перемешивание индексов)
        var order = new List<int>();
        for (int i = 0; i < templates.Count; i++) order.Add(i);
        for (int i = 0; i < order.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, order.Count);
            int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
        }

        MathTemplate chosen = null;
        int correctValue = int.MinValue;

        // Попытка найти корректное значение для каждого шаблона
        foreach (int idx in order)
        {
            var t = templates[idx];
            // сначала случайные попытки
            for (int attempt = 0; attempt < 300; attempt++)
            {
                int cand = UnityEngine.Random.Range(t.correctMin, t.correctMax + 1);
                if (t.predicate(cand)) { chosen = t; correctValue = cand; break; }
            }
            if (chosen != null) break;
            // перебор как fallback
            for (int v = t.correctMin; v <= t.correctMax; v++)
            {
                if (t.predicate(v)) { chosen = t; correctValue = v; break; }
            }
            if (chosen != null) break;
        }

        // если ничего не найдено — откатим к простому шаблону ≤6 (маловероятно)
        if (chosen == null)
        {
            hintImage.sprite = hintSprite;
            hintImage.color = Color.white;
            hintText.text = "2 + 2 * 2 - ... (page torn). Result ≤ 6.";
            var list = SpawnGrid(3, 3, chestNumberPrefab);
            int correctLocal = UnityEngine.Random.Range(0, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                int v = (i == correctLocal) ? UnityEngine.Random.Range(1, 7) : UnityEngine.Random.Range(7, 50);
                var nc = list[i].GetComponent<NumberChest>(); if (nc != null) nc.SetNumber(v);
            }
            correctIndex = correctLocal;
            return;
        }

        // Заполняем подсказку
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = chosen.expr;

        // Спавним 3x3 и вставляем правильное значение в случайную ячейку
        var spawnedList = SpawnGrid(3, 3, chestNumberPrefab);
        int correctIdx = UnityEngine.Random.Range(0, spawnedList.Count);

        HashSet<int> used = new HashSet<int>();
        used.Add(correctValue);

        for (int i = 0; i < spawnedList.Count; i++)
        {
            int val;
            if (i == correctIdx)
            {
                val = correctValue;
            }
            else
            {
                // генерируем дистрактор в заданном диапазоне, проверяя, что он НЕ удовлетворяет predicate
                int attempts = 0;
                int minD = chosen.distractorMin;
                int maxD = chosen.distractorMax;
                if (minD > maxD) { minD = -100; maxD = 200; }
                do
                {
                    val = UnityEngine.Random.Range(minD, maxD + 1);
                    attempts++;
                    // гарантируем, что дистрактор не проходит predicate и не дублирует значения
                } while ((used.Contains(val) || chosen.predicate(val)) && attempts < 800);

                if (attempts >= 800)
                {
                    // аварийный дистрактор: подобрать гарантированно не подходящее значение
                    if (chosen.predicate(minD)) val = maxD + 100 + i;
                    else val = minD - 100 - i;
                }
                used.Add(val);
            }

            var nc = spawnedList[i].GetComponent<NumberChest>();
            if (nc != null) nc.SetNumber(val);
        }

        correctIndex = correctIdx;
    }

    void GenerateLevel6()
    {
        List<string> zero = new List<string> { "г", "ж", "з", "и", "к", "л", "м", "н", "п", "с", "т", "у", "щ", "ч", "ш", "э" }; // нет дырок
        List<string> one = new List<string> { "а", "б", "д", "е", "о", "р", "ъ", "ю", "ь", "я" }; // одна дырка
        List<string> two = new List<string> { "в", "ф" };      // две дырки

        Dictionary<int, List<string>> holeMap = new Dictionary<int, List<string>> {
            {0, zero}, {1, one}, {2, two}
        };

        // choose target holes 0..2 inclusive
        int targetHoles = UnityEngine.Random.Range(0, 3);
        // if targetHoles == 0 the correct letter must always be "щ"
        string correctLetter;
        if (targetHoles == 0)
        {
            correctLetter = "щ";
        }
        else
        {
            var pool = holeMap[targetHoles];
            correctLetter = pool[UnityEngine.Random.Range(0, pool.Count)];
        }


        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;

        // 6 chests in 2x3 grid
        var list = SpawnGrid(2, 3, chestNumberPrefab);
        int correctLocal = UnityEngine.Random.Range(0, list.Count);
        hintText.text = $"{targetHoles}... о";

        // build wrong-pool: all letters from other hole counts (so correctLetter appears only once)
        List<string> wrongPool = new List<string>();
        for (int k = 0; k <= 2; k++)
        {
            if (k == targetHoles) continue;
            wrongPool.AddRange(holeMap[k]);
        }
        // ensure wrongPool does not accidentally contain the correctLetter
        wrongPool.RemoveAll(s => s == correctLetter);

        // assign letters
        for (int i = 0; i < list.Count; i++)
        {
            var numChest = list[i].GetComponent<NumberChest>();
            if (numChest == null) continue;
            if (i == correctLocal)
            {
                numChest.SetNumber(correctLetter);
            }
            else
            {
                // pick a wrong letter (may repeat among wrongs)
                if (wrongPool.Count == 0)
                {
                    // fallback: pick any letter that's not the correct one
                    foreach (var kv in holeMap)
                    {
                        foreach (var ch in kv.Value)
                        {
                            if (ch != correctLetter) wrongPool.Add(ch);
                        }
                    }
                    wrongPool.RemoveAll(s => s == correctLetter);
                }
                string letter = wrongPool[UnityEngine.Random.Range(0, wrongPool.Count)];
                numChest.SetNumber(letter);
            }
        }

        correctIndex = correctLocal;
    }

    // 5) Arrow path: 6x6 grid (36). hint is textual arrows from top-left to target (precompute path)
    void GenerateLevel7()
    {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.font = arrowFont;
        hintText.text = "";
        var list = SpawnGrid(6, 6, chestGenericPrefab);
        var firstChest = list[0];
        firstChest.GetComponent<Image>().color = new Color(0.8f, 1f, 0.8f); // highlight start cell
        int cols = 6;
        // choose random target cell (not 0)
        int target = UnityEngine.Random.Range(1, 36);
        correctIndex = target;

        // короткий путь (для визуального подсказа) — самый прямой
        var shortPath = ComputePathIndices(0, target, cols);

        // длинный извилистый путь (на самом деле ведущий в ту же точку),
        // который можно использовать, если нужно "подглядеть" реальный маршрут.
        // Длинный путь строится как случайная блуждающая дорожка, а затем доводится до цели.
        int minLongSteps = Mathf.Clamp(shortPath.Count * 3, 12, 60); // минимум шагов в длинном пути
        var longPath = ComputeLongWindingPath(0, target, cols, minLongSteps);

        // показываем пользователю короткий, компактный набор стрелок, но при этом
        // реально существует длинный запутанный маршрут (longPath) который всё равно ведёт в target.
        hintText.text = PathToArrowString(longPath, cols);

        // при необходимости можно сохранить longPath куда-то (например в лог) для отладки:
        // Debug.Log("Long path length: " + longPath.Count + "  -> " + PathToArrowString(longPath, cols));
    }

    // returns list of grid indices from start to target (simple dx/dy greedy)
    List<int> ComputePathIndices(int start, int target, int cols)
    {
        List<int> p = new List<int>();
        int sx = start % cols, sy = start / cols;
        int tx = target % cols, ty = target / cols;
        int x = sx, y = sy;
        p.Add(start);
        while (x != tx || y != ty)
        {
            if (x < tx) x++;
            else if (x > tx) x--;
            else if (y < ty) y++;
            else if (y > ty) y--;
            p.Add(y * cols + x);
        }
        return p;
    }

    // строит длинный извилистый путь: делает случайные шаги, затем по кратчайшему пути идёт в цель
    List<int> ComputeLongWindingPath(int start, int target, int cols, int minSteps)
    {
        int rows = cols; // здесь поле квадратное 6x6, если нет — можно передать отдельно
        System.Random rnd = new System.Random();
        List<int> path = new List<int>();
        int cur = start;
        path.Add(cur);
        int prev = -1;

        int attempts = 0;
        while (path.Count < minSteps && attempts < minSteps * 8)
        {
            attempts++;
            // собрать доступные соседние клетки
            List<int> neigh = new List<int>();
            int x = cur % cols, y = cur / cols;
            if (x > 0) neigh.Add(cur - 1);
            if (x < cols - 1) neigh.Add(cur + 1);
            if (y > 0) neigh.Add(cur - cols);
            if (y < rows - 1) neigh.Add(cur + cols);

            // исключить мгновенный возврат туда, откуда пришли (уменьшит тривиальные циклы)
            if (prev != -1 && neigh.Contains(prev) && neigh.Count > 1) neigh.Remove(prev);

            if (neigh.Count == 0) break;
            int next = neigh[rnd.Next(neigh.Count)];
            path.Add(next);
            prev = cur;
            cur = next;

            // для дополнительной запутанности иногда делаем рывок в сторону цели, а затем снова в стороны
            if (rnd.NextDouble() < 0.08)
            {
                var shortTail = ComputePathIndices(cur, target, cols);
                // добавим пару первых шагов кратчайшего пути, чтобы не застрять
                int take = Mathf.Min(2, shortTail.Count - 1);
                for (int i = 1; i <= take; i++)
                {
                    path.Add(shortTail[i]);
                    prev = cur;
                    cur = shortTail[i];
                }
            }
        }

        // в конце примыкаем кратчайшим путем от текущей клетки до цели
        var finish = ComputePathIndices(cur, target, cols);
        // finish[0] == cur, поэтому добавляем с 1
        for (int i = 1; i < finish.Count; i++) path.Add(finish[i]);

        // очистим повторяющиеся подряд элементы (на всякий случай)
        List<int> clean = new List<int>();
        int last = -1;
        foreach (var v in path)
        {
            if (v != last) clean.Add(v);
            last = v;
        }
        return clean;
    }

    string PathToArrowString(List<int> path, int cols)
    {
        string s = "";
        for (int i = 0; i < path.Count - 1; i++)
        {
            int a = path[i], b = path[i + 1];
            int ax = a % cols, ay = a / cols;
            int bx = b % cols, by = b / cols;
            if (bx > ax) s += "> ";
            else if (bx < ax) s += "< ";
            else if (by > ay) s += "v ";
            else s += "^ ";
        }
        return s;
    }

    // 6) Two shown not correct; there is 1 hidden chest somewhere barely visible
    void GenerateLevel8()
    {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = "Может где-то слева... А может где-то справа... Но точно не в центре.";
        // show two big chests (2 visible) and place hidden chest random (outside grid)
        // but user said "both chests and 1 hidden" — implement: spawn 2 large chests and spawn one hidden at random pos
        var list = SpawnGrid(1, 2, chestGenericPrefab); // two visible
        // choose indices 0.. in spawned
        //int not1 = 0, not2 = 1;
        // spawn hidden somewhere in chestParent bounds, ensuring not overlapping existing chests
        Vector2 hiddenPos = FindRandomFreeSpot();
        Debug.Log($"FindRandomFreeSpot returned: {hiddenPos}");
        var hiddenGo = Instantiate(chestHiddenPrefab, chestParent);
        hiddenGo.GetComponent<RectTransform>().anchoredPosition = hiddenPos;
        var hiddenChest = hiddenGo.GetComponent<Chest>();
        hiddenChest.Init(2, OnChestClicked); // index 2
        spawned.Add(hiddenGo);
        // correct chest is hidden one
        correctIndex = 2;
    }

    Vector2 FindRandomFreeSpot()
    {
        Rect r = chestParent.rect;
        const float padding = 0.1f;
        const float minDistance = 0.5f;

        float minX = r.xMin + padding;
        float maxX = r.xMax - padding;
        float minY = r.yMin + padding;
        float maxY = r.yMax - padding;

        // Защита: если контейнер слишком мал
        if (minX > maxX || minY > maxY)
        {
            Debug.LogWarning("Parent rect too small for padding.");
            return new Vector2(6.34f, -3.98f);
        }

        Canvas canvas = chestParent.GetComponentInParent<Canvas>();

        for (int attempts = 0; attempts < 50; attempts++)
        {
            Vector2 cand = new Vector2(
                UnityEngine.Random.Range(minX, maxX),
                UnityEngine.Random.Range(minY, maxY)
            );

            bool ok = true;

            // Проверка расстояния до уже существующих сундуков (works if same parent coordinate space)
            foreach (var g in spawned)
            {
                if (g == null) continue;
                var rt = g.GetComponent<RectTransform>();
                if (rt == null) continue;
                if (Vector2.Distance(rt.anchoredPosition, cand) < minDistance)
                {
                    ok = false;
                    continue;
                }
            }

            if (!ok) continue;

            // ВАРИАНТ A: проверка перекрытия с noChestZone через преобразование координат в локальную систему noChestZone
            if (noChestZoneSize != null)
            {
                // cand — локальная позиция относительно chestParent.
                // Преобразуем её в мировой, затем в локальные координаты noChestZoneSize.
                Vector3 worldPos = chestParent.TransformPoint(cand);
                Vector3 localInNoChest = noChestZoneSize.InverseTransformPoint(worldPos);
                if (noChestZoneSize.rect.Contains((Vector2)localInNoChest))
                {
                    ok = false;
                }
            }

            if (ok) return cand;
        }

        Debug.Log("FindRandomFreeSpot: failed to find free spot");
        return new Vector2(6.34f, -3.98f);
    }

    void GenerateLevel9()
    {
        hintText.text = "";
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;

        // Спавним 6 сундуков
        var list = SpawnGrid(2, 3, chestColorPrefab);

        // Генерируем два разных цвета
        Color colorA = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 0.9f);
        Color colorB;

        // Гарантируем, что второй цвет отличается от первого
        do
        {
            colorB = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.7f, 0.9f);
        } while (ColorDistance(colorA, colorB) < 0.5f);

        // Смешанный цвет
        Color mixedColor = (colorA + colorB) * 0.5f;

        // Создаем список цветов
        List<Color> colors = new List<Color>();

        // Добавляем правильный цвет
        colors.Add(mixedColor);

        // Добавляем исходные цвета
        colors.Add(colorA);
        colors.Add(colorB);

        // Добавляем 3 случайных цвета, которые сильно отличаются от всех существующих
        for (int i = 0; i < 3; i++)
        {
            Color newColor;
            int attempts = 0;
            bool isDistinct;

            do
            {
                newColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 0.9f);
                isDistinct = true;

                // Проверяем, что новый цвет достаточно отличается от всех существующих
                foreach (Color existingColor in colors)
                {
                    if (ColorDistance(newColor, existingColor) < 0.4f) // Увеличили минимальное расстояние
                    {
                        isDistinct = false;
                        break;
                    }
                }

                attempts++;
                if (attempts > 50) // Защита от бесконечного цикла
                {
                    Debug.LogWarning("Could not generate sufficiently distinct color after 50 attempts");
                    break;
                }
            } while (!isDistinct);

            colors.Add(newColor);
        }

        // Перемешиваем
        for (int i = 0; i < colors.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, colors.Count);
            Color temp = colors[i];
            colors[i] = colors[randomIndex];
            colors[randomIndex] = temp;
        }

        // Назначаем цвета сундукам и находим правильный индекс
        correctIndex = -1;
        for (int i = 0; i < list.Count; i++)
        {
            var colorChest = list[i].GetComponent<ColorChest>();
            if (colorChest != null)
            {
                colorChest.SetColor(colors[i]);

                // Проверяем, является ли этот цвет смешанным
                if (IsColorSimilar(colors[i], mixedColor, 0.05f))
                {
                    correctIndex = i;
                }
            }
        }

        // Если по какой-то причине не нашли правильный цвет, устанавливаем первый
        if (correctIndex == -1)
        {
            correctIndex = 0;
            Debug.LogWarning("Could not find mixed color, using first chest as correct");
        }

        // Показываем два цвета-подсказки
        CreateHintColorImage(colorA, "HintColor1", new Vector2(-50, 0));
        CreateHintColorImage(colorB, "HintColor2", new Vector2(50, 0));

        // Добавляем плюс между ними
        CreatePlusSign();

        // Логируем расстояния между цветами для отладки
        Debug.Log($"Level 7 - ColorA: {colorA}, ColorB: {colorB}, Mixed: {mixedColor}");
        Debug.Log($"Correct index: {correctIndex}");
        Debug.Log($"Color distances:");
        for (int i = 0; i < colors.Count; i++)
        {
            for (int j = i + 1; j < colors.Count; j++)
            {
                Debug.Log($"  Color {i} to {j}: {ColorDistance(colors[i], colors[j]):F3}");
            }
        }
    }

    // Проверяет, похожи ли два цвета
    bool IsColorSimilar(Color a, Color b, float tolerance)
    {
        return Mathf.Abs(a.r - b.r) < tolerance &&
               Mathf.Abs(a.g - b.g) < tolerance &&
               Mathf.Abs(a.b - b.b) < tolerance;
    }

    // Создает знак плюс между цветами для наглядности
    void CreatePlusSign()
    {
        GameObject plus = new GameObject("PlusSign", typeof(RectTransform), typeof(TextMeshProUGUI));
        plus.transform.SetParent(hintImage.transform, false);

        var text = plus.GetComponent<TextMeshProUGUI>();
        text.text = "+";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        RectTransform rt = plus.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(30, 30);
    }

    // Вычисляет "расстояние" между двумя цветами в RGB пространстве
    float ColorDistance(Color c1, Color c2)
    {
        return Mathf.Sqrt(
            Mathf.Pow(c1.r - c2.r, 2) +
            Mathf.Pow(c1.g - c2.g, 2) +
            Mathf.Pow(c1.b - c2.b, 2)
        );
    }

    // Создание маленького Image на подсказке
    void CreateHintColorImage(Color color, string name, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.tag = "ColorImage";
        go.transform.SetParent(hintImage.transform, false);

        var image = go.GetComponent<Image>();
        image.color = color;

        // Используем спрайты для цветных изображений
        image.sprite = name == "HintColor1" ? colorImage1 : colorImage2;
        image.preserveAspect = true;

        RectTransform rt = go.GetComponent<RectTransform>();

        // Настраиваем привязку к центру родителя
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Устанавливаем позицию
        rt.anchoredPosition = anchoredPos;

        // Устанавливаем размер (настроить под ваш UI)
        rt.sizeDelta = new Vector2(60, 60);

        // Убеждаемся, что изображение видимо
        image.raycastTarget = false;
    }

    void GenerateLevel10()
    {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = "\"Ничем не могу помочь...\"";
        var list = SpawnGrid(1, 2, chestGenericPrefab);
        correctIndex = UnityEngine.Random.Range(0, 2);
    }

}