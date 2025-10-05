using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace IMS.Services
{
    public class VendorPaymentService : IVendorPaymentService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<VendorPaymentService> _logger;

        public VendorPaymentService(IDbContextFactory dbContextFactory, ILogger<VendorPaymentService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<VendorPaymentViewModel> GetAllBillPaymentsAsync(int pageNumber, int? pageSize, VendorPaymentFilters? filters)
        {
            var viewModel = new VendorPaymentViewModel();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllBillPayments", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                        // Set parameters for the stored procedure
                        command.Parameters.AddWithValue("@pBillId", filters?.BillId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pSupplierId", filters?.VendorId ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pPaymentDateFrom", filters?.BillDateFrom ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@pPaymentDateTo", filters?.BillDateTo ?? (object)DBNull.Value);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var paymentsList = new List<VendorBillViewModel>();
                            decimal totalAmount = 0;
                            decimal totalDiscountAmount = 0;
                            decimal totalPaidAmount = 0;
                            decimal totalPayableAmount = 0;

                            // Read payment data
                            while (await reader.ReadAsync())
                            {
                                var payment = new VendorBillViewModel
                                {
                                    BillId = reader.IsDBNull(reader.GetOrdinal("BillId")) ? 0 : reader.GetInt64(reader.GetOrdinal("BillId")),
                                    BillNumber = reader.IsDBNull(reader.GetOrdinal("BillNumber")) ? 0 : reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    VendorName = reader.IsDBNull(reader.GetOrdinal("SupplierName")) ? string.Empty : reader.GetString(reader.GetOrdinal("SupplierName")),
                                    BillDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("PaymentDate")),
                                    TotalAmount = reader.IsDBNull(reader.GetOrdinal("PaymentAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("PaymentAmount")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description"))
                                };

                                paymentsList.Add(payment);

                                // Calculate totals
                                totalAmount += payment.TotalAmount;
                                totalPaidAmount += payment.TotalAmount;
                            }

                            viewModel.BillsList = paymentsList;
                            viewModel.TotalAmount = totalAmount;
                            viewModel.TotalDiscountAmount = totalDiscountAmount;
                            viewModel.TotalPaidAmount = totalPaidAmount;
                            viewModel.TotalPayableAmount = totalPayableAmount;
                            viewModel.CurrentPage = pageNumber;
                            viewModel.PageSize = pageSize ?? 10;
                            viewModel.TotalCount = paymentsList.Count;
                            viewModel.TotalPages = (int)Math.Ceiling((double)paymentsList.Count / (pageSize ?? 10));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all bill payments");
                throw;
            }
            return viewModel;
        }

        public async Task<List<AdminSupplier>> GetAllVendorsAsync()
        {
            var vendors = new List<AdminSupplier>();
            int totalRecords = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", 1);
                        command.Parameters.AddWithValue("@PageSize", 1000); // Get all vendors for dropdown
                        command.Parameters.AddWithValue("@pSupplierName", DBNull.Value);
                        command.Parameters.AddWithValue("@pContactNumber", DBNull.Value);
                        command.Parameters.AddWithValue("@pNTN", DBNull.Value);
                        command.Parameters.AddWithValue("@pIsDeleted", false);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var vendor = new AdminSupplier
                                {
                                    SupplierId = reader.GetInt64("SupplierId"),
                                    SupplierName = reader.GetString("SupplierName")
                                };
                                vendors.Add(vendor);
                            }
                            
                            // Move to second result set (total count) - we don't need it for this method
                            await reader.NextResultAsync();

                            if (await reader.ReadAsync())
                            {
                                totalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all vendors");
                throw;
            }
            return vendors;
        }

        public async Task<List<SupplierBillNumber>> GetSupplierBillNumbersAsync(long supplierId)
        {
            var billNumbers = new List<SupplierBillNumber>();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    
                    using (var command = new SqlCommand("GetAllSupplierBillNumbers", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierId", supplierId);
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var billNumber = new SupplierBillNumber
                                {
                                    BillNumber = reader.GetInt64("BillNumber"),
                                    PurchaseOrderId = reader.GetInt64("PurchaseOrderId"),
                                    TotalDueAmount = reader.GetDecimal("TotalDueAmount")
                                };
                                billNumbers.Add(billNumber);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supplier bill numbers");
                throw;
            }
            return billNumbers;
        }
    }
}
