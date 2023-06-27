using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace ElmahCore.Mvc
{
    [Serializable]
    public class ErrorWrapper
    {
       

        private static readonly List<string> Crawlers = new List<string>
        {
            "googlebot", "bingbot", "yandexbot", "ahrefsbot", "msnbot", "linkedinbot", "exabot", "compspybot",
            "yesupbot", "paperlibot", "tweetmemebot", "semrushbot", "gigabot", "voilabot", "adsbot-google",
            "botlink", "alkalinebot", "araybot", "undrip bot", "borg-bot", "boxseabot", "yodaobot", "admedia bot",
            "ezooms.bot", "confuzzledbot", "coolbot", "internet cruiser robot", "yolinkbot", "diibot", "musobot",
            "dragonbot", "elfinbot", "wikiobot", "twitterbot", "contextad bot", "hambot", "iajabot", "news bot",
            "irobot", "socialradarbot", "ko_yappo_robot", "skimbot", "psbot", "rixbot", "seznambot", "careerbot",
            "simbot", "solbot", "mail.ru_bot", "spiderbot", "blekkobot", "bitlybot", "techbot", "void-bot",
            "vwbot_k", "diffbot", "friendfeedbot", "archive.org_bot", "woriobot", "crystalsemanticsbot", "wepbot",
            "spbot", "tweetedtimes bot", "mj12bot", "who.is bot", "psbot", "robot", "jbot", "bbot", "bot"
        };

        private readonly Error _error;

        public ErrorWrapper()
        {
        }

        public ErrorWrapper(Error error, string[] sourcePath)
        {
            _error = error ?? throw new ArgumentNullException(nameof(error));
            bool markUpStackTrace = true; //may want to config toggle this in future, possible memory leak?
            if (markUpStackTrace)
            {
                var (markup, srcList) = ErrorDetailHelper.MarkupStackTrace(_error.Detail);
                HtmlMessage = markup;
                if (srcList?.Any() == true)
                    Sources = srcList.Select(i
                            => ErrorDetailHelper.GetStackFrameSourceCodeInfo(sourcePath, i.Method, i.Type, i.Source,
                                i.Line))
                        .Where(i => !string.IsNullOrEmpty(i.ContextCode))
                        .ToList();
            }
            else
            {
                HtmlMessage = _error.Detail;
            }
        }

        public List<StackFrameSourceCodeInfo> Sources { get; private set; }

        [XmlElement("ApplicationName")] public string ApplicationName => _error.ApplicationName;

        [XmlElement("HostName")] public string HostName => _error.HostName;

        [XmlElement("Type")] public string Type => _error.Type;

        [XmlElement("Body")] public string Body => _error.Body;

        [XmlElement("Source")] public string Source => _error.Source;

        [XmlElement("Message")] public string Message => _error.Message;

        [XmlElement("Detail")] public string Detail => _error.Detail;

        [XmlElement("User")] public string User => _error.User;

        [XmlElement("Time")] public DateTime Time => _error.Time;

        [XmlElement("StatusCode")] public int? StatusCode => _error.StatusCode == 0 ? (int?)null : _error.StatusCode;

        [XmlIgnore] public string HtmlMessage { get; set; }

        public bool IsMobile
        {
            get
            {
                var u = _error.ServerVariables["Header_User-Agent"];
                return UserAgentHelper.IsMobile(u);
            }
        }

        public string Os
        {
            get
            {
                var userAgent = _error.ServerVariables["Header_User-Agent"];
                if (string.IsNullOrEmpty(userAgent)) return null;

                if (userAgent.Contains("Windows")) return "Windows";
                if (userAgent.Contains("Android")) return "Android";
                if (userAgent.Contains("Linux")) return "Linux";
                if (userAgent.Contains("iPhone")) return "iPhone";
                if (userAgent.Contains("iPad")) return "iPhone";
                if (userAgent.Contains("Macintosh")) return "Macintosh";
                return null;
            }
        }

        public string Browser
        {
            get
            {
                var userAgent = _error.ServerVariables["Header_User-Agent"];
                if (string.IsNullOrEmpty(userAgent)) return null;

                if (Crawlers.Exists(x => userAgent.Contains(x)))
                    return "Bot";

                if (userAgent.Contains("Chrome")) return "Chrome";
                if (userAgent.Contains("Firefox")) return "Firefox";
                if (userAgent.Contains("Safari") || userAgent.Contains("AppleWebKit")) return "Safari";
                if (userAgent.Contains("OP")) return "Opera";
                if (userAgent.Contains("Edge")) return "Edge";
                if (userAgent.Contains("AppleWebKit")) return "AndroidBrowser";
                if (userAgent.Contains("Vivaldi")) return "Vivaldi";
                if (userAgent.Contains("Brave")) return "Brave";
                if (userAgent.Contains("MSIE") || userAgent.Contains("rv:")) return "MSIE";
                return "Generic";
            }
        }

        public string Severity
        {
            get
            {
                if (_error.StatusCode == 0) return "Error";
                if (_error.StatusCode < 200) return "Info";
                if (_error.StatusCode < 400) return "Success";
                if (_error.StatusCode < 500) return "Warning";
                return "Error";
            }
        }

        [XmlElement("Method")] public string Method => _error.ServerVariables["Method"];

        [XmlElement("Url")] public string Url => _error.ServerVariables["PathBase"] + _error.ServerVariables["Path"];

        [XmlElement("Client")] public string Client => _error.ServerVariables["Connection_RemoteIpAddress"];

        public string Version => _error.ServerVariables["Version"];

        [XmlIgnore] public List<ElmahLogMessageEntry> MessageLog => GetMessageLog();

        private List<ElmahLogMessageEntry> GetMessageLog()
        {
            var result = _error.MessageLog.ToList();
            result.AddRange(_error.Params.Select(param => new ElmahLogMessageEntry
            {
                Collapsed = true,
                TimeStamp = param.TimeStamp,
                Level = LogLevel.Information,
                Message = $"Method {param.TypeName}.{param.MemberName} call with parameters:",
                Params = param.Params
            }));

            return result.OrderBy(i => i.TimeStamp).ToList();
        }

        [XmlIgnore] public List<ElmahLogSqlEntry> SqlLog => _error.SqlLog;

        [XmlIgnore] public List<ElmahLogParamEntry> Params => _error.Params;

        public SerializableDictionary<string, string> Form => _error.Form.AllKeys
            .Where(i => i != "$request-body")
            .ToSerializableDictionary(k => k, k => _error.Form[k]);

        public SerializableDictionary<string, string> QueryString => _error.QueryString.AllKeys
            .ToSerializableDictionary(k => k, k => _error.QueryString[k]);

        public SerializableDictionary<string, string> Cookies => _error.Cookies.AllKeys
            .ToSerializableDictionary(k => k, k => _error.Cookies[k]);

        [XmlElement("Header")]
        public SerializableDictionary<string, string> Header
        {
            get
            {
                return _error.ServerVariables.AllKeys.Where(i => i.StartsWith("Header_"))
                    .ToSerializableDictionary(k => k.Substring("Header_".Length), k => _error.ServerVariables[k]);
            }
        }

        public SerializableDictionary<string, string> Connection => _error.ServerVariables.AllKeys
            .Where(i => i.StartsWith("Connection_"))
            .Where(i => i.Contains("Port") && _error.ServerVariables[i] != "0") //ignore empty
            .ToSerializableDictionary(k => k.Substring("Connection_".Length), k => _error.ServerVariables[k]);

        public SerializableDictionary<string, string> Items => _error.ServerVariables.AllKeys
            .Where(i => i.StartsWith("Items_"))
            .ToSerializableDictionary(k => k.Substring("Items_".Length), k => _error.ServerVariables[k]);

        public SerializableDictionary<string, string> Session => _error.ServerVariables.AllKeys
            .Where(i => i.StartsWith("Session_"))
            .ToSerializableDictionary(k => k.Substring("Session_".Length), k => _error.ServerVariables[k]);

        public SerializableDictionary<string, string> UserData => _error.ServerVariables.AllKeys
            .Where(i => i.StartsWith("User_"))
            .ToSerializableDictionary(k => k.Substring("User_".Length), k => _error.ServerVariables[k]);

        private static string[] keyWords = { "User_", "Header_", "Connection_", "Items_", "Session_" };

        public SerializableDictionary<string, string> ServerVariables
        {
            get
            {
                return _error.ServerVariables.AllKeys.Where(i => !keyWords.Any(i.StartsWith))
                    .ToSerializableDictionary(k => k, k => _error.ServerVariables[k]);
            }
        }
    }
}