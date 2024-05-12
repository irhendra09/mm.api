using mm.api.Dtos;
using mm.api.Models;

namespace mm.api.Repository.Interface
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int productId);
        Task<int> InsertProductAsync(ProductDto product);
        Task UpdateProductAsync(ProductDto product);
        Task DeleteProductAsync(int productId);
    }
}
