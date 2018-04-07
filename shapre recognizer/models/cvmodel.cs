using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Storage;
using Windows.AI.MachineLearning.Preview;

// cvmodel

namespace shapre_recognizer
{
    public sealed class CvmodelModelInput
    {
        public VideoFrame data { get; set; }
    }

    public sealed class CvmodelModelOutput
    {
        public IList<string> classLabel { get; set; }
        public IDictionary<string, float> loss { get; set; }
        public CvmodelModelOutput()
        {
            this.classLabel = new List<string>();
            this.loss = new Dictionary<string, float>();
        }
    }

    public sealed class CvmodelModel
    {
        private LearningModelPreview learningModel;
        public static async Task<CvmodelModel> CreateCvmodelModel(StorageFile file)
        {
            LearningModelPreview learningModel = await LearningModelPreview.LoadModelFromStorageFileAsync(file);
            CvmodelModel model = new CvmodelModel();
            model.learningModel = learningModel;
            return model;
        }
        public async Task<CvmodelModelOutput> EvaluateAsync(CvmodelModelInput input) {
            CvmodelModelOutput output = new CvmodelModelOutput();
            LearningModelBindingPreview binding = new LearningModelBindingPreview(learningModel);
            binding.Bind("data", input.data);
            binding.Bind("classLabel", output.classLabel);
            binding.Bind("loss", output.loss);
            LearningModelEvaluationResultPreview evalResult = await learningModel.EvaluateAsync(binding, string.Empty);
            return output;
        }
    }
}
