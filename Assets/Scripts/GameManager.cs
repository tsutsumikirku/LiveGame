using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField]private float gameTime = 180f;
    [SerializeField, InspectorName("好感度の最大値")]
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
        response.Responce(commentData.Responce);
        money += commentData.money;
        uIManager.UpdateMoney(money);
    }
    [ContextMenu("GameStart")]
    public void GameStart()
    {
        likeability = 0;
        money = 0;
        uIManager.UpdateMoney(money);
        FindAnyObjectByType<CommentManager>().StartComments();
        isGameRunning = true;
        currentTime = gameTime;
    }
    public void GameEnd()
    {
        isGameRunning = false;
        FindAnyObjectByType<CommentManager>().EndComments();
    }
    private void Update()
    {
        if (isGameRunning)
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                GameEnd();
            }
        }
        uIManager.UpdateTimer(currentTime);
    }

}
