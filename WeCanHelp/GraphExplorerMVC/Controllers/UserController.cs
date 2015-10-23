using GraphExplorerMVC.Models;
using GraphExplorerMVC.Utils;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Office365.Discovery;
using Microsoft.Office365.SharePoint.CoreServices;

namespace GraphExplorerMVC.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        //public ActionResult Index()
        //{
        // return View();
        //}

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

        [Authorize]
        public async Task<ActionResult> Index()
        {
            var token = await GetAccessToken();
            var client = await EnsureSharePointClientCreatedAsync();
            var myfiles = client.Files;
            var mydrive = client.Drive;
            //var user = await UserDetailModel.GetUserDetail("me", token.AccessToken);
            return View();
        }

        [Authorize]
        public async Task<ActionResult> Detail(Guid id)
        {
            var token = await GetAccessToken();
            var user = await UserDetailModel.GetUserDetail(String.Format("{0}/users/{1}", SettingsHelper.AzureAdTenant, id.ToString()), token.AccessToken);
            return View(user);
        }

        private async Task<AuthenticationResult> GetAccessToken()
        {
            AuthenticationContext context = new AuthenticationContext(SettingsHelper.AzureADAuthority);
            var clientCredential = new ClientCredential(SettingsHelper.ClientId, SettingsHelper.ClientSecret);
            AuthenticationResult result = (AuthenticationResult)this.Session[SettingsHelper.UserTokenCacheKey];
            //return await context.AcquireTokenAsync(SettingsHelper.UnifiedApiResource, clientCredential);
            //return await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, clientCredential, SettingsHelper.UnifiedApiResource);
            return await context.AcquireTokenByRefreshTokenAsync(result.RefreshToken, clientCredential, "https://api.office.com/discovery/");
        }

    }
}