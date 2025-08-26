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
                if (employeesFilters == null)
                {
                    employeesFilters.FirstName = HttpContext.Request.Query["searchUsername"].ToString();
                }
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
                    employee.CreatedByUserIdFk = userId;
                    employee.CreatedDate = DateTime.Now;

                    var result = await _employeeService.CreateEmployeeAsync(employee);
                    if (result)
                    {
                        TempData["Success"] = AlertMessages.RecordAdded;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["ErrorMessage"] = AlertMessages.RecordNotAdded;
                        return View(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(nameof(Index));
            }
            return View(nameof(Index));
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
            //var empViewModel = new EmployeeViewModel();
            //var employee = employeeViewModel.EmployeesList.FirstOrDefault();
            //employee.Gender = employeeViewModel.GenderType;
            //employee.MaritalStatus = employeeViewModel.MaritalStatus;
            if (id != employee.EmployeeId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    employee.ModifiedDate = DateTime.Now;
                    var userIdStr = HttpContext.Session.GetString("UserId");
                    long userId = long.Parse(userIdStr);
                    employee.ModifiedByUserIdFk = userId;
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
                    TempData["ErrorMessage"] = ex.Message;

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
                var modifiedDate = DateTime.Now;
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
    }
}
