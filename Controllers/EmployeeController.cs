using IMS.CommonUtilities;
using IMS.Common_Interfaces;
using IMS.CommonUtilities;
using IMS.DAL.PrimaryDBContext;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private const int DefaultPageSize = 5; // Default page size
        private static readonly int[] AllowedPageSizes = { 5, 10, 25 }; // Allowed page sizes
        private readonly IEmployeeService _employeeService;
        public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
        {

            _employeeService = employeeService;
            _logger = logger;
        }
        // GET: EmployeeController
        public async Task<ActionResult> Index(int pageNumber = 1, int? pageSize = null, EmployeesFilters? employeesFilters = null)
        {
            var viewModel = new EmployeeViewModel();

            try
            {
                int currentPageSize = HttpContext.Session.GetInt32("UserPageSize") ?? DefaultPageSize;
                if (pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value))
                {
                    currentPageSize = pageSize.Value;
                    HttpContext.Session.SetInt32("UserPageSize", currentPageSize);
                }
                
                // Get search parameter from query string
                var searchUsername = HttpContext.Request.Query["searchUsername"].ToString();
                
                if (employeesFilters == null)
                {
                    employeesFilters = new EmployeesFilters
                    {
                        FirstName = searchUsername
                    };
                }
                else if (string.IsNullOrEmpty(employeesFilters.FirstName))
                {
                    employeesFilters.FirstName = searchUsername;
                }
                
                // Preserve search value in ViewData for pagination links
                ViewData["searchUsername"] = searchUsername;
                
                viewModel = await _employeeService.GetAllEmployeesAsync(pageNumber, currentPageSize, employeesFilters);
               
                   
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }
            return View(viewModel);
        }

        // GET: EmployeeController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: EmployeeController/Create
        public ActionResult Create()
        {
            var genderTypes = new List<SelectListItem>
    {
        new SelectListItem { Value = "Male", Text = "Male" },
        new SelectListItem { Value = "Female", Text = "Female" },
        new SelectListItem { Value = "Not Specified", Text = "Not Specified" }
    };
            ViewBag.GenderTypes = genderTypes;
            var maritalStatus = new List<SelectListItem>
    {
        new SelectListItem { Value = "Single", Text = "Single" },
        new SelectListItem { Value = "Married", Text = "Married" },
        new SelectListItem { Value = "Widowed", Text = "Widowed" },
        new SelectListItem { Value = "Separated", Text = "Separated" },
        new SelectListItem { Value = "Divorced", Text = "Divorced" }
    };
            ViewBag.MaritalStatuses = maritalStatus;

            return View();
        }

        // POST: EmployeeController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Employee employee)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    // Handle null values for optional fields
                    employee.CreatedByUserIdFk = userId;
                    employee.CreatedDate = DateTimeHelper.Now;
                    employee.ModifiedByUserIdFk = userId;
                    employee.ModifiedDate = DateTimeHelper.Now;
                    
                    // Ensure optional fields are properly handled (Address is now required)
                    if (string.IsNullOrWhiteSpace(employee.LastName))
                        employee.LastName = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.Cnic))
                        employee.Cnic = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.EmailAddress))
                        employee.EmailAddress = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.MaritalStatus))
                        employee.MaritalStatus = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.HusbandFatherName))
                        employee.HusbandFatherName = null;
                    
                    if (employee.Salary == 0)
                        employee.Salary = null;

                    var result = await _employeeService.CreateEmployeeAsync(employee);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(employee);
                    }
                }
                else
                {
                    // Log validation errors for debugging
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View(employee);
            }
            return View(employee);
        }

        // GET: EmployeeController/Edit/5
        public async Task<ActionResult> Edit(long id)
        {
            var empViewModel = new EmployeeViewModel();
            var emp = await _employeeService.GetEmployeeByIdAsync(id);
            //empViewModel.EmployeesList = new List<Employee> { emp };
            //empViewModel.MaritalStatus = emp.MaritalStatus;
            //empViewModel.GenderType = emp.Gender;
            ViewBag.GenderTypes = new SelectList(
        new List<SelectListItem>
        {
            new SelectListItem { Value = "Male", Text = "Male" },
            new SelectListItem { Value = "Female", Text = "Female" },
            new SelectListItem { Value = "Not Specified", Text = "Not Specified" }
        },
        "Value",
        "Text",
        emp.Gender
    );

            ViewBag.MaritalStatuses = new SelectList(
                new List<SelectListItem>
{
        new SelectListItem { Value = "Single", Text = "Single" },
        new SelectListItem { Value = "Married", Text = "Married" },
        new SelectListItem { Value = "Widowed", Text = "Widowed" },
        new SelectListItem { Value = "Separated", Text = "Separated" },
        new SelectListItem { Value = "Divorced", Text = "Divorced" }
                },
"Value",
"Text",
emp.MaritalStatus
);
            if (emp == null)
            {
                return NotFound();
            }
            return View(emp);
        }

        // POST: EmployeeController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(long id, Employee employee)
        {
            if (id != employee.EmployeeId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    
                    // Handle null values for optional fields
                    employee.ModifiedDate = DateTimeHelper.Now;
                    employee.ModifiedByUserIdFk = userId;
                    
                    // Ensure optional fields are properly handled (Address is now required)
                    if (string.IsNullOrWhiteSpace(employee.LastName))
                        employee.LastName = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.Cnic))
                        employee.Cnic = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.EmailAddress))
                        employee.EmailAddress = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.MaritalStatus))
                        employee.MaritalStatus = null;
                    
                    if (string.IsNullOrWhiteSpace(employee.HusbandFatherName))
                        employee.HusbandFatherName = null;
                    
                    if (employee.Salary == 0)
                        employee.Salary = null;
                    
                    var response = await _employeeService.UpdateEmployeeAsync(employee);
                    
                    if (response != 0)
                    {
                        TempData["Success"] = AlertMessages.RecordUpdated;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotUpdated;
                        return View(employee);
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                    return View(employee);
                }
            }
            else
            {
                // Log validation errors for debugging
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                }
            }
            return View(employee);
        }

        // GET: EmployeeController/Delete/5
        public async Task<ActionResult> Delete(long id)
        {
            var employee = new Employee();
            try
            {
                employee = await _employeeService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    return NotFound();
                }


            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return View(employee);
        }

        // POST: EmployeeController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(long id)
        {
            try
            {
                var userIdStr = HttpContext.Session.GetString("UserId");
                long userId = long.Parse(userIdStr);
                var modifiedDate = DateTimeHelper.Now;
                var res = await _employeeService.DeleteEmployeeAsync(id, modifiedDate, userId);
                if (res != 0)
                {
                    TempData["Success"] = AlertMessages.RecordDeleted;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = AlertMessages.RecordNotDeleted;
                    return RedirectToAction(nameof(Index));
                }

            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: Employee/GetTransactionHistory - Returns JSON for employee ledger (transaction history) modal.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTransactionHistory(long employeeId, int pageNumber = 1, int pageSize = 10,
            DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var allLedger = await _employeeService.GetEmployeeLedgerReportAsync(employeeId);
                var list = allLedger ?? new List<EmployeeLedgerReportVM>();

                if (fromDate.HasValue)
                    list = list.Where(x => x.VoucherDate.Date >= fromDate.Value.Date).ToList();
                if (toDate.HasValue)
                    list = list.Where(x => x.VoucherDate.Date <= toDate.Value.Date).ToList();

                int totalCount = list.Count;
                int totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
                pageNumber = Math.Max(1, Math.Min(pageNumber, totalPages));

                var paged = list
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        voucherDate = x.VoucherDate,
                        voucherTypeName = x.VoucherTypeName ?? "",
                        referenceNo = x.ReferenceNo ?? "",
                        debitAmount = x.DebitAmount,
                        creditAmount = x.CreditAmount,
                        runningBalance = x.RunningBalance,
                        remarks = x.Remarks ?? ""
                    })
                    .ToList();

                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
                decimal currentBalance = await _employeeService.GetEmployeeBalanceAsync(employeeId);

                return Json(new
                {
                    transactions = paged,
                    totalCount,
                    currentPage = pageNumber,
                    totalPages,
                    employeeSummary = new
                    {
                        employeeName = employee != null ? (employee.FirstName + " " + (employee.LastName ?? "")).Trim() : "",
                        currentBalance,
                        transactionCount = totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error loading transaction history: " + ex.Message,
                    transactions = new List<object>(),
                    totalCount = 0,
                    currentPage = 1,
                    totalPages = 0,
                    employeeSummary = new { employeeName = "", currentBalance = 0m, transactionCount = 0 }
                });
            }
        }

     

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddLedgerEntry(EmployeeLedgerEntryVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Get logged-in user
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    if (string.IsNullOrEmpty(userIdStr))
                    {
                        TempData["ErrorMessage"] = "Session expired. Please login again.";
                        return RedirectToAction("Login", "Account");
                    }

                    long userId = long.Parse(userIdStr);

                    // Get voucher type
                    //var voucherType = await _context.EmployeeVoucherTypes
                    //    .FirstOrDefaultAsync(x => x.VoucherTypeId == model.VoucherTypeId);

                    var voucherType = await _employeeService.GetVoucherTypeByIdAsync(model.VoucherTypeId);

                    if (voucherType == null)
                    {
                        TempData["ErrorMessage"] = "Invalid voucher type selected.";
                        return View(model);
                    }

                    // Amount validation
                    if (model.Amount <= 0)
                    {
                        TempData["ErrorMessage"] = "Amount must be greater than zero.";
                        return View(model);
                    }

                    // Opening balance validation (only once per employee)
                    if (voucherType.VoucherTypeName == "Opening Balance")
                    {
                        bool openingExists = await _employeeService
                  .IsOpeningBalanceExistsAsync(model.EmployeeId, model.VoucherTypeId);

                        if (openingExists)
                        {
                            TempData["ErrorMessage"] = "Opening balance already exists for this employee.";
                            return View(model);
                        }
                    }

                    // Prepare ledger entity
                    var ledger = new EmployeeLedger
                    {
                        EmployeeId = model.EmployeeId,
                        VoucherTypeId = model.VoucherTypeId,
                        VoucherDate = model.VoucherDate,
                        ReferenceNo = string.IsNullOrWhiteSpace(model.ReferenceNo) ? null : model.ReferenceNo,
                        DebitAmount = voucherType.Nature == "D" ? model.Amount : 0,
                        CreditAmount = voucherType.Nature == "C" ? model.Amount : 0,
                        Remarks = string.IsNullOrWhiteSpace(model.Remarks) ? null : model.Remarks,
                        CreatedBy = userId
                    };

                    var result = await _employeeService.AddEmployeeLedgerAsync(ledger);

                    TempData["Success"] = "Ledger entry saved successfully.";
                    return RedirectToAction("EmployeeLedger", new { employeeId = model.EmployeeId });
                }
                else
                {
                    // Log validation errors (same pattern as your Create method)
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return View(model);
            }

            return View(model);
        }


   

        [HttpGet]
        public async Task<ActionResult> EmployeeLedger(long? employeeId = null, int pageNumber = 1, int? pageSize = null)
        {
            var ledger = await _employeeService.GetAllEmployeeLedgerReportAsync();
            var list = ledger ?? new List<EmployeeLedgerReportVM>();

            // Filter by selected employee when employeeId is provided
            if (employeeId.HasValue && employeeId.Value > 0)
            {
                list = list.Where(x => x.EmployeeId_FK == employeeId.Value).ToList();
            }

            int totalCount = list.Count;
            int currentPageSize = pageSize ?? 25;
            if (currentPageSize < 1) currentPageSize = 25;
            int totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)currentPageSize) : 1;
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages) pageNumber = totalPages;

            var pagedList = list
                .Skip((pageNumber - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToList();

            var viewModel = new EmployeeViewModel
            {
                EmployeeLedgerList = pagedList,
                TotalCount = totalCount,
                PageSize = currentPageSize,
                TotalPages = totalPages,
                CurrentPage = pageNumber
            };
            ViewData["employeeId"] = employeeId?.ToString() ?? "";
            ViewBag.Employees = new SelectList(
                await _employeeService.GetAllEmployeesAsync(),
                "EmployeeId",
                "EmployeeName",
                employeeId);
            return View(viewModel);
        }
        [HttpGet]
        public async Task<ActionResult> AddLedgerEntry(long employeeId)
        {
            try
            {
                
                ViewBag.Employees = new SelectList(
                    await _employeeService.GetAllEmployeesAsync(),
                    "EmployeeId",
                    "EmployeeName",
                    employeeId);

                ViewBag.VoucherTypes = new SelectList(
                    await _employeeService.GetAllVoucherTypesAsync(),
                    "VoucherTypeId",
                    "VoucherTypeName");

                return View(new EmployeeLedgerEntryVM
                {
                    EmployeeId = employeeId,
                    VoucherDate = DateTimeHelper.Today
                });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index", "Employee");
            }
        }
    

        private async Task LoadDropdownsAsync(long? employeeId = null)
        {
            ViewBag.Employees = new SelectList(
                await _employeeService.GetAllEmployeesAsync(),
                "EmployeeId",
                "FirstName",
                employeeId);

            ViewBag.VoucherTypes = new SelectList(
                await _employeeService.GetAllVoucherTypesAsync(),
                "VoucherTypeId",
                "VoucherTypeName");
        }




    }
}
