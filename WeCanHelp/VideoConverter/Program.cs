using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace VideoConverter
{
    class Program
    {
        static void Main()
        {
            ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"];
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(connectionString.ConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue queue = queueClient.GetQueueReference("videoqueue");

            queue.CreateIfNotExists();

            VideoInformation videoInformation = new VideoInformation();
            videoInformation.Uri = new Uri("https://wchhack.blob.core.windows.net/asset-9320445d-1500-80c4-b779-f1e5791e1257/WP_20150422_19_37_17_Pro.mp4");
            videoInformation.Id = Guid.NewGuid();

            CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(videoInformation));
            queue.AddMessage(message);

            JobHost host = new JobHost();
            host.RunAndBlock();
        }
    }
}
