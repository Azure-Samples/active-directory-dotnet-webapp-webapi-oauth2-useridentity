/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using OAuth2_UserIdentity.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace OAuth2_UserIdentity.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        //
        // GET: /UserProfile/
        public async Task<ActionResult> Index(string authError)
        {
            UserProfile profile = null;
            AuthenticationContext authContext = null;
            AuthenticationResult result = null;
            bool reauth = false;
            string userObjectID = string.Empty;
            UserIdentifier user = UserIdentifier.AnyUser;

            var claimsIdentity = (ClaimsIdentity)ClaimsPrincipal.Current?.Identity;
            if (claimsIdentity != null && claimsIdentity.IsAuthenticated)
            {
                //userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                userObjectID = claimsIdentity.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;
                user = new UserIdentifier( claimsIdentity.Name, UserIdentifierType.OptionalDisplayableId);
            }

            try
            {
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
                authContext = new AuthenticationContext(Startup.Authority, new TokenDbCache(userObjectID));

                if (authError != null)
                {
                    Uri redirectUri = new Uri(Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/OAuth");
                    string state = GenerateState(userObjectID, Request.Url.ToString());
                    ViewBag.AuthorizationUrl = await authContext.GetAuthorizationRequestUrlAsync(Startup.graphResourceId, Startup.clientId, redirectUri, user, state == null ? null : "&state=" + state);

                    profile = new UserProfile();
                    profile.DisplayName = " ";
                    profile.GivenName = " ";
                    profile.Surname = " ";
                    ViewBag.ErrorMessage = "UnexpectedError";
                    return View(profile);
                }

                result = await authContext.AcquireTokenSilentAsync(Startup.graphResourceId, credential, UserIdentifier.AnyUser);
            }
            catch (AdalException e)
            {
                if (e.ErrorCode == "failed_to_acquire_token_silently")
                {
                    // Capture error for handling outside of catch block
                    reauth = true;
                }
                else
                {
                    ViewBag.ErrorMessage = "Error while Acquiring Token from Cache.";
                    return View("Error");
                }
            }

            if (reauth)
            {
                // The user needs to re-authorize.  Show them a message to that effect.
                // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.

                profile = new UserProfile();
                profile.DisplayName = " ";
                profile.GivenName = " ";
                profile.Surname = " ";
                ViewBag.ErrorMessage = "AuthorizationRequired";

                authContext = new AuthenticationContext(Startup.Authority);
                Uri redirectUri = new Uri(Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/OAuth");

                string state = this.GenerateState(userObjectID, Request.Url.ToString());

                ViewBag.AuthorizationUrl = await authContext.GetAuthorizationRequestUrlAsync(Startup.graphResourceId, Startup.clientId, redirectUri, user, state == null ? null : "&state=" + state);

                return View(profile);
            }


            try
            {
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
                    return View(profile);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //
                    // If the call failed, then drop the current access token and show the user an error indicating they might need to sign-in again.
                    //
                    authContext.TokenCache.Clear();

                    Uri redirectUri = new Uri(Request.Url.GetLeftPart(UriPartial.Authority).ToString() + "/OAuth");
                    string state = GenerateState(userObjectID, Request.Url.ToString());
                    ViewBag.AuthorizationUrl = await authContext.GetAuthorizationRequestUrlAsync(Startup.graphResourceId, Startup.clientId, redirectUri, UserIdentifier.AnyUser, state == null ? null : "&state=" + state);

                    profile = new UserProfile();
                    profile.DisplayName = " ";
                    profile.GivenName = " ";
                    profile.Surname = " ";
                    ViewBag.ErrorMessage = "UnexpectedError";
                    return View(profile);
                }

                ViewBag.ErrorMessage = "Error Calling Graph API.";
                return View("Error");
            }
            catch
            {
                ViewBag.ErrorMessage = "Error Calling Graph API.";
                return View("Error");
            }
        }

        /// Generate a state value using a random Guid value and the origin of the request.
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

                var stream = new MemoryStream();

                DataContractSerializer ser = new DataContractSerializer(typeof(List<String>));
                ser.WriteObject(stream, stateList);

                var stateBits = stream.ToArray();

                return Url.Encode(Convert.ToBase64String(stateBits));
            }
            catch
            {
                return null;
            }

        }
    }
}