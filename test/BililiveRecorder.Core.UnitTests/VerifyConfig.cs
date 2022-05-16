using System.IO;
using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace BililiveRecorder.Core.UnitTests
{
    public static class VerifyConfig
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.DerivePathInfo(Expectations.Initialize);
            VerifierSettings.ModifySerialization(_ => _.IgnoreMembersWithType<Stream>());
            DiffRunner.Disabled = true;
        }
    }
}
