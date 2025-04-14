using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace DoAn2025.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Route("Admin/Brand")]
    [Authorize(Roles = "Admin,Sale")]
	public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        public BrandController(DataContext context)
        {
            _dataContext = context;

        }
		[Route("Index")]
		public async Task<IActionResult> Index()
        {
            return View(await _dataContext.BrandModels.OrderByDescending(P => P.Id).ToListAsync());
        }

		[Route("Edit")]
		public async Task<IActionResult> Edit(int Id)
        {
            BrandModel brand = await _dataContext.BrandModels.FindAsync(Id);
            return View(brand);
        }


		[Route("Create")]
		public async Task<IActionResult> Create()
        {
            return View();
        }

		[Route("Create")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(BrandModel brand)
        {

            if (ModelState.IsValid)
            {
                brand.Slug = brand.Name.Replace(" ", "-");
                var slug = await _dataContext.BrandModels.FirstOrDefaultAsync(p => p.Slug == brand.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "The brand already exists.");
                    return View(brand);
                }


                _dataContext.Add(brand);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Brand added successfully.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model has some errors.";
                List<String> errors = new List<String>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);

                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            return View(brand);

        }

		[Route("Edit")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(BrandModel brand)
        {

            if (ModelState.IsValid)
            {
                brand.Slug = brand.Name.Replace(" ", "-");
                var slug = await _dataContext.BrandModels.FirstOrDefaultAsync(p => p.Slug == brand.Slug);
                if (slug != null)
                {
                    ModelState.AddModelError("", "The brand already exists.");
                    return View(brand);
                }


                _dataContext.Update(brand);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Brand updated successfully.";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["error"] = "Model has some errors. ";
                List<String> errors = new List<String>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);

                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            return View(brand);

        }
        public async Task<IActionResult> Delete(int Id)
        {
            BrandModel brand = await _dataContext.BrandModels.FindAsync(Id);


            _dataContext.BrandModels.Remove(brand);
            await _dataContext.SaveChangesAsync();
            TempData["success"] = "Brand deleted successfully. ";
            return RedirectToAction("Index");
        }
    }
}
