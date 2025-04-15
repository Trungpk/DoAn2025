using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace DoAn2025.Controllers
{
	public class ChatbotController : Controller
	{
	
		private readonly DataContext _context;
		private readonly IWebHostEnvironment _env;

		// Thông tin cửa hàng với phương thức thanh toán, trả góp, và bảo hành
		private readonly (string Name, string Address, string Email, string Phone, string OpenHours, string CloseHours, string Description, string[] PaymentMethods, string InstallmentInfo, string WarrantyInfo) _shopInfo = (
			Name: "CuCu Shop",
			Address: "Thanh Hải Thanh Hà Hải Dương",
			Email: "Trungdz@cucushop.vn",
			Phone: "0867640635",
			OpenHours: "08:00",
			CloseHours: "20:00",
			Description: "CuCu Shop - Nơi mang đến những sản phẩm công nghệ chất lượng cao với dịch vụ tận tâm!",
			PaymentMethods: new[] { "Thanh toán tiền mặt (COD)", "Thẻ tín dụng/ghi nợ (Visa, MasterCard)", "Chuyển khoản ngân hàng", "Ví điện tử (Momo, ZaloPay)" },
			InstallmentInfo: "Hỗ trợ trả góp 0% lãi suất cho đơn hàng từ 3 triệu đồng trở lên qua thẻ tín dụng (tùy ngân hàng).",
			WarrantyInfo: "Bảo hành chính hãng 12 tháng cho tất cả sản phẩm, hỗ trợ đổi trả trong 30 ngày nếu có lỗi từ nhà sản xuất."
		);

		public ChatbotController(DataContext context, IWebHostEnvironment env)
		{
			_context = context;
			_env = env;
		}

		[HttpPost]
		public async Task<JsonResult> GetChatbotResponse(string message, string? language)
		{
			language ??= await DetectLanguage(message);
			var chatHistory = HttpContext.Session.GetString("ChatHistory") ?? "";
			var lowerMessage = message.ToLower();
			var budget = ExtractBudgetFromMessage(lowerMessage);
			var (minPrice, maxPrice) = ExtractPriceRangeFromMessage(lowerMessage);
			var reply = await GenerateReply(lowerMessage, language, budget, minPrice, maxPrice);
			chatHistory += $"<div><b>Bạn / You:</b> {message}</div><div><b>Bot:</b> {reply.text}</div>";
			HttpContext.Session.SetString("ChatHistory", chatHistory);
			return Json(new { reply = reply.text, productUrl = reply.productUrl });
		}

		[HttpGet]
		public JsonResult GetChatHistory() =>
			Json(new { history = HttpContext.Session.GetString("ChatHistory") ?? "" });

		private async Task<(string text, string? productUrl)> GenerateReply(string message, string language, decimal? budget, decimal? minPrice, decimal? maxPrice)
		{
			var isVietnamese = language == "vi";
			if (string.IsNullOrEmpty(HttpContext.Session.GetString("ChatHistory")))
				return (isVietnamese ? "Chào bạn! Mình là trợ lý bán hàng siêu vui vẻ đây! 😊 Bạn đang tìm gì nào?"
									: "Hi! I'm your cheerful sales assistant! 😊 What are you looking for?", null);

			// Kiểm tra câu hỏi về cửa hàng, thanh toán, hoặc bảo hành
			if (IsShopQuery(message))
				return (BuildShopInfoReply(isVietnamese, IsPaymentQuery(message), IsWarrantyQuery(message)), null);

			if (minPrice.HasValue && maxPrice.HasValue)
				return await HandlePriceRangeQuery(minPrice.Value, maxPrice.Value, isVietnamese);
			if (budget.HasValue && IsCategoryQuery(message))
				return await HandleCategoryBudgetQuery(message, budget.Value, isVietnamese);
			if (budget.HasValue)
				return await HandleBudgetQuery(budget.Value, isVietnamese);
			if (IsCategoryAndBrandQuery(message))
				return await HandleCategoryAndBrandQuery(message, isVietnamese);
			if (IsCategoryQuery(message))
				return await HandleCategoryQuery(message, isVietnamese);
			if (IsBrandQuery(message))
				return await HandleBrandQuery(message, isVietnamese);
			if (IsProductRelatedQuery(message))
				return await HandleProductQuery(message, isVietnamese);

			var reply = await GetNormalResponse(message, language);
			return (isVietnamese ? $"Hi 😜 Đây là câu trả lời dành riêng cho bạn:\n{reply}\nBạn muốn khám phá thêm gì nữa không?"
								 : $"Hi 😜 Here’s my answer just for you:\n{reply}\nWhat else can I help you explore?", null);
		}

		private bool IsShopQuery(string message) =>
			new[] { "shop", "cửa hàng", "liên hệ", "địa chỉ", "contact", "address", "store", "giờ mở", "giờ đóng", "open hours", "close hours", "thanh toán", "trả góp", "payment", "installment", "bảo hành", "warranty" }
				.Any(t => message.Contains(t));

		private bool IsPaymentQuery(string message) =>
			new[] { "thanh toán", "trả góp", "payment", "installment" }
				.Any(t => message.Contains(t));

		private bool IsWarrantyQuery(string message) =>
			new[] { "bảo hành", "warranty" }
				.Any(t => message.Contains(t));

		private string BuildShopInfoReply(bool isVietnamese, bool isPaymentQuery, bool isWarrantyQuery)
		{
			var reply = new StringBuilder();
			if (isVietnamese)
			{
				if (isWarrantyQuery)
				{
					reply.AppendLine("Chào bạn! Đây là thông tin bảo hành tại CuCu Shop! 😊");
					reply.AppendLine($"- {_shopInfo.WarrantyInfo}");
					reply.AppendLine($"Nếu bạn muốn biết thêm chi tiết, liên hệ qua email: {_shopInfo.Email} hoặc số điện thoại: {_shopInfo.Phone} nhé! 😄 Có gì mình hỗ trợ thêm không?");
				}
				else if (isPaymentQuery)
				{
					reply.AppendLine("Chào bạn! Đây là các phương thức thanh toán tại CuCu Shop! 😊");
					foreach (var method in _shopInfo.PaymentMethods)
						reply.AppendLine($"- {method}");
					reply.AppendLine($"- Trả góp: {_shopInfo.InstallmentInfo}");
					reply.AppendLine($"Nếu bạn muốn biết thêm chi tiết, liên hệ qua email: {_shopInfo.Email} hoặc số điện thoại: {_shopInfo.Phone} nhé! 😄 Có gì mình hỗ trợ thêm không?");
				}
				else
				{
					reply.AppendLine("Chào bạn! Cảm ơn bạn đã quan tâm đến CuCu Shop! 😊");
					reply.AppendLine($"{_shopInfo.Description}");
					reply.AppendLine("- Tên: " + _shopInfo.Name);
					reply.AppendLine("- Địa chỉ: " + _shopInfo.Address);
					reply.AppendLine($"- Giờ mở cửa: {_shopInfo.OpenHours} - {_shopInfo.CloseHours} (tất cả các ngày trong tuần)");
					reply.AppendLine("- Email liên hệ: " + _shopInfo.Email);
					reply.AppendLine("- Số điện thoại: " + _shopInfo.Phone);
					reply.AppendLine("Hãy ghé thăm hoặc liên hệ để trải nghiệm dịch vụ tuyệt vời của chúng mình nhé! Bạn cần hỗ trợ gì thêm không? 😄");
				}
			}
			else
			{
				if (isWarrantyQuery)
				{
					reply.AppendLine("Hello! Here’s the warranty info for CuCu Shop! 😊");
					reply.AppendLine($"- {_shopInfo.WarrantyInfo}");
					reply.AppendLine($"For more details, contact us via email: {_shopInfo.Email} or phone: {_shopInfo.Phone}. 😄 Anything else we can help with?");
				}
				else if (isPaymentQuery)
				{
					reply.AppendLine("Hello! Here are the payment methods at CuCu Shop! 😊");
					foreach (var method in _shopInfo.PaymentMethods)
						reply.AppendLine($"- {method}");
					reply.AppendLine($"- Installments: {_shopInfo.InstallmentInfo}");
					reply.AppendLine($"For more details, contact us via email: {_shopInfo.Email} or phone: {_shopInfo.Phone}. 😄 Anything else we can help with?");
				}
				else
				{
					reply.AppendLine("Hello! Thank you for your interest in CuCu Shop! 😊");
					reply.AppendLine($"{_shopInfo.Description}");
					reply.AppendLine("- Name: " + _shopInfo.Name);
					reply.AppendLine("- Address: " + _shopInfo.Address);
					reply.AppendLine($"- Opening Hours: {_shopInfo.OpenHours} - {_shopInfo.CloseHours} (every day)");
					reply.AppendLine("- Contact Email: " + _shopInfo.Email);
					reply.AppendLine("- Phone Number: " + _shopInfo.Phone);
					reply.AppendLine("Come visit us or get in touch for an amazing shopping experience! Anything else we can help with? 😄");
				}
			}
			return reply.ToString();
		}

		private async Task<(string text, string? productUrl)> HandlePriceRangeQuery(decimal minPrice, decimal maxPrice, bool isVietnamese)
		{
			var products = await _context.Products.Where(p => p.Price >= minPrice && p.Price <= maxPrice).Take(5).ToListAsync();
			return products.Any()
				? BuildProductReply(products, isVietnamese ? $"Sản phẩm trong khoảng giá từ {minPrice} đến {maxPrice} đô đây! 😍"
														 : $"Products from {minPrice} to {maxPrice} USD! 😍", isVietnamese)
				: (isVietnamese ? $"Chưa có sản phẩm trong khoảng giá từ {minPrice} đến {maxPrice} đô. Thử mức khác nhé? 😅"
								: $"No products from {minPrice} to {maxPrice} USD. Try another range? 😅", null);
		}

		private async Task<(string text, string? productUrl)> HandleCategoryBudgetQuery(string message, decimal budget, bool isVietnamese)
		{
			var products = await FindProductsByCategoryAndBudget(message, budget);
			return products.Any()
				? BuildProductReply(products, isVietnamese ? $"Sản phẩm trong danh mục với ngân sách {budget} đô! 😄"
														 : $"Products in category with {budget} USD budget! 😄", isVietnamese)
				: (isVietnamese ? $"Chưa có sản phẩm trong danh mục này dưới {budget} đô. Thử lại nhé? 😔"
								: $"No products in this category under {budget} USD. Try again? 😔", null);
		}

		private async Task<(string text, string? productUrl)> HandleBudgetQuery(decimal budget, bool isVietnamese)
		{
			var products = await _context.Products.Where(p => p.Price <= budget).Take(5).ToListAsync();
			return products.Any()
				? BuildProductReply(products, isVietnamese ? $"Sản phẩm dưới {budget} đô đây! 🤑"
														 : $"Products under {budget} USD! 🤑", isVietnamese)
				: (isVietnamese ? $"Chưa có sản phẩm dưới {budget} đô. Thử mức khác nhé? 😅"
								: $"No products under {budget} USD. Try another budget? 😅", null);
		}

		private async Task<(string text, string? productUrl)> HandleCategoryAndBrandQuery(string message, bool isVietnamese)
		{
			var products = await FindProductsByCategoryAndBrand(message);
			return products.Any()
				? BuildProductReply(products, isVietnamese ? "Sản phẩm theo danh mục và thương hiệu! 😍"
														 : "Products by category and brand! 😍", isVietnamese)
				: (isVietnamese ? "Chưa tìm thấy sản phẩm theo danh mục và thương hiệu này. Thử lại nhé? 😔"
								: "No products for this category and brand. Try again? 😔", null);
		}

		private async Task<(string text, string? productUrl)> HandleCategoryQuery(string message, bool isVietnamese)
		{
			var products = await FindProductsByCategory(message);
			return products.Any()
				? BuildProductReply(products, isVietnamese ? "Sản phẩm trong danh mục này! 😜"
														 : "Products in this category! 😜", isVietnamese)
				: (isVietnamese ? "Chưa có sản phẩm trong danh mục này. Thử lại nhé? 😅"
								: "No products in this category. Try again? 😅", null);
		}

		private async Task<(string text, string? productUrl)> HandleBrandQuery(string message, bool isVietnamese)
		{
			var products = await FindProductsByBrand(message);
			return products.Any()
				? BuildProductReply(products, isVietnamese ? "Sản phẩm của thương hiệu này! 😎"
														 : "Products from this brand! 😎", isVietnamese)
				: (isVietnamese ? "Chưa có sản phẩm của thương hiệu này. Thử lại nhé? 😔"
								: "No products for this brand. Try again? 😔", null);
		}

		private async Task<(string text, string? productUrl)> HandleProductQuery(string message, bool isVietnamese)
		{
			var generalInfo = await GetGeneralProductInfo(message, isVietnamese ? "vi" : "en");
			var product = await FindProductByMessage(message);
			var reply = isVietnamese ? $"Thông tin chi tiết đây:\n{generalInfo}" : $"Here’s the full scoop:\n{generalInfo}";

			if (product == null)
				return (reply + (isVietnamese ? "\n\nHơi tiếc, món này chưa có trong shop. 😅" : "\n\nBummer, this item isn’t in our store. 😅"), null);

			var productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
			var imagePath = GetImagePath(product);
			reply += isVietnamese
				? $"\n\nWow, {product.Name} đây! 😍 <img src='{imagePath}' alt='{product.Name}' width='100'/> {product.Name} - ${product.Price} - [Xem ngay!]({productUrl})"
				: $"\n\nGuess what? {product.Name} is here! 😍 <img src='{imagePath}' alt='{product.Name}' width='100'/> {product.Name} - ${product.Price} - [Check it out!]({productUrl})";
			return (reply, productUrl);
		}

		private (string text, string? productUrl) BuildProductReply(List<ProductModel> products, string intro, bool isVietnamese)
		{
			var reply = new StringBuilder(intro + "\n");
			string? productUrl = null;
			foreach (var product in products)
			{
				productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
				var imagePath = GetImagePath(product);
				reply.AppendLine($"<img src='{imagePath}' alt='{product.Name}' width='100'/> {product.Name} - ${product.Price} - [{(isVietnamese ? "Xem ngay" : "Check it out")}]({productUrl})");
			}
			reply.AppendLine(isVietnamese ? "\nChốt đơn ngay nhé! 😉" : "\nGrab them now! 😉");
			return (reply.ToString(), productUrl);
		}

		private decimal? ExtractBudgetFromMessage(string message) =>
			Regex.Match(message, @"(?:tôi có |với |I have )?(\d+(?:\.\d+)?)\s*(đô|dollars|usd|\$)", RegexOptions.IgnoreCase) is { Success: true } match
			&& decimal.TryParse(match.Groups[1].Value, out var budget) ? budget : null;

		private (decimal? minPrice, decimal? maxPrice) ExtractPriceRangeFromMessage(string message)
		{
			var match = Regex.Match(message, @"(?:từ |from )?(\d+(?:\.\d+)?)\s*(?:đến |to )\s*(\d+(?:\.\d+)?)\s*(đô|dollars|usd|\$)", RegexOptions.IgnoreCase);
			return match.Success && decimal.TryParse(match.Groups[1].Value, out var min) && decimal.TryParse(match.Groups[2].Value, out var max) ? (min, max) : (null, null);
		}

		private async Task<string> DetectLanguage(string message) =>
			message.Any(c => c >= 'À' && c <= 'ỹ') ? "vi" : "en";

		private async Task<string> GetGeneralProductInfo(string message, string language)
		{
			using var client = new HttpClient { DefaultRequestHeaders = { { "Authorization", $"Bearer {ApiKey}" } } };
			var prompt = language == "vi"
				? $"Thông tin tổng quan về sản phẩm trong: '{message}'. Bao gồm năm ra mắt, cấu hình chính, đặc điểm nổi bật, trả lời bằng tiếng Việt."
				: $"Overview of the product in: '{message}'. Include release year, main specs, key features, respond in English.";
			var requestBody = new { model = "gpt-3.5-turbo", messages = new[] { new { role = "user", content = prompt } }, max_tokens = 500 };
			var response = await client.PostAsync(ApiUrl, new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
			if (response.IsSuccessStatusCode)
			{
				var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
				return result.choices[0].message.content;
			}
			return language == "vi" ? "Ôi, chưa lấy được thông tin! 😅 Thử lại nha!" : "Oops, couldn’t get the info! 😅 Try again!";
		}

		private async Task<string> GetNormalResponse(string message, string language)
		{
			var products = await _context.Products.ToListAsync();
			var productSuggestion = products.FirstOrDefault(p => message.ToLower().Contains(p.Name.ToLower())) is { } product
				? BuildProductReply(new() { product }, language == "vi" ? $"Bạn nhắc đến {product.Name} nè!" : $"You mentioned {product.Name}!", language == "vi").text
				: "";

			using var client = new HttpClient { DefaultRequestHeaders = { { "Authorization", $"Bearer {ApiKey}" } } };
			var prompt = language == "vi" ? message : $"Respond in English: {message}";
			var requestBody = new { model = "gpt-3.5-turbo", messages = new[] { new { role = "user", content = prompt } }, max_tokens = 500 };
			var response = await client.PostAsync(ApiUrl, new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
			if (response.IsSuccessStatusCode)
			{
				var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
				return result.choices[0].message.content + productSuggestion;
			}
			return (language == "vi" ? "Ôi, lỗi nhỏ rồi! 😅 Thử lại nha!" : "Tiny error! 😅 Try again!") + productSuggestion;
		}

		private bool IsProductRelatedQuery(string message) =>
			new[] { "thông tin", "sản phẩm", "giá", "cấu hình", "ra mắt", "chi tiết", "information", "product", "price", "specs", "release", "details" }
				.Any(t => message.Contains(t)) && _context.Products.Any(p => message.Contains(p.Name.ToLower()));

		private async Task<ProductModel?> FindProductByMessage(string message)
		{
			var products = await _context.Products.Include(p => p.Brand).Include(p => p.Category).ToListAsync();
			var keywords = message.ToLower().Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
			return products.MaxBy(p =>
			{
				var name = p.Name.ToLower();
				var desc = p.Description.ToLower();
				return name == message || desc.Contains(message) ? int.MaxValue : keywords.Count(k => name.Contains(k) || desc.Contains(k));
			});
		}

		private bool IsCategoryQuery(string message) =>
			_context.Categories.Any(c => message.Contains(c.Name.ToLower()) || (message.Contains("laptop") && c.Name.ToLower() == "máy tính xách tay"));

		private async Task<List<ProductModel>> FindProductsByCategory(string message)
		{
			var category = await _context.Categories.FirstOrDefaultAsync(c => message.Contains(c.Name.ToLower()) || (message.Contains("laptop") && c.Name.ToLower() == "máy tính xách tay"));
			return category != null ? await _context.Products.Where(p => p.CategoryId == category.Id).ToListAsync() : new();
		}

		private bool IsBrandQuery(string message) =>
			_context.BrandModels.Any(b => message.Contains(b.Name.ToLower()));

		private async Task<List<ProductModel>> FindProductsByBrand(string message)
		{
			var brand = await _context.BrandModels.FirstOrDefaultAsync(b => message.Contains(b.Name.ToLower()));
			return brand != null ? await _context.Products.Where(p => p.BrandId == brand.Id).ToListAsync() : new();
		}

		private bool IsCategoryAndBrandQuery(string message) =>
			IsCategoryQuery(message) && IsBrandQuery(message);

		private async Task<List<ProductModel>> FindProductsByCategoryAndBrand(string message)
		{
			var category = await _context.Categories.FirstOrDefaultAsync(c => message.Contains(c.Name.ToLower()));
			var brand = await _context.BrandModels.FirstOrDefaultAsync(b => message.Contains(b.Name.ToLower()));
			return (category, brand) != (null, null)
				? await _context.Products.Where(p => p.CategoryId == category.Id && p.BrandId == brand.Id).ToListAsync()
				: new();
		}

		private async Task<List<ProductModel>> FindProductsByCategoryAndBudget(string message, decimal budget)
		{
			var category = await _context.Categories.FirstOrDefaultAsync(c => message.Contains(c.Name.ToLower()));
			return category != null
				? await _context.Products.Where(p => p.CategoryId == category.Id && p.Price <= budget).Take(5).ToListAsync()
				: new();
		}

		private string GetImagePath(ProductModel product)
		{
			const string defaultImage = "/media/products/noimage.jpg";
			if (string.IsNullOrEmpty(product.Image) || product.Image == "noimage.jpg")
				return defaultImage;
			var imagePath = Path.Combine(_env.WebRootPath, "media/products", product.Image);
			return System.IO.File.Exists(imagePath) ? $"/media/products/{product.Image}" : defaultImage;
		}
	}
}