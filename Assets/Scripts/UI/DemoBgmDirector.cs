using UnityEngine;
public enum DemoBgmPhase { MainMenu, FreeDigging, Invasion, Silent }
public sealed class DemoBgmDirector : MonoBehaviour
{
    private const string LibraryPath = "Audio/demo_bgm_library";
    private MVPResultUI ui;
    private HeroWaveDirector waveDirector;
    private DemoBgmLibrary library;
    private AudioSource source;
    private bool hasPhase;
    public DemoBgmPhase CurrentPhase { get; private set; }
    public AudioClip CurrentClip => source != null ? source.clip : null;
    public bool IsMusicPaused { get; private set; }
    public void Initialize(MVPResultUI owner)
    {
        ui = owner;
        library = Resources.Load<DemoBgmLibrary>(LibraryPath);
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        waveDirector = HeroWaveDirector.Active;
        if (library == null || !library.IsComplete)
        {
            Debug.LogError("[DemoBgmDirector] Resources/Audio/demo_bgm_library is missing or incomplete.");
            enabled = false;
            return;
        }
        RefreshNow();
    }
    private void Update()
    {
        RefreshNow();
    }
    public void RefreshNow()
    {
        if (ui == null || source == null || library == null) return;
        if (ui.IsPauseMenuVisible)
        {
            SetPaused(true);
            return;
        }
        SetPaused(false);
        if (waveDirector == null) waveDirector = HeroWaveDirector.Active;
        HeroWavePhase wavePhase = waveDirector != null
            ? waveDirector.Phase
            : HeroWavePhase.NotStarted;
        DemoBgmPhase next = ResolvePhase(
            ui.IsMainMenuVisible, ui.IsResultVisible, wavePhase);
        if (!hasPhase || next != CurrentPhase) SwitchPhase(next);
    }
    public static DemoBgmPhase ResolvePhase(
        bool mainMenuVisible, bool resultVisible, HeroWavePhase wavePhase)
    {
        if (mainMenuVisible) return DemoBgmPhase.MainMenu;
        if (resultVisible) return DemoBgmPhase.Silent;
        return wavePhase == HeroWavePhase.Invading
            ? DemoBgmPhase.Invasion
            : DemoBgmPhase.FreeDigging;
    }

    private void SetPaused(bool pause)
    {
        if (pause == IsMusicPaused) return;
        IsMusicPaused = pause;
        if (!Application.isPlaying) return;
        if (pause) source.Pause();
        else source.UnPause();
    }

    private void SwitchPhase(DemoBgmPhase phase)
    {
        AudioClip next = null;
        if (phase == DemoBgmPhase.MainMenu) next = library.mainMenu;
        else if (phase == DemoBgmPhase.Invasion) next = library.invasion;
        else if (phase == DemoBgmPhase.FreeDigging)
            next = library.freeDigging[Random.Range(0, library.freeDigging.Length)];

        if (Application.isPlaying) source.Stop();
        source.clip = next;
        CurrentPhase = phase;
        hasPhase = true;
        if (next != null && Application.isPlaying) source.Play();
    }
}
