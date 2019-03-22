namespace DotnetSpider.Data.Storage.Model
{
    public class Column
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Length { get; set; }
        public bool Required { get; set; }
    }
}