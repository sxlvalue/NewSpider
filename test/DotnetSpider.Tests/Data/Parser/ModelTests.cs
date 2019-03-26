using Xunit;

namespace DotnetSpider.Tests.Data.Parser
{
    public class ModelTests
    {
        /// <summary>
        /// 测试实体模型的 TypeName 是否解析到 Model 对象中
        /// </summary>
        [Fact(DisplayName = "ModelTypeName")]
        public void ModelTypeName()
        {
            // TODO
        }

        /// <summary>
        /// 测试实体模型上的 EntitySelector 是否解析到 Model 对象中
        /// 1. Type
        /// 2. Expression
        /// 3. Arguments
        /// 4. Take
        /// 5. TakeFromHead
        /// </summary>
        [Fact(DisplayName = "EntitySelector")]
        public void EntitySelector()
        {
            // TODO
        }

        /// <summary>
        /// 测试实体模型上的 ValueSelector 有没有正确解析到 Model 对象中
        /// 1. 无 ValueSelector
        /// 2. 单个 ValueSelector
        /// 3. 多个 ValueSelector 无重复
        /// 4. 多个 ValueSelector 并有重复
        /// </summary>
        [Fact(DisplayName = "ShareValueSelectors")]
        public void ShareValueSelectors()
        {
            // TODO
        }

        /// <summary>
        /// 测试实体模型上的 FollowSelectors 有没有正确解析到 Model 对象中
        /// 1. 无 FollowSelector
        /// 2. 单个 FollowSelector
        /// 3. 多个 FollowSelector 无重复
        /// 4. 多个 FollowSelector 并有重复
        /// </summary>
        [Fact(DisplayName = "FollowSelectors")]
        public void FollowSelectors()
        {
            // TODO
        }
    }
}