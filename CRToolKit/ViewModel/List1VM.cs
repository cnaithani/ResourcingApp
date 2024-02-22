using System;
using CRToolKit.DTO;
using CRToolKit.Models;
using System.Linq;
using CRToolKit.ViewModel;

namespace CRToolKit.Views
{
	public class List1VM:BaseViewModel
	{
        public List1VM()
        {
            PopulateCandidates();
        }

        public List<CandidateDTO> CandidateList { get; set; }
        public List<ProductListDTO> ProductList { get; set; }
        public string DisplayProductDescription { get; set; }
        public bool ShowAdd { get; set; } = false;

        public Command OnProductDetailNavigation
        {
            get
            {
                return new Command<Int32>(async (activitySummaryId) =>
                {
                    //var detailPage = new MainPage();
                    //await Navigation.PushModalAsync(detailPage);
                });
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
                Dimentions = string.Concat( x.Height.ToString() , " X " , x.Weidth.ToString(), " X " , x.Length.ToString()),
                DetailedDescription = x.DetailDescription

            }).ToList();
            ShowAdd = true;
            OnPropertyChanged("ProductList");
            OnPropertyChanged("ShowAdd");
        }

        private async void PopulateCandidates()
        {
            var candList = await App.Database.database.Table<Candidate>().OrderByDescending(x=>x.Modified).ToListAsync();
            CandidateList = candList.Select(x => new CandidateDTO
            {
                Id = x.Id,  
                Name = x.Name,
                Email = x.Email,
                Phone = x.Phone,
                KeySkills = x.Skills,
                FilePath = x.FilePath,
                Rating = x.Rating,  

            }).ToList();
            ShowAdd = true;
            OnPropertyChanged("ShowAdd");
            OnPropertyChanged("CandidateList");
        }

    }
}

