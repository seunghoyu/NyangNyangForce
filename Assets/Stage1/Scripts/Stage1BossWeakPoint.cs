using UnityEngine;

public sealed class Stage1BossWeakPoint : MonoBehaviour
{
    private Stage1Boss boss;

    public void Initialize(Stage1Boss owner)
    {
        boss = owner;
    }

    public bool TryDamage(int damage)
    {
        return boss != null && boss.TakeDamage(damage);
    }
}
