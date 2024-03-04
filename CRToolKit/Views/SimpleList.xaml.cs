using ResourcingToolKit.Models;

namespace ResourcingToolKit.Views;

public partial class SimpleList : ContentPage
{
    public List1VM currentVM;
    public SimpleList()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        if (currentVM == null)
        {
            currentVM = new List1VM();
            BindingContext = currentVM;
        }

        this.isLoadig.IsRunning = true;
        candidateList.IsVisible = false;
        await currentVM.PopulateCandidates();
        candidateList.IsVisible = true;
        this.isLoadig.IsRunning = false;
    }


    private void ListView_Loaded(object sender, EventArgs e)
    {
        //this.isLoadig.IsRunning = false;
    }

    void Add_Clicked(System.Object sender, System.EventArgs e)
    {
        AppShell.Current.GoToAsync("///Home");
    }
}
