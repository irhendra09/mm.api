namespace mm.api.Dtos
{
    public class PaymentResponseDto
    {
        public int PaymentId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public bool IsCreated { get; set; }
    }
}
