using UnityEngine;

public class OddChest : Chest {
    public void SetNormalAppearance() {
        body.transform.localScale = Vector3.one;
        body.color = Color.white;
    }
    public void SetVariantAppearance(int seed) {
        body.transform.localScale = Vector3.one * (1.0f + 0.08f * (seed % 3));
        body.color = UnityEngine.Random.ColorHSV(0f,1f,0.6f,1f,0.6f,1f);
        // maybe add border or icon to distinguish
    }
}