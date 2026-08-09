using System.Collections;
using UnityEngine;

public sealed class TutorialScarecrow : MonoBehaviour
{
    private static readonly Color HitFlashColor = new Color(1f, 0.45f, 0.45f);
    private const float HitFlashDuration = 0.16f;
    private const float HitFlashInterval = 0.035f;
    private const float HitBurstDuration = 0.18f;

    public Sprite hitBurstSprite;
    private SpriteRenderer spriteRenderer;
    private Stage1HitShake hitShake;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hitShake = GetComponent<Stage1HitShake>();
        if (hitShake == null) hitShake = gameObject.AddComponent<Stage1HitShake>();
        if (hitBurstSprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>("Tutorial/player_basic_bullet_burst");
            if (texture != null)
                hitBurstSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 30f);
        }
    }

    public void Hit(Vector2 hitPosition)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());
        if (hitShake != null) hitShake.Play();
        SpawnHitBurst(hitPosition);
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        float elapsed = 0f;
        bool showHitColor = true;
        while (elapsed < HitFlashDuration)
        {
            spriteRenderer.color = showHitColor ? HitFlashColor : Color.white;
            showHitColor = !showHitColor;
            yield return new WaitForSecondsRealtime(HitFlashInterval);
            elapsed += HitFlashInterval;
        }
        spriteRenderer.color = Color.white;
        flashRoutine = null;
    }

    private void SpawnHitBurst(Vector2 hitPosition)
    {
        if (hitBurstSprite == null) return;
        GameObject burst = new GameObject("TutorialHitBurst", typeof(SpriteRenderer));
        burst.transform.position = new Vector3(hitPosition.x, hitPosition.y, transform.position.z - 0.01f);
        SpriteRenderer renderer = burst.GetComponent<SpriteRenderer>();
        renderer.sprite = hitBurstSprite;
        renderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 10;
        StartCoroutine(HitBurstRoutine(burst.transform, renderer));
    }

    private IEnumerator HitBurstRoutine(Transform target, SpriteRenderer renderer)
    {
        float elapsed = 0f;
        while (elapsed < HitBurstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / HitBurstDuration);
            if (target != null) target.localScale = Vector3.one * Mathf.Lerp(0.85f, 1.75f, t);
            if (renderer != null) renderer.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        if (target != null) Destroy(target.gameObject);
    }
}
