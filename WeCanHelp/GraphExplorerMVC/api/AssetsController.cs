using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Routing;
using GraphExplorerMVC.Models;

namespace GraphExplorerMVC.api
{
    /*
    The WebApiConfig class may require additional changes to add a route for this controller. Merge these statements into the Register method of the WebApiConfig class as applicable. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using GraphExplorerMVC.Models;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Asset>("Assets");
    builder.EntitySet<Application>("Applications"); 
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
    public class AssetsController : ODataController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: odata/Assets
        // SAMPLE: For getting all assets for a certain application use
        // "http://localhost:30883/odata/Assets?$filter=Application/Id eq 2"
        [EnableQuery]
        public IQueryable<Asset> GetAssets()
        {
            return db.Assets;
        }

        // GET: odata/Assets(5)
        [EnableQuery]
        public SingleResult<Asset> GetAsset([FromODataUri] int key)
        {
            return SingleResult.Create(db.Assets.Where(asset => asset.Id == key));
        }

        // GET: odata/Assets(5)/Application
        [EnableQuery]
        public SingleResult<Application> GetApplication([FromODataUri] int key)
        {
            return SingleResult.Create(db.Assets.Where(m => m.Id == key).Select(m => m.Application));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AssetExists(int key)
        {
            return db.Assets.Count(e => e.Id == key) > 0;
        }
    }
}
