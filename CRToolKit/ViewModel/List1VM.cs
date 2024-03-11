using System;
using ResourcingToolKit.DTO;
using ResourcingToolKit.Models;
using System.Linq;
using ResourcingToolKit.ViewModel;
using CommunityToolkit.Maui.Core.Views;
using System.Collections.ObjectModel;

namespace ResourcingToolKit.Views
{
	public class List1VM:BaseViewModel
	{
        public  List1VM()
        {
            
        }

        public ObservableCollection<CandidateDTO> CandidateList { get; set; }
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

        public async Task PopulateCandidates()
        {
            try
            {
                if (CandidateList == null){
                    CandidateList = new ObservableCollection<CandidateDTO>();
                }

                var candList = await App.Database.database.Table<Candidate>().OrderByDescending(x => x.Modified).Take(10).ToListAsync();
                var cList = candList.Select(x => new CandidateDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    KeySkills = x.Skills,
                    FilePath = x.FilePath,
                    Rating = x.Rating,

                }).ToList();
                CandidateList.Clear();

                foreach(var item in cList){
                    CandidateList.Add(item);
                }

            }
            catch(Exception e)
            {
                var srr = e.Message; 
            }
            ShowAdd = true;
            OnPropertyChanged("ShowAdd");
            OnPropertyChanged("CandidateList");
        }

    }
}

