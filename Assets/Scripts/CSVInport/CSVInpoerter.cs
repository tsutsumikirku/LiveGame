using System.Linq;
using UnityEngine;

/// <summary>
/// CSVインポーター
/// </summary>
public class CSVInpoerter : MonoBehaviour
{
    [SerializeField, InspectorName("CSVファイル")] private TextAsset _textAsset;
    CommentData[] chatData;

    void Start()
    {
        string csvData = _textAsset.text;
        string[] rows = csvData.Split('\n');
        chatData = new CommentData[rows.Length];
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            chatData[i].CommentType = columns[0];
            chatData[i].id = int.Parse(columns[1]);
            chatData[i].Comment = columns[2];
            chatData[i].Responce = columns[3];
            chatData[i].Damage = int.Parse(columns[4]);
            chatData[i].Popularity = int.Parse(columns[5]);
            chatData[i].topic = columns[6];
            chatData[i].emotion = columns[7];
            chatData[i].money = int.Parse(columns[8]);
        }
    }

    /// <summary>
    /// 一部のタイプのコメントを取得するためのメソッド
    /// </summary>
    /// <param name="commentType"></param>
    /// <returns></returns>
    public CommentData[] GetCommandType(string commentType)
    {
        return chatData.Where(x => x.CommentType== commentType).ToArray();
    }
}
/// <summary>
/// チャットのデータを格納するためのクラス
/// </summary>
public class CommentData
{
    // コメントの種類
    public string CommentType;

    // コメントの番号
    public int id;
    
    // コメント
    public string Comment;
    
    // 反応
    public string Responce;
    
    // ダメージ
    public int Damage;
    
    // 人気度
    public int Popularity;
    
    // 話題
    public string topic;

    // 感情
    public string emotion;

    //スパチャ額
    public int money;
}


