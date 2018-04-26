using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// inkshapes

namespace ShapeTrainer
{
    public sealed class InkshapesModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class InkshapesModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public InkshapesModelOutput(int lossCount)
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>();
            for (int i = 0; i < lossCount; i++)
            {
                this.loss.Add(i.ToString(), float.NaN);
            }
        }
    }

    public sealed class InkshapesModel
    {
        private int _lossCount;

        private LearningModelPreview learningModel;
        public static async Task<InkshapesModel> CreateInkshapesModel(StorageFile file, int lossCount)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            InkshapesModel model = new InkshapesModel();
            model.learningModel = learningModel;
            model._lossCount = lossCount;
            return model;
        }
        public async Task<InkshapesModelOutput> EvaluateAsync(InkshapesModelInput input) {
            InkshapesModelOutput output = new InkshapesModelOutput(_lossCount);
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
