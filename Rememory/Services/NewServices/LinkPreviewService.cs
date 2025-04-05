using HtmlAgilityPack;
using Microsoft.UI.Dispatching;
using Rememory.Models;
using Rememory.Models.NewModels;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rememory.Services.NewServices
{
    public class LinkPreviewService
    {
        private readonly NewSqliteService _sqliteService = new();

        public void TryAddLinkMetadata(DataModel dataModel)
        {
            if (SettingsContext.Instance.EnableLinkPreviewLoading
                && Uri.TryCreate(dataModel.Data, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                LoadMetaInfo(dataModel);
            }
        }

        private void LoadMetaInfo(DataModel dataModel)
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
                        _sqliteService.AddLinkMetadata(linkMetadata, dataModel.Id);
                    });
                }
            }).Start();
        }
    }
}
