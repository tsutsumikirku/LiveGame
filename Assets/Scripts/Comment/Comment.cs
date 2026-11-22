using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using Unity.Collections;

/// <summary>
/// コメントのクラス　コメントの移動と表示を行うクラス
/// </summary>
public class Comment : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // スコア計算時に呼び出されるイベント
    public event Action<CommentData> vanishAction;

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

    // コメントのインスタンスを作成するクラスにて呼び出す初期化処理　見た目などの変更を行う
    void Init(CommentData initCommentData, float initCommentSpeed, float initDeleteTime, 
    RectTransform initCharacterPosition, Action<CommentData> action)
    {
        commentState = CommentState.flowing;
        commentData = initCommentData;
        commentSpeed = initCommentSpeed;
        deleteTime = initDeleteTime;
        characterPosition = initCharacterPosition;
        Text.text = commentData.Comment;
        vanishAction += action;

    }
    void Update()
    {
        // コメントがflowingの時は左に向かっていく
        if(commentState == CommentState.flowing)
        {
            ((RectTransform)transform).anchoredPosition += Vector2.left * commentSpeed;
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
                commentSpeed
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
            transform.DOScale(Vector2.zero, deleteTime).OnComplete(() =>
            {
                vanishAction(commentData);
                Destroy(this.gameObject);
            });
        }
    }

    // ドラッグ開始時のインターフェイスメソッド
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 消滅処理中はドラッグできないように
        if(commentState == CommentState.vanish)return;
    }

    // ドラッグ中のインターフェイスメソッド
    public void OnDrag(PointerEventData eventData)
    {
        // 消滅処理中はドラッグできないように
        if(commentState == CommentState.vanish)return;
    }

    // ドラッグ終了時のインターフェイスメソッド
    public void OnEndDrag(PointerEventData eventData)
    {
        // 消滅処理中はドラッグできないように
        if(commentState == CommentState.vanish)return;
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
    vanish
}