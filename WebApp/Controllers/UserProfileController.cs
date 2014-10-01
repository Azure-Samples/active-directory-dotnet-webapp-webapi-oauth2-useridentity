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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Cookies;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace WebApp.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        //
        // GET: /UserProfile/
        public async Task<ActionResult> Index()
        {
            UserProfile profile = null;
            AuthenticationContext authContext = null;
            AuthenticationResult result = null;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

            try
            {
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
                authContext = new AuthenticationContext(Startup.Authority, new TokenDbCache(userObjectID));
                result = authContext.AcquireTokenSilent(Startup.graphResourceId, credential, UserIdentifier.AnyUser);
            }
            catch (AdalException e)
            {
                //
                // The user needs to re-authorize.  Show them a message to that effect.
                // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                //

                profile = new UserProfile();
                profile.DisplayName = " ";
                profile.GivenName = " ";
                profile.Surname = " ";
                ViewBag.ErrorMessage = "AuthorizationRequired";
                authContext = new AuthenticationContext(Startup.Authority);
                Uri redirectUri = new Uri(Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/OAuth");

                string state = GenerateState(userObjectID, Request.Url.ToString());

                ViewBag.AuthorizationUrl = authContext.GetAuthorizationRequestURL(Startup.graphResourceId, Startup.clientId, redirectUri, UserIdentifier.AnyUser, state == null ? null : "&state=" + state); //TODO

                return View(profile);
            
            }

            //
            // Call the Graph API and retrieve the user's profile.
            //
            string requestUrl = String.Format(
                CultureInfo.InvariantCulture,
                Startup.graphUserUrl,
                HttpUtility.UrlEncode(result.TenantId));
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
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
                authContext.TokenCache.Clear();

                profile = new UserProfile();
                profile.DisplayName = " ";
                profile.GivenName = " ";
                profile.Surname = " ";
                ViewBag.ErrorMessage = "UnexpectedError";
            }

            return View(profile);
        }

        /// Generate a state value using the DpApi to combine a random Guid value and the origin of the request.
        /// The state value will be consumed by the OAuth controller for validation and redirection after login.
        /// Here we store the random Guid in the database cache for validation by the OAuth controller.
        public string GenerateState(string userObjId, string requestUrl)
        {
            try
            {
                string stateGuid = Guid.NewGuid().ToString();
                ApplicationDbContext db = new ApplicationDbContext();
                db.UserStateValues.Add(new UserStateValue { stateGuid = stateGuid, userObjId = userObjId });
                db.SaveChanges();

                List<String> stateList = new List<String>();
                stateList.Add(stateGuid);
                stateList.Add(requestUrl);

                var formatter = new BinaryFormatter();
                var stream = new MemoryStream();
                formatter.Serialize(stream, stateList);
                var stateBits = stream.ToArray();

                var dataProvider = new DpapiDataProtectionProvider("UserIdentityApp");
                var dataProtector = dataProvider.Create(CookieAuthenticationDefaults.AuthenticationType, Startup.appKey, "v1");
                return Url.Encode(Convert.ToBase64String(dataProtector.Protect(stateBits)));
            }
            catch 
            {
                return null;
            }
            
        }
    }
}