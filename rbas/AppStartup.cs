namespace RBAS.Startup
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Html;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using RBAS.Entities;
    using RBAS.Services;
    using System.Data;
    using System.Diagnostics;
    using System.Security.AccessControl;
    using System.Text; 

    /// <summary>
    /// Defines the <see cref="AppStartup" />
    /// </summary>
    public class AppStartup : IAppStartup
    {
        /// <summary>
        /// The App
        /// </summary>
        /// <param name="mPermission">The mPermission<see cref="IPermission"/></param>
        /// <param name="_env">The _env<see cref="IWebHostEnvironment"/></param>
        /// <param name="mAccessor">The mAccessor<see cref="IHttpContextAccessor"/></param> 
        /// <param name="htmlHelper">The htmlHelper<see cref="IHtmlHelper"/></param>
        /// <param name="mConfiguration">The mConfiguration<see cref="IConfiguration"/></param>
        /// <param name="mUserId">The mUserId<see cref="int?"/></param> 
        /// <param name="fileInternalName">The fileInternalName<see cref="string"/></param>
        /// <param name="versionNumber">The versionNumber<see cref="string"/></param>
        /// <param name="fileModifiedTime">The fileModifiedTime<see cref="string"/></param> 
        /// <returns>The <see cref="IHtmlContent"/></returns>
        public IHtmlContent App(IPermission mPermission, IWebHostEnvironment _env, IHttpContextAccessor mAccessor, IHtmlHelper htmlHelper, IConfiguration mConfiguration, ref int? mUserId, ref string menuLoader, ref string fileInternalName,
                              ref string versionNumber, ref string fileModifiedTime)
        {
            mUserId = mAccessor?.HttpContext?.Session.GetInt32("_UserId");
            var rd = mAccessor?.HttpContext?.GetRouteData();
            menuLoader = mConfiguration?.GetSection("AppSettings").GetSection("MenuLoader")?.Value!;
            string contentRootPath = _env.ContentRootPath;

            var userReturnUrl = mAccessor?.HttpContext?.Session.GetString("_userReturnUrl");
            var mMenus = mPermission.Menus(mUserId);
            string currentController = rd?.Values["controller"]?.ToString()!;
            string currentAction = rd?.Values["action"]?.ToString()?.ToLower() == "index" ? "" : rd?.Values["action"]?.ToString()!;
            string currentArea = "";
            string requestUrl = "";
            if (rd.Values["area"] != null)
            {
                currentArea = rd?.Values["area"]?.ToString()!;
            }
            if (!string.IsNullOrEmpty(currentArea))
            {
                requestUrl = "/" + currentArea + "/" + currentController;
            }
            else
            {
                requestUrl = "/" + currentController;
            }
            var actn = !string.IsNullOrEmpty(currentAction) ? "/" + currentAction : "";
            var url = mAccessor?.HttpContext?.Request.Scheme + "://" + mAccessor?.HttpContext?.Request.Host + requestUrl + actn;
            
            var currentHost = mAccessor?.HttpContext?.Request.Scheme + "://" + mAccessor?.HttpContext?.Request.Host;
            var controllerPath = AppDomain.CurrentDomain.BaseDirectory + @"/rbas.startup.dll";
            FileVersionInfo fvo = FileVersionInfo.GetVersionInfo(controllerPath);
            fileInternalName = fvo?.InternalName!;
            versionNumber = fvo?.FileVersion!;
            string lastModified = File.GetLastWriteTime(controllerPath).ToString("dd/MM/yyyy HH:mm:ss");
            fileModifiedTime = lastModified.Contains("1601") ? "" : lastModified;
           
            if (mUserId != 0)
            {
                int? userRollId = mAccessor?.HttpContext?.Session.GetInt32("_RoleId");
                var mDataSet = Utils.Utility.ToDataSet(mMenus);
                DataTable mTable = mDataSet.Tables[0];
                DataRow[] mParentRows = mTable.Select("ParentId = 0");
                StringBuilder mStringBuilder = new StringBuilder();
                string menuString = GenerateUrl(mParentRows, mTable, mStringBuilder, menuLoader, userRollId);
                mAccessor?.HttpContext?.Session.SetString("mMenuString", menuString);
                if (menuString.Trim().ToLower().Contains("Copyright modification not allowed.".Trim().ToLower()))
                {
                    copyRight = "false";
                }
            }
            var access = mMenus?.FirstOrDefault(p => p.Url.Contains(userReturnUrl!));
            if (access != null)
            {
                if (!access.IsActive)
                {
                    mAccessor?.HttpContext?.Response.Redirect("/StatusCode/ErrorCode?code=400");
                }
            }

            StringBuilder htmlBulder = new StringBuilder();
            htmlBulder.AppendLine("<!-------------------Signature---------------------");
            htmlBulder.AppendLine("    ---- " + "Build Time: " + fileModifiedTime);
            htmlBulder.AppendLine("    ---- " + "File Name: " + fileInternalName);
            htmlBulder.AppendLine("    ---- " + "File Version: " + versionNumber);
            htmlBulder.AppendLine("    ---- " + "AMIT KUMAR"); 
            htmlBulder.AppendLine("    ---- " + "Email ID. iamnomandra@gmail.com,"); 
            htmlBulder.AppendLine("-------------------------------------------------->");
            return new HtmlString(htmlBulder.ToString());
        }

        private bool Signeture(string mSigneture)
        {
            var mResult = false;
            string _Signeture = "Design & Developed By: Amit Kumar,";
            try
            {
                if ((mSigneture.Length > 34) || (mSigneture.Length < 34))
                {
                    mResult = false;
                }
                else if (string.IsNullOrEmpty(mSigneture))
                {
                    mResult = false;
                }
                else if (!mSigneture.Contains(_Signeture))
                {
                    mResult = false;
                }
                else if (mSigneture.Contains(_Signeture))
                {
                    mResult = true;
                }
                else
                {
                    mResult = false;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return mResult;
        }

        /// <summary>
        /// The GenerateUrl
        /// </summary>
        /// <param name="mMenuRows">The mMenuRows<see cref="DataRow[]"/></param>
        /// <param name="mTable">The mTable<see cref="DataTable"/></param>
        /// <param name="mStringBuilder">The mStringBuilder<see cref="StringBuilder"/></param>
        /// <param name="menuLoader">The menuLoader<see cref="string"/></param>
        /// <param name="userRollId">The userRollId<see cref="int?"/></param>
        /// <returns>The <see cref="string"/></returns>
        private string GenerateUrl(DataRow[] mMenuRows, DataTable mTable, StringBuilder mStringBuilder, string menuLoader, int? userRollId)
        {
            if (mMenuRows.Length > 0)
            {
                int mCount = 0;

                foreach (DataRow mDr in mMenuRows)
                {
                    bool IsActive = Convert.ToBoolean(mDr["IsActive"].ToString());
                    if (userRollId > 2)
                    {
                        if (!IsActive) continue;
                    }
                    string? mUrl = mDr["Url"]?.ToString()?.Replace("~/", "/");
                    string? mMenuName = mDr["MName"].ToString();
                    string? mOrderSequence = mDr["OrderSequence"].ToString();
                    mOrderSequence = mOrderSequence == "" ? "" : "OrderSequence ='" + mOrderSequence + "'";
                    string? mControllerName = mDr["Controller"].ToString();
                    string? mSMName = mDr["SMName"].ToString();
                    string? mIcon = mDr["Icon"].ToString();

                    string? mMPId = mDr["MenuId"].ToString();
                    string? mParentId = mDr["ParentId"].ToString();
                    DataRow[] mSubMenu = mTable.Select(String.Format("ParentId = '{0}'", mMPId));

                    if (mCount == 0)
                    {
                        if (mParentId == "0")
                        {
                            mStringBuilder.AppendLine($"<li class=\"app-sidebar__heading\">MENU</li>");
                        }
                    }

                    if (mSubMenu.Length > 0 && !mMPId.Equals(mParentId))
                    {
                        mStringBuilder.AppendLine($"<li><a href=\"#\" aria-expanded=\"true\">");
                        mStringBuilder.AppendLine($"<i class=\"{mIcon}\" {mOrderSequence}></i>{mMenuName}");
                        mStringBuilder.AppendLine($"<i class=\"metismenu-state-icon pe-7s-angle-down caret-left\"></i></a>");
                        var subMenuBuilder = new StringBuilder();
                        mStringBuilder.AppendLine(String.Format($"<ul class=\"mm-collapse\">"));
                        mStringBuilder.Append(GenerateUrl(mSubMenu, mTable, subMenuBuilder, menuLoader, userRollId));
                        mStringBuilder.Append("</ul>");
                    }
                    else
                    {
                        mSMName = mSMName == "" ? mControllerName : mSMName;
                        mStringBuilder.AppendLine($"<li><a href =\"{mUrl}\" aria-expanded=\"false\"><i class=\"{mIcon}\" {mOrderSequence}></i>{mSMName}</a>");
                    }
                    mSMName = "";
                    mStringBuilder.Append("</li>");
                    mCount += 1;
                }
            }
            if ((Signeture(menuLoader)) == false)
            {
                return $"<li class=\"app-sidebar__heading\"><span class=\"fa-fade\" style=\"font-size: 0.75rem;text-transform: capitalize;font-weight: 500;color: white;text-shadow: 2px 7px 5px rgba(0,0,0,0.3),0px -4px 10px rgba(255,255,255,0.3);\"><i class=\"fa-regular fa-bell fa-fw mr-1\"></i>Signeture modification not allowed.</span></li>";
            }
            if (!menuLoader.Contains("Design & Developed By: Amit Kumar"))
            {
                return $"<li class=\"app-sidebar__heading\"><span class=\"fa-fade\" style=\"font-size: 0.75rem;text-transform: capitalize;font-weight: 500;color: white;text-shadow: 2px 7px 5px rgba(0,0,0,0.3),0px -4px 10px rgba(255,255,255,0.3);\"><i class=\"fa-regular fa-bell fa-fw mr-1\"></i>Signeture modification not allowed.</span></li>";
            }
            return mStringBuilder.ToString();
        }
    }
}
