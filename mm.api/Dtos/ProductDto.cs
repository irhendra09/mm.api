using System.ComponentModel.DataAnnotations;

namespace mm.api.Dtos
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int Stock { get; set; }
    }
}
