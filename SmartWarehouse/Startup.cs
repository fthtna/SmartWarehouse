using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SmartWarehouse.Startup))]
namespace SmartWarehouse
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
