using System.Text;

namespace ContentForge.Editor
{
    /// <summary>Turns a display name into a stable, file-safe slug used as the diff key
    /// and the generated asset's file name.</summary>
    internal static class ContentNaming
    {
        public static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(name.Length);
            var pendingDash = false;
            foreach (var ch in name.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (pendingDash && sb.Length > 0)
                    {
                        sb.Append('-');
                    }
                    sb.Append(ch);
                    pendingDash = false;
                }
                else
                {
                    pendingDash = true;
                }
            }

            return sb.ToString();
        }
    }
}
