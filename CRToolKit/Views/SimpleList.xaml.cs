﻿using CRToolKit.Models;

namespace CRToolKit.Views;

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
        this.isLoadig.IsRunning = false;
        candidateList.IsVisible = true;


    }
}
