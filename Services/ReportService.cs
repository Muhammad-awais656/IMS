using IMS.Common_Interfaces;
using IMS.DAL;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System;
using System.Data;
using System.Net.Mail;


namespace IMS.Services
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory _dbContextFactory;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IDbContextFactory dbContextFactory, ILogger<ReportService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<ReportsViewModel> GetAllSales(int pageNumber, int? pageSize, SalesReportsFilters? salesReportsFilters)
        {
            var sale = new List<salesReportItems>();
            var customers = new List<Customer>();
            int totalRecords = 0;
            try
            {

                using (var connection = new SqlConnection(_dbContextFactory.DBConnectionString()))
                {

                    await connection.OpenAsync();
                    using (var command = new SqlCommand("GetAllSales", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@PageNo", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", pageSize);
                        command.Parameters.AddWithValue("@pIsDeleted", DBNull.Value);
                        command.Parameters.AddWithValue("@pCustomerId", (object)salesReportsFilters.CustomerId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@pBillNumber",  DBNull.Value);
                        command.Parameters.AddWithValue("@pSaleFrom", salesReportsFilters.FromDate == default(DateTime) ? DateTime.Now : salesReportsFilters.FromDate);
                        command.Parameters.AddWithValue("@pSaleDateTo", salesReportsFilters.ToDate == default(DateTime) ? DateTime.Now : salesReportsFilters.ToDate);
                        command.Parameters.AddWithValue("@pDescription",  DBNull.Value);
                        using (var reader = await command.ExecuteReaderAsync())
                        {

                            while (await reader.ReadAsync())
                            {
                                sale.Add(new salesReportItems
                                {
                                    SaleId = reader.GetInt64(reader.GetOrdinal("SaleId")),
                                    CustomerName = reader.GetString(reader.GetOrdinal("CustomerName")),
                                    BillNumber = reader.GetInt64(reader.GetOrdinal("BillNumber")),
                                    SaleDate = reader.GetDateTime(reader.GetOrdinal("SaleDate")),
                                    TotalAmount = reader.GetDecimal(reader.GetOrdinal("TotalAmount")),
                                    DiscountAmount = reader.GetDecimal(reader.GetOrdinal("DiscountAmount")),
                                    TotalReceivedAmount = reader.GetDecimal(reader.GetOrdinal("TotalReceivedAmount")),
                                    CustomerIdFk = reader.GetInt64(reader.GetOrdinal("CustomerId_FK")),
                                    TotalDueAmount = reader.GetDecimal(reader.GetOrdinal("TotalDueAmount")),
                                    SaleDescription = reader.GetString(reader.GetOrdinal("SaleDescription")),
                                  
                                });
                              

                            }

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
                _logger.LogError(ex, ex.Message);

            }



            return new ReportsViewModel
            {
                SalesList = sale,
               
                CurrentPage = pageNumber,
                TotalPages = pageSize.HasValue && pageSize.Value > 0
                   ? (int)Math.Ceiling(totalRecords / (double)pageSize.Value)
                   : 1,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
        }

        

     
    }
}
