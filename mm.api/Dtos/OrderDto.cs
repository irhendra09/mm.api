using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class OrderDto
    {
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public List<OrderItemDto> Items { get; set; }
    }
}
