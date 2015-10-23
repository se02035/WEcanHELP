using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace FakeAPI.Controllers
{
    
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<Video> Get()
        {
            return new List<Video> {
                new Video()
                {
                    Name = "Encoded from Media Services", Url = "https://aka.ms/azuremediaplayeriframe?autoplay=false&url=" + HttpUtility.UrlEncode("https://wchhack.blob.core.windows.net/asset-cc1e445d-1500-80c4-497e-f1e5791e3519/WP_20150422_19_37_17_Pro_H264_4500kbps_AAC_und_ch2_128kbps.mp4?sv=2012-02-12&sr=c&si=251b77b2-1657-4433-840f-286123d5c127&sig=WnYfAF3XDXsWV7Hwb6rweDAWugoh8oHmsEumZL1RfO4%3D&st=2015-10-23T00%3A38%3A00Z&se=2115-09-29T00%3A38%3A00Z")             
                },
                new Video()
                {
                    Name = "Sample", Url = "https://aka.ms/azuremediaplayeriframe?url=%2F%2Famssamples.streaming.mediaservices.windows.net%2F91492735-c523-432b-ba01-faba6c2206a2%2FAzureMediaServicesPromo.ism%2Fmanifest"
                }
            };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }

    public class Video
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }

    }

}
