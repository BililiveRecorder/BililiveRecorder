using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BililiveRecorder.Flv.RuleTests.Integrated
{
    public class BadTests : TestBase
    {
        [Theory(Skip = "no data yet")]
        [SampleDirectoryTestData("samples/bad")]
        public void Test(string path)
        {
            var path_input = Path.Combine(path, "input.xml");
            var path_output = Path.Combine(path, "output.xml");


        }
    }
}
