using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IMS.Controllers
{
    public class VendorBillsController : Controller
    {
        // GET: VendorBillsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: VendorBillsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: VendorBillsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: VendorBillsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: VendorBillsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: VendorBillsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: VendorBillsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: VendorBillsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
