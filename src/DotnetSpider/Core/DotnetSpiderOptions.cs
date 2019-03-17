using Microsoft.Extensions.Configuration;

namespace DotnetSpider.Core
{
    public class DotnetSpiderOptions
    {
        private readonly IConfiguration _configuration;

        public DotnetSpiderOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        
    }
}