using System;
using CRToolKit.Global;

namespace CRToolKit.ViewModel
{
	public class AppSettingsVM : BaseViewModel
    {
		private bool _isDarkTheme;

        public AppSettingsVM()
		{

			_isDarkTheme = Application.Current.UserAppTheme == AppTheme.Dark;         

        }

		public bool IsDarkTheme
		{
			get
			{
				return _isDarkTheme;

            }
			set
			{
				_isDarkTheme = value;
                if (!_isDarkTheme)
                {
                    Application.Current.UserAppTheme = AppTheme.Light;
                    Preferences.Set(Constants.ThemeCaption, Constants.LightThemeName);

                }
                else
                {
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    OnPropertyChanged("IsDarkTheme");
                    Preferences.Set(Constants.ThemeCaption, Constants.LightThemeName);
                }
            }
		}
	}
}

