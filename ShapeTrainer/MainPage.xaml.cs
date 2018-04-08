using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Training;
using MLHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ShapeTrainer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static string _trainingKey = "<TRAINING KEY>";
        private static TrainingApi _trainingApi = new TrainingApi() { ApiKey = _trainingKey };

        public MainPage()
        {
            this.InitializeComponent();

            Inker.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Pen |
            CoreInputDeviceTypes.Touch |
            CoreInputDeviceTypes.Mouse;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var bitmap = Inker.GetCropedSoftwareBitmap(newWidth: 200, newHeight: 200, keepRelativeSize: true);

            StorageFile file1 = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("test.png", CreationCollisionOption.GenerateUniqueName);
            await bitmap.SaveToFile(file1);
        }

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            Inker.InkPresenter.StrokeContainer.Clear();
        }
    }
}
