using System;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using PvPSoundboard;

namespace PvPSoundboard.Services;

/// <summary>
/// PvP events from ActorControl (local player only).
/// Kill:   0x6D, a2=5, a3=1  (validated in Frontline)
/// Assist: 0x6D, a2=5, a3=2  (validated in Frontline)
/// Death:  0x6
/// BH:     0x14, a1=2131–2135
/// Signature may break on game patches.
/// </summary>
public sealed class ActorControlLogger : IDisposable
{
    private readonly Plugin Plugin;

    private delegate void ProcessPacketActorControlDelegate(
        uint entityId,
        uint category,
        uint arg1,
        uint arg2,
        uint arg3,
        uint arg4,
        uint arg5,
        uint arg6,
        uint arg7,
        uint arg8,
        ulong targetId,
        byte isRecorded);

    [Signature("E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64", DetourName = nameof(ProcessPacketActorControlDetour))]
    private Hook<ProcessPacketActorControlDelegate>? actorControlHook;

    public event Action? OnKill;
    public event Action? OnDeath;
    public event Action? OnAssist;
    public event Action<int>? OnBattleHighTierUp;

    public ActorControlLogger(Plugin plugin)
    {
        Plugin = plugin;

        try
        {
            Plugin.GameInteropProvider.InitializeFromAttributes(this);

            if (actorControlHook == null)
            {
                Plugin.Log.Error("[PvpSoundboard] ActorControl hook was null after init — signature may be outdated");
                return;
            }

            actorControlHook.Enable();
            Plugin.Log.Information("[PvpSoundboard] ActorControlLogger enabled");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[PvpSoundboard] ActorControlLogger failed to hook — signature may be outdated");
        }
    }

    private void ProcessPacketActorControlDetour(
        uint entityId,
        uint category,
        uint arg1,
        uint arg2,
        uint arg3,
        uint arg4,
        uint arg5,
        uint arg6,
        uint arg7,
        uint arg8,
        ulong targetId,
        byte isRecorded)
    {
        actorControlHook?.Original(
            entityId, category, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, targetId, isRecorded);

        try
        {
            if (!Plugin.Configuration.Enabled)
                return;

            if (Plugin.Configuration.OnlyInPvp && !Plugin.ClientState.IsPvP)
                return;

            if (!Plugin.ClientState.IsPvP)
                return;

            var local = Plugin.ObjectTable.LocalPlayer;
            if (local == null)
                return;

            var localId = local.EntityId;
            if (entityId != localId && (uint)targetId != localId)
                return;

            // Death
            if (category == 0x6 && entityId == localId)
            {
                Plugin.Log.Debug("[PvpSoundboard] Death (ActorControl)");
                OnDeath?.Invoke();
                return;
            }

            // Frontline KDA / BH rating director update
            if (category == 0x6D && arg2 == 5)
            {
                if (arg3 == 1)
                {
                    Plugin.Log.Debug($"[PvpSoundboard] Kill (ActorControl) BH→{arg4}");
                    OnKill?.Invoke();
                }
                else if (arg3 == 2)
                {
                    Plugin.Log.Debug($"[PvpSoundboard] Assist (ActorControl) BH→{arg4}");
                    OnAssist?.Invoke();
                }

                return;
            }

            // Battle High I–V
            if (category == 0x14 && arg1 is >= 2131 and <= 2135)
            {
                var level = (int)(arg1 - 2130);
                Plugin.Log.Debug($"[PvpSoundboard] BH status {level} (ActorControl)");
                OnBattleHighTierUp?.Invoke(level);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Debug(ex, "ActorControlLogger detour error");
        }
    }

    public void Dispose()
    {
        actorControlHook?.Disable();
        actorControlHook?.Dispose();
        actorControlHook = null;
    }
}