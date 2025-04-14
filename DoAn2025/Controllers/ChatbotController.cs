using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using DoAn2025.Models;
using DoAn2025.Repository;

namespace DoAn2025.Controllers
{
	public class ChatbotController : Controller
	{
		private readonly DataContext _context;

		public ChatbotController(DataContext context)
		{
			_context = context;
		}

		[HttpPost]
		public async Task<JsonResult> GetChatbotResponse(string message, string language = null)
		{
			// Tự động nhận diện ngôn ngữ nếu không được cung cấp
			language = language ?? await DetectLanguage(message);

			var chatHistory = HttpContext.Session.GetString("ChatHistory") ?? "";
			string lowerMessage = message.ToLower();
			decimal? budget = ExtractBudgetFromMessage(lowerMessage);
			var priceRange = ExtractPriceRangeFromMessage(lowerMessage);
			string reply;
			string productUrl = null;

			// Thêm lời chào nếu là tin nhắn đầu tiên
			if (string.IsNullOrEmpty(chatHistory))
			{
				reply = language == "vi"
					? "Chào bạn! Mình là trợ lý bán hàng siêu vui vẻ đây! 😊 Rất hào hứng được giúp bạn tìm sản phẩm ưng ý. Bạn đang tìm gì nào? "
					: "Hi there! I'm your super cheerful sales assistant! 😊 So excited to help you find the perfect product. What are you looking for today? ";
				chatHistory += $"<div><b>Bạn / You:</b> {message}</div><div><b>Bot:</b> {reply}</div>";
			}

			// Trường hợp 1: Tìm kiếm theo khoảng giá
			if (priceRange.minPrice.HasValue && priceRange.maxPrice.HasValue)
			{
				var productsInRange = await _context.Products
					.Where(p => p.Price >= priceRange.minPrice.Value && p.Price <= priceRange.maxPrice.Value)
					.Take(5)
					.ToListAsync();
				if (productsInRange.Any())
				{
					reply = language == "vi"
						? $"Tuyệt vời! Mình tìm được vài sản phẩm siêu xịn trong khoảng giá từ {priceRange.minPrice} đến {priceRange.maxPrice} đô đây! 😍 Hãy xem nào:\n"
						: $"Awesome! I found some fantastic products in the price range from {priceRange.minPrice} to {priceRange.maxPrice} USD! 😍 Check these out:\n";
					foreach (var product in productsInRange)
					{
						productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
						reply += $"- {product.Name}: ${product.Price} - [Click ngay để xem chi tiết!]({productUrl})\n";
					}
					reply += language == "vi" ? "\nBạn ưng món nào thì chốt đơn ngay nhé! 😉" : "\nLove any of these? Grab them now! 😉";
				}
				else
				{
					reply = language == "vi"
						? $"Ôi, hơi tiếc chút xíu! 😅 Hiện tại chưa có sản phẩm nào trong khoảng giá từ {priceRange.minPrice} đến {priceRange.maxPrice} đô. Bạn muốn mình gợi ý thêm gì khác không nào?"
						: $"Oh, just a tiny bummer! 😅 No products found in the price range from {priceRange.minPrice} to {priceRange.maxPrice} USD. Want me to suggest something else?";
				}
			}
			// Trường hợp 2: Người dùng cung cấp cả danh mục và ngân sách
			else if (budget.HasValue && IsCategoryQuery(lowerMessage))
			{
				var categoryBudgetProducts = await FindProductsByCategoryAndBudget(lowerMessage, budget.Value);
				if (categoryBudgetProducts.Any())
				{
					reply = language == "vi"
						? $"Hài hước sao  khi ngân sách {budget.Value} đô của bạn lại hợp với danh mục này thế! 😄 Đây là vài món mình chọn riêng cho bạn:\n"
						: $"Your {budget.Value} USD budget fits this category perfectly! 😄 Here are some picks just for you:\n";
					foreach (var product in categoryBudgetProducts)
					{
						productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
						reply += $"- {product.Name}: ${product.Price} - [Click để khám phá ngay!]({productUrl})\n";
					}
					reply += language == "vi" ? "\nThấy món nào ưng chưa? Mình chờ bạn chốt đơn đây! 😜" : "\nSpot anything you love? I'm ready for your order! 😜";
				}
				else
				{
					reply = language == "vi"
						? $"Hơi tiếc xíu nha! 😔 Hiện chưa có sản phẩm nào trong danh mục này dưới {budget.Value} đô. Để mình gợi ý thêm cho bạn nhé?"
						: $"Tiny hiccup! 😔 No products in this category under {budget.Value} USD yet. Shall I suggest something else?";
				}
			}
			// Trường hợp 3: Người dùng chỉ cung cấp ngân sách
			else if (budget.HasValue)
			{
				var affordableProducts = await _context.Products
					.Where(p => p.Price <= budget.Value)
					.ToListAsync();
				if (affordableProducts.Any())
				{
					reply = language == "vi"
						? $"Wow, ngân sách {budget.Value} đô của bạn mở ra cả kho báu đây! 🤑 Mình chọn giúp bạn vài món siêu hot:\n"
						: $"Wow, your {budget.Value} USD budget unlocks a treasure trove! 🤑 Here are some hot picks for you:\n";
					foreach (var product in affordableProducts)
					{
						productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
						reply += $"- {product.Name}: ${product.Price} - [Click để xem chi tiết nè!]({productUrl})\n";
					}
					reply += language == "vi" ? "\nBạn thấy sao? Chốt đơn hôm nay để nhận ưu đãi siêu xịn nhé! 😎" : "\nWhat do you think? Order today for some awesome deals! 😎";
				}
				else
				{
					reply = language == "vi"
						? $"Ôi, hơi khó chút nha! 😅 Chưa có sản phẩm nào dưới {budget.Value} đô lúc này. Bạn muốn mình tìm thêm hay thử mức ngân sách khác không?"
						: $"Oops, a bit tricky! 😅 No products under {budget.Value} USD right now. Want me to keep looking or try another budget?";
				}
			}
			// Trường hợp 4: Kết hợp danh mục và thương hiệu
			else if (IsCategoryAndBrandQuery(lowerMessage))
			{
				var products = await FindProductsByCategoryAndBrand(lowerMessage);
				if (products.Any())
				{
					reply = language == "vi"
						? $"Bạn chọn danh mục và thương hiệu đỉnh thế này, mình mê quá! 😍 Đây là các sản phẩm siêu chất dành cho bạn:\n"
						: $"Your category and brand combo is top-notch! 😍 Here are some awesome products for you:\n";
					foreach (var product in products)
					{
						productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
						reply += $"- {product.Name}: ${product.Price} - [Click để xem ngay nào!]({productUrl})\n";
					}
					reply += language == "vi" ? "\nThích món nào thì đừng bỏ lỡ nhé, hàng hot lắm đấy! 🔥" : "\nDon’t miss out on these hot items—grab your fave! 🔥";
				}
				else
				{
					reply = language == "vi"
						? $"Hơi tiếc nha! 😔 Chưa tìm thấy sản phẩm nào khớp danh mục và thương hiệu này. Để mình gợi ý thêm cho bạn nhé?"
						: $"Small hiccup! 😔 No products match this category and brand yet. Want some other suggestions?";
				}
			}
			// Trường hợp 5: Tìm kiếm sản phẩm theo danh mục
			else if (IsCategoryQuery(lowerMessage))
			{
				var categoryProducts = await FindProductsByCategory(lowerMessage);
				if (categoryProducts.Any())
				{
					reply = language == "vi"
						? $"Danh mục này hot lắm nè! 😜 Mình chọn giúp bạn vài món siêu đỉnh đây:\n"
						: $"This category is super hot! 😜 I’ve picked some amazing items for you:\n";
					foreach (var product in categoryProducts)
					{
						productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
						reply += $"- {product.Name}: ${product.Price} - [Click để khám phá nào!]({productUrl})\n";
					}
					reply += language == "vi" ? "\nBạn ưng món nào chưa? Mình sẵn sàng hỗ trợ tiếp đây! 😊" : "\nFound something you love? I’m here to help more! 😊";
				}
				else
				{
					reply = language == "vi"
						? $"Hơi tiếc xíu nha! 😅 Chưa có sản phẩm nào trong danh mục này. Bạn muốn mình tìm thêm không nào?"
						: $"Tiny bummer! 😅 No products in this category yet. Shall I keep searching for you?";
				}
			}
			// Trường hợp 6: Tìm kiếm sản phẩm theo thương hiệu
			else if (IsBrandQuery(lowerMessage))
			{
				var brandProducts = await FindProductsByBrand(lowerMessage);
				if (brandProducts.Any())
				{
					reply = language == "vi"
						? $"Thương hiệu này chất khỏi bàn luôn! 😎 Đây là vài sản phẩm siêu xịn mình chọn cho bạn:\n"
						: $"This brand is pure gold! 😎 Here are some awesome products I picked for you:\n";
					foreach (var product in brandProducts)
					{
						productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
						reply += $"- {product.Name}: ${product.Price} - [Click để xem chi tiết nè!]({productUrl})\n";
					}
					reply += language == "vi" ? "\nMón nào cũng hot, bạn chốt đơn ngay nhé! 🔥" : "\nEverything’s a hit—order now! 🔥";
				}
				else
				{
					reply = language == "vi"
						? $"Ôi, hơi tiếc nha! 😔 Chưa có sản phẩm nào của thương hiệu này. Bạn muốn mình tìm thêm không?"
						: $"Oops, a bit sad! 😔 No products for this brand yet. Want me to look further?";
				}
			}
			// Trường hợp 7: Tin nhắn liên quan đến sản phẩm
			else if (IsProductRelatedQuery(lowerMessage))
			{
				string generalInfo = await GetGeneralProductInfo(lowerMessage, language);
				var product = await FindProductByMessage(lowerMessage);
				reply = language == "vi"
					? $"Hài hước sao khi bạn hỏi đúng món hot thế này! 😄 Đây là thông tin chi tiết cho bạn:\n{generalInfo}"
					: $"You’re asking about a hot item—love it! 😄 Here’s the full scoop:\n{generalInfo}";
				if (product != null)
				{
					productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
					reply += language == "vi"
						? $"\n\nWow, {product.Name} đang có sẵn trong shop nè! 😍 Xem ngay tại đây nào: [{product.Name}]({productUrl})"
						: $"\n\nGuess what? {product.Name} is in our store! 😍 Check it out here: [{product.Name}]({productUrl})";
				}
				else
				{
					reply += language == "vi"
						? "\n\nHơi tiếc xíu, món này chưa có trong shop. 😅 Nhưng đừng lo, mình sẽ gợi ý thêm cho bạn ngay!"
						: "\n\nTiny bummer, this item isn’t in our store yet. 😅 No worries, I’ll suggest more for you!";
				}
			}
			// Trường hợp 8: Tin nhắn thông thường
			else
			{
				reply = await GetNormalResponse(message, language);
				reply = language == "vi"
					? $"Hi 😜 Đây là câu trả lời dành riêng cho bạn:\n{reply}\nBạn muốn khám phá thêm gì nữa không nào?"
					: $"Hi 😜 Here’s my answer just for you:\n{reply}\nWhat else can I help you explore?";
			}

			chatHistory += $"<div><b>Bạn / You:</b> {message}</div><div><b>Bot:</b> {reply}</div>";
			HttpContext.Session.SetString("ChatHistory", chatHistory);

			return Json(new { reply = reply, productUrl = productUrl });
		}

		[HttpGet]
		public JsonResult GetChatHistory()
		{
			var chatHistory = HttpContext.Session.GetString("ChatHistory") ?? "";
			return Json(new { history = chatHistory });
		}

		private decimal? ExtractBudgetFromMessage(string message)
		{
			var regex = new Regex(@"(?:tôi có |với |I have )?(\d+(?:\.\d+)?)\s*(đô|dollars|usd|\$)", RegexOptions.IgnoreCase);
			var match = regex.Match(message);
			if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal budget))
			{
				return budget;
			}
			return null;
		}

		private (decimal? minPrice, decimal? maxPrice) ExtractPriceRangeFromMessage(string message)
		{
			var regex = new Regex(@"(?:từ |from )?(\d+(?:\.\d+)?)\s*(?:đến |to )\s*(\d+(?:\.\d+)?)\s*(đô|dollars|usd|\$)", RegexOptions.IgnoreCase);
			var match = regex.Match(message);
			if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal minPrice) && decimal.TryParse(match.Groups[2].Value, out decimal maxPrice))
			{
				return (minPrice, maxPrice);
			}
			return (null, null);
		}

		private async Task<string> DetectLanguage(string message)
		{
			if (message.Any(c => c >= 'À' && c <= 'ỹ')) return "vi";
			return "en";
		}

		private async Task<string> GetGeneralProductInfo(string message, string language)
		{
			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
				var prompt = language == "vi"
					? $"Cung cấp thông tin tổng quan về sản phẩm được đề cập trong câu sau (nếu có), bao gồm năm ra mắt, cấu hình chính, và đặc điểm nổi bật. Trả lời chi tiết và đầy đủ bằng tiếng Việt: '{message}'"
					: $"Provide an overview of the product mentioned in the following sentence (if any), including release year, main specifications, and key features. Respond in English, detailed and complete: '{message}'";

				var requestBody = new
				{
					model = "gpt-3.5-turbo",
					messages = new[] { new { role = "user", content = prompt } },
					max_tokens = 500
				};

				var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
				var response = await client.PostAsync(_apiUrl, jsonContent);

				if (response.IsSuccessStatusCode)
				{
					var responseData = await response.Content.ReadAsStringAsync();
					var result = JsonConvert.DeserializeObject<dynamic>(responseData);
					return result.choices[0].message.content;
				}

				return language == "vi"
					? "Ôi, mình chưa lấy được thông tin ngay bây giờ! 😅 Vui lòng thử lại nha, mình sẽ cố gắng hơn!"
					: "Oops, I couldn’t grab the info just now! 😅 Please try again, I’ll do my best!";
			}
		}

		private async Task<string> GetNormalResponse(string message, string language)
		{
			var products = await _context.Products.ToListAsync();
			string productSuggestion = "";
			foreach (var product in products)
			{
				if (message.ToLower().Contains(product.Name.ToLower()))
				{
					var productUrl = Url.Action("DetailsWithSlug", "Product", new { id = product.Id, slug = product.Slug }, Request.Scheme);
					productSuggestion = language == "vi"
						? $"\n\nHihi, bạn nhắc đến '{product.Name}' đúng không? 😍 Xem món này siêu hot tại đây nè: [{product.Name}]({productUrl})"
						: $"\n\nHey, you mentioned '{product.Name}', right? 😍 Check out this awesome item here: [{product.Name}]({productUrl})";
					break;
				}
			}

			using (var client = new HttpClient())
			{
				client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
				var prompt = language == "vi" ? message : $"Respond in English: {message}";

				var requestBody = new
				{
					model = "gpt-3.5-turbo",
					messages = new[] { new { role = "user", content = prompt } },
					max_tokens = 500
				};

				var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
				var response = await client.PostAsync(_apiUrl, jsonContent);

				if (response.IsSuccessStatusCode)
				{
					var responseData = await response.Content.ReadAsStringAsync();
					var result = JsonConvert.DeserializeObject<dynamic>(responseData);
					return result.choices[0].message.content + productSuggestion;
				}

				return (language == "vi" ? "Ôi, có chút lỗi nhỏ rồi! 😅 Thử lại nha, mình sẽ làm tốt hơn!" : "Oops, a tiny error! 😅 Try again, I’ll nail it!") + productSuggestion;
			}
		}

		private bool IsProductRelatedQuery(string message)
		{
			string[] productTriggers = { "thông tin", "sản phẩm", "giá", "cấu hình", "ra mắt", "chi tiết", "information", "product", "price", "specs", "release", "details" };
			return productTriggers.Any(trigger => message.Contains(trigger)) &&
				   !_context.Products.All(p => !message.Contains(p.Name.ToLower()));
		}

		private async Task<ProductModel> FindProductByMessage(string message)
		{
			var products = await _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.ToListAsync();

			string[] keywords = message.Split(new[] { ' ', ',', '.', '!' }, StringSplitOptions.RemoveEmptyEntries);
			ProductModel bestMatch = null;
			int highestScore = 0;

			foreach (var product in products)
			{
				string productNameLower = product.Name.ToLower();
				string descriptionLower = product.Description.ToLower();

				if (productNameLower == message || descriptionLower.Contains(message))
				{
					return product;
				}

				int score = 0;
				foreach (var keyword in keywords)
				{
					if (productNameLower.Contains(keyword) || descriptionLower.Contains(keyword))
					{
						score++;
					}
				}

				if (score > highestScore)
				{
					highestScore = score;
					bestMatch = product;
				}
			}

			return bestMatch;
		}

		private bool IsCategoryQuery(string message)
		{
			var categories = _context.Categories.Select(c => c.Name.ToLower()).ToList();
			if (message.Contains("laptop") && categories.Contains("máy tính xách tay"))
			{
				return true;
			}
			return categories.Any(c => message.Contains(c));
		}

		private async Task<List<ProductModel>> FindProductsByCategory(string message)
		{
			var categories = await _context.Categories.ToListAsync();
			var matchingCategory = categories.FirstOrDefault(c => message.Contains(c.Name.ToLower()));
			if (matchingCategory != null)
			{
				return await _context.Products
					.Where(p => p.CategoryId == matchingCategory.Id)
					.ToListAsync();
			}
			return new List<ProductModel>();
		}

		private bool IsBrandQuery(string message)
		{
			var brands = _context.BrandModels.Select(b => b.Name.ToLower()).ToList();
			return brands.Any(b => message.Contains(b));
		}

		private async Task<List<ProductModel>> FindProductsByBrand(string message)
		{
			var brands = await _context.BrandModels.ToListAsync();
			var matchingBrand = brands.FirstOrDefault(b => message.Contains(b.Name.ToLower()));
			if (matchingBrand != null)
			{
				return await _context.Products
					.Where(p => p.BrandId == matchingBrand.Id)
					.ToListAsync();
			}
			return new List<ProductModel>();
		}

		private bool IsCategoryAndBrandQuery(string message)
		{
			var categories = _context.Categories.Select(c => c.Name.ToLower()).ToList();
			var brands = _context.BrandModels.Select(b => b.Name.ToLower()).ToList();
			return categories.Any(c => message.Contains(c)) && brands.Any(b => message.Contains(b));
		}

		private async Task<List<ProductModel>> FindProductsByCategoryAndBrand(string message)
		{
			var categories = await _context.Categories.ToListAsync();
			var brands = await _context.BrandModels.ToListAsync();

			var matchingCategory = categories.FirstOrDefault(c => message.Contains(c.Name.ToLower()));
			var matchingBrand = brands.FirstOrDefault(b => message.Contains(b.Name.ToLower()));

			if (matchingCategory != null && matchingBrand != null)
			{
				return await _context.Products
					.Where(p => p.CategoryId == matchingCategory.Id && p.BrandId == matchingBrand.Id)
					.ToListAsync();
			}
			return new List<ProductModel>();
		}

		private async Task<List<ProductModel>> FindProductsByCategoryAndBudget(string message, decimal budget)
		{
			var categories = await _context.Categories.ToListAsync();
			var matchingCategory = categories.FirstOrDefault(c => message.Contains(c.Name.ToLower()));
			if (matchingCategory != null)
			{
				return await _context.Products
					.Where(p => p.CategoryId == matchingCategory.Id && p.Price <= budget)
					.Take(5)
					.ToListAsync();
			}
			return new List<ProductModel>();
		}
	}
}