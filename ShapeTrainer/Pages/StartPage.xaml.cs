using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace ShapeTrainer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (await TrainingService.Initialize())
            {
                FakeSplashScreen.Visibility = Visibility.Collapsed;
                Title.Visibility = Visibility.Visible;

                if (SystemInformation.IsFirstRun)
                {
                    FindName("FirstRun");
                    MarkdownText.Text = (await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///FirstRun.md"))));
                }
                else
                {
                    MainContent.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void FirstRunStartClicked(object sender, RoutedEventArgs e)
        {
            FirstRun.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
        }

        private async void MarkdownText_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }

        private void DrawClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DrawPage));
        }

        private void GuessClicked(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(GuessPage));
        }
    }
}
