using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SnackBites.Erp2.Startup))]
namespace SnackBites.Erp2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
