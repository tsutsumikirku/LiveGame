using TMPro;
using UnityEngine;

public class Result : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentScore;
    [SerializeField] private TextMeshProUGUI[] rankingScore;

    public void Init(int CurrentScore, int[] RankingScore)
    {
        currentScore.text = CurrentScore.ToString();
        for(int i = 0; i < RankingScore.Length; i++)
        {
            rankingScore[i].text = RankingScore[i].ToString();
        }
    }
    // インゲームを再度開始します
    public void RePlay()
    {
        
    }
}
