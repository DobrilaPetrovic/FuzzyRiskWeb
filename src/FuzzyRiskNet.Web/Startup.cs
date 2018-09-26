using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(FuzzyRiskNet.Startup))]
namespace FuzzyRiskNet
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
