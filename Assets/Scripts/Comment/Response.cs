
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;
using System.Threading;

public class Response : MonoBehaviour
{
    [SerializeField]
    private float sizeTime;
    [SerializeField, InspectorName("1文字ごとの表示間隔（秒）"), Min(0f)]
    private float responseTime = 0.5f;
    [SerializeField, InspectorName("応答のテキスト")] TextMeshProUGUI responseText;
    Vector2 defaultSize;
    CancellationTokenSource cancellationTokenSource;
    Tween tween;
    private void Start()
    {
        defaultSize = ((RectTransform)transform).localScale;
        ((RectTransform)transform).localScale = Vector2.zero;
    }

    public void Responce(string text)
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();
        ResponceText(text, cancellationTokenSource.Token).Forget();
    }

    private async UniTask ResponceText(string text, CancellationToken ct)
    {
        ((RectTransform)transform).localScale = Vector2.zero;
        tween?.Kill();
        tween = ((RectTransform)transform).DOScale(defaultSize,sizeTime);
        foreach (char c in text)
        {
            responseText.text += c;
            await UniTask.WaitForSeconds(responseTime, cancellationToken: ct);
        }
    }
}
