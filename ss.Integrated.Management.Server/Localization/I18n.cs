using System.Globalization;
using ss.Internal.Management.Server.Resources;

namespace ss.Internal.Management.Server.Localization;

public static class I18n
{
    public static CultureInfo CurrentCulture { get; set; } = CultureInfo.InvariantCulture;

    public static string T(string key, params object[] args)
    {
        var template = Strings.ResourceManager.GetString(key, CurrentCulture)
                       ?? $"[[{key}]]";

        return args.Length == 0
            ? template
            : string.Format(template, args);
    }
}