using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(ExpenseTracker.API.Startup))]

namespace ExpenseTracker.API
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseWebApi(WebApiConfig.Register());
        }
    }
}