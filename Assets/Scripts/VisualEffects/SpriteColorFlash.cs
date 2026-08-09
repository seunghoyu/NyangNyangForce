using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteColorFlash : MonoBehaviour
{
    private const string ShaderResourcePath = "Shaders/Effects/SpriteColorFlash";
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashStrengthId = Shader.PropertyToID("_FlashStrength");

    private SpriteRenderer spriteRenderer;
    private Material originalMaterial;
    private Material flashMaterial;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;

    private void Awake()
    {
        Initialize();
    }

    public void Show(Color color, float strength)
    {
        Initialize();
        if (spriteRenderer == null) return;

        if (flashMaterial == null)
        {
            spriteRenderer.color = Color.Lerp(originalColor, color, Mathf.Clamp01(strength));
            return;
        }

        spriteRenderer.sharedMaterial = flashMaterial;
        spriteRenderer.color = Color.white;
        propertyBlock.Clear();
        propertyBlock.SetColor(FlashColorId, color);
        propertyBlock.SetFloat(FlashStrengthId, Mathf.Clamp01(strength));
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    public void Clear()
    {
        Initialize();
        if (spriteRenderer == null) return;

        spriteRenderer.SetPropertyBlock(null);
        spriteRenderer.sharedMaterial = originalMaterial;
        spriteRenderer.color = originalColor;
    }

    private void Initialize()
    {
        if (spriteRenderer != null) return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        originalMaterial = spriteRenderer.sharedMaterial;
        originalColor = spriteRenderer.color;
        propertyBlock = new MaterialPropertyBlock();

        Shader shader = Resources.Load<Shader>(ShaderResourcePath);
        if (shader != null)
            flashMaterial = new Material(shader) { name = "Player Color Flash (Runtime)" };
    }

    private void OnDestroy()
    {
        if (flashMaterial != null) Destroy(flashMaterial);
    }
}
