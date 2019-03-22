using Microsoft.Extensions.Configuration;

namespace DotnetSpider.Core
{
    public class SpiderOptions
    {
        private readonly IConfiguration _configuration;

        public string ConnectionString => _configuration["ConnectionString"];

        public bool Distribute => "true" == _configuration["Distribute"];

        public SpiderOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}