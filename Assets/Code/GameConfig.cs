using UnityEngine;

// Choices made on the main menu, read by ItemSpawner.BeginWorld() when the soundscape starts.
// Static so it survives even if we later split the menu into its own scene.
public static class GameConfig
{
    public static bool Configured = false;            // set true when the player presses Start

    // Statics persist across editor Play sessions (and if domain reload is off), which would make
    // the spawner skip the menu on the 2nd Play. Reset the flag every time the game starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() { Configured = false; }

    public static DayPeriod Period = DayPeriod.Midday;
    public static bool WeatherEnabled = true;
    public static float WeatherChance = 0.10f;        // WeatherController.formChance (per check)
    public static bool StormLocked = false;           // "Stormy" pick → permanent max storm, never fades
    public static string Biome = "Forest";            // only Forest for now; hook for future biomes
}
