using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// コメントのクラス　コメントの移動と表示を行うクラス
/// </summary>
public class Comment : MonoBehaviour
{
    TextMeshPro Text;
    // コメントの速度
    float commentSpeed;

    // コメントの状態 none=何もなし代入されていないときはこれに　flowing=コメントが流れる
    // tracking=コメントがキャラクターを追跡する drag=コメントをつかんでいる状態
    CommentState commentState;

    // コメントのデータ初期化時に渡される
    CommentData commentData;

    //コメントのステート変更時に一時保存料域として活用する変数
    CommentState cashState;

    // コメントのインスタンスを作成するクラスにて呼び出す初期化処理　見た目などの変更を行う
    void Init(CommentData initCommentData, float initCommentSpeed)
    {
        commentState = CommentState.flowing;
        commentData = initCommentData;
        commentSpeed = initCommentSpeed;
        Text.text = commentData.Comment;
    }
    void Update()
    {
        // コメントがflowingの時は右に向かっていく
        if(commentState == CommentState.flowing)
        ((RectTransform)transform).anchoredPosition += Vector2.left * commentSpeed;
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
    drag
}