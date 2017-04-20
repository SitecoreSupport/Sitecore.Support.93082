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
            Assert.ArgumentNotNull(context, "context");
            MediaRequest mediaRequest = this.GetMediaRequest(context.Request);
            if (mediaRequest == null)
            {
                return false;
            }
            string url = null;
            bool isRedirectToLoginPage = false;
            Sitecore.Resources.Media.Media media = MediaManager.GetMedia(mediaRequest.MediaUri);
            if (media == null)
            {
                using (new SecurityDisabler())
                {
                    media = MediaManager.GetMedia(mediaRequest.MediaUri);
                }
                if (media == null)
                {
                    url = Settings.ItemNotFoundUrl;
                }
                else
                {
                    Assert.IsNotNull(Context.Site, "site");
                    if ((!Context.User.IsAuthenticated && Context.Site.RequireLogin) && !string.IsNullOrEmpty(Context.Site.LoginPage))
                    {
                        url = Context.Site.LoginPage;
                        if (Settings.Authentication.SaveRawUrl)
                        {
                            UrlString str2 = new UrlString(url);
                            str2.Append("url", HttpUtility.UrlEncode(Context.RawUrl));
                            url = str2.GetUrl();
                        }
                        isRedirectToLoginPage = true;
                    }
                    else
                    {
                        url = Settings.NoAccessUrl;
                    }
                }
            }
            else
            {
                bool flag = mediaRequest.Options.Thumbnail || media.MediaData.HasContent;
                string str3 = media.MediaData.MediaItem.InnerItem["path"].ToLowerInvariant();
                if (!flag && !string.IsNullOrEmpty(str3))
                {
                    MediaUri mediaUri = new MediaUri(str3, Language.Current, Sitecore.Data.Version.Latest, Context.Database);
                    Sitecore.Resources.Media.Media media2 = MediaManager.GetMedia(mediaUri);
                    if (media2 != null)
                    {
                        media = media2;
                    }
                }
                else if (mediaRequest.Options.UseDefaultIcon && !flag)
                {
                    url = Themes.MapTheme(Settings.DefaultIcon).ToLowerInvariant();
                }
                else if (!mediaRequest.Options.UseDefaultIcon && !flag)
                {
                    url = Settings.ItemNotFoundUrl;
                }
            }
            if (!string.IsNullOrEmpty(url))
            {
                string siteName = (Context.Site != null) ? Context.Site.Name : string.Empty;
                List<string> list = new List<string>(new string[] { "item", context.Request.FilePath, "user", Context.User.Name, "site", siteName });
                url = WebUtil.AddQueryString(url, list.ToArray());
                if (isRedirectToLoginPage)
                {
                    this.RedirectToLoginPage(url);
                }
                else
                {
                    this.PerformRedirect(url);
                }
                return true;
            }
            return this.DoProcessRequest(context, mediaRequest, media);
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
