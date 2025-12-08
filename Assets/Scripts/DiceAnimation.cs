using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Daifugo
{
    public class DiceAnimation : MonoBehaviour
    {
        private Text diceText;
        private int finalValue;
        private bool isRolling;

        public void Setup(Transform parent, Vector2 position)
        {
            transform.SetParent(parent);
            RectTransform rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(120, 120); // Slightly larger

            // Simple visual representation: A panel with text
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.9f); // White, slight transparent? No, solid.
            bg.raycastTarget = false;

            GameObject textObj = new GameObject("Value");
            textObj.transform.SetParent(transform);
            diceText = textObj.AddComponent<Text>();
            
            // Safe Font Loading (Inline for now or use helper if accessible)
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) {
                var all = Resources.FindObjectsOfTypeAll<Font>();
                if (all.Length > 0) f = all[0];
            }
            diceText.font = f;
            
            diceText.alignment = TextAnchor.MiddleCenter;
            diceText.color = Color.black;
            diceText.fontSize = 80; // Large for dice face
            diceText.resizeTextForBestFit = false; // Fixed large size
            diceText.raycastTarget = false;
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        public void Roll(int result)
        {
            finalValue = result;
            isRolling = true;
            StartCoroutine(RollRoutine());
        }

        IEnumerator RollRoutine()
        {
            float duration = 1.0f;
            float elapsed = 0f;
            float interval = 0.05f;

            while (elapsed < duration)
            {
                int randomVal = Random.Range(1, 7);
                diceText.text = GetDiceFace(randomVal);
                yield return new WaitForSeconds(interval);
                elapsed += interval;
                // Slow down
                interval *= 1.1f;
            }

            diceText.text = GetDiceFace(finalValue);
            isRolling = false;
        }

        string GetDiceFace(int val)
        {
            // Fallback to numbers if unicode symbols are missing in font
            // Many default Unity fonts (Arial) might not have ⚀..⚅
            // Let's use numbers for safety as requested "Make it visible"
            return val.ToString();
        }
    }
}
