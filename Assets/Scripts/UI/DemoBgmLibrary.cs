using UnityEngine;

[CreateAssetMenu(fileName = "demo_bgm_library", menuName = "WhatDidIDo/Audio/Demo BGM Library")]
public sealed class DemoBgmLibrary : ScriptableObject
{
    public AudioClip mainMenu;
    public AudioClip[] freeDigging = new AudioClip[0];
    public AudioClip invasion;

    public bool IsComplete
    {
        get
        {
            if (mainMenu == null || invasion == null
                || freeDigging == null || freeDigging.Length == 0)
                return false;

            for (int i = 0; i < freeDigging.Length; i++)
            {
                if (freeDigging[i] == null) return false;
            }

            return true;
        }
    }
}
