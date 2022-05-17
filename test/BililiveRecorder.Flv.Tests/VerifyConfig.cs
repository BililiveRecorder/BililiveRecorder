using System.IO;
using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace BililiveRecorder.Flv.Tests
{
    public static class VerifyConfig
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.DerivePathInfo(Expectations.Initialize);
            VerifierSettings.ModifySerialization(_ => _.IgnoreMembersWithType<Stream>());
            DiffRunner.Disabled = false;
            DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.Rider, DiffTool.WinMerge, DiffTool.VisualStudio);
        }
    }
}
