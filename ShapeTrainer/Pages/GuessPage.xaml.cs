﻿using Microsoft.Cognitive.CustomVision.Training.Models;
using MLHelpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
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
    public sealed partial class GuessPage : Page
    {
        private InkshapesModel _model;

        private static Random random = new Random(Guid.NewGuid().GetHashCode());
        private Queue<Tag> _previousTags = new Queue<Tag>();

        private ObservableCollection<Tag> _currentTags = new ObservableCollection<Tag>();
        private Tag _guessedTag;

        public GuessPage()
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
            Inker.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ModelInfo.SetupModelInfo();
            _model = await InkshapesModel.CreateInkshapesModel(ModelInfo.Instance.ModelFile, ModelInfo.Instance.NumShapes);

            SetupNextTags();
        }

        private async void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            var bitmap = Inker.GetCropedSoftwareBitmap(newWidth: 227, newHeight: 227, keepRelativeSize: true);
            var frame = VideoFrame.CreateWithSoftwareBitmap(bitmap);
            var input = new InkshapesModelInput();
            input.data = frame;

            var output = await _model.EvaluateAsync(input);

            var guessedTag = output.classLabel.First();
            var guessedPercentage = output.loss.OrderByDescending(kv => kv.Value).First().Value.ToString();

            GuessText.Text = $"Current Guess: {guessedTag}({guessedPercentage})";
        }

        private void SetupNextTags()
        {
            _currentTags.Clear();

            for (var i = 0; i < 5; i++)
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

                _currentTags.Add(tag);
            }

        }
    }
}
