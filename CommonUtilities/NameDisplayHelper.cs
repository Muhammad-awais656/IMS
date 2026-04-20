namespace IMS.CommonUtilities
{
    public static class NameDisplayHelper
    {
        /// <summary>English name with Urdu in parentheses when both are present, e.g. Beej(بیج).</summary>
        public static string EnglishWithUrdu(string? english, string? urdu)
        {
            var e = english?.Trim() ?? "";
            var u = urdu?.Trim();
            if (string.IsNullOrEmpty(u))
                return string.IsNullOrEmpty(e) ? "" : e;
            if (string.IsNullOrEmpty(e))
                return u;
            return $"{e}({u})";
        }

        /// <summary>Urdu when present; otherwise English (same pattern as other screens).</summary>
        public static string UrduOrEnglish(string? english, string? urdu)
        {
            var u = urdu?.Trim();
            if (!string.IsNullOrEmpty(u))
                return u;
            return english?.Trim() ?? "";
        }

        /// <summary>English-name column; placeholder when empty.</summary>
        public static string EnglishNameCell(string? english, string emptyPlaceholder = "-")
        {
            var e = english?.Trim();
            return string.IsNullOrEmpty(e) ? emptyPlaceholder : e;
        }

        /// <summary>Urdu-name column; placeholder when empty.</summary>
        public static string UrduNameCell(string? urdu, string emptyPlaceholder = "-")
        {
            var u = urdu?.Trim();
            return string.IsNullOrEmpty(u) ? emptyPlaceholder : u;
        }
    }
}
