using UnityEngine;

public enum GameState { Playing, Victory, Defeat }

public class MVPGameManager : MonoBehaviour
{
    private HeroManager heroManager;

    public GameState CurrentState { get; private set; } = GameState.Playing;

    private void Start()
    {
        heroManager = GetComponent<HeroManager>() ?? FindObjectOfType<HeroManager>();
        if (heroManager == null)
            Debug.LogError("[MVPGameManager] HeroManager not found.");

        Debug.Log("[MVPGameManager] Initialized. State=Playing.");
    }

    public bool IsPlaying() => CurrentState == GameState.Playing;

    public GameState GetCurrentState() => CurrentState;

    public void NotifyHeroReachedDemonLordRoom(int heroId)
    {
        if (!IsPlaying()) return;
        CurrentState = GameState.Defeat;
        Debug.Log($"[MVPGameManager] Game Over - Hero {heroId} reached DemonLordRoom.");
    }

    public void NotifyHeroDefeated(int heroId)
    {
        if (!IsPlaying()) return;
        if (!heroManager.HasAnyHero())
        {
            CurrentState = GameState.Victory;
            Debug.Log("[MVPGameManager] Victory - All heroes defeated.");
        }
    }
}
