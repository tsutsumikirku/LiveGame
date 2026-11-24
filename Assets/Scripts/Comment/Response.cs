
using UnityEngine;
using Cysharp.Threading.Tasks;
using TMPro;
using DG.Tweening;
using System.Threading;
using UnityEngine.UI;

public class Response : MonoBehaviour
{
    [SerializeField]
    private float sizeTime;
    [SerializeField, InspectorName("1文字ごとの表示間隔（秒）"), Min(0f)]
    private float responseTime = 0.5f;
    [SerializeField, InspectorName("応答のテキスト")] TextMeshProUGUI responseText;

    [SerializeField] Image CharacterImage;
    [SerializeField] Sprite[] CharacterSprites;
    [SerializeField, InspectorName("キャラクター揺れの時間（秒）"), Min(0f)]
    private float characterPunchTime = 0.35f;
    [SerializeField, InspectorName("キャラクター揺れの強さ")]
    private Vector3 characterPunchScale = new Vector3(0.25f, 0.25f, 0f);
    [SerializeField, InspectorName("キャラクター揺れの回数"), Range(1, 20)]
    private int characterPunchVibrato = 8;
    [SerializeField, InspectorName("キャラクター揺れの弾力"), Range(0f, 1f)]
    private float characterPunchElasticity = 0.8f;
    Vector2 defaultSize;
    Vector3 characterDefaultScale;
    CancellationTokenSource cancellationTokenSource;
    Tween tween;
    private void Start()
    {
        defaultSize = ((RectTransform)transform).localScale;
        ((RectTransform)transform).localScale = Vector2.zero;
        characterDefaultScale = CharacterImage.rectTransform.localScale;
    }

    public void Responce(string text, int id)
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource = new CancellationTokenSource();
        ResponceText(text, cancellationTokenSource.Token).Forget();
        CharacterImage.sprite = CharacterSprites[id];
        CharacterImage.rectTransform.DOKill();
        CharacterImage.rectTransform.localScale = characterDefaultScale;
        CharacterImage.rectTransform.DOPunchScale(
            characterPunchScale,
            characterPunchTime,
            characterPunchVibrato,
            characterPunchElasticity
        );
    }

    private async UniTask ResponceText(string text, CancellationToken ct)
    {
        ((RectTransform)transform).localScale = Vector2.zero;
        tween?.Kill();
        tween = ((RectTransform)transform).DOScale(defaultSize,sizeTime);
        responseText.text = "";
        foreach (char c in text)
        {
            responseText.text += c;
            await UniTask.WaitForSeconds(responseTime, cancellationToken: ct);
        }
    }
}
