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
                MediaServicesCredentials credentials = new MediaServicesCredentials(
                    ConfigurationManager.AppSettings["MediaServiceAccountName"],
                    ConfigurationManager.AppSettings["MediaServiceAccountKey"]);

                credentials.RefreshToken();
                return credentials;
            });

        public static async void ProcessQueueMessage(
            [QueueTrigger("videoqueue")] VideoInformation videoInfo,
            [Blob("videos/{Name}", FileAccess.Read)] Stream input)
        {
            CloudMediaContext context = new CloudMediaContext(MediaServicesCredentials.Value);

            string mediaId = await UploadVideo(context, videoInfo.Name, videoInfo.Uri.ToString());

            IJob job = await CreateMediaEncodeJob(context, mediaId);
            await job.GetExecutionProgressTask(CancellationToken.None);
        }

        private static async Task<IJob> CreateMediaEncodeJob(CloudMediaContext context, string assetId)
        {
            string encodingPreset = "H264 Broadband 720p";
            IAsset assetToEncode = context.Assets.FirstOrDefault(a => a.Id == assetId);
            if (assetToEncode == null)
            {
                throw new ArgumentException("Could not find assetId: " + assetId);
            }

            IJob job = context.Jobs.Create("Encoding " + assetToEncode.Name + " to " + encodingPreset);

            IMediaProcessor latestWameMediaProcessor =
                (from p in context.MediaProcessors where p.Name == "Azure Media Encoder" select p).ToList()
                    .OrderBy(wame => new Version(wame.Version))
                    .LastOrDefault();
            ITask encodeTask = job.Tasks.AddNew("Encoding", latestWameMediaProcessor, encodingPreset, TaskOptions.None);
            encodeTask.InputAssets.Add(assetToEncode);
            encodeTask.OutputAssets.AddNew(assetToEncode.Name + " as " + encodingPreset, AssetCreationOptions.None);

            job.StateChanged += new EventHandler<JobStateChangedEventArgs>((sender, jsc) => Console.WriteLine(
                $"{((IJob) sender).Name}\n  State: {jsc.CurrentState}\n  Time: {DateTime.UtcNow.ToString(@"yyyy_M_d_hhmmss")}\n\n"));
            return await job.SubmitAsync();
        }

        public static async Task<string> UploadVideo(CloudMediaContext context, string name, string address)
        {
            IAsset uploadAsset =
                await
                    context.Assets.CreateAsync(Path.GetFileNameWithoutExtension(name), AssetCreationOptions.None,
                        CancellationToken.None);
            IAssetFile assetFile = await uploadAsset.AssetFiles.CreateAsync(name, CancellationToken.None);

            IAccessPolicy accessPolicy = context.AccessPolicies.Create(uploadAsset.Name, TimeSpan.FromMinutes(5),
                AccessPermissions.Write);
            ILocator locator = context.Locators.CreateSasLocator(uploadAsset, accessPolicy);
            BlobTransferClient client = new BlobTransferClient()
            {
                NumberOfConcurrentTransfers = 5,
                ParallelTransferThreadCount = 5
            };
            await assetFile.UploadAsync(address, client, locator, CancellationToken.None);

            locator.Delete();
            accessPolicy.Delete();

            return uploadAsset.Id;
        }
    }
}