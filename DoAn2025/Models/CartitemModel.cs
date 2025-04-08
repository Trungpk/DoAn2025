namespace DoAn2025.Models
{
	public class CartitemModel
	{
		public  long ProductId { get; set; }
		public string ProductName { get; set; }

		public int Quantity { get; set; }

		public decimal Price { get; set; }

		public decimal Total {
			get { return Quantity* Price; }
		}
		public string Image { get; set; }
		public CartitemModel() 
		{
			
		}
		public CartitemModel(ProductModel product)
		{
			ProductId = product.Id;
			ProductName = product.Name;
			Price = product.Price;
			Quantity = 1;
			Image = product.Image;
		}
	}
}
