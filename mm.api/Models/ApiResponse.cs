using System.Net;

namespace mm.api.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Error { get; set; }

        public ApiResponse(T data, string message = "", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            Success = true;
            StatusCode = statusCode;
            Message = message;
            Data = data;
            Error = null;
        }
        
        public ApiResponse(HttpStatusCode statusCode, string message, object error = null)
        {
            Success = false;
            StatusCode = statusCode;
            Message = message;
            Data = default(T);
            Error = error;
        }
    }
}
