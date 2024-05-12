namespace mm.api.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public int Point { get; set; }
        public bool IsDeleted { get; set; }
    }
}
