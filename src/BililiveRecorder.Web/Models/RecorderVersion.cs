namespace BililiveRecorder.Web.Models
{
    public class RecorderVersion
    {
        public static readonly RecorderVersion Instance = new();

        public string Major { get; } = GitVersionInformation.Major;
        public string Minor { get; } = GitVersionInformation.Minor;
        public string Patch { get; } = GitVersionInformation.Patch;
        public string PreReleaseTag { get; } = GitVersionInformation.PreReleaseTag;
        public string PreReleaseTagWithDash { get; } = GitVersionInformation.PreReleaseTagWithDash;
        public string PreReleaseLabel { get; } = GitVersionInformation.PreReleaseLabel;
        public string PreReleaseLabelWithDash { get; } = GitVersionInformation.PreReleaseLabelWithDash;
        public string PreReleaseNumber { get; } = GitVersionInformation.PreReleaseNumber;
        public string WeightedPreReleaseNumber { get; } = GitVersionInformation.WeightedPreReleaseNumber;
        public string BuildMetaData { get; } = GitVersionInformation.BuildMetaData;
        public string BuildMetaDataPadded { get; } = GitVersionInformation.BuildMetaDataPadded;
        public string FullBuildMetaData { get; } = GitVersionInformation.FullBuildMetaData;
        public string MajorMinorPatch { get; } = GitVersionInformation.MajorMinorPatch;
        public string SemVer { get; } = GitVersionInformation.SemVer;
        public string LegacySemVer { get; } = GitVersionInformation.LegacySemVer;
        public string LegacySemVerPadded { get; } = GitVersionInformation.LegacySemVerPadded;
        public string AssemblySemVer { get; } = GitVersionInformation.AssemblySemVer;
        public string AssemblySemFileVer { get; } = GitVersionInformation.AssemblySemFileVer;
        public string FullSemVer { get; } = GitVersionInformation.FullSemVer;
        public string InformationalVersion { get; } = GitVersionInformation.InformationalVersion;
        public string BranchName { get; } = GitVersionInformation.BranchName;
        public string EscapedBranchName { get; } = GitVersionInformation.EscapedBranchName;
        public string Sha { get; } = GitVersionInformation.Sha;
        public string ShortSha { get; } = GitVersionInformation.ShortSha;
        public string NuGetVersionV2 { get; } = GitVersionInformation.NuGetVersionV2;
        public string NuGetVersion { get; } = GitVersionInformation.NuGetVersion;
        public string NuGetPreReleaseTagV2 { get; } = GitVersionInformation.NuGetPreReleaseTagV2;
        public string NuGetPreReleaseTag { get; } = GitVersionInformation.NuGetPreReleaseTag;
        public string VersionSourceSha { get; } = GitVersionInformation.VersionSourceSha;
        public string CommitsSinceVersionSource { get; } = GitVersionInformation.CommitsSinceVersionSource;
        public string CommitsSinceVersionSourcePadded { get; } = GitVersionInformation.CommitsSinceVersionSourcePadded;
        public string UncommittedChanges { get; } = GitVersionInformation.UncommittedChanges;
        public string CommitDate { get; } = GitVersionInformation.CommitDate;
    }
}
