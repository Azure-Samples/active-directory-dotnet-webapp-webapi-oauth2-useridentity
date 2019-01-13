---
services: active-directory
platforms: dotnet
author: jmprieur
level: 400
client: .NET Web App (MVC)
service: Microsoft Graph
endpoint: AAD v1.0
---

# Calling Azure AD protected web APIs in a web app using OAuth 2.0

![Build badge](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/756/badge)

## About this sample

### Overview

In the sample, an existing web app with its own way of signing in users adds the ability to call an Azure AD protected web API, in this case the Graph API.  This sample uses the [OAuth 2.0 authorization code grant with confidential client] (https://docs.microsoft.com/en-us/azure/active-directory/develop/v1-protocols-oauth-code) and the Active Directory Authentication Library (ADAL) to obtain access tokens for the user signed into th web app to call the Graph API with the user's identity.

This sample is useful if you want to add the capability to call an Azure protected Web API to an existing application. One example couldbe an application that authenticates Azure AD users using Windows Identity Foundation and WS-Federation or a user authentiated using ADFS.

If you want to build a new web application that signs users in using Azure AD as well as calling web APIs protected using Azure AD, check out the [active-directory-dotnet-webapp-webapi-openidconnect](https://github.com/Azure-Samples/active-directory-dotnet-webapp-webapi-openidconnect) sample instead.  OpenIDConnect provides a more efficient way to get access tokens for a user to call a web API, by obtaining an authorization code for the user at the time they sign in.  This also means your application can skip having logic for sending OAuth authorization requests and processing responses.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

![Overview](./ReadmeFiles/Local-And-Graph-topology.png)

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## How to run this sample

To run this sample, you'll need:

- [Visual Studio 2017](https://aka.ms/vsdownload)
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/)
- A user account in your Azure AD tenant. This sample will not work with a Microsoft account (formerly Windows Live account). Therefore, if you signed in to the [Azure portal](https://portal.azure.com) with a Microsoft account and have never created a user account in your directory before, you need to do that now.

### Step 1:  Clone or download this repository

From your shell or command line:

```Shell
git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-webapi-oauth2-useridentity.git`
```

or download and extract the repository .zip file.

> Given that the name of the sample is pretty long, and so are the name of the referenced NuGet packages, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.

### Step 2:  Register the sample with your Azure Active Directory tenant

There is one project in this sample. To register it, you can:

- either follow the steps in the paragraphs below ([Step 2](#step-2--register-the-sample-with-your-azure-active-directory-tenant) and [Step 3](#step-3--configure-the-sample-to-use-your-azure-ad-tenant))
- or use PowerShell scripts that:
  - **automatically** create for you the Azure AD applications and related objects (passwords, permissions, dependencies)
  - modify the Visual Studio projects' configuration files.

If you want to use this automation, read the instructions in [App Creation Scripts](./AppCreationScripts/AppCreationScripts.md)

#### Choose the Azure AD tenant where you want to create your applications

As a first step you'll need to:

1. Sign in to the [Azure portal](https://portal.azure.com) using either a work or school account or a personal Microsoft account.
1. If your account gives you access to more than one tenant, select your account in the top right corner, and set your portal session to the desired Azure AD tenant
   (using **Switch Directory**).
1. In the left-hand navigation pane, select the **Azure Active Directory** service, and then select **App registrations (Preview)**.

#### Register the service app (OAuth2-UserIdentity)

1. In **App registrations (Preview)** page, select **New registration**.
1. When the **Register an application page** appears, enter your application's registration information:
   - In the **Name** section, enter a meaningful application name that will be displayed to users of the app, for example `OAuth2-UserIdentity`.
   - In the **Supported account types** section, select **Accounts in any organizational directory and personal Microsoft accounts (e.g. Skype, Xbox, Outlook.com)**.
   - In the Redirect URI (optional) section, select **Web** in the combo-box and enter the following redirect URIs.
       - `https://localhost:44323/`
       - `https://localhost:44323/OAuth`
1. Select **Register** to create the application.
1. On the app **Overview** page, find the **Application (client) ID** value and record it for later. You'll need it to configure the Visual Studio configuration file for this project.
1. In the list of pages for the app, select **Authentication**.
   - In the **Advanced settings** section set **Logout URL** to `https://localhost:44323/Account/LogOff`
   - In the **Advanced settings** | **Implicit grant** section, check **Access tokens** and **ID tokens** as this sample requires the [Implicit grant flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-implicit-grant-flow)to be enabled to
   sign-in the user, and call an API.
1. Select **Save**.
1. From the **Certificates & secrets** page, in the **Client secrets** section, choose **New client secret**:

   - Type a key description (of instance `app secret`),
   - Select a key duration of either **In 1 year**, **In 2 years**, or **Never Expires**.
   - When you press the **Add** button, the key value will be displayed, copy, and save the value in a safe location.
   - You'll need this key later to configure the project in Visual Studio. This key value will not be displayed again, nor retrievable by any other means,
     so record it as soon as it is visible from the Azure portal.
1. In the list of pages for the app, select **API permissions**
   - Click the **Add a permission** button and then,
   - Ensure that the **Microsoft APIs** tab is selected
   - In the *Commonly used Microsoft APIs* section, click on **Microsoft Graph**
   - In the **Delegated permissions** section, ensure that the right permissions are checked: **User.Read**. Use the search box if necessary.
   - Select the **Add permissions** button


### Step 3:  Configure the sample to use your Azure AD tenant

In the steps below, "ClientID" is the same as "Application ID" or "AppId".

Open the solution in Visual Studio to configure the projects

#### Configure the service project

1. Open the `OAuth2-UserIdentity\Web.Config` file
1. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `OAuth2-UserIdentity` application copied from the Azure portal.
1. Find the app key `ida:AppKey` and replace the existing value with the key you saved during the creation of the `OAuth2-UserIdentity` app, in the Azure portal.
1. Find the app key `ida:Tenant` and replace the existing value with your Azure AD tenant name.

### Step 4: Run the sample

Clean the solution, rebuild the solution, and run it.

Explore the sample by

1. registering an account in the application, 
1. signing in using that account, clicking the Profile link, 
1. on the Profile page linking an AAD user's account and seeing their profile information, 
1. sign out from the application, and starting again.

## How To Recreate This Sample

1. In Visual Studio 2017, create a new ASP.Net MVC web application with Authentication set to `Invididual User Accounts`.
1. Set SSL Enabled to be True.  Note the SSL URL.
1. In the project properties, Web properties, set the Project Url to be the SSL URL.
1. Add the latest [Active Directory Authentication Library NuGet] (`Microsoft.IdentityModel.Clients.ActiveDirectory`).
1. In the `Models` folder add a new class called `UserProfile.cs`.  Copy the implementation of `UserProfile` from this sample into the class.
1. Add a new empty MVC5 controller `UserProfileController` to the project.  Copy the implementation of the controller from the sample.  Remember to include the [Authorize] attribute on the class definition.
1. In `Views` --> `UserProfile` create a new view, `Index.cshtml`, and copy the implementation from this sample.
1. In the shared `_Layout` view, add the Action Link for Profile that is in the sample.
1. Add a new empty MVC5 controller `OAuthController` to the project.  Copy the implemementation of the controller from the sample.
1. In AccountController's LogOff() method, copy the code which clears the token cache.
1. In `web.config`, in `<appSettings>`, create keys for `ida:ClientId`, `ida:AppKey`, `ida:AADInstance`, `ida:Tenant`, `ida:GraphResourceId`, and `ida:GraphUserUrl` and set the values accordingly.  For the public Azure AD, the value of `ida:AADInstance` is `https://login.microsoftonline.com/{0}` the value of `ida:GraphResourceId` is `https://graph.microsoft.com`, and the value of `ida:GraphUserUrl` is `https://graph.microsoft.com/me`.
1. In `web.config` add this line in the `<system.web>` section: `<sessionState timeout="525600" />`.  This increases the ASP.Net session state timeout to it's maximum value so that access tokens and refresh tokens cache in session state aren't cleared after the default timeout of 20 minutes.

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/msal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`adal` `msal` `dotnet`].

If you find a bug in the sample, please raise the issue on [GitHub Issues](../../issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## More information

For more information, see the following links:

- [Quickstart: Register an application with the Microsoft identity platform (Preview)](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [Quickstart: Configure a client application to access web APIs (Preview)](https://docs.microsoft.com/azure/active-directory/develop/quickstart-configure-app-access-web-apis)
- [Recommended pattern to acquire a token](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token#recommended-pattern-to-acquire-a-token)
- [Customizing Token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization)
- [Understanding Azure AD application consent experiences](https://docs.microsoft.com/en-us/azure/active-directory/develop/application-consent-experience)
- [Customizing Token cache serialization](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki/Token-cache-serialization)
- [ADAL.NET's conceptual documentation](https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/wiki)

For more information about how OAuth 2.0 protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).
