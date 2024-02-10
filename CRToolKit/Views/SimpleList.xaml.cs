using CRToolKit.Models;

namespace CRToolKit.Views;

public partial class SimpleList : ContentPage
{
	public List1VM currentVM;
	public SimpleList()
	{
        InitializeComponent();       
    }

    protected override void OnAppearing()
    {
        currentVM = new List1VM();
        BindingContext = currentVM;
    }
}
