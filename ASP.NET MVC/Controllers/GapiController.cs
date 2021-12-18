using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace GuestbookSystem.Controllers
{
    public class GapiController :   Controller
    {
        string UserId = "empty";
        string ApplicationName = "Google Calendar API .NET Quickstart";
        // GET: Gapi
        public ActionResult Index()
        {
            GoogleClientSecrets clientSecret;
            //OAuth 2.0 用戶端 ID的憑證jsoin 檔案
            //https://console.cloud.google.com/apis
            using (var stream = new FileStream(Server.MapPath("~/client_secret.json"), FileMode.Open, FileAccess.Read))
            {
                clientSecret = GoogleClientSecrets.Load(stream);
            }

            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(
                                            new GoogleAuthorizationCodeFlow.Initializer
                                            {
                                                ClientSecrets = clientSecret.Secrets,
                                                //存放access_token 位置
                                                //檔名會是Google.Apis.Auth.OAuth2.Responses.TokenResponse- [UserId](登入使用這帳號之類的)
                                                DataStore = new FileDataStore(Server.MapPath("~/token1")),
                                                Scopes = new[] { CalendarService.Scope.Calendar }
                                            });

            //需要對應[OAuth 2.0 用戶端]中設定的[已授權的重新導向 URI]
            var uri = "http://localhost:9579/gapi/index";
            var code = Request["code"];
            if (code != null)
            {
                uri = Request.Url.ToString();
                var token = flow.ExchangeCodeForTokenAsync(UserId, code,
                                  uri.Substring(0, uri.IndexOf("?")), CancellationToken.None).Result;

                //儲存使用者的Token
                var oauthState = AuthWebUtility.ExtracRedirectFromState(
                    flow.DataStore, UserId, Request["state"]).Result;

                return RedirectToAction("LoginGapi");               
            }
            else
            {

                var result = new AuthorizationCodeWebApp(flow, uri, "success").AuthorizeAsync(UserId, CancellationToken.None).Result;
                if (result.RedirectUri != null)
                {
                    //導去授權頁
                    return Redirect(result.RedirectUri);
                }
                if(result.Credential != null)
                {
                    return RedirectToAction("LoginGapi");
                }
            }

            
            return View();
        }

        public ActionResult LoginGapi()
        {
            //var d = JsonConvert.DeserializeObject<GoogleTokenModel>( System.IO.File.ReadAllText(Server.MapPath("~/token1/") + "Google.Apis.Auth.OAuth2.Responses.TokenResponse-" + UserId));
            CalendarService service = null;

            GoogleClientSecrets clientSecret;

            using (var stream = new FileStream(Server.MapPath("~/client_secret.json"), FileMode.Open, FileAccess.Read))
            {
                clientSecret = GoogleClientSecrets.Load(stream);
            }

            IAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecret.Secrets,
                    DataStore = new FileDataStore(Server.MapPath("~/token1/")),
                    Scopes = new[] { CalendarService.Scope.Calendar }
                });

            #region 取得calender的清單
            var result = new AuthorizationCodeWebApp(flow, "", "").AuthorizeAsync(UserId, CancellationToken.None).Result;
            service = new CalendarService(new BaseClientService.Initializer
            {
                ApplicationName = ApplicationName,
                HttpClientInitializer = result.Credential
            });
            EventsResource.ListRequest request = service.Events.List("primary");
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    string when = eventItem.Start.DateTime.ToString();
                    if (String.IsNullOrEmpty(when))
                    {
                        when = eventItem.Start.Date;
                    }
                    ViewBag.calender += string.Format("{0} ({1})<BR>", eventItem.Summary, when) ;
                }
            }
            else
            {
                ViewBag.calender = "No upcoming events found.";
            }
            #endregion 
            return View();
        }

        public ActionResult authorize()
        {
            return View();
        }
    }

    internal class GoogleTokenModel
    {
    }
}