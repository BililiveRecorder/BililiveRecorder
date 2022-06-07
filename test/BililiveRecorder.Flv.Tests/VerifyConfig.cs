using System;
using System.IO;
using System.Reflection;
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
            VerifierSettings.DerivePathInfo((string sourceFile, string projectDirectory, Type type, MethodInfo method) =>
            {
                if (type != typeof(PublicApi) && type != typeof(TestData))
                    projectDirectory = Path.Combine(projectDirectory, "..", "data", "flv");

                return Expectations.Initialize(sourceFile, projectDirectory, type, method);
            });
            VerifierSettings.ModifySerialization(_ => _.IgnoreMembersWithType<Stream>());
            DiffRunner.Disabled = false;
            DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.Rider, DiffTool.WinMerge, DiffTool.VisualStudio);
        }
    }
}
