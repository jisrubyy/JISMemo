using System;
using System.Reflection;

namespace JISMemo;

public static class AppInfo
{
    public const string Version = "1.6";
    public static string FullVersion => $"{Version}.{GetBuildDate(Assembly.GetExecutingAssembly()):yyyyMMdd}";
    public const string AppName = "JISMemo";
    public const string Developer = "Jisrubyy";
    public const string Description = "포스트잇 스타일 메모 애플리케이션.";
    public const string ContactEmail1 = "jisrubyy@gmail.com";
    public const string ContactEmail2 = "zegtern@kakao.com";
    public static string ContactEmails => $"{ContactEmail1}, {ContactEmail2}";

    public static DateTime GetBuildDate(Assembly assembly)
    {
        const string BuildVersionMetadataPrefix = "+build";

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(BuildVersionMetadataPrefix);
            if (index > 0)
            {
                value = value.Substring(index + BuildVersionMetadataPrefix.Length);
                if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }
        }

        return new DateTime(2000, 1, 1); // Fallback date
    }
}
