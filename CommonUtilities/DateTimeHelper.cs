namespace IMS.CommonUtilities
{
    /// <summary>
    /// Helper class for handling DateTime operations with Pakistan Standard Time (PKT)
    /// Pakistan Standard Time is UTC+5
    /// </summary>
    public static class DateTimeHelper
    {
        // Pakistan Standard Time is UTC+5
        // Try different timezone IDs for Windows and Linux compatibility
        private static readonly TimeZoneInfo PakistanTimeZone = GetPakistanTimeZone();

        private static TimeZoneInfo GetPakistanTimeZone()
        {
            // Try Windows timezone ID first
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
            }
            catch
            {
                // Try Linux timezone ID
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
                }
                catch
                {
                    // Fallback: Create custom timezone (UTC+5)
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "Pakistan Standard Time",
                        TimeSpan.FromHours(5),
                        "Pakistan Standard Time (PKT)",
                        "Pakistan Standard Time");
                }
            }
        }

        /// <summary>
        /// Gets the current date and time in Pakistan Standard Time
        /// </summary>
        public static DateTime Now => GetPakistanTime(DateTime.UtcNow);

        /// <summary>
        /// Gets the current date in Pakistan Standard Time
        /// </summary>
        public static DateTime Today => GetPakistanTime(DateTime.UtcNow).Date;

        /// <summary>
        /// Converts UTC DateTime to Pakistan Standard Time
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to convert</param>
        /// <returns>DateTime in Pakistan Standard Time</returns>
        public static DateTime GetPakistanTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                // If kind is unspecified, assume it's UTC
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            else if (utcDateTime.Kind == DateTimeKind.Local)
            {
                // Convert local to UTC first
                utcDateTime = utcDateTime.ToUniversalTime();
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, PakistanTimeZone);
        }

        /// <summary>
        /// Converts Pakistan Standard Time DateTime to UTC
        /// </summary>
        /// <param name="pakistanDateTime">Pakistan Standard Time DateTime to convert</param>
        /// <returns>DateTime in UTC</returns>
        public static DateTime GetUtcTime(DateTime pakistanDateTime)
        {
            if (pakistanDateTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's already in Pakistan time
                pakistanDateTime = DateTime.SpecifyKind(pakistanDateTime, DateTimeKind.Unspecified);
            }

            return TimeZoneInfo.ConvertTimeToUtc(pakistanDateTime, PakistanTimeZone);
        }

        /// <summary>
        /// Converts any DateTime to Pakistan Standard Time
        /// </summary>
        /// <param name="dateTime">DateTime to convert</param>
        /// <returns>DateTime in Pakistan Standard Time</returns>
        public static DateTime ToPakistanTime(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
            {
                return GetPakistanTime(dateTime);
            }
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                // Convert local to UTC, then to Pakistan time
                return GetPakistanTime(dateTime.ToUniversalTime());
            }
            else
            {
                // Unspecified - assume it's already in Pakistan time or treat as UTC
                return GetPakistanTime(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            }
        }

        /// <summary>
        /// Gets Pakistan Standard Time zone info
        /// </summary>
        public static TimeZoneInfo PakistanTimeZoneInfo => PakistanTimeZone;
    }
}

