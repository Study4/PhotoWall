using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PhotoWall.UI.Web.Startup))]
namespace PhotoWall.UI.Web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
