using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

namespace PvPSoundboard.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    private string saveMessage = string.Empty;
    private float saveMessageTimer = 0f;

    public ConfigWindow(Plugin plugin) : base("PVP Soundboard###PvPSoundboardConfig")
    {
        Flags = ImGuiWindowFlags.None;
        Size = new Vector2(520, 560);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 300),
            MaximumSize = new Vector2(1000, 1000)
        };
        Plugin = plugin;
    }

    public override void Draw()
    {
        var config = Plugin.Configuration;

        // ========== GENERAL ==========
        if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var enabled = config.Enabled;
            if (ImGui.Checkbox("Enable plugin", ref enabled))
            {
                config.Enabled = enabled;
                config.Save();
            }

            var showStats = config.ShowMatchStats;
            if (ImGui.Checkbox("Show current match stats window during PvP", ref showStats))
            {
                config.ShowMatchStats = showStats;
                config.Save();
            }

            ImGui.Separator();
            ImGui.Spacing();

            ImGui.SetNextItemWidth(200);
            var volume = config.Volume;
            if (ImGui.SliderInt("Volume", ref volume, 0, 100, "%d"))
            {
                config.Volume = volume;
                config.Save();
            }

            ImGui.Separator();
            ImGui.Spacing();

            // ----- Kill -----
            DrawSoundRow(
                config,
                "Play on Kill",
                () => config.PlayOnKill, v => config.PlayOnKill = v,
                () => config.KillSoundPath, v => config.KillSoundPath = v,
                "Kill",
                () => Plugin.SoundPlayer?.PlayKill());

            ImGui.Spacing();
            ImGui.Separator();

            // ----- Death -----
            DrawSoundRow(
                config,
                "Play on Death",
                () => config.PlayOnDeath, v => config.PlayOnDeath = v,
                () => config.DeathSoundPath, v => config.DeathSoundPath = v,
                "Death",
                () => Plugin.SoundPlayer?.PlayDeath());

            ImGui.Spacing();
            ImGui.Separator();

            // ----- Assist -----
            DrawSoundRow(
                config,
                "Play on Assist",
                () => config.PlayOnAssist, v => config.PlayOnAssist = v,
                () => config.AssistSoundPath, v => config.AssistSoundPath = v,
                "Assist",
                () => Plugin.SoundPlayer?.PlayAssist());

            ImGui.Spacing();
            ImGui.Separator();

            // ----- Killstreaks (nested under Assist / inside General) -----
            ImGui.Indent(12f);
            if (ImGui.TreeNode("Killstreaks"))
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(?)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 28f);
                    ImGui.TextUnformatted(
                        "Killstreaks count consecutive kills without dying.\n\n" +
                        "If a streak sound is disabled or has no file set, the normal kill sound is used.");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                ImGui.Indent(8f);

                DrawKillstreakRow(config, 3, "3 kills",
                    () => config.PlayOnKillstreak3, v => config.PlayOnKillstreak3 = v,
                    () => config.Killstreak3SoundPath, v => config.Killstreak3SoundPath = v);
                DrawKillstreakRow(config, 5, "5 kills",
                    () => config.PlayOnKillstreak5, v => config.PlayOnKillstreak5 = v,
                    () => config.Killstreak5SoundPath, v => config.Killstreak5SoundPath = v);
                DrawKillstreakRow(config, 10, "10 kills",
                    () => config.PlayOnKillstreak10, v => config.PlayOnKillstreak10 = v,
                    () => config.Killstreak10SoundPath, v => config.Killstreak10SoundPath = v);
                DrawKillstreakRow(config, 15, "15 kills",
                    () => config.PlayOnKillstreak15, v => config.PlayOnKillstreak15 = v,
                    () => config.Killstreak15SoundPath, v => config.Killstreak15SoundPath = v);
                DrawKillstreakRow(config, 20, "20 kills",
                    () => config.PlayOnKillstreak20, v => config.PlayOnKillstreak20 = v,
                    () => config.Killstreak20SoundPath, v => config.Killstreak20SoundPath = v);

                ImGui.Unindent(8f);
                ImGui.TreePop();
            }
            ImGui.Unindent(12f);

            ImGui.Spacing();
            ImGui.Separator();
        }

        ImGui.Spacing();

        // ========== FRONTLINE ==========
        if (ImGui.CollapsingHeader("Frontline", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Spacing();
            ImGui.Separator();

            ImGui.Indent(12f);
            if (ImGui.TreeNode("Battle High"))
            {
                ImGui.Indent(8f);

                DrawBattleHighRow(config, 1, "Battle High I",
                    () => config.PlayOnBattleHigh1, v => config.PlayOnBattleHigh1 = v,
                    () => config.BattleHigh1SoundPath, v => config.BattleHigh1SoundPath = v);
                DrawBattleHighRow(config, 2, "Battle High II",
                    () => config.PlayOnBattleHigh2, v => config.PlayOnBattleHigh2 = v,
                    () => config.BattleHigh2SoundPath, v => config.BattleHigh2SoundPath = v);
                DrawBattleHighRow(config, 3, "Battle High III",
                    () => config.PlayOnBattleHigh3, v => config.PlayOnBattleHigh3 = v,
                    () => config.BattleHigh3SoundPath, v => config.BattleHigh3SoundPath = v);
                DrawBattleHighRow(config, 4, "Battle High IV",
                    () => config.PlayOnBattleHigh4, v => config.PlayOnBattleHigh4 = v,
                    () => config.BattleHigh4SoundPath, v => config.BattleHigh4SoundPath = v);
                DrawBattleHighRow(config, 5, "Battle High V",
                    () => config.PlayOnBattleHigh5, v => config.PlayOnBattleHigh5 = v,
                    () => config.BattleHigh5SoundPath, v => config.BattleHigh5SoundPath = v);

                ImGui.Unindent(8f);
                ImGui.TreePop();
            }

            ImGui.Unindent(12f);

            ImGui.Spacing();
            ImGui.Separator();

        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextWrapped("Tip: Use Browse to pick a .wav, .mp3, or .ogg files (.ogg requires you to switch filetype to '.*').");
        ImGui.Spacing();

        if (ImGui.Button("Save", new Vector2(100, 0)))
        {
            config.Save();
            saveMessage = "Settings saved!";
            saveMessageTimer = 2.5f;
        }

        ImGui.SameLine();
        if (ImGui.Button("Save & Close", new Vector2(120, 0)))
        {
            config.Save();
            IsOpen = false;
        }

        if (saveMessageTimer > 0f)
        {
            saveMessageTimer -= ImGui.GetIO().DeltaTime;
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.3f, 1.0f), saveMessage);
        }
    }

    private void DrawBattleHighRow(
        Configuration config,
        int level,
        string label,
        Func<bool> getEnabled,
        Action<bool> setEnabled,
        Func<string> getPath,
        Action<string> setPath)
    {
        var enabled = getEnabled();
        if (ImGui.Checkbox(label, ref enabled))
        {
            setEnabled(enabled);
            config.Save();
        }

        var path = getPath();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140);
        if (ImGui.InputText($"##BH{level}Path", ref path, 512))
            setPath(path);
        if (ImGui.IsItemDeactivatedAfterEdit())
            config.Save();

        ImGui.SameLine();
        if (ImGui.Button($"Browse##BH{level}"))
        {
            Plugin.FileDialogManager.OpenFileDialog(
                $"Select Battle High {level} Sound",
                "Audio Files{.wav,.mp3},.*",
                (success, selected) =>
                {
                    if (success && !string.IsNullOrEmpty(selected))
                    {
                        setPath(selected);
                        config.Save();
                    }
                });
        }

        ImGui.SameLine();
        if (ImGui.Button($"Test##BH{level}"))
            Plugin.SoundPlayer?.PlayBattleHigh(level);
    }

    private void DrawKillstreakRow(
        Configuration config,
        int streak,
        string label,
        Func<bool> getEnabled,
        Action<bool> setEnabled,
        Func<string> getPath,
        Action<string> setPath)
    {
        var enabled = getEnabled();
        if (ImGui.Checkbox(label, ref enabled))
        {
            setEnabled(enabled);
            config.Save();
        }

        var path = getPath();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140);
        if (ImGui.InputText($"##Streak{streak}Path", ref path, 512))
            setPath(path);
        if (ImGui.IsItemDeactivatedAfterEdit())
            config.Save();

        ImGui.SameLine();
        if (ImGui.Button($"Browse##Streak{streak}"))
        {
            Plugin.FileDialogManager.OpenFileDialog(
                $"Select {streak}-Kill Streak Sound",
                "Audio Files{.wav,.mp3},.*",
                (success, selected) =>
                {
                    if (success && !string.IsNullOrEmpty(selected))
                    {
                        setPath(selected);
                        config.Save();
                    }
                });
        }

        ImGui.SameLine();
        if (ImGui.Button($"Test##Streak{streak}"))
            Plugin.SoundPlayer?.PlayKillstreak(streak);
    }

    private void DrawSoundRow(
        Configuration config,
        string checkboxLabel,
        Func<bool> getEnabled,
        Action<bool> setEnabled,
        Func<string> getPath,
        Action<string> setPath,
        string id,
        Action testAction)
    {
        var enabled = getEnabled();
        if (ImGui.Checkbox(checkboxLabel, ref enabled))
        {
            setEnabled(enabled);
            config.Save();
        }

        var path = getPath();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 140);
        if (ImGui.InputText($"##{id}Path", ref path, 512))
            setPath(path);
        if (ImGui.IsItemDeactivatedAfterEdit())
            config.Save();

        ImGui.SameLine();
        if (ImGui.Button($"Browse##{id}"))
        {
            Plugin.FileDialogManager.OpenFileDialog(
                $"Select {id} Sound",
                "Audio Files{.wav,.mp3},.*",
                (success, selected) =>
                {
                    if (success && !string.IsNullOrEmpty(selected))
                    {
                        setPath(selected);
                        config.Save();
                    }
                });
        }

        ImGui.SameLine();
        if (ImGui.Button($"Test##{id}"))
            testAction();
    }

    public void Dispose()
    {
    }
}