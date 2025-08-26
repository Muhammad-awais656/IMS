using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IProductService
    {
        //Task<ProductViewModel> GetAllProductAsync(int pageNumber, int? pageSize, ProductFilters productFilters);

        Task<ProductViewModel> GetAllProductAsync(int pageNumber, int? pageSize, ProductFilters productFilters);
        Task<ProductViewModel> GetProductByIdAsync(long id);
        Task<bool> CreateProductAsync(Product product);
        Task<int> UpdateProductAsync(Product product);
        Task<int> DeleteProductAsync(long id);

        
    }
}
