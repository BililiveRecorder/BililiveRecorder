using System.IO;
using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;
using VerifyXunit;

namespace BililiveRecorder.Core.UnitTests
{
    public static class VerifyConfig
    {
        [ModuleInitializer]
        public static void Init()
        {
            Verifier.DerivePathInfo(Expectations.Initialize);
            VerifierSettings.IgnoreMembersWithType<Stream>();
            DiffRunner.Disabled = false;
            DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.Rider, DiffTool.WinMerge, DiffTool.VisualStudio);
        }
    }
}
