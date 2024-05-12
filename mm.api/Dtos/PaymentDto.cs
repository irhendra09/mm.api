using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class PaymentDto
    {
        [Required]
        public int OrderId { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public string PaymentType { get; set; }
    }
}
