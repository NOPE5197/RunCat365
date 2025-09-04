namespace RunCat365
{
    public enum UpdateInterval
    {
        Fast,
        Normal,
        Slow,
        VerySlow
    }

    public static class UpdateIntervalExtensions
    {
        public static int GetInterval(this UpdateInterval interval)
        {
            return interval switch
            {
                UpdateInterval.Fast => 500,
                UpdateInterval.Normal => 1000,
                UpdateInterval.Slow => 2000,
                UpdateInterval.VerySlow => 5000,
                _ => 1000,
            };
        }

        public static string GetString(this UpdateInterval interval)
        {
            return interval switch
            {
                UpdateInterval.Fast => "Fast (0.5s)",
                UpdateInterval.Normal => "Normal (1s)",
                UpdateInterval.Slow => "Slow (2s)",
                UpdateInterval.VerySlow => "Very Slow (5s)",
                _ => "Normal (1s)",
            };
        }

        public static bool TryParse(string? s, out UpdateInterval result)
        {
            if (s is null)
            {
                result = UpdateInterval.Normal;
                return false;
            }
            foreach (UpdateInterval interval in Enum.GetValues(typeof(UpdateInterval)))
            {
                if (interval.GetString() == s)
                {
                    result = interval;
                    return true;
                }
            }
            result = UpdateInterval.Normal;
            return false;
        }
    }
}
