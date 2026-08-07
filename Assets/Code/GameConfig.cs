// Choices made on the main menu, read by ItemSpawner.BeginWorld() when the soundscape starts.
// Static so it survives even if we later split the menu into its own scene.
public static class GameConfig
{
    public static bool Configured = false;            // set true when the player presses Start
    public static DayPeriod Period = DayPeriod.Midday;
    public static bool WeatherEnabled = true;
    public static float WeatherChance = 0.10f;        // WeatherController.formChance (per check)
    public static bool StormLocked = false;           // "Stormy" pick → permanent max storm, never fades
    public static string Biome = "Forest";            // only Forest for now; hook for future biomes
}
