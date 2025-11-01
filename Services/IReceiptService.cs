using IMS.Models;

namespace IMS.Services
{
    public interface IReceiptService
    {
        Task<string> GenerateReceiptAsync(ReceiptGenerationRequest request);
        Task<byte[]> GenerateReceiptPdfAsync(ReceiptGenerationRequest request);
        Task<List<ReceiptTemplate>> GetAvailableTemplatesAsync();
        Task<ReceiptData> GetSalesReceiptDataAsync(long saleId);
        Task<SalesReceiptData> GetSalesReceiptDetailedDataAsync(long saleId);
    }
}
