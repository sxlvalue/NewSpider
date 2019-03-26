using System.Linq;
using DotnetSpider.Data.Parser;
using DotnetSpider.Data.Parser.Attribute;
using DotnetSpider.Data.Parser.Formatter;
using DotnetSpider.Data.Storage.Model;
using DotnetSpider.Selector;
using Xunit;

namespace DotnetSpider.Tests.Data.Parser
{
    public class EntityParseTests
    {       
        [Fact(DisplayName = "Formatters")]
        public void Formatters()
        {
            var parser = new DataParser<Entity11>();
            var fields = parser.Model.ValueSelectors.ToArray();
            var formatters = (fields[0]).Formatters;
            Assert.Equal(2, formatters.Length);
            var replaceFormatter = (ReplaceFormatter) formatters[0];
            Assert.Equal("a", replaceFormatter.NewValue);
            Assert.Equal("b", replaceFormatter.OldValue);
        }

        [Fact(DisplayName = "EntitySelector")]
        public void EntitySelector()
        {
            var parser = new DataParser<Entity7>();
            Assert.Equal("expression", parser.Model.Selector.Expression);
            Assert.Equal(SelectorType.XPath, parser.Model.Selector.Type);


            var entity2 = new DataParser<Entity8>();
            Assert.Equal("expression2", entity2.Model.Selector.Expression);
            Assert.Equal(SelectorType.Css, entity2.Model.Selector.Type);

            var entity3 = new DataParser<Entity9>();
            Assert.Null(entity3.Model.Selector);
            Assert.Equal(typeof(Entity9).FullName, entity3.Model.TypeName);
            Assert.Equal(typeof(Entity9).FullName, entity3.TableMetadata.TypeName);
        }


        [Fact(DisplayName = "TableInfoEntityModelDefine")]
        public void TableInfoEntityModelDefine()
        {
            var parser = new DataParser<TableInfoEntity>();
            Assert.Equal(2, parser.Model.ValueSelectors.Count);

            var field1 = parser.Model.ValueSelectors.First();
            Assert.Equal("CategoryName", field1.Name);
            Assert.Equal("cat", field1.Expression);
            Assert.Equal(SelectorType.Enviroment, field1.Type);
        }

        [EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
        private class NullTableInfoEntity : EntityBase<NullTableInfoEntity>
        {
            [ValueSelector(Expression = "cat", Type = SelectorType.Enviroment)]
            public string CategoryName { get; set; }

            [ValueSelector(Expression = "./@jdzy_shop_id")]
            public string JdzyShopId { get; set; }
        }

        [EntitySelector(Expression = "//li[@class='gl-item']/div[contains(@class,'j-sku-item')]")]
        private class TableInfoEntity : EntityBase<TableInfoEntity>
        {
            [ValueSelector(Expression = "cat", Type = SelectorType.Enviroment)]
            public string CategoryName { get; set; }

            [ValueSelector(Expression = "./@jdzy_shop_id")]
            public string JdzyShopId { get; set; }
        }

        [EntitySelector(Expression = "expression")]
        private class Entity7 : EntityBase<Entity7>
        {
            [ValueSelector(Expression = "")] public string Name { get; set; }
        }

        [EntitySelector(Expression = "expression2", Type = SelectorType.Css)]
        private class Entity8 : EntityBase<Entity8>
        {
            [ValueSelector(Expression = "")] public string Name { get; set; }
        }

        private class Entity9 : EntityBase<Entity9>
        {
            [ValueSelector(Expression = "")] public string Name { get; set; }
        }

        private class Entity4 : EntityBase<Entity4>
        {
            [ValueSelector(Expression = "")] public string Name { get; set; }
        }

        private class Entity14 : EntityBase<Entity14>
        {
            [ValueSelector(Expression = "Url")] public string Url { get; set; }
        }

        private class Entity10 : EntityBase<Entity10>
        {
            [ValueSelector(Expression = "")] public string Name { get; set; }

            [ValueSelector(Expression = "")] public string Name2 { get; set; }

            [ValueSelector(Expression = "")] public string Name3 { get; set; }
        }

        private class Entity18 : EntityBase<Entity18>
        {
            [ValueSelector(Expression = "")] public string c1 { get; set; }
        }

        private class Entity19 : EntityBase<Entity19>
        {
            [ValueSelector(Expression = "")] public string c1 { get; set; }
        }

        private class Entity11 : EntityBase<Entity11>
        {
            [ReplaceFormatter(NewValue = "a", OldValue = "b")]
            [RegexFormatter(Pattern = "a(*)")]
            [ValueSelector(Expression = "Name")]
            public string Name { get; set; }
        }


        private class Entity3 : EntityBase<Entity3>
        {
            [ValueSelector(Expression = "")] public string Url { get; set; }
        }

        private class Entity2 : EntityBase<Entity2>
        {
            [ValueSelector(Expression = "")] public string Url { get; set; }
        }
    }
}