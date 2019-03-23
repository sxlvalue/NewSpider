using System;
using DotnetSpider.Data.Storage;
using Microsoft.Extensions.Configuration;

namespace DotnetSpider.Core
{
    public class SpiderOptions
    {
        private readonly IConfiguration _configuration;

        public string ConnectionString => _configuration["ConnectionString"];

        public string Storage => _configuration["Storage"];

        public StorageType StorageType => string.IsNullOrWhiteSpace(_configuration["StorageType"])
            ? StorageType.InsertIgnoreDuplicate
            : (StorageType) Enum.Parse(typeof(StorageType), _configuration["StorageType"]);

        public bool Distribute => "true" == _configuration["Distribute"];

        public string EmailHost => _configuration["EmailHost"];

        public string EmailAccount => _configuration["EmailAccount"];

        public string EmailPassword => _configuration["EmailPassword"];

        public string EmailDisplayName => _configuration["EmailDisplayName"];

        public string EmailPort => _configuration["EmailPort"];


        public SpiderOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
    }
}