using Microsoft.AspNetCore.Mvc;
using mm.api.Dtos;
using mm.api.Models;
using mm.api.Repository;
using System.Net;

namespace mm.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly CustomerRepository _customerRepository;
        public CustomerController(CustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }
        // GET: api/customer
        [HttpGet]
        public async Task<ApiResponse<List<Customer>>> GetAllCustomers()
        {
            try
            {
                var customers = await _customerRepository.GetAllCustomersAsync();
                return new ApiResponse<List<Customer>>(customers, "Retrieved all customers successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<Customer>>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // GET: api/customer/5
        [HttpGet("{id}")]
        public async Task<ApiResponse<Customer>> GetCustomerById(int id)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return new ApiResponse<Customer>(HttpStatusCode.NotFound, "Customer not found.");
                }
                return new ApiResponse<Customer>(customer, "Customer retrieved successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<Customer>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // POST: api/customer
        [HttpPost]
        public async Task<ApiResponse<CustomerResponseDto>> CreateCustomer([FromBody] CustomerDto customerDto)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<CustomerResponseDto>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            try
            {
                var customerId = await _customerRepository.InsertCustomerAsync(customerDto);
                var responseDTO = new CustomerResponseDto { CustomerId = customerId };
                return new ApiResponse<CustomerResponseDto>(responseDTO, "Customer Created Successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<CustomerResponseDto>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // PUT: api/customer/5
        [HttpPut("{id}")]
        public async Task<ApiResponse<bool>> UpdateCustomer(int id, [FromBody] CustomerDto customerDto)
        {
            if (!ModelState.IsValid)
            {
                return new ApiResponse<bool>(HttpStatusCode.BadRequest, "Invalid data", ModelState);
            }
            if (id != customerDto.CustomerId)
            {
                return new ApiResponse<bool>(HttpStatusCode.BadRequest, "Mismatched Customer ID");
            }
            try
            {
                await _customerRepository.UpdateCustomerAsync(customerDto);
                return new ApiResponse<bool>(true, "Customer Updated Successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
        // DELETE: api/customer/5
        [HttpDelete("{id}")]
        public async Task<ApiResponse<bool>> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerRepository.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return new ApiResponse<bool>(HttpStatusCode.NotFound, "Customer not found.");
                }
                await _customerRepository.DeleteCustomerAsync(id);
                return new ApiResponse<bool>(true, "Customer deleted successfully.");
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>(HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
            }
        }
    }
}
