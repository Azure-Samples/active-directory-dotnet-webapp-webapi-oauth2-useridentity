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
using System.Globalization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;
using WebApp.Models;
using System.Security.Claims;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Cookies;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace WebApp.Controllers
{
    public class OAuthController : Controller
    {
        //
        // This method will be invoked as a call-back from an authentication service (e.g., https://login.windows.net/).
        // It is not intended to be called directly, only as a redirect from the authorization request in UserProfileController.
        // On completion, the method will cache the refresh token and access tokens, and redirect to the URL
        //     specified in the state parameter.
        //
        public ActionResult Index(string code, string error, string error_description, string resource, string state)
        {
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

            // NOTE: In production, OAuth must be done over a secure HTTPS connection.
            if (Request.Url.Scheme != "https" && !Request.Url.IsLoopback)
                return View("Error");

            // Ensure there is a state value on the response.  If there is none, stop OAuth processing and display an error.
            if (state == null) {
                ViewBag.ErrorMessage = "Error Generating State.";
                return View("Error");
            }

            // Handle errors from the OAuth response, if any.  If there are errors, stop OAuth processing and display an error.
            if (error != null)
                return View("Error");

            string redirectUri = ValidateState(state, userObjectID);

            if (redirectUri == null) {
                ViewBag.ErrorMessage = "Error Validating State.";
                return View("Error");
            }

 
            // Redeem the authorization code from the response for an access token and refresh token.
            try
            {
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.appKey);
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority, new TokenDbCache(userObjectID));
                AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                    code, new Uri(Request.Url.GetLeftPart(UriPartial.Path)), credential, Startup.graphResourceId);

                // Return to the originating page where the user triggered the sign-in
                return Redirect(redirectUri);
            }
            catch (Exception e)
            {
                return Redirect("/UserProfile/Index?authError=token");
            }
        }

        
        /// Validate the state parameter in the OAuth message by checking its Guid portion against the value in the cache.
        /// Again, we have combined the Guid and RedirectUri values for sending in our authorization request.
        public string ValidateState(string state, string userObjectID) {

            try
            {
                var stateBits = Convert.FromBase64String(state);
                var formatter = new BinaryFormatter();
                var stream = new MemoryStream(stateBits);
                List<String> stateList = (List<String>)formatter.Deserialize(stream);
                var stateGuid = stateList[0];

                ApplicationDbContext db = new ApplicationDbContext();
                var userStateValues = from u in db.UserStateValues
                                      where (u.userObjId == userObjectID && u.stateGuid == stateGuid)
                                      select u;
                var stateValuesList = userStateValues.ToList();
                foreach (var stateValue in stateValuesList)
                    db.UserStateValues.Remove(stateValue);
                db.SaveChanges();

                if (stateValuesList.Count == 0)
                    return null;

                return stateList[1];
            }
            catch
            {
                return null;
            }
        }
    }
}