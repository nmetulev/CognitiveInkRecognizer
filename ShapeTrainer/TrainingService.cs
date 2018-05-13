using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Cognitive.CustomVision.Training;
using Microsoft.Cognitive.CustomVision.Training.Models;
using MLHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace ShapeTrainer
{
    public class TrainingService
    {
        private static string _trainingKey = "12c3a485406448c9b85b9d83b08a896f";
        private static string _customVisionProjectName = "InkShapes";

        private static string _blobConnectionString = "";
        private static CloudStorageAccount _storageAccount;
        private static CloudBlobClient _blobClient;
        private static Dictionary<string, CloudBlobContainer> _containers = new Dictionary<string, CloudBlobContainer>();

        private static TrainingApi _trainingApi = new TrainingApi() { ApiKey = _trainingKey };
        private static Project _project;

        private static bool _initialized = false;

        public static TagList Tags { get; private set; }

        public static async Task<bool> Initialize()
        {
            if (!_initialized)
            {
                _initialized = await SetupTraining();
            }

            return _initialized;
        }

        public static async Task SendShapeForTraining(SoftwareBitmap bitmap, string tagId)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetSoftwareBitmap(bitmap);

                try
                {
                    await encoder.FlushAsync();
                    await _trainingApi.CreateImagesFromDataAsync(_project.Id, stream.AsStream(), new List<string> { tagId });
                }
                catch (Exception)
                {
                    // nop
                }
            }
        }

        public static async Task SaveShapeDataToBlobStorage(Stream stream, string tag)
        {
            if (_blobClient == null)
            {
                return;
            }

            tag = tag.Replace("_", "");

            if (!_containers.TryGetValue(tag, out var container))
            {
                container = _blobClient.GetContainerReference(tag);
                await container.CreateIfNotExistsAsync();
                _containers.Add(tag, container);
            }

            var blob = container.GetBlockBlobReference($"{tag}-{Guid.NewGuid().ToString()}.gif");
            await blob.UploadFromStreamAsync(stream);

            stream.Dispose();
        }

        public static async Task SaveBitmapToFile(SoftwareBitmap bitmap, string filename)
        {
            StorageFile file = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync($"{filename}.png", CreationCollisionOption.GenerateUniqueName).AsTask();
            await bitmap.SaveToFile(file);
        }

        private static async Task<bool> SetupTraining()
        {
            try
            {
                var projects = _trainingApi.GetProjects();
                _project = (from p in projects where p.Name == _customVisionProjectName select p).FirstOrDefault();

                if (_project == null)
                {
                    return false;
                }

                Tags = await _trainingApi.GetTagsAsync(_project.Id);

                if (CloudStorageAccount.TryParse(_blobConnectionString, out _storageAccount))
                {
                    _blobClient = _storageAccount.CreateCloudBlobClient();
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                return false;
            }

            return true;
        }
    }
}
