using IMS.DAL.PrimaryDBContext;
using IMS.Models;

namespace IMS.Services
{
    public interface IPersonalPaymentService
    {
        Task<PersonalPaymentViewModel> GetAllPersonalPaymentsAsync(int pageNumber, int pageSize, PersonalPaymentFilters filters);
        Task<PersonalPayment?> GetPersonalPaymentByIdAsync(long id);
        Task<bool> CreatePersonalPaymentAsync(PersonalPayment personalPayment);
        Task<long> UpdatePersonalPaymentAsync(PersonalPayment personalPayment);
        Task<long> DeletePersonalPaymentAsync(long id);
        Task<List<string>> GetBankNamesAsync();
        Task<List<(long PersonalPaymentId, string BankName)>> GetBankAccountsAsync();
        Task<string?> GetBankNameByIdAsync(long personalPaymentId);
        Task<decimal> GetTotalCreditAmountAsync();
        Task<decimal> GetTotalDebitAmountAsync();
        Task<decimal> GetNetAmountAsync();
        Task<object> GetTransactionHistoryAsync(long personalPaymentId, int pageNumber, int pageSize, 
            DateTime? fromDate, DateTime? toDate, string? transactionType);
        Task<decimal> GetAccountBalanceAsync(long personalPaymentId);
        Task<bool> ProcessBankDepositAsync(long personalPaymentId, decimal amount, string description, long createdBy, DateTime paymentDate);
        Task<bool> ProcessBankWithdrawAsync(long personalPaymentId, decimal amount, string description, long createdBy, DateTime paymentDate);
    }
}
