using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class CustomerDto
    {
        public int CustomerId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string Address { get; set; }
    }
}
