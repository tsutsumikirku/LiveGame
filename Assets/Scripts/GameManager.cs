using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    [SerializeField]private float gameTime = 180f;
    public int likeability = 0;
    private float currentTime;
    private bool isGameRunning = false;

    private void Awake()
    {
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
        FindAnyObjectByType<Response>().Responce(commentData.Responce);
    }
    public void GameStart()
    {
        likeability = 0;
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
    }

}
