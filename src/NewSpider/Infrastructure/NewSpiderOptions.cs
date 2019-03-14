using Microsoft.Extensions.Configuration;

namespace NewSpider.Infrastructure
{
    public class NewSpiderOptions
    {
        private readonly IConfiguration _configuration;

        public NewSpiderOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        
    }
}