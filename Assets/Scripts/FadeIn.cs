using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

public class FadeIn : MonoBehaviour
{
    public static FadeIn Instance;
    [SerializeField] private RectTransform fadeInPanel;
    [SerializeField] private float fadeInDuration = 1f;
    Vector2 originalScale;
    void Start()
    {
        originalScale = fadeInPanel.localScale;
        fadeInPanel.localScale = Vector2.zero;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void FadeInPlay(Action action)
    {
        FadeInCoroutine(action).Forget();
    }
    // フェードインとフェードアウトを行うそれらの間で指定されたアクションを実行する
    public async UniTask FadeInCoroutine(Action action)
    {
        // フェードイン
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            float scale = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            fadeInPanel.localScale = new Vector2(scale, scale);
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
        fadeInPanel.localScale = originalScale;

        // 指定されたアクションを実行
        action?.Invoke();

        // フェードアウト
        elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            float scale = Mathf.Lerp(1f, 0f, elapsedTime / fadeInDuration);
            fadeInPanel.localScale = new Vector2(scale, scale);
            elapsedTime += Time.deltaTime;
            await UniTask.Yield();
        }
        fadeInPanel.localScale = Vector2.zero;
    }
}
