// DisableGitVersionTask=true 时的 GitVersion 信息桩（GitVersion 正常时不会编入）。
// 与 BililiveRecorder.Desktop.csproj 的 Version 保持一致。
internal static class GitVersionInformation
{
    public const string FullSemVer = "2.0.0-local";
    public const string InformationalVersion = "2.0.0-local";
}
