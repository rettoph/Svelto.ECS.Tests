using Svelto.DataStructures;

namespace Svelto.ECS.Core.Groups
{
    public class EntityGroupQuery
    {
        public abstract class WithTags<TTag1>
            where TTag1 : IGroupTag
        {
            public abstract class WithComponents<TComponent1>
                where TComponent1 : IEntityComponent
            {
                public static FasterList<ExclusiveGroupStruct> Groups { get; set; }
            }
        }
    }
}
