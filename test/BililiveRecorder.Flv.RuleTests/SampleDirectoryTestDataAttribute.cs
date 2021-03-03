using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace BililiveRecorder.Flv.RuleTests
{
    public class SampleDirectoryTestDataAttribute : DataAttribute
    {
        public SampleDirectoryTestDataAttribute(string basePath)
        {
            this.BasePath = basePath;
        }

        public string BasePath { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            var fullPath = Path.IsPathRooted(this.BasePath) ? this.BasePath : Path.GetRelativePath(Directory.GetCurrentDirectory(), this.BasePath);

            if (!Directory.Exists(fullPath))
                throw new ArgumentException($"Could not find directory at path: {fullPath}");

            return Directory.GetDirectories(fullPath).Select(x => new object[] { x });
        }
    }
}
