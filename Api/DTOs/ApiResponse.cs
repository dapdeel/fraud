namespace AuthApi.DTOs
{
    public class ApiResponse<T>
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public ApiError Error { get; set; }
    }

    public class ApiError
    {
        public string Code { get; set; }
        public string Details { get; set; }
    }
}
