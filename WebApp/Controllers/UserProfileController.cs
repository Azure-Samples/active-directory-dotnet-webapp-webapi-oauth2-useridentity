//----------------------------------------------------------------------------------------------
//    Copyright 2014 Microsoft Corporation
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//----------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

// The following using statements were added for this sample.
using System.Configuration;
using System.Threading.Tasks;
using WebApp.Models;
using System.Security.Claims;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace WebApp.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private string graphResourceId = ConfigurationManager.AppSettings["ida:GraphResourceId"];
        private string graphUserUrl = ConfigurationManager.AppSettings["ida:GraphUserUrl"];
        
        // If you are adapting an application that authenticates Azure AD users using Windows Identity Foundation,
        // you can get the user's Tenant ID from ClaimsPrincipal.Current.  Otherwise, this sample caches the user's
        // Tenant ID when it is obtained during the OAuth authorization flow.
        // private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

        //
        // GET: /UserProfile/
        public async Task<ActionResult> Index()
        {
            //
            // Retrieve the user's name, tenantID, and access token since they are parameters used to query the Graph API.
            //
            UserProfile profile = null;
            string accessToken = null;
            
            // If you authenticated an Azure AD user using Windows Identity Foundation, you can use ClaimsPrincipal.Current to get the user's Tenant ID.
            // string tenantId = ClaimsPrincipal.Current.FindFirst(TenantIdClaimType).Value;
            string tenantId = (string)OAuthController.GetFromCache("TenantId");

            if (tenantId != null)
            {
                accessToken = OAuthController.GetAccessTokenFromCacheOrRefreshToken(tenantId, graphResourceId);
            }

            //
            // If the user doesn't have an access token, they need to re-authorize.
            //
            if (accessToken == null)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                //

                // Remember where to bring the user back to in the application after the authorization code response is handled.
                OAuthController.SaveInCache("RedirectTo", Request.Url);

                profile = new UserProfile();
                profile.DisplayName = " ";
                profile.GivenName = " ";
                profile.Surname = " ";
                ViewBag.ErrorMessage = "AuthorizationRequired";
                ViewBag.AuthorizationUrl = OAuthController.GetAuthorizationUrl(graphResourceId, this.Request);

                return View(profile);
            }

            //
            // Call the Graph API and retrieve the user's profile.
            //
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                graphUserUrl,
                HttpUtility.UrlEncode(tenantId));
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            //
            // Return the user's profile in the view.
            //
            if (response.IsSuccessStatusCode)
            {
                string responseString = await response.Content.ReadAsStringAsync();
                profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
            }
            else
            {
                //
                // If the call failed, then drop the current access token and show the user an error indicating they might need to sign-in again.
                //
                OAuthController.RemoveAccessTokenFromCache(graphResourceId);

                profile = new UserProfile();
                profile.DisplayName = " ";
                profile.GivenName = " ";
                profile.Surname = " ";
                ViewBag.ErrorMessage = "UnexpectedError";
            }

            return View(profile);
        }
    }
}