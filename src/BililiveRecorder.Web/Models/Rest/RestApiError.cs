namespace BililiveRecorder.Web.Models.Rest
{
    public class RestApiError
    {
        public RestApiErrorCode Code { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
