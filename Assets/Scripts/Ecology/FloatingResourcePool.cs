using UnityEngine;

// Floating resource pool: receives resources that have no valid Soil cell to land on.
// Future: redistribute back into the system based on triggers (player action, time, etc.).
// Current scope (TASK-037): deposit + log + internal accumulation only.
public static class FloatingResourcePool
{
    public static int TotalNutrient { get; private set; }
    public static int TotalMagic    { get; private set; }

    public static void Deposit(int nutrient, int magic, string reason)
    {
        int n = Mathf.Max(0, nutrient);
        int m = Mathf.Max(0, magic);
        if (n == 0 && m == 0) return;
        TotalNutrient += n;
        TotalMagic    += m;
        // (Console log removed to avoid spam; slime ecology events are captured by SlimeEcologyDiagnostics file.)
    }
}
