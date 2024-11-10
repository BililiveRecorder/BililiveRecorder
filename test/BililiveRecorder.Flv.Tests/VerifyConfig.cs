using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;
using VerifyXunit;

namespace BililiveRecorder.Flv.Tests
{
    public static class VerifyConfig
    {
        [ModuleInitializer]
        public static void Init()
        {
            Verifier.DerivePathInfo((string sourceFile, string projectDirectory, Type type, MethodInfo method) =>
            {
                if (type != typeof(TestData))
                    projectDirectory = Path.Combine(projectDirectory, "..", "data", "flv");

                return Expectations.Initialize(sourceFile, projectDirectory, type, method);
            });
            VerifierSettings.IgnoreMembersWithType<Stream>();
            DiffRunner.Disabled = false;
            DiffTools.UseOrder(DiffTool.VisualStudioCode, DiffTool.Rider, DiffTool.WinMerge, DiffTool.VisualStudio);
        }
    }
}
