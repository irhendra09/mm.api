namespace mm.api.Dtos
{
    public class CreateOrderResponseDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public bool IsCreated { get; set; }
    }
}
