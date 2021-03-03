using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    public class TempTest
    {
        [Theory]
        [SampleDirectoryTestData("samples")]
        public void Test(string path)
        {
            string.IsNullOrWhiteSpace(path);
        }
    }
}
