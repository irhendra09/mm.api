using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class OrderItemDto
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int Quantity { get; set; }
    }
}
