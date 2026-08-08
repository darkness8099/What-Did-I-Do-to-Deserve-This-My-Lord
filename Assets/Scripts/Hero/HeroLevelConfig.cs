using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
[Serializable]
public class HeroSpawnEntryConfig
{
    [SerializeField] private HeroArchetypeConfig hero;
    [FormerlySerializedAs("firstSpawnDelay")]
    [SerializeField] private float spawnDelay;
    public HeroArchetypeConfig Hero => hero;
    public float SpawnDelay => Mathf.Max(0f, spawnDelay);
    public HeroSpawnEntryConfig(HeroArchetypeConfig heroValue, float delay)
    {
        hero = heroValue;
        spawnDelay = delay;
    }
}
[Serializable]
public class HeroWaveConfig
{
    [SerializeField] private int waveNumber = 1;
    [SerializeField] private float preparationSeconds = 10f;
    [FormerlySerializedAs("entries")]
    [SerializeField] private List<HeroSpawnEntryConfig> heroes = new List<HeroSpawnEntryConfig>();
    public int WaveNumber => Mathf.Max(1, waveNumber);
    public float PreparationSeconds => Mathf.Max(0f, preparationSeconds);
    public IReadOnlyList<HeroSpawnEntryConfig> Heroes => heroes;
    public HeroWaveConfig(int number, float preparation, IEnumerable<HeroSpawnEntryConfig> heroSlots)
    {
        waveNumber = number;
        preparationSeconds = preparation;
        heroes = heroSlots != null
            ? new List<HeroSpawnEntryConfig>(heroSlots)
            : new List<HeroSpawnEntryConfig>();
    }
}
[CreateAssetMenu(menuName = "Game/Hero Level Schedule", fileName = "hero_level_001")]
public class HeroLevelConfig : ScriptableObject
{
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private List<HeroWaveConfig> waves = new List<HeroWaveConfig>();
    public int LevelNumber => Mathf.Max(1, levelNumber);
    public IReadOnlyList<HeroWaveConfig> Waves => waves;
    public bool IsValid(out string error)
    {
        if (waves == null || waves.Count == 0)
        {
            error = "Level has no hero waves.";
            return false;
        }
        for (int w = 0; w < waves.Count; w++)
        {
            HeroWaveConfig wave = waves[w];
            if (wave == null || wave.Heroes == null || wave.Heroes.Count == 0)
            {
                error = $"Wave {w + 1} has no heroes.";
                return false;
            }
            for (int h = 0; h < wave.Heroes.Count; h++)
            {
                HeroSpawnEntryConfig slot = wave.Heroes[h];
                if (slot == null || slot.Hero == null)
                {
                    error = $"Wave {w + 1}, hero {h + 1} is invalid.";
                    return false;
                }
            }
        }
        error = string.Empty;
        return true;
    }
    public void Configure(int number, IEnumerable<HeroWaveConfig> configuredWaves)
    {
        levelNumber = number;
        waves = configuredWaves != null
            ? new List<HeroWaveConfig>(configuredWaves)
            : new List<HeroWaveConfig>();
    }
    public static HeroLevelConfig CreateRuntimeDefault(int number, float preparationSeconds)
    {
        var config = CreateInstance<HeroLevelConfig>();
        config.hideFlags = HideFlags.HideAndDontSave;
        config.name = $"RuntimeHeroLevel_{Mathf.Max(1, number):000}";
        config.Configure(number, new[]
        {
            new HeroWaveConfig(1, preparationSeconds, new[]
            {
                new HeroSpawnEntryConfig(HeroArchetypeConfig.RuntimeDefault, 0f),
            }),
        });
        return config;
    }
}
