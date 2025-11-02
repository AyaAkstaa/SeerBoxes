using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using TMPro;
using System.Security.Cryptography;

public class LevelManager : MonoBehaviour {
    [Header("UI")]
    public RectTransform chestParent;
    public Image hintImage;
    public Sprite hintSprite;
    public TextMeshProUGUI hintText;

    [Header("Prefabs")]
    public GameObject chestGenericPrefab;
    public GameObject chestNumberPrefab;
    public GameObject chestImagePrefab;
    public GameObject chestHiddenPrefab;
    public GameObject chestOddPrefab;
    public GameObject chestColorPrefab;

    List<GameObject> spawned = new List<GameObject>();
    int correctIndex = -1;
    int currentLevel = 1;

    // example assets you must assign:
    [Header("Level 2 assets")]
    public Sprite islandHintSprite;
    public Sprite xMarkSprite; // for overlay on island map
    [Header("Level 4 assets")]
    public Sprite[] symmetryHintSprites; // for level 4

    [Header("Level 7 assets")]
    public Sprite[] iqHintSprites; // level 7

    [Header("Level 8 assets")]
    public Sprite oddoneSprite; // template for 8 if needed

    [Header("Level 9 assets")]
    public Sprite twoColorsSprite; // for level 9 - shows two color circles

    class MathTemplate
    {
        public string expr;
        public Func<int, bool> predicate; // проверяет правильность
        public int correctMin, correctMax; // диапазон, в котором берём правильное значение
        public int distractorMin, distractorMax; // диапазон для генерации дистракторов
        public MathTemplate(string e, Func<int, bool> pred, int cmin, int cmax, int dmin, int dmax)
        {
            expr = e; predicate = pred; correctMin = cmin; correctMax = cmax; distractorMin = dmin; distractorMax = dmax;
        }
    }


    List<MathTemplate> GetMathTemplates()
    {
            return new List<MathTemplate> {
            // 1) пример: результат ≤ 6
            new MathTemplate("2 + 2 * 2 - ...", x => x <= 6, 1, 6, 7, 50),

            // 2) "8-3*(6-1)+1..." (многоточие — продолжение недописанного числа, первая цифра 1)
            //    Требование: 8 вариантов (дистракторы) — числа < 2, правильный — число >= 3
            new MathTemplate("8 - 3*(6-1) + 1... ", x => x >= 3, 3, 50, -20, 1),

            // 3) "65*4-20..." (многоточие — продолжение недописанного числа, первые цифры '20')
            //    Требование: 8 вариантов — числа > 60, правильный — число <= 60
            new MathTemplate("65 * 4 - 20...", x => x <= 60, 0, 60, 61, 200),

            // 4) "496:16+..."  (496 / 16 = 31)
            //    Требование: 8 вариантов — числа <= 31, правильный — число > 31
            new MathTemplate("496 : 16 + ...", x => x > 31, 32, 200, -20, 31),

            // 5) "(11-8+2)*0-..." (выражение даёт 0)
            //    Требование: 8 вариантов — положительные числа, правильный — 0 или меньше
            new MathTemplate("(11 - 8 + 2) * 0 - ...", x => x <= 0, -20, 0, 1, 80)
        };
    }


    void Start() {
        StartLevel(1);
    }

    void Clear() {
        foreach (var g in spawned) Destroy(g);
        spawned.Clear();
        correctIndex = -1;
        RectTransform rect = hintImage.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(238,412);
        hintImage.sprite = null; hintImage.color = Color.clear;
        RectTransform TextRect = hintText.GetComponent<RectTransform>();
        TextRect.sizeDelta = new Vector2(4.84f,7.35f);
        hintText.text = "";
        var old = hintImage.transform.Find("HintX");
        if (old) Destroy(old.gameObject);
    }

    public void StartLevel(int lvl) {
        Clear();
        currentLevel = lvl;
        switch (lvl) {
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
    }

    // helper: spawn grid rows x cols with given prefab, returns list
    List<GameObject> SpawnGrid(int rows, int cols, GameObject prefab) {
        List<GameObject> list = new List<GameObject>();
        float spacingX = 2.3f, spacingY = 1.8f;
        Vector2 origin = new Vector2(-(cols - 1) * spacingX / 2f, (rows - 1) * spacingY / 2f);
        int idx = 0;

        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                Vector2 pos = origin + new Vector2(c * spacingX, -r * spacingY);

                // Instantiate and set parent without preserving world position
                var go = Instantiate(prefab);
                go.transform.SetParent(chestParent, false); // chestParent — RectTransform

                // Если prefab — UI элемент (имеет RectTransform) — используем anchoredPosition
                var rect = go.GetComponent<RectTransform>();
                if (rect != null) {
                    rect.anchoredPosition = pos;
                } else {
                    // Иначе — обычный Transform (world object). Устанавливаем локальную позицию.
                    // Применяем ту же систему координат: localPosition = (pos.x, pos.y, 0)
                    go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                }

                // Инициализация Chest (если компонент есть)
                var chest = go.GetComponent<Chest>();
                if (chest != null) {
                    chest.Init(idx, OnChestClicked);
                } else {
                    Debug.LogWarning($"Spawned prefab '{prefab.name}' does not contain Chest component.");
                }

                list.Add(go);
                idx++;
            }
        }

        spawned = list;
        return list;
    }


    void OnChestClicked(int idx)
    {
        Debug.Log("Clicked: " + idx);
        if (idx == correctIndex)
        {
            hintText.text = "Correct!";
            // GO NEXT after small delay
            NextLevel();
        }
        else
        {
            Lose();
        }
    }
    
    void Lose() {
        hintText.text = "Wrong! Restarting level.";

        StartLevel(1);
    }

    public void NextLevel() {
        StartLevel(Mathf.Min(currentLevel + 1, 10));
    }

    // ====== LEVEL IMPLEMENTATIONS ======

    // +1) Left/Right training: 2 identical chests
    void GenerateLevel1() {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        correctIndex = UnityEngine.Random.Range(0, 2);
        if (correctIndex == 0)
            hintText.text = "Сокровище слева";
        else
            hintText.text = "Сокровище справа";
        var list = SpawnGrid(1,2,chestGenericPrefab);
        
    }

    // 2) Island map: 5x5 grid, island image divided into 25 sectors, X at random sector.
    void GenerateLevel2() {
        hintImage.sprite = islandHintSprite;
        RectTransform rect = hintImage.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300,300);
        hintImage.color = Color.white;
        // spawn 5x5 same chest prefab
        var list = SpawnGrid(5,5,chestGenericPrefab);
        // choose a random sector 0..24
        int sel = UnityEngine.Random.Range(0,25);
        correctIndex = sel;
        // draw an overlay 'X' on hintImage programmatically: we can set hintImage UV or place a child X sprite at corresponding sector
        // simplest: place small X GameObject as child of hintImage with anchored position
        PlaceXOnHint(sel,5,5);
    }

    void PlaceXOnHint(int index, int cols, int rows) {
        // remove old Xs
        var old = hintImage.transform.Find("HintX");
        if (old) Destroy(old.gameObject);
        var go = new GameObject("HintX", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(hintImage.transform, false);
        var img = go.GetComponent<Image>();
        // Use configured xMarkSprite when available, otherwise fall back to a colored placeholder
        if (xMarkSprite != null) {
            img.sprite = xMarkSprite;
            img.color = Color.white;
            img.preserveAspect = true;
            img.raycastTarget = false;
        } else {
            img.color = new Color(1,0,0,0.7f);
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        // compute cell anchored pos
        int col = index % cols;
        int row = index / cols;
        float px = (col + 0.5f) / cols - 0.5f; // normalized centered
        float py = 0.5f - (row + 0.5f) / rows;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = new Vector2(px * hintImage.rectTransform.rect.width, py * hintImage.rectTransform.rect.height);
        // size: use sprite native size if available, otherwise fallback to small square
        if (xMarkSprite != null) {
            // sprite.rect is in pixels — clamp to a reasonable UI size so it doesn't dominate
            Vector2 native = new Vector2(xMarkSprite.rect.width, xMarkSprite.rect.height);
            // limit larger dimension to 64 px
            float maxDim = 64f;
            float scale = 1f;
            if (native.x > native.y && native.x > maxDim) scale = maxDim / native.x;
            else if (native.y > native.x && native.y > maxDim) scale = maxDim / native.y;
            else if (native.x > maxDim && native.y > maxDim) scale = maxDim / Mathf.Max(native.x, native.y);
            rt.sizeDelta = native * scale;
        } else {
            rt.sizeDelta = new Vector2(24,24);
        }
    }

    // 3) Math: 3x3 numbered chests; hint partial expression implies result <=6 => choose chest with number <=6 (only one such)
    void GenerateLevel3()
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


    // 4) Symmetry puzzle: 6 chests with pictures; hint is one of 3 random symmetry puzzles; chests arranged randomly
    void GenerateLevel4() {
        hintText.text = "Which picture matches symmetry clue?";
        // choose one hint sprite at random
        var spr = symmetryHintSprites[UnityEngine.Random.Range(0, symmetryHintSprites.Length)];
        hintImage.sprite = spr; hintImage.color = Color.white;
        // 6 chests, use ImageChest prefab
        var list = SpawnGrid(2,3,chestImagePrefab); // 2 rows, 3 cols
        // prepare 6 sprites: exactly one chest is symmetric according to hint; others are distractors
        // For prototype: assign one correct sprite (choose index) and fill others with random sprites
        int correctLocal = UnityEngine.Random.Range(0,6);
        for (int i=0;i<list.Count;i++) {
            var ic = list[i].GetComponent<ImageChest>();
            if (i == correctLocal) ic.SetContent( GetSymmetricSpriteForHint(spr) );
            else ic.SetContent( GetRandomDistractorSprite() );
        }
        correctIndex = correctLocal;
    }

    // helper stubs (implement asset selection)
    Sprite GetSymmetricSpriteForHint(Sprite hint) { return hint; /* or map hint->correct sprite */ }
    Sprite GetRandomDistractorSprite() { return symmetryHintSprites[UnityEngine.Random.Range(0, symmetryHintSprites.Length)]; }

    // 5) Arrow path: 6x6 grid (36). hint is textual arrows from top-left to target (precompute path)
    void GenerateLevel5() {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = "Follow the arrows from top-left.";
        var list = SpawnGrid(6,6,chestGenericPrefab);
        int cols = 6;
        // choose random target cell (not 0)
        int target = UnityEngine.Random.Range(1, 36);
        correctIndex = target;
        // compute simple shortest path from 0 to target (grid)
        var path = ComputePathIndices(0, target, 6);
        // build arrows string or draw arrow sprites in hintImage / hintText
        hintText.text = "Path: " + PathToArrowString(path, 6);
    }

    // returns list of grid indices from start to target (simple dx/dy greedy)
    List<int> ComputePathIndices(int start, int target, int cols) {
        List<int> p = new List<int>();
        int sx = start % cols, sy = start / cols;
        int tx = target % cols, ty = target / cols;
        int x = sx, y = sy;
        p.Add(start);
        while (x != tx || y != ty) {
            if (x < tx) x++;
            else if (x > tx) x--;
            else if (y < ty) y++;
            else if (y > ty) y--;
            p.Add(y * cols + x);
        }
        return p;
    }
    string PathToArrowString(List<int> path, int cols) {
        string s = "";
        for (int i = 0; i < path.Count-1; i++) {
            int a = path[i], b = path[i+1];
            int ax = a % cols, ay = a / cols;
            int bx = b % cols, by = b / cols;
            if (bx > ax) s += "→";
            else if (bx < ax) s += "←";
            else if (by > ay) s += "↓";
            else s += "↑";
        }
        return s;
    }

    // 6) Two shown not correct; there is 1 hidden chest somewhere barely visible
    void GenerateLevel6() {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = "Both shown are NOT the treasure. There's a faint one somewhere else.";
        // show two big chests (2 visible) and place hidden chest random (outside grid)
        // but user said "both chests and 1 hidden" — implement: spawn 2 large chests and spawn one hidden at random pos
        var list = SpawnGrid(1,2,chestGenericPrefab); // two visible
        // choose indices 0.. in spawned
        int not1 = 0, not2 = 1;
        // spawn hidden somewhere in chestParent bounds, ensuring not overlapping existing chests
        Vector2 hiddenPos = FindRandomFreeSpot();
        var hiddenGo = Instantiate(chestHiddenPrefab, chestParent);
        hiddenGo.GetComponent<RectTransform>().anchoredPosition = hiddenPos;
        var hiddenChest = hiddenGo.GetComponent<Chest>();
        hiddenChest.Init(2, OnChestClicked); // index 2
        spawned.Add(hiddenGo);
        // correct chest is hidden one
        correctIndex = 2;
    }

    Vector2 FindRandomFreeSpot() {
        // pick random anchored position inside parent but not inside visible chests rects
        Rect r = chestParent.rect;
        for (int attempts = 0; attempts < 50; attempts++) {
            float x = UnityEngine.Random.Range(r.xMin + 20, r.xMax - 20);
            float y = UnityEngine.Random.Range(r.yMin + 20, r.yMax - 20);
            Vector2 cand = new Vector2(x,y);
            bool ok = true;
            foreach (var g in spawned) {
                var rt = g.GetComponent<RectTransform>();
                if (Vector2.Distance(rt.anchoredPosition, cand) < 90f) { ok = false; break; }
            }
            if (ok) return cand;
        }
        return Vector2.zero;
    }

    // 7) IQ test: like level 4, 6 image chests with one correct according to hint
    void GenerateLevel7() {
        hintText.text = "Select the missing element (IQ style).";
        hintImage.sprite = iqHintSprites[UnityEngine.Random.Range(0, iqHintSprites.Length)];
        hintImage.color = Color.white;
        var list = SpawnGrid(2,3,chestImagePrefab);
        int correctLocal = UnityEngine.Random.Range(0,6);
        for (int i=0;i<list.Count;i++) {
            var ic = list[i].GetComponent<ImageChest>();
            if (i == correctLocal) ic.SetContent( GetIQCorrectSprite() );
            else ic.SetContent( GetIQDistractorSprite() );
        }
        correctIndex = correctLocal;
    }

    Sprite GetIQCorrectSprite() { return iqHintSprites[0]; }
    Sprite GetIQDistractorSprite() { return iqHintSprites[ UnityEngine.Random.Range(0, iqHintSprites.Length) ]; }

    // 8) Odd-one-out (5 chests). Use visual differences (size, border, shape). Answer is the one that is NOT highlighted.
    void GenerateLevel8() {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = "Which one is special? (the trick: the correct one is the one not standing out)";
        // spawn 1x5
        var list = SpawnGrid(1,5,chestOddPrefab);
        // create one "normal" and 4 "variations" or vice versa. Per user's instruction: correct is that which is nothing special (i.e., non-distinct)
        // Approach: make 4 chests have an obvious difference; 1 chest is plain -> that plain one is correct
        int plainIndex = UnityEngine.Random.Range(0,5);
        for (int i=0;i<list.Count;i++) {
            var odd = list[i].GetComponent<OddChest>();
            if (i == plainIndex) odd.SetNormalAppearance();
            else odd.SetVariantAppearance(i); // different patterns
        }
        correctIndex = plainIndex;
    }

    // 9) Color-XOR / color-mix: 6 colored chests, hint shows two colors; answer is chest whose color equals computed mix
    void GenerateLevel9() {
        hintText.text = "Which color is the product of these two?";
        hintImage.sprite = twoColorsSprite; hintImage.color = Color.white;
        var list = SpawnGrid(2,3,chestColorPrefab);
        // define two base colors (random)
        Color A = Color.red;
        Color B = Color.blue;
        // compute target color as XOR or average — choose XOR-like but for visual simplicity do average or complementary
        Color target = new Color( (A.r + B.r)/2f, (A.g + B.g)/2f, (A.b + B.b)/2f );
        // ensure exactly one chest approximates target; fill others with random colors
        int match = UnityEngine.Random.Range(0, list.Count);
        for (int i=0;i<list.Count;i++) {
            var cc = list[i].GetComponent<ColorChest>();
            if (i == match) cc.SetColor(target);
            else cc.SetColor(UnityEngine.Random.ColorHSV(0f,1f,0.35f,1f,0.35f,1f));
        }
        correctIndex = match;
    }

    // 10) No hint: two chests only with jokey text
    void GenerateLevel10() {
        hintImage.sprite = hintSprite;
        hintImage.color = Color.white;
        hintText.text = "\"Thought it'd be easy? Now real clairvoyance time.\"";
        var list = SpawnGrid(1,2,chestGenericPrefab);
        correctIndex = UnityEngine.Random.Range(0,2);
    }

}
