using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Net.Mail;

namespace IMS.Services
{
    public class VendorService : IVendor
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<VendorService> _logger;

        public VendorService(IDbContextFactory dbContextFactory, ILogger<VendorService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> CreateVendorAsync(AdminSupplier vendor)
        {
            bool response = false;
            try
            {
                
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("AddSupplier", connection))
                    {
                         
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierName", vendor.SupplierName);
                        command.Parameters.AddWithValue("@pSupplierDescription", vendor.SupplierDescription);
                        command.Parameters.AddWithValue("@pSupplierPhoneNumber", vendor.SupplierPhoneNumber);
                        command.Parameters.AddWithValue("@pSupplierNTN", vendor.SupplierNtn);
                        command.Parameters.AddWithValue("@pSupplierEmail", vendor?.SupplierEmail != null ? vendor?.SupplierEmail : DBNull.Value);
                        command.Parameters.AddWithValue("@pSupplierAddress", vendor?.SupplierAddress != null ? vendor?.SupplierAddress : DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedDate", vendor?.CreatedDate == default(DateTime) ? DBNull.Value : vendor?.CreatedDate);
                        command.Parameters.AddWithValue("@pModifiedDate", vendor?.ModifiedDate == default(DateTime) ? DBNull.Value : vendor?.ModifiedDate);
                        command.Parameters.AddWithValue("@pIsDeleted", vendor?.IsDeleted != null ? vendor?.IsDeleted: DBNull.Value);
                        command.Parameters.AddWithValue("@pCreatedBy", vendor?.CreatedBy != null ? vendor.CreatedBy : DBNull.Value);
                        command.Parameters.AddWithValue("@pModifiedBy", vendor?.ModifiedBy != null ? vendor.ModifiedBy : DBNull.Value);
                        
                        var VEndorIdParam = new SqlParameter("@pSupplierId", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(VEndorIdParam);
                        await command.ExecuteNonQueryAsync();
                        long newAdmincategoryIdParam = (long)VEndorIdParam.Value;
                        if (newAdmincategoryIdParam != 0)
                        {
                            response = true;
                        }

                    }
                }
            }
            catch
            {

            }
            return response;
        }

        public async Task<int> DeleteVendorAsync(long id)
        {
            int response = 0;
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("DeleteSupplier", connection))
                    {
                        command.Parameters.AddWithValue("@pSupplierId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        var RowAffected = new SqlParameter("@RowsAffected", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(RowAffected);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected != 0)
                        {
                            response = rowsAffected;
                        }

                    }
                }
            }
            catch
            {

            }
            return response;
        }

        public async Task<AdminSupplier> GetVendorByIdAsync(long id)
        {
            AdminSupplier supplier = new AdminSupplier();
            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetSupplier", connection))
                    {
                        command.Parameters.AddWithValue("@pSupplierId", id);
                        command.CommandType = CommandType.StoredProcedure;
                        try
                        {
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    return new AdminSupplier
                                    {
                                        SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
                                        SupplierName=reader.GetString(reader.GetOrdinal("SupplierName")),
                                        SupplierDescription =reader.GetString(reader.GetOrdinal("SupplierDescription")),
                                        SupplierPhoneNumber=reader.GetString(reader.GetOrdinal("SupplierPhoneNumber")),
                                        SupplierNtn = reader.GetString(reader.GetOrdinal("SupplierNTN")),
                                        SupplierEmail=reader.GetString(reader.GetOrdinal("SupplierEmail")),
                                        SupplierAddress=reader.GetString(reader.GetOrdinal("SupplierAddress")),
                                        IsDeleted=reader.GetBoolean(reader.GetOrdinal("IsDeleted")),
                                        CreatedBy=reader.GetInt64(reader.GetOrdinal("CreatedBy")),
                                        CreatedDate= reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                        ModifiedBy= reader.GetInt64(reader.GetOrdinal("ModifiedBy")),
                                        ModifiedDate= reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
                                    };
                                }
                            }
                        }
                        catch
                        {

                        }

                    }
                }
            }
            catch
            {

            }
            return supplier;
        }

        public async Task<VendorViewModel> GetAllVendors(int pageNumber, int? pageSize, string? SupplierName, string? contactNo, string? NTN)
        {
            var vendor = new List<AdminSupplier>();
            int totalRecords = 0;
            try
            {
               
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                    
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@pSupplierName", (object)SupplierName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pContactNumber", (object)contactNo ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pNTN", (object)NTN ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pIsDeleted", DBNull.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                vendor.Add(new AdminSupplier
                                {
                                    SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
                                    SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                                    SupplierPhoneNumber = reader.IsDBNull(reader.GetOrdinal("SupplierPhoneNumber")) ? null : reader.GetString(reader.GetOrdinal("SupplierPhoneNumber")),
                                    SupplierNtn = reader.IsDBNull(reader.GetOrdinal("SupplierNTN")) ? null : reader.GetString(reader.GetOrdinal("SupplierNtn")),
                                    SupplierEmail = reader.IsDBNull(reader.GetOrdinal("SupplierEmail")) ? null : reader.GetString(reader.GetOrdinal("SupplierEmail")),
                                    SupplierAddress = reader.GetString(reader.GetOrdinal("SupplierAddress")),
                                    IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
                                });
                            }
                            // Move to second result set for total count
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
                _logger.LogError(ex, "Error fetching customers");

            }



            return new VendorViewModel
            {
                VendorList = vendor,
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
        }

        public async Task<List<AdminSupplier>> GetAllEnabledVendors()
        {
            var vendor = new List<AdminSupplier>();
           
            try
            {

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllEnabledSupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        
                    
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                vendor.Add(new AdminSupplier
                                {
                                    SupplierId = reader.GetInt64(reader.GetOrdinal("SupplierId")),
                                    SupplierName = reader.GetString(reader.GetOrdinal("SupplierName")),
                                    
                                });
                            }
                           
                            

                        }
                    }



                }


            }
            catch 
            {
                

            }



            return vendor;
        }

        public async Task<int> UpdateVendorAsync(AdminSupplier customer)
        {
            var RowsAffectedResponse = 0;


            try
            {
                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {
                     


                    await connection.OpenAsync();
                    using (var command = new SqlCommand("ModifySupplier", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pSupplierName", customer.SupplierName);
                        command.Parameters.AddWithValue("@pSupplierDescription", customer.SupplierDescription);
                        command.Parameters.AddWithValue("@pSupplierPhoneNumber", customer.SupplierPhoneNumber);
                        command.Parameters.AddWithValue("@pSupplierNTN", customer.SupplierNtn);
                        command.Parameters.AddWithValue("@pSupplierEmail", customer.SupplierEmail);
                        command.Parameters.AddWithValue("@pSupplierAddress", customer.SupplierAddress);
                        command.Parameters.AddWithValue("@pIsDeleted", customer.IsDeleted);
                       
                        command.Parameters.AddWithValue("@pModifiedDate", customer?.ModifiedDate == default(DateTime) ? DBNull.Value : customer?.ModifiedDate);
                        command.Parameters.AddWithValue("@pModifiedBy", customer?.ModifiedBy);
                        command.Parameters.AddWithValue("@pSupplierId", customer?.SupplierId);

                        var expenseTypeidParam = new SqlParameter("@RowsAffected", SqlDbType.BigInt)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(expenseTypeidParam);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected != 0)
                        {
                            RowsAffectedResponse = rowsAffected;
                        }
                    }
                }
            }
            catch
            {
            }
            return RowsAffectedResponse;
        }
    }
}
