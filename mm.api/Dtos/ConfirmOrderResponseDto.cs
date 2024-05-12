namespace mm.api.Dtos
{
    public class ConfirmOrderResponseDto
    {
        public int OrderId { get; set; }
        public bool IsConfirmed { get; set; }
        public string Message { get; set; }
    }
}
