using System;
using Dalamud.Configuration;

namespace PvPSoundboard;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool Enabled { get; set; } = true;

    public int Volume { get; set; } = 50; /// <summary>0 = mute, 100 = max playback gain.</summary>
    public bool OnlyInPvp { get; set; } = true;

    public bool PlayOnKill { get; set; } = true;
    public string KillSoundPath { get; set; } = string.Empty;

    public bool PlayOnDeath { get; set; } = true;
    public string DeathSoundPath { get; set; } = string.Empty;

    public bool PlayOnAssist { get; set; } = true;
    public string AssistSoundPath { get; set; } = string.Empty;

    public bool PlayOnBattleHigh1 { get; set; } = true;
    public bool PlayOnBattleHigh2 { get; set; } = true;
    public bool PlayOnBattleHigh3 { get; set; } = true;
    public bool PlayOnBattleHigh4 { get; set; } = true;
    public bool PlayOnBattleHigh5 { get; set; } = true;
    public string BattleHigh1SoundPath { get; set; } = string.Empty;
    public string BattleHigh2SoundPath { get; set; } = string.Empty;
    public string BattleHigh3SoundPath { get; set; } = string.Empty;
    public string BattleHigh4SoundPath { get; set; } = string.Empty;
    public string BattleHigh5SoundPath { get; set; } = string.Empty;

    public bool PlayOnKillstreak3 { get; set; } = true;
    public bool PlayOnKillstreak5 { get; set; } = true;
    public bool PlayOnKillstreak10 { get; set; } = true;
    public bool PlayOnKillstreak15 { get; set; } = true;
    public bool PlayOnKillstreak20 { get; set; } = true;
    public bool ShowMatchStats { get; set; } = true;
    public string Killstreak3SoundPath { get; set; } = string.Empty;
    public string Killstreak5SoundPath { get; set; } = string.Empty;
    public string Killstreak10SoundPath { get; set; } = string.Empty;
    public string Killstreak15SoundPath { get; set; } = string.Empty;
    public string Killstreak20SoundPath { get; set; } = string.Empty;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}