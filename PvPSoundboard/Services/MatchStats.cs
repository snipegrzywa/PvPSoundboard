namespace PvPSoundboard;

public class MatchStats
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public int BattleHighLevel { get; set; }
    public int Killstreak { get; set; }

    public void Reset()
    {
        Kills = 0;
        Deaths = 0;
        Assists = 0;
        BattleHighLevel = 0;
        Killstreak = 0;
    }
}