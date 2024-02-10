using CRToolKit.ViewModel;

namespace CRToolKit.Views;

public partial class GridViewList : ContentPage
{
    public GridViewListVM currentVM;
    public GridViewList()
    {
        InitializeComponent();
    }
    protected override void OnAppearing()
    {
        currentVM = new GridViewListVM();
        BindingContext = currentVM;


        //double spanCount = (this.listView.ItemsLayout as Syncfusion.Maui.ListView.GridLayout).SpanCount;
        ////Below calulation is to find the individual imageWidth

        //this.currentVM.ImageHeightRequest = (Window.Width - (spanCount * this.listView.ItemSpacing.Left) - (spanCount * this.listView.ItemSpacing.Right)) / spanCount;
        //this.listView.ItemSize = this.currentVM.ImageHeightRequest + 30;
    }
}


