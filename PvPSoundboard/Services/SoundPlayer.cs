using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PvPSoundboard;

namespace PvPSoundboard.Services;

public sealed class SoundPlayer : IDisposable
{
    private const int MaxQueue = 8;

    private readonly Plugin Plugin;
    private readonly object playLock = new();
    private readonly Queue<string> queue = new();

    private IWavePlayer? currentDevice;
    private WaveStream? currentReader;
    private bool isPlaying;
    private bool disposed;

    public SoundPlayer(Plugin plugin)
    {
        Plugin = plugin;
    }

    private static WaveStream OpenAudioFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".ogg" => new VorbisWaveReader(path),
            _ => new AudioFileReader(path)
        };
    }

    public void PlayKill() =>
        PlayConfigured(Plugin.Configuration.PlayOnKill, Plugin.Configuration.KillSoundPath);

    public void PlayDeath() =>
        PlayConfigured(Plugin.Configuration.PlayOnDeath, Plugin.Configuration.DeathSoundPath);

    public void PlayAssist() =>
        PlayConfigured(Plugin.Configuration.PlayOnAssist, Plugin.Configuration.AssistSoundPath);

    public void PlayBattleHigh(int level)
    {
        var (enabled, path) = level switch
        {
            1 => (Plugin.Configuration.PlayOnBattleHigh1, Plugin.Configuration.BattleHigh1SoundPath),
            2 => (Plugin.Configuration.PlayOnBattleHigh2, Plugin.Configuration.BattleHigh2SoundPath),
            3 => (Plugin.Configuration.PlayOnBattleHigh3, Plugin.Configuration.BattleHigh3SoundPath),
            4 => (Plugin.Configuration.PlayOnBattleHigh4, Plugin.Configuration.BattleHigh4SoundPath),
            5 => (Plugin.Configuration.PlayOnBattleHigh5, Plugin.Configuration.BattleHigh5SoundPath),
            _ => (false, string.Empty)
        };
        PlayConfigured(enabled, path);
    }

    public bool HasKillstreakSound(int streak) => streak switch
    {
        3 => Plugin.Configuration.PlayOnKillstreak3 && !string.IsNullOrWhiteSpace(Plugin.Configuration.Killstreak3SoundPath),
        5 => Plugin.Configuration.PlayOnKillstreak5 && !string.IsNullOrWhiteSpace(Plugin.Configuration.Killstreak5SoundPath),
        10 => Plugin.Configuration.PlayOnKillstreak10 && !string.IsNullOrWhiteSpace(Plugin.Configuration.Killstreak10SoundPath),
        15 => Plugin.Configuration.PlayOnKillstreak15 && !string.IsNullOrWhiteSpace(Plugin.Configuration.Killstreak15SoundPath),
        20 => Plugin.Configuration.PlayOnKillstreak20 && !string.IsNullOrWhiteSpace(Plugin.Configuration.Killstreak20SoundPath),
        _ => false
    };

    public void PlayKillstreak(int streak)
    {
        var (enabled, path) = streak switch
        {
            3 => (Plugin.Configuration.PlayOnKillstreak3, Plugin.Configuration.Killstreak3SoundPath),
            5 => (Plugin.Configuration.PlayOnKillstreak5, Plugin.Configuration.Killstreak5SoundPath),
            10 => (Plugin.Configuration.PlayOnKillstreak10, Plugin.Configuration.Killstreak10SoundPath),
            15 => (Plugin.Configuration.PlayOnKillstreak15, Plugin.Configuration.Killstreak15SoundPath),
            20 => (Plugin.Configuration.PlayOnKillstreak20, Plugin.Configuration.Killstreak20SoundPath),
            _ => (false, string.Empty)
        };
        PlayConfigured(enabled, path);
    }

    private void PlayConfigured(bool enabled, string? path)
    {
        if (disposed || !enabled || string.IsNullOrWhiteSpace(path))
            return;

        Enqueue(path);
    }

    private void Enqueue(string path)
    {
        if (!File.Exists(path))
        {
            Plugin.Log.Warning($"[PvpSoundboard] Sound file not found: {path}");
            return;
        }

        lock (playLock)
        {
            if (disposed)
                return;

            while (queue.Count >= MaxQueue)
                queue.Dequeue();

            queue.Enqueue(path);
            if (!isPlaying)
                PlayNext_NoLock();
        }
    }

    private void PlayNext_NoLock()
    {
        DisposeCurrent_NoLock();

        if (disposed)
        {
            isPlaying = false;
            queue.Clear();
            return;
        }

        if (queue.Count == 0)
        {
            isPlaying = false;
            return;
        }

        isPlaying = true;
        var path = queue.Dequeue();

        try
        {
            var volume = Math.Clamp(Plugin.Configuration.Volume / 100f, 0f, 1f);
            var reader = OpenAudioFile(path);
            currentReader = reader;

            var volumeProvider = new VolumeSampleProvider(reader.ToSampleProvider());
            volumeProvider.Volume = 3.0f*volume; // adjust the volume to compensate for the low default volume of some files

            currentDevice = new WaveOutEvent();
            currentDevice.PlaybackStopped += OnPlaybackStopped;
            currentDevice.Init(volumeProvider);
            currentDevice.Play(); // required
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, $"[PvpSoundboard] Failed to play sound: {path}");
            DisposeCurrent_NoLock();
            PlayNext_NoLock();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        lock (playLock)
        {
            try
            {
                if (currentDevice != null)
                    currentDevice.PlaybackStopped -= OnPlaybackStopped;
            }
            catch { /* ignore */ }

            DisposeCurrent_NoLock();
            PlayNext_NoLock();
        }
    }

    private void DisposeCurrent_NoLock()
    {
        if (currentDevice != null)
        {
            try { currentDevice.PlaybackStopped -= OnPlaybackStopped; }
            catch { /* ignore */ }

            try
            {
                if (currentDevice.PlaybackState != PlaybackState.Stopped)
                    currentDevice.Stop();
            }
            catch (NAudio.MmException) { /* ignore */ }
            catch { /* ignore */ }

            try { currentDevice.Dispose(); } catch { /* ignore */ }
            currentDevice = null;
        }

        try { currentReader?.Dispose(); } catch { /* ignore */ }
        currentReader = null;
    }

    public void Dispose()
    {
        lock (playLock)
        {
            disposed = true;
            queue.Clear();
            DisposeCurrent_NoLock();
            isPlaying = false;
        }
    }
}