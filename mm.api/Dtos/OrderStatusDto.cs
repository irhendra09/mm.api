using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class OrderStatusDto
    {
        [Required]
        public int OrderId { get; set; }
        [Required]
        public string Status { get; set; }
    }
}
