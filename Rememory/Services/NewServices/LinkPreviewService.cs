using HtmlAgilityPack;
using Microsoft.UI.Dispatching;
using Rememory.Helper;
using Rememory.Models;
using Rememory.Models.NewModels;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rememory.Services.NewServices
{
    // Move to helpers?
    public static class LinkPreviewService
    {
        public static bool TryCreateLinkMetadata(ClipboardFormat format, string data, byte[] hash, out LinkMetadataModel? linkMetadata)
        {
            if (SettingsContext.Instance.EnableLinkPreviewLoading
                && Uri.TryCreate(data, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                linkMetadata = new LinkMetadataModel(format, data, hash);
                LoadMetaInfo(linkMetadata);
                return true;
            }
            linkMetadata = null;
            return false;
        }

        private static void LoadMetaInfo(LinkMetadataModel linkMetadata)
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            new Task(async () =>
            {
                HttpResponseMessage? response = null;
                try
                {
                    response = await new HttpClient().GetAsync(linkMetadata.Data);
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                catch (UriFormatException) { }

                if (response == null || !response.IsSuccessStatusCode)
                {
                    return;
                }

                string str = string.Empty;
                try
                {
                    str = await response.Content.ReadAsStringAsync();
                }
                catch (InvalidOperationException) { }

                HtmlDocument html = new();
                html.LoadHtml(str);
                var titleNode = html.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                var descriptionNode = html.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
                var imageNode = html.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

                if (titleNode != null && descriptionNode != null && imageNode != null)
                {
                    dispatcherQueue.TryEnqueue(() =>
                    {
                        linkMetadata.Title = titleNode.GetAttributeValue("content", string.Empty);
                        linkMetadata.Description = descriptionNode.GetAttributeValue("content", string.Empty);
                        linkMetadata.Image = imageNode.GetAttributeValue("content", string.Empty);
                    });
                }
            }).Start();
        }
    }
}
