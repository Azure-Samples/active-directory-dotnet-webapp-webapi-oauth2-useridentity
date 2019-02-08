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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using OAuth2_UserIdentity.Models;

namespace OAuth2_UserIdentity.Controllers
{
    public class OAuthController : Controller
    {
        //
        // This method will be invoked as a call-back from an authentication service (e.g., https://login.microsoftonline.com/).
        // It is not intended to be called directly, only as a redirect from the authorization request in UserProfileController.
        // On completion, the method will cache the refresh token and access tokens, and redirect to the URL
        //     specified in the state parameter.
        //
        public async Task<ActionResult> Index(string code, string error, string error_description, string resource, string state)
        {
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier").Value;

            // NOTE: In production, OAuth must be done over a secure HTTPS connection.
            if (Request.Url.Scheme != "https" && !Request.Url.IsLoopback)
                return View("Error");

            // Ensure there is a state value on the response.  If there is none, stop OAuth processing and display an error.
            if (state == null)
            {
                ViewBag.ErrorMessage = "Error Generating State.";
                return View("Error");
            }

            // Handle errors from the OAuth response, if any.  If there are errors, stop OAuth processing and display an error.
            if (error != null)
                return View("Error");

            string redirectUri = ValidateState(state, userObjectID);

            if (redirectUri == null)
            {
                ViewBag.ErrorMessage = "Error Validating State.";
                return View("Error");
            }


            // Redeem the authorization code from the response for an access token and refresh token.
            try
            {
                ClientCredential credential = new ClientCredential(Startup.clientId, Startup.clientSecret);
                AuthenticationContext authContext = new AuthenticationContext(Startup.Authority, new TokenDbCache(userObjectID));
                AuthenticationResult result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                    code, new Uri(Request.Url.GetLeftPart(UriPartial.Path)), credential, Startup.graphResourceId);

                // Return to the originating page where the user triggered the sign-in
                return Redirect(redirectUri);
            }
            catch (Exception)
            {
                return Redirect("/UserProfile/Index?authError=token");
            }
        }


        /// Validate the state parameter in the OAuth message by checking its Guid portion against the value in the cache.
        /// Again, we have combined the Guid and RedirectUri values for sending in our authorization request.
        public string ValidateState(string state, string userObjectID)
        {

            try
            {
                var stateBits = Convert.FromBase64String(state);
                var stream = new MemoryStream(stateBits);

                DataContractSerializer ser = new DataContractSerializer(typeof(List<String>));

                // Deserialize the data and read it from the instance.
                List<String> stateList = (List<String>)ser.ReadObject(stream);

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