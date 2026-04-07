namespace SV22T1020293.Admin
{
    /// <summary>
    /// Lớp biểu diễn kết quả khi gọi API
    /// </summary>
    public class ApiResult
    {
        public ApiResult(int code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>
        /// 0:looix / hoac khong thanh cong , lon hon 0 : thanh cong
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Thong bao loi (neu cos)
        /// </summary>
        public string Message { get; set; } = "";
    }
}