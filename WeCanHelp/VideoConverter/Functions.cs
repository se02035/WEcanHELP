using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.MediaServices.Client;

namespace VideoConverter
{
    public class Functions
    {
        private static readonly Lazy<MediaServicesCredentials> MediaServicesCredentials =
        new Lazy<MediaServicesCredentials>(() =>
        {
            var credentials = new MediaServicesCredentials(
                System.Configuration.ConfigurationManager.AppSettings["MediaServiceAccountName"],
                System.Configuration.ConfigurationManager.AppSettings["MediaServiceAccountKey"]);

            credentials.RefreshToken();
            return credentials;
        });

        public static void ProcessQueueMessage(
            [QueueTrigger("videoQueue")] VideoInformation videoInfo,
            [Blob("videos/{Name}", FileAccess.Read)] Stream input)
        {
            //using (Stream output = outputBlob.OpenWrite())
            //{
            //    ConvertImageToThumbnailJPG(input, output);
            //    outputBlob.Properties.ContentType = "image/jpeg";
            //}

            CancellationToken cancellationToken=new CancellationToken();
            CloudMediaContext context = new CloudMediaContext(MediaServicesCredentials.Value);

            UploadVideo(context, videoInfo.Name, videoInfo.Uri.ToString(), cancellationToken).ContinueWith(task=>CreateMediaEncodeJob(context, task.Result), cancellationToken);

        }

        private static async Task CreateMediaEncodeJob(CloudMediaContext context, string assetId)
        {
            var encodingPreset = "H264 Broadband 720p";
            IAsset assetToEncode = context.Assets.FirstOrDefault(a => a.Id == assetId);
            if (assetToEncode == null)
            {
                throw new ArgumentException("Could not find assetId: " + assetId);
            }

            IJob job = context.Jobs.Create("Encoding " + assetToEncode.Name + " to " + encodingPreset);

            IMediaProcessor latestWameMediaProcessor = (from p in context.MediaProcessors where p.Name == "Azure Media Encoder" select p).ToList().OrderBy(wame => new Version(wame.Version)).LastOrDefault();
            ITask encodeTask = job.Tasks.AddNew("Encoding", latestWameMediaProcessor, encodingPreset, TaskOptions.None);
            encodeTask.InputAssets.Add(assetToEncode);
            encodeTask.OutputAssets.AddNew(assetToEncode.Name + " as " + encodingPreset, AssetCreationOptions.None);

            job.StateChanged += new EventHandler<JobStateChangedEventArgs>((sender, jsc) => Console.WriteLine(string.Format("{0}\n  State: {1}\n  Time: {2}\n\n", ((IJob)sender).Name, jsc.CurrentState, DateTime.UtcNow.ToString(@"yyyy_M_d_hhmmss"))));
            //IJob job = await job.SubmitAsync();
            //await job.GetExecutionProgressTask(CancellationToken.None);

            var preparedAsset = job.OutputMediaAssets.FirstOrDefault();
        }

        public static async Task<string> UploadVideo(CloudMediaContext context, string name, string address, CancellationToken cancellationToken)
        {
            IAsset uploadAsset = await context.Assets.CreateAsync(Path.GetFileNameWithoutExtension(name), AssetCreationOptions.None, cancellationToken);
            IAssetFile assetFile = await uploadAsset.AssetFiles.CreateAsync(name, cancellationToken);
            BlobTransferClient client = new BlobTransferClient();
            IAccessPolicy accessPolicy = context.AccessPolicies.Create("Write", TimeSpan.FromMinutes(5), AccessPermissions.Write);
            ILocator locator = context.Locators.CreateSasLocator(uploadAsset, accessPolicy);
            await assetFile.UploadAsync(address,client, locator, cancellationToken);
            return uploadAsset.Id;
        }
    }
}