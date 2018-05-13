using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;

namespace ShapeTrainer
{
    public class ModelInfo
    {
        private const string ROOT_URI = "https://github.com/nmetulev/CognitiveInkRecognizer/raw/winml/ShapeTrainer/model/";
        private const string VERSION_INFO_FILENAME = "model.json";
        private const string LOCAL_MODEL_FILENAME = "model.onnx";

        private const string SETTINGS_CURRENT_VERSION = "model_version";
        private const string SETTINGS_CURRENT_NUM_SHAPES = "model_num_shapes";

        private bool _isLocal = true;

        public static ModelInfo Instance;

        public StorageFile ModelFile;

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("modelName")]
        public string ModelName { get; set; }

        [JsonProperty("numShapes")]
        public int NumShapes { get; set; }


        

        public static async Task SetupModelInfo()
        {
            Instance = await GetLatestModelInfo();
            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.TryGetValue(SETTINGS_CURRENT_VERSION, out var currentVersionSetting);

            //if (!Instance._isLocal && (currentVersionSetting == null || (currentVersionSetting is int currentVersion && Instance.Version > currentVersion)))
            //{
            //    // download latest
            //    Instance.ModelFile = await Instance.DownloadLatestModel();
            //    if (Instance.ModelFile != null)
            //    {
            //        settings.Values[SETTINGS_CURRENT_VERSION] = Instance.Version;
            //        settings.Values[SETTINGS_CURRENT_NUM_SHAPES] = Instance.NumShapes;
            //    }
            //}

            //if (Instance.ModelFile == null)
            //{
            //    // get cached if available
            //    try
            //    {
            //        Instance.ModelFile = await ApplicationData.Current.LocalFolder.GetFileAsync(LOCAL_MODEL_FILENAME);
            //    }
            //    catch (Exception) {  }
            //}

            if (Instance.ModelFile == null)
            {
                // get packaged model if all else fails
                Instance.ModelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///model/model.onnx"));
            }
        }

        private static async Task<ModelInfo> GetLatestModelInfo()
        {
            ModelInfo modelInfo = null;
            try
            {
                using (var client = new HttpClient())
                {
                    var str = await client.GetStringAsync(new Uri(ROOT_URI + VERSION_INFO_FILENAME));
                    modelInfo = JsonConvert.DeserializeObject<ModelInfo>(str);
                    modelInfo._isLocal = false;
                    return modelInfo;
                }
            }
            catch (Exception) { }

            var settings = ApplicationData.Current.LocalSettings;
            settings.Values.TryGetValue(SETTINGS_CURRENT_VERSION, out var currentVersionSetting);
            settings.Values.TryGetValue(SETTINGS_CURRENT_NUM_SHAPES, out var currentNumShapesSetting);

            if (modelInfo == null && currentNumShapesSetting is int currentNumShapes && currentVersionSetting is int currentVersion)
            {
                modelInfo = new ModelInfo()
                {
                    Version = currentVersion,
                    NumShapes = currentNumShapes,
                    ModelName = LOCAL_MODEL_FILENAME
                };
            }
            else
            {
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///model/" + VERSION_INFO_FILENAME));
                var str = await FileIO.ReadTextAsync(file);
                modelInfo = JsonConvert.DeserializeObject<ModelInfo>(str);
            }

            return modelInfo;
        }

        private async Task<StorageFile> DownloadLatestModel()
        {
            try
            {

                using (var client = new HttpClient())
                using (var stream = await client.GetInputStreamAsync(new Uri(ROOT_URI + ModelName)))
                {
                    var modelFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(LOCAL_MODEL_FILENAME, CreationCollisionOption.ReplaceExisting);
                    using (var fileStream = await modelFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await stream.AsStreamForRead().CopyToAsync(fileStream.AsStream());
                    }
                }

                return await ApplicationData.Current.LocalFolder.GetFileAsync(LOCAL_MODEL_FILENAME);
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}
