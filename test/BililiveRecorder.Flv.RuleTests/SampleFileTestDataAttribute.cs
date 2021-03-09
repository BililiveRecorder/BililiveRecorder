using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace BililiveRecorder.Flv.RuleTests
{
    public class SampleFileTestDataAttribute : DataAttribute
    {
        public SampleFileTestDataAttribute(string basePath)
        {
            this.BasePath = basePath;
        }

        public string BasePath { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var fullPath = Path.IsPathRooted(this.BasePath) ? this.BasePath : Path.GetRelativePath(Directory.GetCurrentDirectory(), this.BasePath);

            if (!Directory.Exists(fullPath))
                throw new ArgumentException($"Could not find directory at path: {fullPath}");

            return new[] { "*.xml", "*.gz" }.SelectMany(x => Directory.GetFiles(fullPath, x)).Select(x => new object[] { x });
        }
    }
}
