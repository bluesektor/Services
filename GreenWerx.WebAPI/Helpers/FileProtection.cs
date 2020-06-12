using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.SessionState;
using GreenWerx.Web;
using System.Threading;
using System.Web.Routing;
using System.Web.Mvc;

namespace GreenWerx.WebAPI.Helpers
{
 
    public class FileProtection  //: IHttpHandler, IRequiresSessionState
    {
        public FileProtection()
        {
            //
            // TODO: Add constructor logic here
            //
        }

       

        public void ProcessRequest(HttpContext context)
        {
            
            // Define your Domain Name Here
            String strDomainName = Globals.Application.AppSetting("SiteDomain");
            // Add the RELATIVE folder where you keep your stuff here
            String strFolder = "~/Content/Uploads";
            // Add the RELATIVE PATH of your "no" image
            // todo set this to a custom image
            String strNoImage = "~/Content/Default/Images/add.png"; // if this is set to null or empty string then an empty response is return as per 'maxxnostra's comment on Codeproject. Thanks.
            switch (context.Request.HttpMethod)
            {
                case "GET":
                    String strRequestedFile = context.Server.MapPath(context.Request.FilePath);
                    if (context.Request.UrlReferrer != null)
                    {
                        String strUrlRef = context.Request.UrlReferrer.ToString();
                        String strUrlImageFull = ResolveUrl(strFolder);
                        if (strUrlRef.Contains(strUrlImageFull))
                        {
                            context = SendContentTypeAndFile(context, strNoImage);
                        }
                        else if (strUrlRef.StartsWith(strDomainName))
                        {
                            context = SendContentTypeAndFile(context, strRequestedFile);
                        }
                        else
                        {
                            context = SendContentTypeAndFile(context, strNoImage);
                        }
                    }
                    else
                    {
                        context = SendContentTypeAndFile(context, strNoImage);
                    }
                    break;
                //case "POST":
                //    context = SendContentTypeAndFile(context, strNoImage);
                //    break;
            }
        }

        public string GetContentType(string filename)
        {
            // used to set the encoding for the reponse stream
            string res = null;
            FileInfo fileinfo = new System.IO.FileInfo(filename);
            if (fileinfo.Exists)
            {
                switch (fileinfo.Extension.Remove(0, 1).ToLower())
                {
                    case "png":
                        res = "image/png";
                        break;
                    case "jpeg":
                        res = "image/jpg";
                        break;
                    case "jpg":
                        res = "image/jpg";
                        break;
                    case "js":
                        res = "application/javascript";
                        break;
                    case "css":
                        res = "text/css";
                        break;
                }
                return res;
            }
            return null;
        }

        HttpContext SendContentTypeAndFile(HttpContext context, String strFile)
        {
            if (String.IsNullOrEmpty(strFile))
            {
                return null;
            }
            else
            {
                context.Response.ContentType = GetContentType(strFile);
                context.Response.TransmitFile(strFile);
                context.Response.End();
                return context;
            }
        }

        // NOTE:: I have not written this function. I found it on the web a while back. All credits for this function go to the author (whose name I cannot remember).
        public string ResolveUrl(string originalUrl)
        {
            if (originalUrl == null)
                return null;
            // *** Absolute path - just return   
            if (originalUrl.IndexOf("://") != -1)
                return originalUrl;
            // *** Fix up image path for ~ root app dir directory    
            if (originalUrl.StartsWith("~"))
            {
                string newUrl = "";
                if (HttpContext.Current != null)
                    newUrl = HttpContext.Current.Request.ApplicationPath + originalUrl.Substring(1).Replace("//", "/");
                else // *** Not context: assume current directory is the base directory        
                    throw new ArgumentException("Invalid URL: Relative URL not allowed.");
                return newUrl;
            }// *** Just to be sure fix up any double slashes        
            return originalUrl;
        }
    }

}