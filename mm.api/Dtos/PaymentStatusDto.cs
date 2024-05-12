using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class PaymentStatusDto
    {
        [Required]
        public int PaymentId { get; set; }
        [Required]
        public string Status { get; set; }
    }
}
