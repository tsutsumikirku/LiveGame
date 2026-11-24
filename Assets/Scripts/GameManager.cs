using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField]private float gameTime = 180f;
    [SerializeField, InspectorName("好感度の最大値")] private int goalMoney; 
    private List<int> moneyRanking;
    private UIManager uIManager;
    private Response response;
    [SerializeField]private int likeability;
    private int money = 0;
    public int Likeability
    {
        get { return likeability; }
        set
        {
            if (value > maxLikeAbility)
            {
                likeability = maxLikeAbility;
            }
            else if(value < minLikeAbility)
            {
                likeability = minLikeAbility;
            }
            else
            {
                likeability = value;
            }
            Debug.Log($"好感度が{likeability}に変更されました。");
        }
    }
    public int maxLikeAbility;
    public int minLikeAbility;
    private float currentTime;
    private bool isGameRunning = false;

    private void Awake()
    {
        moneyRanking = new List<int>();
        uIManager = FindAnyObjectByType<UIManager>();
        response = FindAnyObjectByType<Response>();
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void GetComment(CommentData commentData)
    {
        Debug.Log($"コメントを受信しました。{commentData.Responce}");
        switch (commentData.emotion)
        {
            case "普通":
                response.Responce(commentData.Responce, 0);
                break;
            case "笑顔":
                response.Responce(commentData.Responce, 1);
                break;
            case "泣き":
                response.Responce(commentData.Responce, 2);
                break;
            case "驚き":
                response.Responce(commentData.Responce, 3);
                break;
            case "困惑":
                response.Responce(commentData.Responce, 4);
                break;
        }
        money += commentData.money;
        if(money >= goalMoney)
        uIManager.BombStop();
        uIManager.UpdateMoney(money);
    }
    [ContextMenu("GameStart")]
    public void GameStart()
    {
        likeability = 0;
        money = 0;
        uIManager.UpdateMoney(money);
        uIManager.Init();
        FindAnyObjectByType<CommentManager>().StartComments();
        isGameRunning = true;
        currentTime = gameTime;
    }
    public async UniTask GameEnd()
    {
        moneyRanking.Add(money);
        moneyRanking.Sort((a, b) => b.CompareTo(a));
        isGameRunning = false;
        FindAnyObjectByType<CommentManager>().EndComments();
        await FindAnyObjectByType<UIManager>().GameEnd(() =>
        {
            Result result = FindAnyObjectByType<Result>();
            result.Init(money, moneyRanking.ToArray());
        });
    }

    private void Update()
    {
        if (isGameRunning)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                GameEnd().Forget();
            }
        }
        uIManager.UpdateTimer(currentTime);
    }
}
