using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// emotion

namespace shapre_recognizer
{
    public sealed class EmotionModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class EmotionModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public EmotionModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>();
            for (int i = 0; i < 7; i++)
            {
                this.loss.Add(i.ToString(), float.NaN);
            }
        }
    }

    public sealed class EmotionModel
    {
        private LearningModelPreview learningModel;
        public static async Task<EmotionModel> CreateEmotionModel(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            EmotionModel model = new EmotionModel();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<EmotionModelOutput> EvaluateAsync(EmotionModelInput input) {
            EmotionModelOutput output = new EmotionModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
