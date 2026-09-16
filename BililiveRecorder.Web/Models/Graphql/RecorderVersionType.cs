using GraphQL.Types;

namespace BililiveRecorder.Web.Models.Graphql
{
    public class RecorderVersionType : ObjectGraphType<RecorderVersion>
    {
        public RecorderVersionType()
        {
            this.Field(x => x.Major);
            this.Field(x => x.Minor);
            this.Field(x => x.Patch);
            this.Field(x => x.PreReleaseTag);
            this.Field(x => x.PreReleaseTagWithDash);
            this.Field(x => x.PreReleaseLabel);
            this.Field(x => x.PreReleaseLabelWithDash);
            this.Field(x => x.PreReleaseNumber);
            this.Field(x => x.WeightedPreReleaseNumber);
            this.Field(x => x.BuildMetaData);
            this.Field(x => x.FullBuildMetaData);
            this.Field(x => x.MajorMinorPatch);
            this.Field(x => x.SemVer);
            this.Field(x => x.AssemblySemVer);
            this.Field(x => x.AssemblySemFileVer);
            this.Field(x => x.FullSemVer);
            this.Field(x => x.InformationalVersion);
            this.Field(x => x.BranchName);
            this.Field(x => x.EscapedBranchName);
            this.Field(x => x.Sha);
            this.Field(x => x.ShortSha);
            this.Field(x => x.VersionSourceSha);
            this.Field(x => x.CommitsSinceVersionSource);
            this.Field(x => x.UncommittedChanges);
            this.Field(x => x.CommitDate);
        }
    }
}
