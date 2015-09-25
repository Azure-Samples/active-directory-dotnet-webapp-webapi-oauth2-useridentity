---
services: active-directory
platforms: dotnet
author: dstrockis
---

Calling Azure AD protected web APIs in a web app using OAuth 2.0
========================================

In the sample, an existing web app with its own way of signing in users adds the ability to call an Azure AD protected web API, in this case the Graph API.  This sample uses the OAuth 2.0 authorization code grant with confidential client and the Active Directory Authentication Library (ADAL) to obtain access tokens for the web app to call the Graph API with the user's identity.

This sample is useful if you want to add a web API calling ability to an existing application that authenticates Azure AD users using Windows Identity Foundation and WS-Federation.  If you want to build a new web application that signs users in using Azure AD as well as calling web APIs protected using Azure AD, check out the WebApp-WebAPI-OpenIDConnect-DotNet sample.  OpenIDConnect provides a more efficient way to get access tokens for a user to call a web API, by obtaining an authorization code for the user at the time they sign in.  This also means your application can skip having logic for sending OAuth authorization requests and processing OAuth authorization responses.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

## How To Run This Sample

To run this sample you will need:
- Visual Studio 2013
- An Internet connection
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [https://azure.microsoft.com](https://azure.microsoft.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-webapi-oauth2-useridentity.git`

### Step 2:  Create a user account in your Azure Active Directory tenant

If you already have a user account in your Azure Active Directory tenant, you can skip to the next step.  This sample will not work with a Microsoft account, so if you signed in to the Azure portal with a Microsoft account and have never created a user account in your directory before, you need to do that now.  If you create an account and want to use it to sign-in to the Azure portal, don't forget to add the user account as a co-administrator of your Azure subscription.

### Step 3:  Register the sample with your Azure Active Directory tenant

1. Sign in to the [Azure management portal](https://manage.windowsazure.com).
2. Click on Active Directory in the left hand nav.
3. Click the directory tenant where you wish to register the sample application.
4. Click the Applications tab.
5. In the drawer, click Add.
6. Click "Add an application my organization is developing".
7. Enter a friendly name for the application, for example "WebApp-WebAPI-OAuth2-UserIdentity-DotNet", select "Web Application and/or Web API", and click next.
8. For the sign-on URL, enter the base URL for the sample, which is by default `https://localhost:44323/`.
9. For the App ID URI, enter `https://<your_tenant_name>/WebApp-WebAPI-OAuth2-UserIdentity-DotNet`, replacing `<your_tenant_name>` with the name of your Azure AD tenant.  Click OK to complete the registration.
10. While still in the Azure portal, click the Configure tab of your application.
11. Find the Client ID value and copy it aside, you will need this later when configuring your application.
12. Create a new key for the application.  Save the configuration so you can view the key value.  Save this aside for when you configure the project in Visual Studio.

### Step 4:  Configure the sample to use your Azure AD tenant

1. Open the solution in Visual Studio 2013.
2. Open the `web.config` file.
3. Find the app key `ida:Tenant` and replace the value with your AAD tenant name.
4. Find the app key `ida:ClientId` and replace the value with the Client ID for WebApp-WebAPI-OAuth2-UserIdentity-DotNet from the Azure portal.
5. Find the app key `ida:AppKey` and replace the value with the key for WebApp-WebAPI-OAuth2-UserIdentity-DotNet from the Azure portal.

### Step 5:  Run the sample

Clean the solution, rebuild the solution, and run it.

Explore the sample by registering an account in the application, signing in using that account, clicking the Profile link, on the Profile page linking an AAD user's account and seeing their profile information, signing out from the application, and starting again.

## How To Deploy This Sample to Azure

Coming soon.

## About The Code

Coming soon.

## How To Recreate This Sample

1. In Visual Studio 2013, create a new ASP.Net MVC web application called WebApp with Authentication set to Invididual User Accounts.
2. Set SSL Enabled to be True.  Note the SSL URL.
3. In the project properties, Web properties, set the Project Url to be the SSL URL.
4. Add the (stable) Active Directory Authentication Library NuGet (`Microsoft.IdentityModel.Clients.ActiveDirectory`), version 1.0.3 (or higher).
5. In the `Models` folder add a new class called `UserProfile.cs`.  Copy the implementation of UserProfile from this sample into the class.
6. Add a new empty MVC5 controller UserProfileController to the project.  Copy the implementation of the controller from the sample.  Remember to include the [Authorize] attribute on the class definition.
7. In `Views` --> `UserProfile` create a new view, `Index.cshtml`, and copy the implementation from this sample.
8. In the shared `_Layout` view, add the Action Link for Profile that is in the sample.
9. Add a new empty MVC5 controller OAuthController to the project.  Copy the implemementation of the controller from the sample.
10. Open the AccountController controller,  find the `LogOff()` method, and add this line at the beginning of the method: `OAuthController.RemoveAllFromCache();`.  Also note the comments that are included in the sample file in the `LogOff()` method.
11. In `web.config`, in `<appSettings>`, create keys for `ida:ClientId`, `ida:AppKey`, `ida:AADInstance`, `ida:Tenant`, `ida:GraphResourceId`, and `ida:GraphUserUrl` and set the values accordingly.  For the public Azure AD, the value of `ida:AADInstance` is `https://login.windows.net/{0}` the value of `ida:GraphResourceId` is `https://graph.windows.net`, and the value of `ida:GraphUserUrl` is `https://graph.windows.net/{0}/me?api-version=2013-11-08`.
12. In `web.config` add this line in the `<system.web>` section: `<sessionState timeout="525600" />`.  This increases the ASP.Net session state timeout to it's maximum value so that access tokens and refresh tokens cache in session state aren't cleared after the default timeout of 20 minutes.
