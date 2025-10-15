
// This file holds static settings that persist across scene loads.
public static class GameSettings
{
    public enum GameMode { PlayerVsPlayer, PlayerVsAI }

    // The game mode selected in the main menu.
    public static GameMode DesiredMode { get; set; } = GameMode.PlayerVsPlayer;
}
