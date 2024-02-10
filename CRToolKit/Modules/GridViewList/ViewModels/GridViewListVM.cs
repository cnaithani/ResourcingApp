using System;
using CRToolKit.DTO;
using CRToolKit.Models;

namespace CRToolKit.ViewModel
{
	public class GridViewListVM:BaseViewModel
	{
        private double imageHeightRequest;
        public GridViewListVM()
		{
            PopulateProds();

        }
        public List<ProductListDTO> ProductList { get; set; }

        public double ImageHeightRequest
        {
            get { return imageHeightRequest; }
            set
            {
                this.imageHeightRequest = value;
                this.OnPropertyChanged(nameof(ImageHeightRequest));
            }
        }

        private async void PopulateProds()
        {
            var prodList = await App.Database.database.Table<Product>().ToListAsync();
            ProductList = prodList.Select(x => new ProductListDTO
            {
                ProductId = x.Id,
                Code = x.Code,
                Description = x.Description,
                Dimentions = string.Concat(x.Height.ToString(), " X ", x.Weidth.ToString(), " X ", x.Length.ToString()),
                DetailedDescription = x.DetailDescription,
                Image= "dotnet_bot.png"               

            }).ToList();
            OnPropertyChanged("ProductList");
        }

    }
}

