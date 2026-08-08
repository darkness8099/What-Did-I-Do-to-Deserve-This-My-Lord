using System.Collections;
using UnityEngine;

public enum HeroWavePhase
{
    NotStarted,
    Preparing,
    WaitingForDemonLordPlacement,
    Invading,
    Completed,
}

public class HeroWaveDirector : MonoBehaviour
{
    private LevelConfig levelConfig;
    private DemonLordManager demonLordManager;
    private HeroManager heroManager;
    private HeroRenderer heroRenderer;
    private HeroMover heroMover;
    private MVPGameManager gameManager;

    private int activeSpawnRoutines;
    private bool spawnFailed;

    public static HeroWaveDirector Active { get; private set; }
    public int CurrentWaveIndex { get; private set; } = -1;
    public bool IsRunning { get; private set; }
    public bool AllWavesCompleted { get; private set; }
    public HeroWavePhase Phase { get; private set; } = HeroWavePhase.NotStarted;

    private void Awake()
    {
        Active = this;
    }

    public void Initialize(
        LevelConfig configuredLevel,
        DemonLordManager demonLord,
        HeroManager heroes,
        HeroRenderer renderer,
        HeroMover mover,
        MVPGameManager game)
    {
        if (IsRunning) return;

        levelConfig = configuredLevel;
        demonLordManager = demonLord;
        heroManager = heroes;
        heroRenderer = renderer;
        heroMover = mover;
        gameManager = game;

        if (levelConfig == null || demonLordManager == null || heroManager == null
            || heroRenderer == null || heroMover == null || gameManager == null)
        {
            Debug.LogError("[HeroWaveDirector] Initialization failed because a dependency is missing.");
            return;
        }

        StartCoroutine(RunLevel());
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }

    public static int CountScheduledHeroes(HeroWaveConfig wave)
    {
        if (wave == null || wave.Heroes == null) return 0;

        int total = 0;
        for (int i = 0; i < wave.Heroes.Count; i++)
        {
            HeroSpawnEntryConfig slot = wave.Heroes[i];
            if (slot != null && slot.Hero != null) total++;
        }
        return total;
    }

    private IEnumerator RunLevel()
    {
        IsRunning = true;
        HeroLevelConfig schedule = levelConfig.GetHeroLevelConfig();
        if (schedule == null)
        {
            Debug.LogError("[HeroWaveDirector] Hero level schedule is missing.");
            IsRunning = false;
            yield break;
        }
        if (!schedule.IsValid(out string error))
        {
            Debug.LogError($"[HeroWaveDirector] Invalid hero level schedule: {error}");
            IsRunning = false;
            yield break;
        }

        Debug.Log($"[HeroWaveDirector] Starting level {schedule.LevelNumber} with {schedule.Waves.Count} wave(s).");
        for (int waveIndex = 0; waveIndex < schedule.Waves.Count; waveIndex++)
        {
            if (!gameManager.IsPlaying()) break;

            CurrentWaveIndex = waveIndex;
            Phase = HeroWavePhase.Preparing;
            HeroWaveConfig wave = schedule.Waves[waveIndex];
            if (wave.PreparationSeconds > 0f)
            {
                Debug.Log($"[HeroWaveDirector] Wave {wave.WaveNumber} begins in {wave.PreparationSeconds:0.##} seconds.");
                yield return new WaitForSeconds(wave.PreparationSeconds);
            }
            if (!gameManager.IsPlaying()) break;

            demonLordManager.RequestReposition();
            Phase = HeroWavePhase.WaitingForDemonLordPlacement;
            Debug.Log($"[HeroWaveDirector] Wave {wave.WaveNumber} is waiting for DemonLord placement.");
            while (gameManager.IsPlaying() && !demonLordManager.IsPlaced)
                yield return null;
            if (!gameManager.IsPlaying()) break;

            Phase = HeroWavePhase.Invading;
            activeSpawnRoutines = 0;
            spawnFailed = false;
            for (int heroIndex = 0; heroIndex < wave.Heroes.Count; heroIndex++)
            {
                HeroSpawnEntryConfig slot = wave.Heroes[heroIndex];
                activeSpawnRoutines++;
                StartCoroutine(SpawnHero(wave.WaveNumber, slot));
            }

            while (gameManager.IsPlaying() && activeSpawnRoutines > 0)
                yield return null;
            if (!gameManager.IsPlaying()) break;
            if (spawnFailed)
            {
                Debug.LogError($"[HeroWaveDirector] Wave {wave.WaveNumber} aborted because a hero could not be spawned.");
                IsRunning = false;
                yield break;
            }

            while (gameManager.IsPlaying() && heroManager.HasAnyHero())
                yield return null;
            if (!gameManager.IsPlaying()) break;

            Debug.Log($"[HeroWaveDirector] Wave {wave.WaveNumber} cleared.");
        }

        if (gameManager.IsPlaying() && CurrentWaveIndex == schedule.Waves.Count - 1
            && !heroManager.HasAnyHero())
        {
            Phase = HeroWavePhase.Completed;
            AllWavesCompleted = true;
            gameManager.NotifyAllWavesCleared();
        }

        IsRunning = false;
    }

    private IEnumerator SpawnHero(int waveNumber, HeroSpawnEntryConfig slot)
    {
        if (slot.SpawnDelay > 0f)
            yield return new WaitForSeconds(slot.SpawnDelay);

        if (!gameManager.IsPlaying())
        {
            activeSpawnRoutines--;
            yield break;
        }

        int heroId = heroManager.SpawnHeroAtEntrance(slot.Hero);
        if (heroId < 0)
            spawnFailed = true;
        else
        {
            heroRenderer.CreateHeroView(heroId);
            heroMover.StartHero(heroId);
            Debug.Log($"[HeroWaveDirector] Wave {waveNumber} spawned Hero {heroId} ({slot.Hero.HeroId}).");
        }
        activeSpawnRoutines--;
    }
}
