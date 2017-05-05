namespace Sitecore.Support.Resources.Media
{
    using Sitecore.Configuration;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Resources;
    using Sitecore.Resources.Media;
    using Sitecore.SecurityModel;
    using Sitecore.Text;
    using Sitecore.Web;
    using System.Collections.Generic;
    using System.Web;

    public class MediaRequestHandler : Sitecore.Resources.Media.MediaRequestHandler
    {
        protected override bool DoProcessRequest(HttpContext context)
        {
            string itemNotFoundUrl;
            bool isRedirectToLoginPage = false;
            Assert.ArgumentNotNull(context, "context");
            MediaRequest request = MediaManager.ParseMediaRequest(context.Request);
            if (request == null)
            {
                return false;
            }
            Sitecore.Resources.Media.Media media = MediaManager.GetMedia(request.MediaUri);
            if (media != null)
            {
                return this.DoProcessRequest(context, request, media);
            }
            using (new SecurityDisabler())
            {
                media = MediaManager.GetMedia(request.MediaUri);
            }
            if (media == null)
            {
                itemNotFoundUrl = Settings.ItemNotFoundUrl;
            }
            else
            {
                Assert.IsNotNull(Context.Site, "site");
                itemNotFoundUrl = (Context.Site.LoginPage != string.Empty) ? Context.Site.LoginPage : Settings.NoAccessUrl;
                isRedirectToLoginPage = itemNotFoundUrl == Context.Site.LoginPage ? true : false;
            }
            string siteName = (Context.Site != null) ? Context.Site.Name : string.Empty;
            List<string> list = new List<string>(new string[] { "item", context.Request.FilePath, "user", Context.User.Name, "site", siteName });
            itemNotFoundUrl = WebUtil.AddQueryString(itemNotFoundUrl, list.ToArray());
            if (isRedirectToLoginPage)
            {
                this.RedirectToLoginPage(itemNotFoundUrl);
            }
            else
            {
                this.PerformRedirect(itemNotFoundUrl);
            }
            return true;
        }

        private void PerformRedirect(string url)
        {
            if (Settings.RequestErrors.UseServerSideRedirect)
            {
                HttpContext.Current.Server.Transfer(url);
            }
            else
            {
                WebUtil.Redirect(url, false);
            }
        }

        private void RedirectToLoginPage(string url)
        {
            UrlString str = new UrlString(url);
            if (string.IsNullOrEmpty(str["returnUrl"]))
            {
                str["returnUrl"] = WebUtil.GetRawUrl();
                str.Parameters.Remove("item");
                str.Parameters.Remove("user");
                str.Parameters.Remove("site");
            }
            WebUtil.Redirect(str.ToString(), false);
        }
    }
}
