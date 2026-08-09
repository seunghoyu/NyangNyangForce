using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(SpriteColorFlash))]
public sealed class Stage1MachineGunPickup : MonoBehaviour
{
    private const float AppearanceFrameInterval = 0.1f;
    private const float ShimmerPeriod = 1f;
    private const float ShimmerDuration = 0.14f;
    private const float ShimmerStrength = 0.82f;
    private const float ShimmerScaleAmount = 0.06f;

    public Sprite[] appearFrames;
    public Sprite idleSprite;

    private Stage1Game game;
    private SpriteRenderer spriteRenderer;
    private SpriteColorFlash colorFlash;
    private BoxCollider2D pickupCollider;
    private Vector3 baseScale;
    private Coroutine visualRoutine;
    private bool appearancePlayed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        colorFlash = GetComponent<SpriteColorFlash>();
        pickupCollider = GetComponent<BoxCollider2D>();
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (visualRoutine != null) StopCoroutine(visualRoutine);
        visualRoutine = StartCoroutine(PlayVisuals());
    }

    private void OnDisable()
    {
        visualRoutine = null;
        if (colorFlash != null) colorFlash.Clear();
        transform.localScale = baseScale;
    }

    public void Initialize(Stage1Game owner)
    {
        game = owner;
        gameObject.SetActive(true);
    }

    public float GetCenterYForSurface(float surfaceY)
    {
        if (idleSprite == null) return surfaceY;
        float scaledBottomOffset = idleSprite.bounds.min.y * Mathf.Abs(transform.localScale.y);
        return surfaceY - scaledBottomOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Stage1Player player = other.GetComponent<Stage1Player>();
        if (player == null) return;

        player.EquipMachineGun();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (game != null && game.BattleEnded) Destroy(gameObject);
    }

    private IEnumerator PlayVisuals()
    {
        if (pickupCollider != null) pickupCollider.enabled = false;
        if (colorFlash != null) colorFlash.Clear();
        transform.localScale = baseScale;

        if (!appearancePlayed && appearFrames != null)
        {
            for (int i = 0; i < appearFrames.Length; i++)
            {
                if (appearFrames[i] == null) continue;
                spriteRenderer.sprite = appearFrames[i];
                yield return new WaitForSeconds(AppearanceFrameInterval);
            }
            appearancePlayed = true;
        }

        if (idleSprite != null) spriteRenderer.sprite = idleSprite;
        if (pickupCollider != null) pickupCollider.enabled = true;

        float restDuration = Mathf.Max(0f, ShimmerPeriod - ShimmerDuration);
        while (true)
        {
            yield return new WaitForSeconds(restDuration);
            yield return PlayShimmer();
        }
    }

    private IEnumerator PlayShimmer()
    {
        float elapsed = 0f;
        while (elapsed < ShimmerDuration)
        {
            float normalized = elapsed / ShimmerDuration;
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            if (colorFlash != null)
                colorFlash.Show(Color.white, pulse * ShimmerStrength);
            transform.localScale = baseScale * (1f + pulse * ShimmerScaleAmount);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (colorFlash != null) colorFlash.Clear();
        transform.localScale = baseScale;
    }
}
