using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using MLHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
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
        private static string _trainingKey = "12c3a485406448c9b85b9d83b08a896f";
        private static string _customVisionProjectName = "InkShapes";
        private static TrainingApi _trainingApi = new TrainingApi() { ApiKey = _trainingKey };
        private static Project _project;
        private static TagList _tags;

        private Tag _currentTag;
        private Random random = new Random((int)DateTime.Now.Ticks);

        public MainPage()
        {
            this.InitializeComponent();

            Inker.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Pen |
            CoreInputDeviceTypes.Touch |
            CoreInputDeviceTypes.Mouse;

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Color.FromArgb(0, 0x36, 0x45, 0x4f);
            drawingAttributes.IgnorePressure = false;
            drawingAttributes.FitToCurve = true;
            drawingAttributes.Size = new Size(4, 4);
            Inker.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        }

        private async Task<bool> SetupTraining()
        {
            try
            {
                var projects = _trainingApi.GetProjects();
                _project = (from p in projects where p.Name == _customVisionProjectName select p).FirstOrDefault();

                if (_project == null)
                {
                    return false;
                }

                _tags = await _trainingApi.GetTagsAsync(_project.Id);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (await SetupTraining())
            {
                SetupNextTag();

                FakeSplashScreen.Visibility = Visibility.Collapsed;
                Title.Visibility = Visibility.Visible;

                if (SystemInformation.IsFirstRun)
                {
                    FindName("FirstRun");
                    MarkdownText.Text = (await FileIO.ReadTextAsync(await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///FirstRun.md"))));
                }
                else
                    TrainerGrid.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void FirstRunStartClicked(object sender, RoutedEventArgs e)
        {
            FirstRun.Visibility = Visibility.Collapsed;
            TrainerGrid.Visibility = Visibility.Visible;
        }

        private void SetupNextTag()
        {
            Tag tag = null;
            do
            {
                tag = _tags.Tags[random.Next(0, _tags.Tags.Count)];
            }
            while (tag == _currentTag);

            _currentTag = tag;


            TagText.Text = $"{ _currentTag.Name.Replace('_', ' ').ToUpper()}";


        }

        private async Task SendShapeForTraining(SoftwareBitmap bitmap, string tagId)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(bitmap);

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception)
                {
                    // nop
                }

                await _trainingApi.CreateImagesFromDataAsync(_project.Id, stream.AsStream(), new List<string> { tagId });
            }

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // skip shape if nothing is drawn
            if (Inker.InkPresenter.StrokeContainer.GetStrokes().Count > 0)
            {
                var tagName = _currentTag.Name;
                var tagId = _currentTag.Id.ToString();
                var bitmap = Inker.GetCropedSoftwareBitmap(newWidth: 200, newHeight: 200, keepRelativeSize: true);

                Inker.InkPresenter.StrokeContainer.Clear();

                StorageFile file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync($"{tagName}.png", CreationCollisionOption.GenerateUniqueName);
                bitmap.SaveToFile(file);
                SendShapeForTraining(bitmap, tagId);
            }

            SetupNextTag();
        }

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            Inker.InkPresenter.StrokeContainer.Clear();
        }

        private async void MarkdownText_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }
    }
}
