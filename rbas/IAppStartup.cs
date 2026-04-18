using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration; 
using RBAS.Services; 

namespace RBAS.Startup
{
    public interface IAppStartup
    {
        public IHtmlContent App(IPermission mPermission, IWebHostEnvironment _env, 
            IHttpContextAccessor mAccessor, IHtmlHelper htmlHelper, IConfiguration mConfiguration, 
            ref int? mUserId, ref string menuLoader, ref string fileInternalName, ref string versionNumber, ref string fileModifiedTime);




    }
}
