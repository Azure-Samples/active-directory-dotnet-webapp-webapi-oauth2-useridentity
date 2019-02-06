using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(OAuth2_UserIdentity.Startup))]
namespace OAuth2_UserIdentity
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
