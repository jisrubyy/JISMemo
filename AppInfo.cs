namespace JISMemo;

public static class AppInfo
{
    public const string Version = "1.2";
    public static string FullVersion => $"{Version}.{DateTime.Now:yyyyMMdd}";
    public const string AppName = "JISMemo";
    public const string Developer = "TeamJistory";
    public const string Description = "포스트잇 스타일 메모 애플리케이션";
    public const string ContactEmail1 = "zegtern@naver.com";
    public const string ContactEmail2 = "zegtern@kakao.com";
    public static string ContactEmails => $"{ContactEmail1}, {ContactEmail2}";
}