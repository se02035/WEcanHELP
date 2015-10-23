using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using DataLayer;
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
            //[Blob("asset-9320445d-1500-80c4-b779-f1e5791e1257/{Name}", FileAccess.Read)] Stream input)
            [Blob("rawvideos/{AssetName}", FileAccess.Read)]
            Stream input)
        {
            CloudMediaContext context = new CloudMediaContext(MediaServicesCredentials.Value);

            //string file = CopyFileLocaly(input, videoInfo.Name);
            string file = CopyFileLocaly(input, videoInfo.AssetName);

            //string mediaId = await UploadVideo(context, videoInfo.Name, videoInfo.Uri.ToString());
            IAsset mediaAsset = await UploadVideo(context, Path.GetFileName(file), file);

            IJob job = await CreateMediaEncodeJob(context, mediaAsset);
            await job.GetExecutionProgressTask(CancellationToken.None);
            using (WeCanHelpContext dataContext = new WeCanHelpContext())
            {
                try
                {
                    Asset asset = dataContext.Assets.First(a => a.Id.Equals(videoInfo.AssetId));
                    asset.RawUrl = string.Concat("http://wchhackmedia.streaming.mediaservices.windows.net/1e6588c7-0dab-40c7-8cf3-1db199a9b6ca/",
                        videoInfo.AssetName);
                    asset.Published = string.Concat("http://wchhackmedia.streaming.mediaservices.windows.net/1e6588c7-0dab-40c7-8cf3-1db199a9b6ca/",
                        Path.GetFileNameWithoutExtension(videoInfo.AssetName),
                        ".ism/Manifest");
                    dataContext.SaveChanges();
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private static string CopyFileLocaly(Stream input, string name)
        {
            //string filaPath = Path.GetTempFileName();
            string filaPath = Path.Combine(Path.GetTempPath(), name);
            //input.CopyTo(File.Open(filaPath, FileMode.Append, FileAccess.Write, FileShare.Read));
            input.CopyTo(File.Open(filaPath, FileMode.Create, FileAccess.Write, FileShare.Read));
            return filaPath;
        }

        private static async Task<IJob> CreateMediaEncodeJob(CloudMediaContext context, IAsset assetToEncode)
        {
            //string encodingPreset = "H264 Broadband 720p";
            string encodingPreset = "H264 Smooth Streaming 720p";

            IJob job = context.Jobs.Create("Encoding " + assetToEncode.Name + " to " + encodingPreset);

            string[] y =
                context.MediaProcessors.Where(mp => mp.Name.Equals("Azure Media Encoder"))
                    .ToArray()
                    .Select(mp => mp.Version)
                    .ToArray();

            IMediaProcessor latestWameMediaProcessor =
                context.MediaProcessors.Where(mp => mp.Name.Equals("Azure Media Encoder"))
                    .ToArray()
                    .OrderByDescending(
                        mp =>
                            new Version(int.Parse(mp.Version.Split('.', ',')[0]),
                                int.Parse(mp.Version.Split('.', ',')[1])))
                    .First();

            ITask encodeTask = job.Tasks.AddNew("Encoding", latestWameMediaProcessor, encodingPreset, TaskOptions.None);
            encodeTask.InputAssets.Add(assetToEncode);
            encodeTask.OutputAssets.AddNew(assetToEncode.Name + " as " + encodingPreset, AssetCreationOptions.None);

            job.StateChanged += new EventHandler<JobStateChangedEventArgs>((sender, jsc) => Console.WriteLine(
                $"{((IJob) sender).Name}\n  State: {jsc.CurrentState}\n  Time: {DateTime.UtcNow.ToString(@"yyyy_M_d_hhmmss")}\n\n"));
            return await job.SubmitAsync();
        }

        public static async Task<IAsset> UploadVideo(CloudMediaContext context, string name, string address)
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

            return uploadAsset;
        }
    }
}