namespace TeamService.Utilities
{
    public static class StringExtensionMethods
    {
        public static string NullSafeToLowerInvariant(this string? str)
        {
            return (str ?? string.Empty).ToLowerInvariant();
        }

        public static bool IsNullOrEmpty(this string? str)
        {
            return (str == null || str == string.Empty);
        }
    }
}
