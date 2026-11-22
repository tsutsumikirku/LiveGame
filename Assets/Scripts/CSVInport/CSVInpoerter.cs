using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Google スプレッドシートからコメントを読み込む。
/// </summary>
public class CSVInpoerter : MonoBehaviour
{
    public static CSVInpoerter Instance;
    [SerializeField, InspectorName("Google スプレッドシートのリンク")]
    [Tooltip("読み込むタブを開いたときの URL を貼り付けます。ログインなしで閲覧できるシートが必要です。")]
    private string spreadsheetUrl =
        "https://docs.google.com/spreadsheets/d/11XwM_buUEHB-mA4P-Qy7_vUYmXKOPkoiAO4FOGxmMsQ/edit?gid=844895722#gid=844895722";

    private CommentData[] chatData = Array.Empty<CommentData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    IEnumerator Start()
    {
        if (Instance != this)
        {
            yield break;
        }

        if (!TryGetCsvUrl(spreadsheetUrl, out string csvUrl))
        {
            Debug.LogError("Inspector に Google スプレッドシートのリンクを設定してください。", this);
            yield break;
        }

        using var request = UnityWebRequest.Get(csvUrl);
        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"スプレッドシートを取得できませんでした（{request.error}）。リンクと共有設定を確認してください。", this);
            yield break;
        }

        try
        {
            chatData = ParseComments(request.downloadHandler.text, out int skippedRows);
            if (skippedRows > 0)
            {
                Debug.LogWarning($"コメントデータの列や数値が不正な {skippedRows} 行を読み飛ばしました。", this);
            }
        }
        catch (FormatException exception)
        {
            Debug.LogError($"スプレッドシートをコメントデータとして読み込めませんでした。{exception.Message}", this);
        }
    }

    private static bool TryGetCsvUrl(string link, out string csvUrl)
    {
        csvUrl = null;
        if (!Uri.TryCreate(link?.Trim(), UriKind.Absolute, out Uri url) ||
            url.Scheme != Uri.UriSchemeHttps || url.Host != "docs.google.com")
        {
            return false;
        }

        Match sheet = Regex.Match(url.AbsolutePath, @"^/spreadsheets/(?:u/\d+/)?d/([A-Za-z0-9_-]+)(?:/|$)");
        if (!sheet.Success)
        {
            return false;
        }

        Match tab = Regex.Match(url.Query + url.Fragment, @"[?&#]gid=([0-9]+)(?=[&#]|$)");
        csvUrl = $"https://docs.google.com/spreadsheets/d/{sheet.Groups[1].Value}/export?format=csv";
        if (tab.Success)
        {
            csvUrl += $"&gid={tab.Groups[1].Value}";
        }

        return true;
    }

    private static CommentData[] ParseComments(string csv, out int skippedRows)
    {
        var comments = new List<CommentData>();
        bool foundHeader = false;
        int popularityMinColumn = -1;
        int popularityMaxColumn = -1;
        int weightColumn = -1;
        int reactionIdColumn = -1;
        skippedRows = 0;

        foreach (string[] columns in ReadCsvRows(csv.TrimStart('\uFEFF')))
        {
            if (columns.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            if (!foundHeader)
            {
                if (columns.Length < 9 ||
                    !string.Equals(columns[0].Trim(), "CommentType", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(columns[1].Trim(), "id", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("対象タブの先頭行が CommentType、id から始まる9列以上の形式か確認してください。");
                }

                popularityMinColumn = Array.FindIndex(columns, name => name.Trim() == "出現好感度下限");
                popularityMaxColumn = Array.FindIndex(columns, name => name.Trim() == "出現好感度上限");
                weightColumn = Array.FindIndex(columns, name => name.Trim() == "重み");
                reactionIdColumn = Array.FindIndex(columns, name => name.Trim() == "反応条件");
                foundHeader = true;
                continue;
            }

            if (columns.Length < 9 || string.IsNullOrWhiteSpace(columns[0]) ||
                !TryReadNumber(columns[1], out int id) ||
                !TryReadNumber(columns[4], out int damage) ||
                !TryReadNumber(columns[5], out int popularity) ||
                !TryReadNumber(columns[8], out int money) ||
                !TryReadOptionalNumber(columns, popularityMinColumn, int.MinValue, out int popularityMin) ||
                !TryReadOptionalNumber(columns, popularityMaxColumn, int.MaxValue, out int popularityMax) ||
                !TryReadOptionalNumber(columns, weightColumn, 1, out int weight) ||
                !TryReadOptionalNumber(columns, reactionIdColumn, 0, out int reactionId))
            {
                skippedRows++;
                continue;
            }

            comments.Add(new CommentData
            {
                CommentType = columns[0].Trim(),
                id = id,
                Comment = columns[2],
                Responce = columns[3],
                Damage = damage,
                Popularity = popularity,
                topic = columns[6],
                emotion = columns[7],
                money = money,
                PopularityMin = popularityMin,
                PopularityMax = popularityMax,
                Weight = weight,
                reactionId = reactionId
            });
        }

        if (!foundHeader)
        {
            throw new FormatException("対象タブにヘッダーがありません。");
        }

        return comments.ToArray();
    }

    private static bool TryReadNumber(string value, out int number)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
    }

    private static bool TryReadOptionalNumber(string[] columns, int index, int defaultValue, out int number)
    {
        number = defaultValue;
        return index < 0 || index >= columns.Length || string.IsNullOrWhiteSpace(columns[index]) ||
            TryReadNumber(columns[index], out number);
    }

    private static IEnumerable<string[]> ReadCsvRows(string csv)
    {
        using var reader = new StringReader(csv);
        var columns = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;

        int next;
        while ((next = reader.Read()) != -1)
        {
            char character = (char)next;
            if (character == '"')
            {
                if (quoted && reader.Peek() == '"')
                {
                    reader.Read();
                    field.Append('"');
                }
                else if (quoted || field.Length == 0)
                {
                    quoted = !quoted;
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (!quoted && (character == ',' || character == '\r' || character == '\n'))
            {
                columns.Add(field.ToString());
                field.Clear();
                if (character != ',')
                {
                    if (character == '\r' && reader.Peek() == '\n')
                    {
                        reader.Read();
                    }

                    yield return columns.ToArray();
                    columns.Clear();
                }
            }
            else
            {
                field.Append(character);
            }
        }

        if (quoted)
        {
            throw new FormatException("CSV に閉じていない引用符があります。");
        }

        if (columns.Count > 0 || field.Length > 0)
        {
            columns.Add(field.ToString());
            yield return columns.ToArray();
        }
    }

    /// <summary>
    /// 現在の好感度で出現できるコメントを、指定された種類から重み付きで1件選びます。
    /// </summary>
    /// <returns>対象がない場合や読み込み中は null。</returns>
    public CommentData GetCommentType(string commentType, int currentPopularity)
    {
        CommentData selected = null;
        long totalWeight = 0;
        foreach (CommentData data in chatData)
        {
            if (data.CommentType != commentType || data.Weight <= 0 ||
                currentPopularity < data.PopularityMin || currentPopularity > data.PopularityMax)
            {
                continue;
            }

            // 候補を追加するたびに、その重みの割合で選択を置き換える。
            totalWeight += data.Weight;
            if (selected == null || UnityEngine.Random.value * (double)totalWeight < data.Weight)
            {
                selected = data;
            }
        }

        return selected;
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

    // 出現好感度下限
    public int PopularityMin = int.MinValue;

    // 出現好感度上限
    public int PopularityMax = int.MaxValue;

    // 重み
    public int Weight = 1;

    // 反応条件
    public int reactionId;
}


