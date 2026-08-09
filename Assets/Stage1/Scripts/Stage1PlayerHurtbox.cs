using UnityEngine;

public sealed class Stage1PlayerHurtbox : MonoBehaviour
{
    private Stage1Player player;

    private void Awake()
    {
        player = GetComponentInParent<Stage1Player>();
    }

    public void TakeDamage(int damage)
    {
        if (player != null) player.TakeDamage(damage);
    }
}
