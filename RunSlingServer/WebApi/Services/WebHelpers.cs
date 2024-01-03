using System.Text;
using System.Web;

namespace RunSlingServer.WebApi.Services
{
    public class WebHelpers : IWebHelpers
    {
        public async Task<string> PostToSlingerServer(string url, IEnumerable<KeyValuePair<string, string>> postData, HttpRequest request, ILogger? logger)
        {
            var httpContext = request.HttpContext;
            var httpClientFactory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();

            try
            {
                using var httpClient = httpClientFactory.CreateClient();
                using var content = new FormUrlEncodedContent(postData);
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                var responseMessage = await httpClient.PostAsync(url, content);

                return await responseMessage.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                logger?.LogError($"HTTP request to {url} failed with exception: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Read the POST data from the request body and return a dictionary of key/value pairs.
        /// </summary>
        /// <param name="request">Example: "Channel:Channel & Digits:0."</param>
        /// <returns></returns>
        public static async Task<Dictionary<string, string>> GetSlingChannelChangeDataFromRequest(HttpRequest request)
        {
            var body = new StreamReader(request.Body);
            var requestBodyData = await body.ReadToEndAsync();

            FixPostmanFormDataIssue(ref requestBodyData);

            Dictionary<string, string> keyValuePairs = new();

            var pairs = requestBodyData.Split('&');

            if (pairs.Length <= 0)
                return keyValuePairs;

            foreach (var keyVal in pairs)
            {
                var kv = keyVal.Split('=');
                if (kv.Length != 2)
                    continue;

                // Ignore custom, non-Slinger parameters, such as "_SlingBoxName"
                if (kv[0].StartsWith("_"))
                    continue;
                
                keyValuePairs.Add(kv[0], kv[1]);
            }

            return keyValuePairs;
        }


        private static bool IsPostmanMalformedBody(string requestBodyData)
        {
            return !string.IsNullOrEmpty(requestBodyData) || requestBodyData.Contains("Content-Disposition:");
        }


        // Data received when testing with Postman may be malformed
        private static void FixPostmanFormDataIssue(ref string requestBodyData)
        {
            if (!IsPostmanMalformedBody(requestBodyData))
            {
                return;
            }

            var formData = new StringBuilder();
            var lines = requestBodyData.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("-------------"))
                {
                    if (i > 0)
                    {
                        formData.Append("&");
                    }
                }
                else if (lines[i].Contains("Content-Disposition:"))
                {
                    var keyName = lines[i]
                        .Replace("Content-Disposition: form-data; name=\"", "")
                        .Replace("\"", "")
                        .Replace("\r", "");
                    formData.Append($"{keyName}=");
                }
                else
                {
                    formData.Append(lines[i].Trim());
                }
            }

            requestBodyData = formData.ToString().TrimEnd('&');
        }



        public static string GetClientIp(HttpRequest request)
        {
            var requestHttpContext = request.HttpContext;
            var remoteIp = requestHttpContext.Connection.RemoteIpAddress;

            var clientIp = "";
            if (remoteIp != null)
            {
                clientIp = remoteIp.ToString().Replace("::ffff:", "");
            }

            return clientIp;
        }

        public static (Uri? uri, bool isValidUrl, string errorMessage) ValidateUrl(HttpRequest request)
        {
            string errMsg;
            const string exampleUrl = "http://domain.com/Remote/sbName";

            var url = request.Query["url"];

            if (string.IsNullOrWhiteSpace(request.Query["url"]))
            {
                errMsg = "Parameter 'url' must be passed in the query string";
                return (null, false, errMsg);
            }

            url = HttpUtility.UrlDecode(url);

#pragma warning disable CS8604 // Possible null reference argument.
            if (!IsValidUri(url))
            {
                errMsg = $"Parameter 'url' must be a valid URL, eg: {exampleUrl}";
                return (null, false, errMsg);
            }

            var uriAddress = new Uri(url);
#pragma warning restore CS8604 // Possible null reference argument.


            if (!uriAddress.IsAbsoluteUri)
            {
                errMsg = $"Parameter 'url' must be an absolute URL, eg: {exampleUrl}";
                return (null, false, errMsg);
            }

            const int segmentsCount = 3; // http://domain.com/Remote/sling_1
            if (uriAddress.Segments.Length < segmentsCount)
            {
                errMsg = $"Parameter 'url' must be an absolute URL with at least {segmentsCount} segments, eg: {exampleUrl}";
                return (null, false, errMsg);
            }

            return (uriAddress, true, "");
        }

        public static bool IsValidUri(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
                   && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
    }
}
