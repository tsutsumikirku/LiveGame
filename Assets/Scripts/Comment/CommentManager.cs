using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CommentManager : MonoBehaviour
{
    private enum StreamState
    {
        Ready,
        Running,
        Ended
    }

    [SerializeField, InspectorName("コメントのプレハブ")]
    private Comment commentPrefab;
    [SerializeField, InspectorName("コメントの生成先"), Tooltip("生成したコメントを配置する Canvas 内の親オブジェクト。")]
    private RectTransform commentParent;
    [SerializeField, InspectorName("追跡対象"), Tooltip("追跡型コメントが向かうキャラクターの RectTransform。")]
    private RectTransform characterPosition;
    [SerializeField, InspectorName("生成位置の目印"), Tooltip("生成位置の基準となる UI オブジェクト。自動生成では X 座標を基準にし、Y 座標の上下にばらつかせます。")]
    private RectTransform spawnPoint;
    [SerializeField, InspectorName("手動生成の予備座標"), Tooltip("目印が未設定の場合に手動生成で使うアンカー基準の座標。右外から生成する場合は高さだけを使います。")]
    private Vector2 spawnPosition = new Vector2(400f, 0f);
    [SerializeField, InspectorName("画面の右外から生成する"), Tooltip("文字全体を生成先の右端より外側に配置します。")]
    private bool spawnOutsideRight = true;
    [SerializeField, InspectorName("右端から外側への距離"), Tooltip("右外から生成するときの、生成先の右端から文字の左端までの最小距離。"), Min(0f)]
    private float outsideRightOffset = 8f;
    [SerializeField, InspectorName("コメントの移動速度"), Tooltip("1 秒あたりに移動する UI 座標の距離。"), Min(0f)]
    private float commentSpeed = 5f;
    [SerializeField, InspectorName("消滅演出の時間（秒）"), Min(0f)]
    private float deleteTime = 0.25f;

    [SerializeField, InspectorName("シーン開始時にコメントを流す"), Tooltip("オフにすると StartComments を呼ぶまで自動生成を待ちます。")]
    private bool autoSpawn = true;
    [SerializeField, InspectorName("現在の好感度"), Tooltip("出現するコメントの抽選に使う値。反応したコメントの好感度ポイントが加算されます。")]
    private int currentPopularity;
    [SerializeField, InspectorName("序盤コメントを流す時間（秒）"), Tooltip("最初の Common コメントを生成してから EarlyStage を追加する時間。0 で序盤コメントを無効にします。"), Min(0f)]
    private float earlyStageDuration = 20f;
    [SerializeField, InspectorName("話題コメントを流す時間（秒）"), Tooltip("話題付きコメントの消滅後、その話題に一致するコメントを追加する時間。0 で無効にします。"), Min(0f)]
    private float topicDuration = 10f;
    [SerializeField, InspectorName("自動生成の最短間隔（秒）"), Min(0.05f)]
    private float minSpawnInterval = 0.35f;
    [SerializeField, InspectorName("自動生成の最長間隔（秒）"), Min(0.05f)]
    private float maxSpawnInterval = 1.2f;
    [SerializeField, InspectorName("目印から上下にずらす最大距離"), Tooltip("自動生成時に目印から上下それぞれにずらせる距離。0 で高さを固定します。"), Min(0f)]
    private float spawnVerticalRange = 300f;
    [SerializeField, InspectorName("画面上下の余白"), Min(0f)]
    private float verticalPadding = 40f;
    [SerializeField, InspectorName("上端の追加余白"), Tooltip("文字が見切れないよう、画面上側だけに加える余白。"), Min(0f)]
    private float extraTopPadding = 24f;
    [SerializeField, InspectorName("画面左右の余白"), Min(0f)]
    private float horizontalPadding;
    [SerializeField, InspectorName("コメント間の最小上下間隔"), Min(1f)]
    private float minVerticalSpacing = 100f;
    [SerializeField, InspectorName("重なりを確認する横方向の距離"), Tooltip("生成候補と既存コメントの横方向の距離がこの値より短い場合、上下の間隔を確認します。"), Min(1f)]
    private float spawnClearanceWidth = 320f;

    private readonly List<Comment> visibleComments = new List<Comment>();
    private readonly List<Comment> spawnedComments = new List<Comment>();
    private StreamState streamState;
    private int? initialPopularity;
    private float nextCommonSpawnTime;
    private float nextEarlyStageSpawnTime;
    private float? earlyStageEndTime;
    private string activeTopic;
    private float topicEndTime;
    private float nextTopicSpawnTime;

    private void Awake()
    {
        initialPopularity = currentPopularity;
    }

    private void Start()
    {
        if (autoSpawn && streamState == StreamState.Ready)
        {
            StartComments();
        }
    }

    /// <summary>
    /// コメントの自動生成を開始します。
    /// </summary>
    public void StartComments()
    {
        if (streamState == StreamState.Running)
        {
            return;
        }

        streamState = StreamState.Running;
        nextCommonSpawnTime = GetNextSpawnTime(Time.time);
    }

    /// <summary>
    /// コメントの生成を止め、生成済みのコメントと配信中の状態をリセットします。
    /// </summary>
    public void EndComments()
    {
        streamState = StreamState.Ended;
        nextCommonSpawnTime = 0f;
        nextEarlyStageSpawnTime = 0f;
        earlyStageEndTime = null;
        activeTopic = null;
        topicEndTime = 0f;
        nextTopicSpawnTime = 0f;
        currentPopularity = initialPopularity ?? currentPopularity;

        foreach (Comment comment in spawnedComments)
        {
            if (comment == null)
            {
                continue;
            }

            // 消滅演出を完了させずに止め、終了時にスコア処理が走らないようにする。
            comment.transform.DOKill();
            comment.gameObject.SetActive(false);
            Destroy(comment.gameObject);
        }

        spawnedComments.Clear();
        visibleComments.Clear();
    }

    private void Update()
    {
        if (streamState != StreamState.Running)
        {
            return;
        }

        float now = Time.time;
        if (now >= nextCommonSpawnTime)
        {
            nextCommonSpawnTime = GetNextSpawnTime(now);
            Comment common = SpawnComment("Common", null, automatic: true);
            if (common != null && !earlyStageEndTime.HasValue)
            {
                // スプレッドシートの取得を待ち、最初のコメントが出てから序盤の時間を数える。
                earlyStageEndTime = now + Mathf.Max(0f, earlyStageDuration);
                nextEarlyStageSpawnTime = now;
            }
        }

        if (earlyStageEndTime.HasValue && now < earlyStageEndTime.Value && now >= nextEarlyStageSpawnTime)
        {
            nextEarlyStageSpawnTime = GetNextSpawnTime(now);
            SpawnComment("EarlyStage", null, automatic: true);
        }

        if (activeTopic != null && now < topicEndTime && now >= nextTopicSpawnTime)
        {
            nextTopicSpawnTime = GetNextSpawnTime(now);
            SpawnComment(activeTopic, null, automatic: true);
        }
    }

    private void HandleCommentVanish(CommentData data)
    {
        currentPopularity += data.Popularity;

        CSVInpoerter importer = CSVInpoerter.Instance;
        if (data.reactionId > 0 && importer != null)
        {
            foreach (CommentData reaction in importer.GetCommentID(data.reactionId))
            {
                SpawnComment(reaction, null, automatic: true, allowOverlapWhenFull: true);
            }
        }

        if (topicDuration <= 0f || string.IsNullOrWhiteSpace(data.topic))
        {
            return;
        }

        string topic = data.topic.Trim();
        float now = Time.time;
        if (now >= topicEndTime || !string.Equals(activeTopic, topic, StringComparison.Ordinal))
        {
            nextTopicSpawnTime = now;
        }

        activeTopic = topic;
        topicEndTime = now + topicDuration;
    }

    private bool TryGetSpawnPosition(Comment comment, bool automatic, bool allowOverlapWhenFull, out Vector2 position)
    {
        Rect area = commentParent.rect;
        float xPadding = Mathf.Clamp(horizontalPadding, 0f, area.width * 0.25f);
        float bottomPadding = Mathf.Clamp(verticalPadding, 0f, area.height * 0.25f);
        float topPadding = Mathf.Min(bottomPadding + Mathf.Max(0f, extraTopPadding), area.height * 0.25f);
        Vector2 availableSize = new Vector2(area.width - 2f * xPadding, area.height - topPadding - bottomPadding);
        position = default;
        if (availableSize.x <= 0f || availableSize.y <= 0f)
        {
            return false;
        }

        Bounds textBounds = GetTextBounds(comment);
        float fit = Mathf.Min(1f, availableSize.x / Mathf.Max(1f, textBounds.size.x));
        fit = Mathf.Min(fit, availableSize.y / Mathf.Max(1f, textBounds.size.y));
        comment.transform.localScale *= fit;

        // 文字の表示範囲が生成先の内側に収まる座標を求める。
        float minX = area.xMin + xPadding - textBounds.min.x * fit;
        float maxX = Mathf.Max(minX, area.xMax - xPadding - textBounds.max.x * fit);
        float minY = area.yMin + bottomPadding - textBounds.min.y * fit;
        float maxY = Mathf.Max(minY, area.yMax - topPadding - textBounds.max.y * fit);
        bool hasSpawnPoint = spawnPoint != null;
        Vector3 referencePosition = hasSpawnPoint
            ? commentParent.InverseTransformPoint(spawnPoint.position)
            : comment.transform.localPosition;
        float x;
        if (spawnOutsideRight)
        {
            float outsideX = area.xMax + Mathf.Max(0f, outsideRightOffset) - textBounds.min.x * fit;
            x = hasSpawnPoint ? Mathf.Max(outsideX, referencePosition.x) : outsideX;
        }
        else
        {
            x = hasSpawnPoint ? Mathf.Clamp(referencePosition.x, minX, maxX) : maxX;
        }

        if (!automatic)
        {
            position = new Vector2(x, Mathf.Clamp(referencePosition.y, minY, maxY));
            return true;
        }

        if (hasSpawnPoint)
        {
            float range = Mathf.Max(0f, spawnVerticalRange);
            minY = Mathf.Clamp(referencePosition.y - range, minY, maxY);
            maxY = Mathf.Clamp(referencePosition.y + range, minY, maxY);
        }

        float spacing = Mathf.Max(minVerticalSpacing, textBounds.size.y * fit, 1f);
        int laneCount = Mathf.FloorToInt((maxY - minY) / spacing) + 1;
        int firstLane = UnityEngine.Random.Range(0, laneCount);

        commentParent.GetComponentsInChildren(false, visibleComments);
        for (int offset = 0; offset < laneCount; offset++)
        {
            int lane = (firstLane + offset) % laneCount;
            float y = laneCount == 1
                ? (minY + maxY) * 0.5f
                : Mathf.Lerp(minY, maxY, (float)lane / (laneCount - 1));

            if (!IsSpawnPositionOccupied(comment, x, y, spacing) ||
                (allowOverlapWhenFull && offset == laneCount - 1))
            {
                position = new Vector2(x, y);
                return true;
            }
        }

        return false;
    }

    private Bounds GetTextBounds(Comment comment)
    {
        TMP_Text label = comment.GetComponentInChildren<TMP_Text>();
        label.ForceMeshUpdate();
        Bounds bounds = label.textBounds;
        Matrix4x4 textToParent = commentParent.worldToLocalMatrix * label.transform.localToWorldMatrix;
        Vector3 center = textToParent.MultiplyPoint3x4(bounds.center) - comment.transform.localPosition;
        Vector3 horizontal = textToParent.MultiplyVector(Vector3.right * bounds.extents.x);
        Vector3 vertical = textToParent.MultiplyVector(Vector3.up * bounds.extents.y);
        Vector3 size = 2f * new Vector3(
            Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x),
            Mathf.Abs(horizontal.y) + Mathf.Abs(vertical.y),
            0f);
        return new Bounds(center, size);
    }

    private bool IsSpawnPositionOccupied(Comment spawningComment, float x, float y, float spacing)
    {
        foreach (Comment comment in visibleComments)
        {
            if (comment == spawningComment)
            {
                continue;
            }

            Vector3 position = commentParent.InverseTransformPoint(comment.transform.position);
            if (Mathf.Abs(position.x - x) < spawnClearanceWidth && Mathf.Abs(position.y - y) < spacing)
            {
                return true;
            }
        }

        return false;
    }

    private float GetNextSpawnTime(float now)
    {
        float minimum = Mathf.Max(0.05f, minSpawnInterval);
        float maximum = Mathf.Max(minimum, maxSpawnInterval);
        return now + UnityEngine.Random.Range(minimum, maximum);
    }

    /// <summary>
    /// 指定された種類のコメントを現在の好感度で抽選して1件生成します。
    /// 出現できるコメントがない場合は null を返します。
    /// </summary>
    public Comment SpawnComment(string commentType, Action<CommentData> onVanish = null)
    {
        return SpawnComment(commentType, onVanish, automatic: false);
    }

    private Comment SpawnComment(string commentType, Action<CommentData> onVanish, bool automatic)
    {
        if (streamState == StreamState.Ended)
        {
            return null;
        }

        CSVInpoerter importer = CSVInpoerter.Instance;
        if (importer == null)
        {
            Debug.LogError("シーンにCSVInpoerterを配置してください。", this);
            return null;
        }

        CommentData data = importer.GetCommentType(commentType, currentPopularity);
        return SpawnComment(data, onVanish, automatic);
    }

    private Comment SpawnComment(CommentData data, Action<CommentData> onVanish, bool automatic,
        bool allowOverlapWhenFull = false)
    {
        if (data == null)
        {
            return null;
        }

        if (commentPrefab == null || commentParent == null ||
            (data.Damage > 0 && characterPosition == null))
        {
            Debug.LogError("CommentManagerのPrefab、生成先、追跡先の設定を確認してください。", this);
            return null;
        }

        Comment comment = Instantiate(commentPrefab, commentParent, false);
        Action<CommentData> callback = HandleCommentVanish;
        callback += onVanish;
        comment.Init(data, commentSpeed, deleteTime, characterPosition, callback);

        RectTransform commentRect = (RectTransform)comment.transform;
        if (!automatic && spawnPoint == null)
        {
            commentRect.anchoredPosition = spawnPosition;
        }

        if (automatic || spawnPoint != null || spawnOutsideRight)
        {
            if (!TryGetSpawnPosition(comment, automatic, allowOverlapWhenFull, out Vector2 position))
            {
                Destroy(comment.gameObject);
                return null;
            }

            commentRect.localPosition = new Vector3(position.x, position.y, commentRect.localPosition.z);
        }
        spawnedComments.RemoveAll(previous => previous == null);
        spawnedComments.Add(comment);
        return comment;
    }
}
