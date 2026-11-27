namespace FrierenPortrait;

// Game time for Frieren's timers that belong to the game (with Core's chance on the seed): the campaign's tick,
// 60 ticks per game second, so a pause stops them and the game speed moves them as for every player. 0 without a campaign. The
// game's main flow is looked up once and again only when it is gone.
internal static class FrierenGameClock
{
    private const float TicksPerSecond = 60f;
    private static MainFlow flow;

    internal static float Now
    {
        get
        {
            try
            {
                if (flow == null || !flow) flow = UnityEngine.Object.FindObjectOfType<MainFlow>();
                var tick = flow?._campaignData?.TimeData?.Tick ?? 0;
                return tick / TicksPerSecond;
            }
            catch (Exception) { return 0f; }
        }
    }
}
