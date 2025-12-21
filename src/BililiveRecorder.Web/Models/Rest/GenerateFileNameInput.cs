namespace BililiveRecorder.Web.Models.Rest
{
    public class GenerateFileNameInput
    {
        /// <summary>
        /// 文件名模板
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// 生成文件名使用的参数，如不提供则使用默认测试数据。
        /// </summary>
        public FileNameTemplateContextDto? Context { get; set; }
    }
}
