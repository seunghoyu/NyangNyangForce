using UnityEngine;

/// <summary>
/// 대시 중 잔상 효과를 담당하는 컴포넌트.
/// PlayerAnimation의 Awake에서 자동으로 AddComponent 됨.
/// </summary>
public sealed class DashAfterimage : MonoBehaviour
{
    [SerializeField] private int poolSize = 10;
    [SerializeField] private float spawnInterval = 0.025f;  // 잔상 생성 간격 (초)
    [SerializeField] private float fadeTime = 0.14f;        // 잔상이 완전히 사라지는 시간
    [SerializeField] private float startAlpha = 0.52f;      // 잔상 초기 투명도

    private SpriteRenderer source;
    private bool dashing;
    private float spawnTimer;

    private struct Ghost
    {
        public GameObject go;
        public SpriteRenderer sr;
        public float remaining;
        public float total;
    }

    private Ghost[] pool;
    private int poolHead;

    public void SetRenderLayer(int layer)
    {
        if (pool == null) return;
        foreach (Ghost ghost in pool)
        {
            if (ghost.go == null) continue;
            ghost.go.layer = layer;
            if (ghost.sr == null || source == null) continue;
            ghost.sr.sortingLayerID = source.sortingLayerID;
            ghost.sr.sortingOrder = source.sortingOrder - 1;
        }
    }

    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();
        pool = new Ghost[poolSize];
        Transform parentTr = transform.parent;

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("_DashGhost") { hideFlags = HideFlags.HideInHierarchy };
            go.transform.SetParent(parentTr);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = source.sortingLayerName;
            sr.sortingOrder = source.sortingOrder - 1; // 플레이어 뒤에 렌더링
            go.SetActive(false);
            pool[i] = new Ghost { go = go, sr = sr };
        }
    }

    public void SetDashing(bool value)
    {
        dashing = value;
        if (value)
            spawnTimer = 0f; // 대시 첫 프레임에 즉시 잔상 생성
    }

    // 대시 외에 다른 동작(예: 슬램 낙하)에서도 동일한 잔상 로직을 재사용하기 위한 별칭
    public void SetTrailActive(bool value) => SetDashing(value);

    private void Update()
    {
        if (pool == null) return;

        if (dashing)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnGhost();
                spawnTimer = spawnInterval;
            }
        }

        for (int i = 0; i < pool.Length; i++)
        {
            ref Ghost g = ref pool[i];
            if (g.remaining <= 0f) continue;

            g.remaining -= Time.deltaTime;
            if (g.remaining <= 0f)
            {
                g.go.SetActive(false);
                continue;
            }

            // 선형으로 투명해짐
            float alpha = startAlpha * (g.remaining / g.total);
            g.sr.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private void SpawnGhost()
    {
        ref Ghost g = ref pool[poolHead];
        poolHead = (poolHead + 1) % pool.Length;

        g.go.transform.position = transform.position;
        g.go.transform.localScale = transform.localScale;
        g.sr.sprite = source.sprite;
        g.sr.flipX = source.flipX;
        g.sr.color = new Color(1f, 1f, 1f, startAlpha);
        g.remaining = fadeTime;
        g.total = fadeTime;
        g.go.SetActive(true);
    }

    private void OnDestroy()
    {
        if (pool == null) return;
        foreach (var g in pool)
            if (g.go != null) Destroy(g.go);
    }
}
