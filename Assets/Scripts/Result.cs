using TMPro;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class Result : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentScore;
    [SerializeField] private TextMeshProUGUI[] rankingScore;

    public void Init(int CurrentScore, int[] RankingScore)
    {
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        currentScore.text = CurrentScore.ToString();
        for(int i = 0; i < RankingScore.Length; i++)
        {
            rankingScore[i].text = RankingScore[i].ToString();
        }
    }

    // インゲームを再度開始します

    public void RePlayButton()
    {
        RePlay().Forget();
    }
    private async UniTask RePlay()
    {
        await FadeIn.Instance.FadeInCoroutine(() =>
        {
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
        });
        GameManager.Instance.GameStart();
    }
}
