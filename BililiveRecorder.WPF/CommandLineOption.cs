using CommandLine;

namespace BililiveRecorder.WPF
{
    public class CommandLineOption
    {
        [Option('w', "workdirectory", Default = null, HelpText = "设置工作目录并跳过选择目录 GUI", Required = false)]
        public string WorkDirectory { get; set; }
    }
}
