using UnityEngine;

public sealed class TileBreakEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] particleVariants;
    [SerializeField, Min(1)] private int minParticles = 4;
    [SerializeField, Min(1)] private int maxParticles = 6;
    [SerializeField, Min(0f)] private float minSpeed = 2.2f;
    [SerializeField, Min(0f)] private float maxSpeed = 3f;
    [SerializeField, Min(0f)] private float destroyDelay = 0.55f;

    private void Awake()
    {
        Play();
    }

    public int Play()
    {
        if (particleVariants == null || particleVariants.Length == 0)
            return 0;

        foreach (ParticleSystem variant in particleVariants)
            if (variant != null) variant.Play(false);

        int emitted = 0;
        int count = Random.Range(minParticles, Mathf.Max(minParticles, maxParticles) + 1);
        for (int i = 0; i < count; i++)
        {
            ParticleSystem variant = particleVariants[Random.Range(0, particleVariants.Length)];
            if (variant == null) continue;

            float angle = Random.value * Mathf.PI * 2f;
            var emission = new ParticleSystem.EmitParams
            {
                position = variant.transform.position,
                velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f)
                    * Random.Range(minSpeed, maxSpeed)
            };
            variant.Emit(emission, 1);
            emitted++;
        }

        if (Application.isPlaying)
            Destroy(gameObject, destroyDelay);
        return emitted;
    }
}
