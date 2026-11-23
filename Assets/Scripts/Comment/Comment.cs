using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// コメントのクラス　コメントの移動と表示を行うクラス
/// </summary>
public class Comment : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // スコア計算時に呼び出されるイベント
    public event Action<CommentData> vanishAction;

    [SerializeField, InspectorName("スパチャのImage")] Image superChatImage;
    [SerializeField, InspectorName("スパチャの値段")] TextMeshProUGUI superChatChatMoney;
    [SerializeField, InspectorName("スパチャのテキスト")] TextMeshProUGUI superChatText;

    [SerializeField, InspectorName("これ以下になったら衝突したことにします")]float hitDistance;
    // テキスト
    [SerializeField, InspectorName("コメントの文章を適用するテキスト")]TextMeshProUGUI Text;
    
    // コメントの速度
    float commentSpeed;

    // コメントが消えるまでの速度
    float deleteTime;

    // コメントの状態 none=何もなし代入されていないときはこれに　flowing=コメントが流れる
    // tracking=コメントがキャラクターを追跡する drag=コメントをつかんでいる状態
    CommentState commentState;

    // コメントのデータ初期化時に渡される
    CommentData commentData;

    // キャラクターの位置
    RectTransform characterPosition;

    //コメントのステート変更時に一時保存料域として活用する変数
    CommentState cashState;
    Tween tween;
    Vector2 dragVelocity;
    bool isInertiaActive;

    [SerializeField, InspectorName("ドラッグ慣性の減衰"), Range(0.80f, 0.99f)]
    float inertiaDamping = 0.92f;

    [SerializeField, InspectorName("慣性停止速度"), Min(0f)]
    float inertiaStopSpeed = 10f;

    // コメントのインスタンスを作成するクラスにて呼び出す初期化処理　見た目などの変更を行う
    public void Init(CommentData initCommentData, float initCommentSpeed, float initDeleteTime, 
    RectTransform initCharacterPosition, Action<CommentData> action)
    {
        if(initCommentData.Popularity < 0)
        {
            Text.color = Color.red;
            commentState = CommentState.tracking;
        }
        else
        {
            Text.color = Color.white;
            commentState = CommentState.flowing;
        }
        if(initCommentData.money > 0)
        {
            Text.gameObject.SetActive(false);
            superChatImage.gameObject.SetActive(true);
            superChatChatMoney.gameObject.SetActive(true);
            superChatText.gameObject.SetActive(true);
            superChatChatMoney.text = initCommentData.money.ToString();
            superChatText.text = initCommentData.Comment;
            if(initCommentData.Popularity < 0)
            {
                superChatText.color = Color.red;
            }
            else
            {
                superChatText.color = Color.white;
            }
        }
        commentData = initCommentData;
        commentSpeed = initCommentSpeed;
        deleteTime = initDeleteTime;
        characterPosition = initCharacterPosition;
        Text.text = commentData.Comment;
        vanishAction += action;

    }
    void Update()
    {
        if(commentState == CommentState.drag)
        {
            if(isInertiaActive)
            {
                RectTransform commentPosition = (RectTransform)transform;
                commentPosition.anchoredPosition += dragVelocity * Time.deltaTime;
                dragVelocity *= Mathf.Pow(inertiaDamping, 60f * Time.deltaTime);

                if (characterPosition != null &&
                    RectTransformOverlaps(commentPosition, characterPosition))
                {
                    dragVelocity = Vector2.zero;
                    isInertiaActive = false;
                    commentState = CommentState.vanish;
                    return;
                }

                if (IsOutsideScreen(commentPosition))
                {
                    dragVelocity = Vector2.zero;
                    isInertiaActive = false;
                    commentState = CommentState.delete;
                    return;
                }

                if (dragVelocity.magnitude <= inertiaStopSpeed)
                {
                    dragVelocity = Vector2.zero;
                    isInertiaActive = false;
                    commentState = cashState;
                }
            }
            return;
        }

        // コメントがflowingの時は左に向かっていく
        if(commentState == CommentState.flowing)
        {
            ((RectTransform)transform).anchoredPosition += Vector2.left * commentSpeed * Time.deltaTime;
            if (((RectTransform)transform).anchoredPosition.x < -Screen.width) Destroy(gameObject);
        }

        // コメントがtrackingの時はキャラクターに向かっていく
        else if(commentState == CommentState.tracking)
        {
            // キャラクターの方向に向かっていく処理
            RectTransform commentPosition = (RectTransform)transform;
            commentPosition.anchoredPosition = Vector2.MoveTowards(
                commentPosition.anchoredPosition,
                characterPosition.anchoredPosition,
                commentSpeed * Time.deltaTime
            );
            // コメントの位置とキャラクターの間隔が衝突間隔より少なくなったら消滅処理に移る
            if ((commentPosition.anchoredPosition - characterPosition.anchoredPosition).magnitude <= hitDistance)
            {
                commentState = CommentState.vanish;
            }
        }
        
        // コメントがvanishの時はサイズを縮小してスコア計算をしてからインスタンスを削除する
        else if(commentState == CommentState.vanish)
        {
            if(tween == null)
            {
                tween = transform.DOScale(Vector2.zero, deleteTime).OnComplete(() =>
                {
                    vanishAction(commentData);
                    GameManager.Instance.GetComment(commentData);
                    Destroy(this.gameObject);
                });
            }
        }
        else if(commentState == CommentState.delete)
        {
            if(tween != null) return;
            tween = transform.DOScale(Vector2.zero, deleteTime).OnComplete(() =>
            {
                Destroy(this.gameObject);
            });
        }
    }

    // ドラッグ開始時のインターフェイスメソッド
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 消滅処理中はドラッグできないように
        if(commentState == CommentState.vanish || commentState == CommentState.delete)return;
        cashState = commentState;
        dragVelocity = Vector2.zero;
        isInertiaActive = false;
        commentState = CommentState.drag;
    }

    // ドラッグ中のインターフェイスメソッド
    public void OnDrag(PointerEventData eventData)
    {
        // 消滅処理中はドラッグできないように
        if(commentState == CommentState.vanish || commentState == CommentState.delete)return;
        // ドラッグ中はマウスの位置にコメントを追従させる
        ((RectTransform)transform).anchoredPosition += eventData.delta;
        dragVelocity = eventData.delta / Mathf.Max(Time.deltaTime, 0.001f);
    }

    // ドラッグ終了時のインターフェイスメソッド
    public void OnEndDrag(PointerEventData eventData)
    {
        // 消滅処理中はドラッグできないように
        if(commentState == CommentState.vanish || commentState == CommentState.delete)return;

        if (characterPosition != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                characterPosition,
                eventData.position,
                eventData.pressEventCamera))
        {
            commentState = CommentState.vanish;
            return;
        }

        RectTransform commentPosition = (RectTransform)transform;
        if (IsOutsideScreen(commentPosition))
        {
            commentState = CommentState.delete;
            return;
        }

        if (dragVelocity.magnitude <= inertiaStopSpeed)
        {
            dragVelocity = Vector2.zero;
            isInertiaActive = false;
            commentState = cashState;
            return;
        }

        isInertiaActive = true;
    }

    private bool IsOutsideScreen(RectTransform rectTransform)
    {
        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera camera = canvas != null ? canvas.worldCamera : null;
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.position);
        return screenPosition.x < 0f ||
               screenPosition.x > Screen.width ||
               screenPosition.y < 0f ||
               screenPosition.y > Screen.height;
    }

    private bool RectTransformOverlaps(RectTransform first, RectTransform second)
    {
        Rect firstRect = GetWorldRect(first);
        Rect secondRect = GetWorldRect(second);
        return firstRect.Overlaps(secondRect);
    }

    private Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

}

/// <summary>
/// コメントのステート管理に活用するEnum
/// </summary>
public enum CommentState
{
    none,
    flowing,
    tracking,
    drag,
    vanish,
    delete
}