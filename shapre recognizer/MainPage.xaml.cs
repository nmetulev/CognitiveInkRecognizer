using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace shapre_recognizer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        CvmodelModel model;
        EmotionModel emotionModel;

        DispatcherTimer _timer;
        DispatcherTimer _gameTimer;
        List<InkStroke> _strokes;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Models/cvmodel.onnx"));
            model = await CvmodelModel.CreateCvmodelModel(file);

            file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Models/emotion.onnx"));
            emotionModel = await EmotionModel.CreateEmotionModel(file);


            Inker.InkPresenter.InputDeviceTypes =
            CoreInputDeviceTypes.Pen |
            CoreInputDeviceTypes.Touch |
            CoreInputDeviceTypes.Mouse;

            Inker.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            Inker.InkPresenter.StrokeInput.StrokeStarted += StrokeInput_StrokeStarted;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += _timer_Tick;

            _gameTimer = new DispatcherTimer();
            //_gameTimer = TimeSpan.FromMilliseconds(1000 / 60);

            _strokes = new List<InkStroke>();
        }

        private void StrokeInput_StrokeStarted(InkStrokeInput sender, PointerEventArgs args)
        {
            _timer.Stop();
        }

        private async void _timer_Tick(object sender, object e)
        {
            _timer.Stop();
            Debug.WriteLine("object to recognize");

            if (_strokes.Count == 0) return;

            var boundingBox = GetSquareBoundingBoxForStrokes(_strokes);
            
            //var rectangle = new Rectangle()
            //{
            //    Stroke = new SolidColorBrush(Colors.Red),
            //    StrokeThickness = 2,
            //    Height = boundingBox.Height,
            //    Width = boundingBox.Width
            //};

            //Canvas.SetTop(rectangle, boundingBox.Y);
            //Canvas.SetLeft(rectangle, boundingBox.X);

            //Drawer.Children.Add(rectangle);

            var bitmapFromStrokes = await RenderStrokes(_strokes);
            var resizedBitmap = await CropAndResize(bitmapFromStrokes, boundingBox, 227, 227);
            var frame = VideoFrame.CreateWithSoftwareBitmap(resizedBitmap);
            CvmodelModelInput input = new CvmodelModelInput();
            input.data = frame;

            var output = await model.EvaluateAsync(input);

            FontIcon icon = new FontIcon();
            

            switch (output.classLabel.First())
            {
                case "flower":
                    icon.Style = this.Resources["FlowerFontIcon"] as Style;
                    break;
                case "car":
                    icon.Style = this.Resources["CarFontIcon"] as Style;
                    break;
                case "stickfigure":
                    icon.Style = this.Resources["StickFontIcon"] as Style;
                    break;
            }

            Canvas.SetTop(icon, boundingBox.Y + boundingBox.Height / 2 - 40);
            Canvas.SetLeft(icon, boundingBox.X + boundingBox.Width / 2 - 40);
            Drawer.Children.Add(icon);

            Inker.InkPresenter.StrokeContainer.DeleteSelected();


            //result.Text = output.classLabel.First();
            //resultPer.Text = output.loss.OrderByDescending(kv => kv.Value).First().Value.ToString();
            _strokes.Clear();
        }

        private void InkPresenter_StrokesCollected(Windows.UI.Input.Inking.InkPresenter sender, Windows.UI.Input.Inking.InkStrokesCollectedEventArgs args)
        {
            Debug.WriteLine("strokes collected");
            _timer.Stop();
            foreach (var stroke in args.Strokes)
            {
                _strokes.Add(stroke);
                stroke.Selected = true;
            }
            _timer.Start();
        }
        
        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            var imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///models/test.jpg"));
            SoftwareBitmap softwareBitmap;

            using (IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap representation of the file
                softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
            VideoFrame frame = VideoFrame.CreateWithSoftwareBitmap(await Resize(softwareBitmap, 277, 277));

            EmotionModelInput input = new EmotionModelInput();
            input.data = frame;

            var output = await emotionModel.EvaluateAsync(input);

            Debug.WriteLine(output.classLabel.First());



            //var button = sender as Button;
            //button.IsEnabled = false;

            //var buffer = await RenderImage();
            //var bitmap = SoftwareBitmap.CreateCopyFromBuffer(buffer, BitmapPixelFormat.Rgba8, 227, 227);
            //var frame = VideoFrame.CreateWithSoftwareBitmap(bitmap);

            //CvmodelModelInput input = new CvmodelModelInput();
            //input.data = frame;

            //var output = await model.EvaluateAsync(input);

            //result.Text = output.classLabel.First();
            //resultPer.Text = output.loss.OrderByDescending(kv => kv.Value).First().Value.ToString();


            //using (var client = new HttpClient())
            //{
            //    var url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v1.0/Prediction/dd105cca-c262-4d53-aa6d-69a3f8da8e9c/image";

            //    client.DefaultRequestHeaders.Add("Prediction-Key", "39c59d59c944411d88eb9e4fd20e84ec");

            //    var bytes = new byte[stream.Size];
            //    var reader = new DataReader(stream.GetInputStreamAt(0));
            //    await reader.LoadAsync((uint)(stream.Size));
            //    reader.ReadBytes(bytes);


            //    using (var content = new ByteArrayContent(bytes))
            //    {
            //        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            //        var response = await client.PostAsync(url, content);
            //        dynamic predictionResponse = await response.Content.ReadAsStringAsync()
            //            .ContinueWith((readTask) => JsonConvert.DeserializeObject(readTask.Result));
            //        result.Text = predictionResponse.Predictions[0].Tag;
            //        resultPer.Text = (predictionResponse.Predictions[0].Probability.Value * 100).ToString();
            //    }
            //}

            //button.IsEnabled = true;

        }

        public Rect GetSquareBoundingBoxForStrokes(List<InkStroke> strokes)
        {
            double xMin = double.PositiveInfinity;
            double xMax = 0;
            double yMin = double.PositiveInfinity;
            double yMax = 0;

            foreach (var stroke in strokes)
            {
                xMin = Math.Min(xMin, stroke.BoundingRect.X);
                xMax = Math.Max(xMax, stroke.BoundingRect.X + stroke.BoundingRect.Width);

                yMin = Math.Min(yMin, stroke.BoundingRect.Y);
                yMax = Math.Max(yMax, stroke.BoundingRect.Y + stroke.BoundingRect.Height);


                // stroke.Selected = true;
            }

            var width = xMax - xMin;
            var height = yMax - yMin;

            if (width > height)
            {
                yMin = yMin - (width - height) / 2;
                height = width;
            }
            else if (height > width)
            {
                xMin = xMin - (height - width) / 2;
                width = height;
            }


            return new Rect(xMin, yMin, width, height);
        }

        private async Task<IBuffer> RenderImage()
        {
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, 227, 227, 96);

            var stream = new InMemoryRandomAccessStream();

            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(Inker.InkPresenter.StrokeContainer.GetStrokes(), true);
            }

            var pixels = renderTarget.GetPixelBytes();
            return pixels.AsBuffer();
            //await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.);
            //StorageFile file1 = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("test.png", CreationCollisionOption.GenerateUniqueName);
            //using (var fileStream1 = await file1.OpenAsync(FileAccessMode.ReadWrite))
            //{
            //    await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), fileStream1.GetOutputStreamAt(0));
            //}
            //return stream;

            //return VideoFrame.CreateWithDirect3D11Surface(renderTarget);
        }

        private async Task<SoftwareBitmap> RenderStrokes(List<InkStroke> strokes)
        {
            CanvasDevice device = CanvasDevice.GetSharedDevice();
            CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)Inker.ActualWidth, (int)Inker.ActualHeight, 96);


            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Colors.White);
                ds.DrawInk(strokes, true);
            }

            //var stream = new InMemoryRandomAccessStream();
            //await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
            //StorageFile file1 = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("test.png", CreationCollisionOption.GenerateUniqueName);
            //using (var fileStream1 = await file1.OpenAsync(FileAccessMode.ReadWrite))
            //{
            //    await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), fileStream1.GetOutputStreamAt(0));
            //}

            return SoftwareBitmap.CreateCopyFromBuffer(renderTarget.GetPixelBytes().AsBuffer(), BitmapPixelFormat.Bgra8, (int)Inker.ActualWidth, (int)Inker.ActualHeight, BitmapAlphaMode.Premultiplied);
        }

        //courtesy of Vlad
        public static async Task<SoftwareBitmap> CropAndResize(SoftwareBitmap softwareBitmap, Rect bounds, float newWidth, float newHeight)
        {
            var resourceCreator = CanvasDevice.GetSharedDevice();
            using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(resourceCreator, softwareBitmap))
            using (var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, newWidth, newHeight, canvasBitmap.Dpi))
            using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
            using (var scaleEffect = new ScaleEffect())
            using (var cropEffect = new CropEffect())
            using (var atlasEffect = new AtlasEffect())
            {
                drawingSession.Clear(Colors.White);
                
                cropEffect.SourceRectangle = bounds;
                cropEffect.Source = canvasBitmap;

                atlasEffect.SourceRectangle = bounds;
                atlasEffect.Source = cropEffect;

                scaleEffect.Source = atlasEffect;
                scaleEffect.Scale = new System.Numerics.Vector2(newWidth / (float)bounds.Width, newHeight / (float)bounds.Height);
                drawingSession.DrawImage(scaleEffect);
                drawingSession.Flush();

                //var stream = new InMemoryRandomAccessStream();
                //await canvasRenderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                //StorageFile file1 = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("test.png", CreationCollisionOption.GenerateUniqueName);
                //using (var fileStream1 = await file1.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), fileStream1.GetOutputStreamAt(0));
                //}
                return SoftwareBitmap.CreateCopyFromBuffer(canvasRenderTarget.GetPixelBytes().AsBuffer(), BitmapPixelFormat.Bgra8, (int)newWidth, (int)newHeight, BitmapAlphaMode.Premultiplied);
            }

        }

        public static async Task<SoftwareBitmap> Resize(SoftwareBitmap softwareBitmap, float newWidth, float newHeight)
        {
            var resourceCreator = CanvasDevice.GetSharedDevice();
            using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(resourceCreator, softwareBitmap))
            using (var canvasRenderTarget = new CanvasRenderTarget(resourceCreator, newWidth, newHeight, canvasBitmap.Dpi))
            using (var drawingSession = canvasRenderTarget.CreateDrawingSession())
            using (var scaleEffect = new ScaleEffect())
            {
                drawingSession.Clear(Colors.White);

                scaleEffect.Source = canvasBitmap;
                scaleEffect.Scale = new System.Numerics.Vector2(newWidth / canvasBitmap.SizeInPixels.Width, newHeight / canvasBitmap.SizeInPixels.Height);
                drawingSession.DrawImage(scaleEffect);
                drawingSession.Flush();

                //var stream = new InMemoryRandomAccessStream();
                //await canvasRenderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                //StorageFile file1 = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync("test.png", CreationCollisionOption.GenerateUniqueName);
                //using (var fileStream1 = await file1.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    await RandomAccessStream.CopyAndCloseAsync(stream.GetInputStreamAt(0), fileStream1.GetOutputStreamAt(0));
                //}
                return SoftwareBitmap.CreateCopyFromBuffer(canvasRenderTarget.GetPixelBytes().AsBuffer(), BitmapPixelFormat.Bgra8, (int)newWidth, (int)newHeight, BitmapAlphaMode.Premultiplied);
            }

        }

        private void ClearClicked(object sender, RoutedEventArgs e)
        {
            Inker.InkPresenter.StrokeContainer.Clear();
        }


        GameObject stickFigure = null;


        private void GameLoop()
        {
            while (true)
            {
                if (stickFigure == null)
                {
                    return;
                }

            }
        }

    }

    public class GameObject
    {
        public double Speed { get; set; }
        public UIElement Element { get; set; }
    }
}
