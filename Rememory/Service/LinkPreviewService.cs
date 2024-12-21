using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Rememory.Helper;
using Rememory.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rememory.Service
{
    public class LinkPreviewService : ILinkPreviewService
    {
        private readonly IStorageService _storageService = App.Current.Services.GetService<IStorageService>();

        public bool TryCreateLinkItem(ClipboardItem item, out ClipboardLinkItem linkItem)
        {
            if (item.DataMap.TryGetValue(ClipboardFormat.Text, out string text)
                    && Uri.TryCreate(text, UriKind.Absolute, out Uri uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                linkItem = new ClipboardLinkItem(item);
                LoadMetaInfo(linkItem);
                return true;
            }
            linkItem = null;
            return false;
        }

        private void LoadMetaInfo(ClipboardLinkItem item)
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            new Task(async () =>
            {
                HttpResponseMessage response = null;
                try
                {
                    response = await new HttpClient().GetAsync(item.DataMap[ClipboardFormat.Text]);
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
                        item.Title = titleNode.GetAttributeValue("content", string.Empty);
                        item.Description = descriptionNode.GetAttributeValue("content", string.Empty);

                        try
                        {
                            item.Image.UriSource = new Uri(imageNode.GetAttributeValue("content", string.Empty));
                        }
                        catch (UriFormatException) { }

                        item.HasInfoLoaded = true;

                        _storageService.SaveLinkPreviewInfo(item);
                    });
                }
            }).Start();
        }
    }
}
