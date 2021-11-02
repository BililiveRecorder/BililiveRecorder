using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace BililiveRecorder.Flv.RuleTests
{
    public static class VerifyConfig
    {
        [ModuleInitializer]
        public static void Init()
        {
            VerifierSettings.DerivePathInfo(Expectations.Initialize);
            VerifierSettings.UseStrictJson();
            DiffRunner.Disabled = true;
        }
    }
}
