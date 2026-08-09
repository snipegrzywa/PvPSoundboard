using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using PvPSoundboard;

namespace PvPSoundboard.Windows;

public class StatsWindow : Window
{
    private readonly Plugin Plugin;

    public StatsWindow(Plugin plugin)
        : base("PvP Stats###PvPSoundboardStats")
    {
        Flags = ImGuiWindowFlags.NoCollapse
              | ImGuiWindowFlags.AlwaysAutoResize;

        Size = new Vector2(220, 160);
        SizeCondition = ImGuiCond.FirstUseEver;

        Plugin = plugin;
    }

    public override void Draw()
    {
        var s = Plugin.MatchStats;

        ImGui.Text($"Kills:   {s.Kills}");
        ImGui.Text($"Deaths:  {s.Deaths}");
        ImGui.Text($"Assists: {s.Assists}");
        ImGui.Separator();
        ImGui.Text($"Battle High: {FormatBattleHigh(s.BattleHighLevel)}");
        ImGui.Text($"Killstreak:  {s.Killstreak}");
    }

    private static string FormatBattleHigh(int level) => level switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        _ => "None"
    };
}