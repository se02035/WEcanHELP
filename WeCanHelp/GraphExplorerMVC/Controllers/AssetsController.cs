using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using GraphExplorerMVC.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Office365.Discovery;
using Microsoft.Office365.SharePoint.CoreServices;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Common;
using Newtonsoft.Json;

namespace GraphExplorerMVC.Controllers
{
    public class AssetsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        #region Helper

        internal static async Task<SharePointClient> EnsureSharePointClientCreatedAsync()
        {
            string _clientId = ConfigurationManager.AppSettings["ida:ClientId"];
            string _clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];

            string _tenantId = ConfigurationManager.AppSettings["ida:TenantID"];
            string _aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
            string _authority = _aadInstance + _tenantId;

            string _discoverySvcEndpointUriStr = "https://api.office.com/discovery/v1.0/me/";
            Uri _discoverySvcEndpointUri = new Uri(_discoverySvcEndpointUriStr);
            string _discoverySvcResourceId = "https://api.office.com/discovery/";


            var signInUserId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var userObjectId = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            AuthenticationContext authContext = new AuthenticationContext(_authority, new ADALTokenCache(signInUserId));

            try
            {
                DiscoveryClient discClient = new DiscoveryClient(_discoverySvcEndpointUri,
                    async () =>
                    {
                        var authResult = await authContext.AcquireTokenSilentAsync(_discoverySvcResourceId,
                                                                                   new ClientCredential(_clientId,
                                                                                                        _clientSecret),
                                                                                   new UserIdentifier(userObjectId,
                                                                                                      UserIdentifierType.UniqueId));
                        return authResult.AccessToken;
                    });

                var dcr = await discClient.DiscoverCapabilityAsync("MyFiles");

                return new SharePointClient(dcr.ServiceEndpointUri,
                    async () =>
                    {
                        var authResult = await authContext.AcquireTokenSilentAsync(dcr.ServiceResourceId,
                                                                                   new ClientCredential(_clientId,
                                                                                                        _clientSecret),
                                                                                   new UserIdentifier(userObjectId,
                                                                                                      UserIdentifierType.UniqueId));

                        return authResult.AccessToken;
                    });
            }
            catch (AdalException exception)
            {
                //Partially handle token acquisition failure here and bubble it up to the controller
                if (exception.ErrorCode == AdalError.FailedToAcquireTokenSilently)
                {
                    authContext.TokenCache.Clear();
                    throw exception;
                }
                return null;
            }
        }

        public static async Task<ICollection<OneDriveFile>> GetFilesAsync(SharePointClient client)
        {
            var filesResults = await client.Files.ExecuteAsync();
            return filesResults.CurrentPage.Select(file => new OneDriveFile()
            {
                Name = file.Name, WebUrl = file.WebUrl, SourceId = file.Id
            }).ToList();
        }

        #endregion

        // GET: Assets
        public async Task<ActionResult> Index()
        {
            return View(await db.Assets.ToListAsync());
        }

        // GET: Assets/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asset asset = await db.Assets.FindAsync(id);
            if (asset == null)
            {
                return HttpNotFound();
            }
            return View(asset);
        }

        // GET: Assets/Create
        public async Task<ActionResult> Create()
        {
            var client = await EnsureSharePointClientCreatedAsync();
            var files = await GetFilesAsync(client);

            var model = new AddAssetViewModel()
            {
                Applications = await db.Applications.ToListAsync(),
                Asset = new Asset(),
                OneDriveFiles = files
            };

            return View(model);
        }

        // POST: Assets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Asset")] AddAssetViewModel model)
        {
            if (ModelState.IsValid)
            {
                var selectedApplication = await db.Applications.FirstAsync(app => app.Id == model.Asset.Application.Id);
                model.Asset.Application = selectedApplication;
                selectedApplication.Assets.Add(model.Asset);
                db.Assets.Add(model.Asset);
                await db.SaveChangesAsync();

                // download the selected file
                var client = await EnsureSharePointClientCreatedAsync();
                using (var stream = await client.Files.GetById(model.Asset.RawUrl).ToFile().DownloadAsync())
                {
                    // and copy it to the destination storage account
                    var storageAccount =
                        CloudStorageAccount.Parse(
                            @"DefaultEndpointsProtocol=https;AccountName=wchstorage;AccountKey=3eEeXTV0hn/sdYEyQQ5fMS6HMrLBZP6Fdm4EStxV+ySCWeUJLCYFddxQYTaDUZn+zZtdkud+JyRAu32kuYwpeA==;BlobEndpoint=https://wchstorage.blob.core.windows.net/;TableEndpoint=https://wchstorage.table.core.windows.net/;QueueEndpoint=https://wchstorage.queue.core.windows.net/;FileEndpoint=https://wchstorage.file.core.windows.net/");
                    var blobclient = storageAccount.CreateCloudBlobClient();
                    var container = blobclient.GetContainerReference("rawvideos");
                    var blockblob = container.GetBlockBlobReference(string.Format("{0}.mp4", model.Asset.RawUrl));

                    await blockblob.UploadFromStreamAsync(stream);
                }
                // queue a message for further processing


                ConnectionStringSettings connectionString = ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"];
                CloudStorageAccount storageAccount2 =
                    CloudStorageAccount.Parse(connectionString.ConnectionString);
                CloudQueueClient queueClient = storageAccount2.CreateCloudQueueClient();

                CloudQueue queue = queueClient.GetQueueReference("videoqueue");

                queue.CreateIfNotExists();

                VideoInformation videoInformation = new VideoInformation();
                videoInformation.Uri = new Uri(model.Asset.RawUrl+".mp4");
                videoInformation.Id = Guid.NewGuid();

                CloudQueueMessage message = new CloudQueueMessage(JsonConvert.SerializeObject(videoInformation));
                queue.AddMessage(message);

                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: Assets/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asset asset = await db.Assets.FindAsync(id);
            if (asset == null)
            {
                return HttpNotFound();
            }
            return View(asset);
        }

        // POST: Assets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Name,Description,RawUrl,Published")] Asset asset)
        {
            if (ModelState.IsValid)
            {
                db.Entry(asset).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(asset);
        }

        // GET: Assets/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Asset asset = await db.Assets.FindAsync(id);
            if (asset == null)
            {
                return HttpNotFound();
            }
            return View(asset);
        }

        // POST: Assets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Asset asset = await db.Assets.FindAsync(id);
            db.Assets.Remove(asset);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
