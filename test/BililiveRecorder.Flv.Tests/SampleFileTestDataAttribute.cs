using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VerifyTests;
using Xunit.Sdk;

namespace BililiveRecorder.Flv.Tests
{
    public class SampleFileTestDataAttribute : DataAttribute
    {
        public SampleFileTestDataAttribute(string basePath, params string[] patterns)
        {
            this.BasePath = basePath;
            this.Patterns = patterns;
            this.FullPath = Path.GetFullPath(Path.Combine(AttributeReader.GetProjectDirectory(), basePath));
        }

        public string BasePath { get; }

        public string FullPath { get; }

        public string[] Patterns { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (!Directory.Exists(this.FullPath))
                throw new ArgumentException($"Could not find directory at path: {this.FullPath}");

            return Patterns.SelectMany(x => Directory.GetFiles(this.FullPath, x)).Select(x => (new object[] { Path.GetFileName(x) }));
        }
    }
}
