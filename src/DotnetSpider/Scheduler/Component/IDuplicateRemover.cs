using DotnetSpider.Downloader;

namespace DotnetSpider.Scheduler.Component
{
    public interface IDuplicateRemover : System.IDisposable
    {
        /// <summary>
        /// Check whether the request is duplicate.
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns>Whether the request is duplicate.</returns>
        bool IsDuplicate(Request request);
        
        /// <summary>
        /// Reset duplicate check.
        /// </summary>
        void ResetDuplicateCheck();
    }
}