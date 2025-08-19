using HtmlAgilityPack;
using Rememory.Contracts;
using Rememory.Models;
using Rememory.Models.Metadata;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Rememory.Services
{
    public class LinkPreviewService : ILinkPreviewService
    {
        private readonly IStorageService _storageService;
        private readonly HttpClient _httpClient;

        public LinkPreviewService(IStorageService storageService)
        {
            _storageService = storageService;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");
        }

        public void TryLoadLinkMetadata(DataModel dataModel)
        {
            Task.Run(async () =>
            {
                HttpResponseMessage? response = null;
                try
                {
                    response = await _httpClient.GetAsync(dataModel.Data);
                }
                catch (HttpRequestException) { }
                catch (TaskCanceledException) { }
                catch (UriFormatException) { }

                if (response is null || !response.IsSuccessStatusCode)
                {
                    return;
                }

                string htmlContent = string.Empty;
                try
                {
                    htmlContent = await response.Content.ReadAsStringAsync();
                }
                catch (InvalidOperationException) { }

                if (string.IsNullOrEmpty(htmlContent))
                {
                    return;
                }

                HtmlDocument html = new();
                html.LoadHtml(htmlContent);

                var ogTitleNode = html.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                var titleNode = ogTitleNode ?? html.DocumentNode.SelectSingleNode("//title");

                var ogDescriptionNode = html.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
                var descriptionNode = ogDescriptionNode ?? html.DocumentNode.SelectSingleNode("//meta[@name='description']");

                var imageNode = html.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

                if (titleNode is not null)
                {
                    App.Current.DispatcherQueue.TryEnqueue(() =>
                    {
                        LinkMetadataModel linkMetadata = new()
                        {
                            Url = dataModel.Data,
                            Title = HtmlDecode(titleNode?.GetAttributeValue("content", titleNode?.InnerText ?? string.Empty))?.Trim(),
                            Description = HtmlDecode(descriptionNode?.GetAttributeValue("content", string.Empty))?.Trim(),
                            Image = imageNode?.GetAttributeValue("content", string.Empty)?.Trim()
                        };
                        dataModel.Metadata = linkMetadata;
                        _storageService.AddLinkMetadata(linkMetadata, dataModel.Id);
                    });
                }
            });
        }

        private static string? HtmlDecode(string? input)
        {
            var temp = HttpUtility.HtmlDecode(input);
            while (!string.Equals(temp, input))
            {
                input = temp;
                temp = HttpUtility.HtmlDecode(input);
            }
            return input;
        }
    }
}
