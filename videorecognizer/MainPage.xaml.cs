using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace videorecognizer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            Init();
        }

        InkshapesModel _model;

        private async Task Init()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///model.onnx"));
            _model = await InkshapesModel.CreateInkshapesModel(file, 21);

        }

        private async void CameraPreview_FrameArrived(object sender, Microsoft.Toolkit.Uwp.Helpers.CameraHelper.FrameEventArgs e)
        {
            if (_model == null || e.VideoFrame == null)
            {
                return;
            }

            var input = new InkshapesModelInput();
            input.data = e.VideoFrame;

            var output = await _model.EvaluateAsync(input);

            var guessedTag = output.classLabel.First();
            var guessedPercentage = output.loss.OrderByDescending(kv => kv.Value).First().Value.ToString();

            await GuessText.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                GuessText.Text = $"Current Guess: {guessedTag}({guessedPercentage})";
            });
        }
    }
}
