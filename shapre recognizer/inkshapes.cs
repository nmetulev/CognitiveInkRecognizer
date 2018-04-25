using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// inkshapes

namespace shapre_recognizer
{
    public sealed class InkshapesModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class InkshapesModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public InkshapesModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>();
            for (int i = 0; i < 21; i++)
            {
                this.loss.Add(i.ToString(), float.NaN);
            }
        }
    }

    public sealed class InkshapesModel
    {
        private LearningModelPreview learningModel;
        public static async Task<InkshapesModel> CreateInkshapesModel(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            InkshapesModel model = new InkshapesModel();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<InkshapesModelOutput> EvaluateAsync(InkshapesModelInput input) {
            InkshapesModelOutput output = new InkshapesModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
