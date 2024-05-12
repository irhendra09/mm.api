using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository;
using System.Net;

namespace mm.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductRepository _productRepository;
        public ProductController(ProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        [Authorize(Roles = "Admin,User")]
        [HttpGet]
        public async Task<ApiResponse<List<Product>>> GetAllProducts()
        {
            try
            {
                var products = await _productRepository.GetAllProductsAsync();
                return new ApiResponse<List<Product>>(products, "Retrieved all products successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Product>>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("{id}")]
        public async Task<ApiResponse<Product>> GetProductById(int id)
        {
            try
            {
                var product = await _productRepository.GetProductByIdAsync(id);
                if (product == null)
                {
                    return new ApiResponse<Product>(HttpStatusCode.NotFound, "Product not found.");
                }
                return new ApiResponse<Product>(product, "Product retrieved successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Product>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ApiResponse<ProductResponseDto>> CreateProduct([FromBody] ProductDto product)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<ProductResponseDto>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            try
            {
                var productId = await _productRepository.InsertProductAsync(product);
                var responseDTO = new ProductResponseDto { ProductId = productId };
                return new ApiResponse<ProductResponseDto>(responseDTO, "Product created successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<ProductResponseDto>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ApiResponse<bool>> UpdateProduct(int id, [FromBody] ProductDto product)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<bool>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            if (id != product.ProductId)
            {
                return new ApiResponse<bool>(HttpStatusCode.BadRequest, "Mismatched product ID");
            }
            try
            {
                await _productRepository.UpdateProductAsync(product);
                return new ApiResponse<bool>(true, "Product updated successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ApiResponse<bool>> DeleteProduct(int id)
        {
            try
            {
                var product = await _productRepository.GetProductByIdAsync(id);
                if (product == null)
                {
                    return new ApiResponse<bool>(HttpStatusCode.NotFound, "Product not found.");
                }
                await _productRepository.DeleteProductAsync(id);
                return new ApiResponse<bool>(true, "Product deleted successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
    }
}
