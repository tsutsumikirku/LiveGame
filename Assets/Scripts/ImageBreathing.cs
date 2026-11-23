using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ImageBreathing : MonoBehaviour
{
    [SerializeField, InspectorName("高さの増加率（%）"), Min(0f)]
    private float heightIncreasePercent = 4f;

    [SerializeField, InspectorName("呼吸の周期（秒）"), Min(0.1f)]
    private float cycleDuration = 3f;

    private RectTransform imageRect;
    private float restingSizeDeltaY;
    private float elapsedTime;

    private void OnEnable()
    {
        imageRect = GetComponent<RectTransform>();
        restingSizeDeltaY = imageRect.sizeDelta.y;
        elapsedTime = 0f;
    }

    private void LateUpdate()
    {
        float duration = Mathf.Max(0.1f, cycleDuration);
        elapsedTime = Mathf.Repeat(elapsedTime + Time.deltaTime, duration);
        float phase = elapsedTime / duration * (2f * Mathf.PI);
        float expansion = (1f - Mathf.Cos(phase)) * 0.5f;

        Vector2 sizeDelta = imageRect.sizeDelta;
        // アンカーが上下に伸びる設定でも、現在の親サイズを反映した元の高さを基準にする。
        float restingHeight = Mathf.Max(0f, imageRect.rect.height - (sizeDelta.y - restingSizeDeltaY));
        float heightOffset = restingHeight * Mathf.Max(0f, heightIncreasePercent) * 0.01f * expansion;
        sizeDelta.y = restingSizeDeltaY + heightOffset;
        imageRect.sizeDelta = sizeDelta;
    }

    private void OnDisable()
    {
        Vector2 sizeDelta = imageRect.sizeDelta;
        sizeDelta.y = restingSizeDeltaY;
        imageRect.sizeDelta = sizeDelta;
    }
}
