using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace ContentForge.Editor
{
    /// <summary>Thin HTTP client for the Content Forge API, using UnityWebRequest + UniTask.</summary>
    internal static class ContentForgeClient
    {
        /// <summary>
        /// Calls <c>POST {baseUrl}/api/v1/generate</c> and returns the raw response body.
        /// Cancellation is honored via <paramref name="cancellationToken"/>.
        /// </summary>
        public static async UniTask<string> GenerateAsync(
            string baseUrl, GenerateRequestDto request, CancellationToken cancellationToken)
        {
            var url = baseUrl.TrimEnd('/') + "/api/v1/generate";
            var json = JsonConvert.SerializeObject(request);
            var body = Encoding.UTF8.GetBytes(json);

            using var www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            www.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await www.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw; // caller cancelled — propagate unchanged
            }
            catch (Exception ex)
            {
                // HTTP (4xx/5xx) or connection error. Prefer the server's response body (ProblemDetails).
                var responseBody = www.downloadHandler != null ? www.downloadHandler.text : null;
                var detail = string.IsNullOrEmpty(responseBody) ? ex.Message : responseBody;
                throw new ContentForgeException($"Request failed ({www.responseCode}): {detail}", ex);
            }

            return www.downloadHandler.text;
        }
    }

    /// <summary>Raised when a Content Forge API call fails.</summary>
    internal sealed class ContentForgeException : Exception
    {
        public ContentForgeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
