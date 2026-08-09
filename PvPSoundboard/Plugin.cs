using System;
using Dalamud.Game.Command;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using PvPSoundboard.Services;
using PvPSoundboard.Windows;

namespace PvPSoundboard;

public sealed class Plugin : IDalamudPlugin
{
    public static string Name => "PVP Soundboard";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    internal readonly FileDialogManager FileDialogManager = new();

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("PvPSoundboard");

    private ConfigWindow ConfigWindow { get; init; }
    private StatsWindow? StatsWindow;

    private const string CommandName = "/pvpsounds";

    internal SoundPlayer? SoundPlayer;
    private ActorControlLogger? ActorControlLogger;

    private int currentKillstreak;
    internal MatchStats MatchStats { get; } = new();

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        StatsWindow = new StatsWindow(this);
        WindowSystem.AddWindow(StatsWindow);

        SoundPlayer = new SoundPlayer(this);
        ActorControlLogger = new ActorControlLogger(this);

        ActorControlLogger.OnKill += () =>
        {
            MatchStats.Kills++;
            currentKillstreak++;
            MatchStats.Killstreak = currentKillstreak;

            Log.Information($">>> KILL detected! (streak: {currentKillstreak})");

            if (SoundPlayer?.HasKillstreakSound(currentKillstreak) == true)
                SoundPlayer.PlayKillstreak(currentKillstreak);
            else
                SoundPlayer?.PlayKill();
        };

        ActorControlLogger.OnDeath += () =>
        {
            MatchStats.Deaths++;
            currentKillstreak = 0;
            MatchStats.Killstreak = 0;

            Log.Information(">>> DEATH detected! (streak reset)");
            SoundPlayer?.PlayDeath();
        };

        ActorControlLogger.OnAssist += () =>
        {
            MatchStats.Assists++;
            Log.Information(">>> ASSIST detected!");
            SoundPlayer?.PlayAssist();
        };

        ActorControlLogger.OnBattleHighTierUp += level =>
        {
            if (level <= MatchStats.BattleHighLevel)
                return;

            MatchStats.BattleHighLevel = level;
            Log.Information($">>> Battle High {level}!");
            SoundPlayer?.PlayBattleHigh(level);
        };

        ClientState.TerritoryChanged += OnTerritoryChanged;
        Framework.Update += OnFrameworkUpdate;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the PVP Soundboard configuration window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        Log.Information("PVP Soundboard loaded.");
    }

    private void OnTerritoryChanged(uint territory)
    {
        MatchStats.Reset();
        currentKillstreak = 0;

        if ((!ClientState.IsPvP || !Configuration.ShowMatchStats) && StatsWindow != null)
            StatsWindow.IsOpen = false;
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (StatsWindow == null)
            return;

        var shouldShow = ClientState.IsPvP && Configuration.ShowMatchStats;
        StatsWindow.IsOpen = shouldShow;

        if (ClientState.IsPvP)
            MatchStats.BattleHighLevel = GetBattleHighLevelFromPlayer();
    }

    private int GetBattleHighLevelFromPlayer()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null)
            return 0;

        foreach (var status in player.StatusList)
        {
            var id = status.StatusId;
            if (id is >= 2131 and <= 2135)
                return (int)(id - 2130);
        }

        return 0;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        ClientState.TerritoryChanged -= OnTerritoryChanged;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        SoundPlayer?.Dispose();
        ActorControlLogger?.Dispose();

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleConfigUI;
    }

    private void OnCommand(string command, string args) => ToggleConfigUI();

    private void DrawUI()
    {
        WindowSystem.Draw();
        FileDialogManager.Draw();
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}