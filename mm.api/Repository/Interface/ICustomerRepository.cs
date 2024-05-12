using mm.api.Dtos;
using mm.api.Models;

namespace mm.api.Repository.Interface
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int customerId);
        Task<int> InsertCustomerAsync(CustomerDto customer);
        Task UpdateCustomerAsync(CustomerDto customer);
        Task DeleteCustomerAsync(int customerId);
    }
}
