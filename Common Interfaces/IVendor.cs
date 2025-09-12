using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Common_Interfaces
{
    public interface IVendor
    {
        Task<VendorViewModel> GetAllVendors(int pageNumber, int? pageSize, string? SupplierName, string? contactNo, string? NTN);
        Task<AdminSupplier> GetVendorByIdAsync(long id);
        Task<bool> CreateVendorAsync(AdminSupplier adminSupplier);
        Task<int> UpdateVendorAsync(AdminSupplier adminSupplier);
        Task<int> DeleteVendorAsync(long id);

        Task<List<AdminSupplier>> GetAllEnabledVendors();
    }
}
