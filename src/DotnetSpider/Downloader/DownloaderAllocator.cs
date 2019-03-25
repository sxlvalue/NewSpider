using System;
using System.Threading.Tasks;
using DotnetSpider.Downloader.Entity;

namespace DotnetSpider.Downloader
{
    public class DownloaderAllocator : IDownloaderAllocator
    {
        public Task<DownloaderEntry> CreateDownloaderEntryAsync(AllotDownloaderMessage allotDownloaderMessage)
        {
            DownloaderEntry downloaderEntry = null;
            // TODO: 添加其它下载器的分配方法
            switch (allotDownloaderMessage.Type)
            {
                case DownloaderType.Empty:
                {
                    downloaderEntry = new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        Downloader = new EmptyDownloader()
                    };
                    break;
                }
                case DownloaderType.Test:
                {
                    downloaderEntry = new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        Downloader = new TestDownloader()
                    };
                    break;
                }
                case DownloaderType.Exception:
                {
                    downloaderEntry = new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        Downloader = new ExceptionDownloader()
                    };
                    break;
                }
                case DownloaderType.WebDriver:
                {
                    throw new NotImplementedException();
                }
                case DownloaderType.HttpClient:
                {
                    var httpClient = new HttpClientDownloader
                    {
                        UseProxy = allotDownloaderMessage.UseProxy,
                        AllowAutoRedirect = allotDownloaderMessage.AllowAutoRedirect,
                        Timeout = allotDownloaderMessage.Timeout,
                        DecodeHtml = allotDownloaderMessage.DecodeHtml,
                        UseCookies = allotDownloaderMessage.UseCookies
                    };
                    httpClient.AddCookies(allotDownloaderMessage.Cookies);
                    downloaderEntry = new DownloaderEntry
                    {
                        LastUsedTime = DateTime.Now,
                        Downloader = httpClient
                    };

                    break;
                }
            }

            return Task.FromResult(downloaderEntry);
        }
    }
}