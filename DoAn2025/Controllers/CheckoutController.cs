using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace DoAn2025.Controllers
{
	public class CheckoutController : Controller
	{
		private readonly DataContext _dataContext;

		public CheckoutController(DataContext context)
		{
			_dataContext = context;
		}

		public async Task<IActionResult> Checkout()
		{
			var userEmail = User.FindFirstValue(ClaimTypes.Email);
			if (userEmail == null)
			{
				return RedirectToAction("Login", "Account");
			}
			else
			{

				HttpContext.Session.Remove("Cart");
				TempData["success"] = "Order has been created, please wait for order approval.";
				return RedirectToAction("Index", "Cart");
			}
			return View();
		}
	}
}
