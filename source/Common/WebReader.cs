using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Zue.Common
{
    public static class WebReader
    {
        #region Initialisation
        private static ILogger _initLog;
        private static ILogger _logger
        {
            get
            {
                if (_initLog == null)
                    _initLog = LogProvider.GetLogger(nameof(WebReader));
                return _initLog ?? NullLogger.Instance;
            }
        }
        private static bool _isInitialised;
        #endregion

        public static void SetBaseAddress(this HttpClient httpClient, string baseAddress)
        {
            if (!string.IsNullOrEmpty(baseAddress))
                httpClient.BaseAddress = new Uri(baseAddress);
        }

        public static void SetBasicAuthorization(this HttpClient httpClient, NetworkCredential cred)
        {
            if (!_isInitialised)
            {
                if (cred != null && cred != default)
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                            Encoding.ASCII.GetBytes($"{cred.UserName}:{cred.Password}")));
                //httpClient.DefaultRequestHeaders.Add("User-Agent", "ShareFile-Service/1.0");
                //httpClient.Timeout = TimeSpan.FromMinutes(10);
                _isInitialised = true;
            }
        }

        public static HttpClient GetAuthorizedHttpClient(ICredentials cred, bool preAuthenticate = false)
        {
            var handler = new HttpClientHandler { Credentials = cred, PreAuthenticate = preAuthenticate };
            var httpClient = new HttpClient(handler);
            //httpClient.Timeout = TimeSpan.FromMinutes(10);
            //var productValue = new ProductInfoHeaderValue("ShareFile-Service", "1.0");
            //var commentValue = new ProductInfoHeaderValue("(+https://example.com)");
            //httpClient.DefaultRequestHeaders.UserAgent.Add(productValue);
            //httpClient.DefaultRequestHeaders.UserAgent.Add(commentValue);
            if (cred is NetworkCredential netCred)
                _logger.LogDebug("User set to {0}", netCred.UserName);
            return httpClient;
        }

        public static async Task<string> GetResponseContentAsync(
            this HttpClient httpClient, string url, CancellationToken ct = default)
        {
            string responseBody = string.Empty;
            _logger.LogTrace("GET {0}", url);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                responseBody = await response.Content
                    .ReadAsStringAsync().ConfigureAwait(false) ?? "";
            stopwatch.Stop();
            int previewLength = 20;
            string responsePreview = responseBody.Length > previewLength ?
                responseBody.Substring(0, previewLength) + "..." : "";
            _logger.LogTrace("{0} {1} {2}ms {3}", response.ReasonPhrase, url,
                stopwatch.ElapsedMilliseconds, responsePreview);
            return responseBody;
            //return await httpClient.GetStringAsync(url, ct);
        }

        public static async Task<string> GetResponseContentAsync(string url, ICredentials cred, CancellationToken ct = default)
        {
            var httpClient = GetAuthorizedHttpClient(cred);
            var uri = new Uri(url);
            uri = new Uri(string.Join("://", uri.Scheme, uri.Host));
            string userName = cred?.GetCredential(uri, "")?.UserName ?? "";
            _logger.LogDebug("GET {0} as {1}", url, userName);
            return await httpClient.GetResponseContentAsync(url, ct);
        }

        public static string GetJsonNodeText(string json, params string[] tags)
            => GetJsonNodeValue<string>(json, tags)?.Trim() ?? string.Empty;

        public static T GetJsonNodeValue<T>(string json, params string[] tags)
        {
            var result = default(T);
            if (tags?.Length > 0)
            {
                var jsonNode = JsonNode.Parse(json);
                if (jsonNode != null)
                {
                    for (int i = 0; i < tags.Length; i++)
                        jsonNode = jsonNode?[tags[i]];
                    try
                    {
                        if (jsonNode != null)
                            result = jsonNode.GetValue<T>();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to get JSON node value '{0}'.",
                            jsonNode?.GetPath() ?? tags.ToEnumeratedString("."));
                    }
                }
            }
            return result;
        }

        public static async Task<string> GetJsonDocumentElementValueAsync(
            this HttpClient httpClient, string url, params string[] tags)
        {
            string result = string.Empty;
            if (tags != null)
            {
                string jsonString = await httpClient.GetStringAsync(url);
                using (JsonDocument document = JsonDocument.Parse(jsonString))
                {
                    var root = document.RootElement;
                    for (int i = 0; i < tags.Length - 1; i++)
                        root = root.GetProperty(tags[i]);
                    if (tags.Length > 0)
                        result = root.GetValue(tags[tags.Length - 1]);
                }
            }
            return result;
        }

        public static string GetValue(this JsonElement jsonElement, string elementName)
        {
            string result = string.Empty;
            if (jsonElement.TryGetProperty(elementName, out JsonElement idElement))
                result = idElement.GetString() ?? "";
            return result;
        }

        public static async Task<Stream> GetContentStreamAsync(
            this HttpClient httpClient, string url, CancellationToken ct = default)
        {
            var stream = default(Stream);
            var httpResponseMessage = await httpClient.GetAsync(url, ct);
            if (httpResponseMessage.IsSuccessStatusCode)
            {
                stream = await httpResponseMessage.Content
                    .ReadAsStreamAsync().ConfigureAwait(false);
                stream.Position = 0;
            }
            return stream;
        }

        public static async Task DownloadPdf(this HttpClient httpClient, string apiUrl, params string[] filePaths)
        {
            if (!string.IsNullOrEmpty(apiUrl) && filePaths?.Length > 0 &&
                !string.IsNullOrEmpty(filePaths[filePaths.Length - 1]))
            {
                _logger.LogTrace("Downloading '{0}'", apiUrl);
                int lastIndex = filePaths.Length - 1;
                if (!filePaths[lastIndex].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    filePaths[lastIndex] = $"{filePaths[lastIndex]}.pdf";
                string filePath = Path.Combine(filePaths);
                using (var stream = await httpClient.GetContentStreamAsync(apiUrl))
                    if (stream != null)
                        await FileHandler.WriteFileAsync(filePath, stream, true);
            }
        }
    }
}
