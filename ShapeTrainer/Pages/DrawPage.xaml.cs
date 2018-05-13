using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Cognitive.CustomVision;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using MLHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
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
    public sealed partial class DrawPage : Page
    {
        
        private static Random random = new Random(Guid.NewGuid().GetHashCode());

        private Queue<Tag> _previousTags = new Queue<Tag>();
        private Tag _currentTag;

        public DrawPage()
        {
            this.InitializeComponent();

            Inker.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Pen |
            CoreInputDeviceTypes.Touch |
            CoreInputDeviceTypes.Mouse;

            InkDrawingAttributes drawingAttributes = new InkDrawingAttributes();
            drawingAttributes.Color = Windows.UI.Color.FromArgb(0, 0x36, 0x45, 0x4f);
            drawingAttributes.IgnorePressure = true;
            drawingAttributes.Size = new Size(4, 4);
            Inker.InkPresenter.UpdateDefaultDrawingAttributes(drawingAttributes);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            SetupNextTag();
        }

        private void SetupNextTag()
        {
            Tag tag = null;
            do
            {
                tag = TrainingService.Tags.Tags[random.Next(0, TrainingService.Tags.Tags.Count)];
            }
            while (_previousTags.Contains(tag));

            _previousTags.Enqueue(tag);
            if (_previousTags.Count > TrainingService.Tags.Tags.Count / 2)
            {
                _previousTags.Dequeue();
            }
            _currentTag = tag;

            TagText.Text = $"{ _currentTag.Name.Replace('_', ' ').ToUpper()}";
        }

        private async void SubmitClicked(object sender, RoutedEventArgs e)
        {
            var dateStart = DateTime.Now;
            // skip shape if nothing is drawn
            if (Inker.InkPresenter.StrokeContainer.GetStrokes().Count > 0)
            {
                var tagName = _currentTag.Name;
                var tagId = _currentTag.Id.ToString();
                var bitmap = Inker.GetCropedSoftwareBitmap(newWidth: 200, newHeight: 200, keepRelativeSize: true);
                Debug.WriteLine($"timespent getting bitmap: {(DateTime.Now - dateStart).TotalMilliseconds}");

                // save ink data to blob storage
                InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
                await Inker.InkPresenter.StrokeContainer.SaveAsync(stream);
                stream.Seek(0);
                var nop = TrainingService.SaveShapeDataToBlobStorage(stream.AsStream(), tagName);
                Inker.InkPresenter.StrokeContainer.Clear();
                Debug.WriteLine($"timespent saving strokes: {(DateTime.Now - dateStart).TotalMilliseconds}");

                if (Debugger.IsAttached)
                {
                    nop = TrainingService.SaveBitmapToFile(bitmap, tagName);
                }

                // send bitmap to custom vision
                nop = TrainingService.SendShapeForTraining(bitmap, tagId);
            }

            SetupNextTag();
            Debug.WriteLine($"total timespent: {(DateTime.Now - dateStart).TotalMilliseconds}");
        }

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            Inker.InkPresenter.StrokeContainer.Clear();
        }

        private void SkipClicked(object sender, RoutedEventArgs e)
        {
            Inker.InkPresenter.StrokeContainer.Clear();
            SetupNextTag();
        }
    }
}
