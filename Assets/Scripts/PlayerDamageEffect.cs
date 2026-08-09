using System.Collections;
using UnityEngine;

public sealed class PlayerDamageEffect : MonoBehaviour
{
    private const float FirstFrameSeconds = 0.035f;
    private const float SecondFrameSeconds = 0.1f;
    private const string FirstFramePath = "Effects/Damage/damage";
    private const string SecondFramePath = "Effects/Damage/damage2";

    private static Sprite firstFrame;
    private static Sprite secondFrame;

    public static void Spawn(Vector2 hitPosition, Component playerPart)
    {
        LoadFrames();
        if (firstFrame == null || secondFrame == null) return;

        GameObject effectObject = new GameObject("Player Damage Effect");
        effectObject.transform.position = new Vector3(hitPosition.x, hitPosition.y, -1f);

        SpriteRenderer effectRenderer = effectObject.AddComponent<SpriteRenderer>();
        SpriteRenderer playerRenderer = playerPart != null
            ? playerPart.GetComponentInParent<SpriteRenderer>()
            : null;

        if (playerRenderer != null)
        {
            effectRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            effectRenderer.sortingOrder = playerRenderer.sortingOrder + 10;
        }

        PlayerDamageEffect effect = effectObject.AddComponent<PlayerDamageEffect>();
        effect.StartCoroutine(effect.Play(effectRenderer));
    }

    private static void LoadFrames()
    {
        if (firstFrame == null) firstFrame = Resources.Load<Sprite>(FirstFramePath);
        if (secondFrame == null) secondFrame = Resources.Load<Sprite>(SecondFramePath);
    }

    private IEnumerator Play(SpriteRenderer effectRenderer)
    {
        effectRenderer.sprite = firstFrame;
        yield return new WaitForSeconds(FirstFrameSeconds);
        effectRenderer.sprite = secondFrame;
        yield return new WaitForSeconds(SecondFrameSeconds);
        Destroy(gameObject);
    }
}
