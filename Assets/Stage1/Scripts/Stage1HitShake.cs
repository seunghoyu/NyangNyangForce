using UnityEngine;

public sealed class Stage1HitShake : MonoBehaviour
{
    public float duration = 0.08f;
    public float magnitude = 0.035f;
    public float frequency = 70f;

    private Vector3 baseLocalPosition;
    private float shakeUntil;
    private float seed;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        seed = Random.value * 100f;
    }

    private void OnDisable()
    {
        transform.localPosition = baseLocalPosition;
    }

    private void Update()
    {
        if (Time.time >= shakeUntil)
        {
            transform.localPosition = baseLocalPosition;
            return;
        }

        float remaining = Mathf.InverseLerp(shakeUntil, shakeUntil - duration, Time.time);
        float strength = magnitude * remaining;
        float x = (Mathf.PerlinNoise(seed, Time.time * frequency) - 0.5f) * 2f * strength;
        float y = (Mathf.PerlinNoise(seed + 17f, Time.time * frequency) - 0.5f) * 2f * strength;
        transform.localPosition = baseLocalPosition + new Vector3(x, y, 0f);
    }

    public void Play()
    {
        // Do not capture the current shake offset as the new origin when hits
        // overlap. Doing so makes the object drift a little after every hit.
        if (Time.time >= shakeUntil)
        {
            baseLocalPosition = transform.localPosition;
        }

        shakeUntil = Time.time + duration;
    }
}
