using Microsoft.AspNetCore.Mvc;
using DoAn2025.Models;
using DoAn2025.Models.ViewModels;
using DoAn2025.Repository;

namespace DoAn2025.Controllers
{
    public class CartController : Controller
    {
		private readonly DataContext _dataContext;
		public CartController(DataContext context)
		{
			_dataContext = context;
		}
		public IActionResult Index()
		{
			List<CartitemModel> cartitems = HttpContext.Session.GetJson<List<CartitemModel>>("cart") ?? new List<CartitemModel>();
			CartItemViewModel cartVM = new()
			{

				Cartitems = cartitems,
				GrandTotal = cartitems.Sum(x => x.Quantity * x.Price)	

			};
			return View(cartVM);
		}

		public IActionResult Checkout()
		{
			return View("~/Views/Checkout/Index.cshtml");
		}

		public async Task<IActionResult> Add(int Id) { 
			
			ProductModel product = await _dataContext.Products.FindAsync(Id);
			List<CartitemModel> cart = HttpContext.Session.GetJson<List<CartitemModel>>("cart") ?? new List<CartitemModel>();
			CartitemModel cartitems = cart.Where(c=>c.ProductId == Id).FirstOrDefault();

			if (cartitems == null)
			{

				cart.Add(new CartitemModel(product));
			}
			else {
				cartitems.Quantity += 1;
				
			}
			HttpContext.Session.SetJson("cart", cart);

			TempData["success"] = "Add Item to cart Successfully";
			return Redirect(Request.Headers["Referer"].ToString());
		}
		public async Task<IActionResult> Decrease(int Id)
		{
			List<CartitemModel> cart = HttpContext.Session.GetJson<List<CartitemModel>>("cart");

			CartitemModel cartitems = cart.Where(c => c.ProductId == Id).FirstOrDefault();

			if (cartitems.Quantity >1) { 
				
				--cartitems.Quantity;
			}
			else
			{
				cart.RemoveAll( p => p.ProductId == Id);
			}
			if (cart.Count == 0) {

				HttpContext.Session.Remove("cart");
			}
			else
			{
				HttpContext.Session.SetJson("cart", cart);
			}
			TempData["success"] = "Decrease Item quantity to cart Successfully";
			return RedirectToAction("Index");
		}

		public async Task<IActionResult> Increase(int Id)
		{
			List<CartitemModel> cart = HttpContext.Session.GetJson<List<CartitemModel>>("cart");

			CartitemModel cartitems = cart.Where(c => c.ProductId == Id).FirstOrDefault();

			if (cartitems.Quantity > 0)
			{

				++cartitems.Quantity;
			}
			else
			{
				cart.RemoveAll(p => p.ProductId == Id);
			}
			if (cart.Count == 0)
			{

				HttpContext.Session.Remove("cart");
			}
			else
			{
				HttpContext.Session.SetJson("cart", cart);
			}

			TempData["success"] = "Increase Item quantity to cart Successfully";
			return RedirectToAction("Index");
		}

		public async Task<IActionResult> Remove(int Id)
		{
			List<CartitemModel> cart = HttpContext.Session.GetJson<List<CartitemModel>>("cart");
			cart.RemoveAll(p => p.ProductId == Id);
			if (cart.Count == 0)
			{
				HttpContext.Session.Remove("cart");
			}
			else {
				HttpContext.Session.SetJson("cart", cart);
			}
			TempData["success"] = "Remove Item of cart Successfully";

			return RedirectToAction("Index");
		}

		public async Task<IActionResult> clear(int Id) {
			HttpContext.Session.Remove("cart");
			TempData["success"] = "Clear all Item of cart Successfully";

			return RedirectToAction("Index");
		}
	}	
}
	