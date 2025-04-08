using HtmlAgilityPack;
using Microsoft.UI.Dispatching;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Models.Metadata;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rememory.Services
{
    public class LinkPreviewService(IStorageService storageService) : ILinkPreviewService
    {
        private readonly IStorageService _storageService = storageService;

        public void TryAddLinkMetadata(ClipModel clip, DataModel dataModel)
        {
            if (SettingsContext.Instance.EnableLinkPreviewLoading && clip.IsLink)
            {
                LoadMetaInfo(clip, dataModel);
            }
        }

        private void LoadMetaInfo(ClipModel clip, DataModel dataModel)
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            new Task(async () =>
            {
                HttpResponseMessage? response = null;
                try
                {
                    response = await new HttpClient().GetAsync(dataModel.Data);
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
                        LinkMetadataModel linkMetadata = new()
                        {
                            Url = dataModel.Data,
                            Title = titleNode.GetAttributeValue("content", string.Empty),
                            Description = descriptionNode.GetAttributeValue("content", string.Empty),
                            Image = imageNode.GetAttributeValue("content", string.Empty)
                        };
                        dataModel.Metadata = linkMetadata;
                        _storageService.AddLinkMetadata(linkMetadata, dataModel.Id);
                        clip.UpdateProperty(nameof(clip.Data));
                    });
                }
            }).Start();
        }
    }
}
