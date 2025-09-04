using Svelto.DataStructures;
using Svelto.ECS.Extensions;
using System;
using System.Linq;

namespace Svelto.ECS.Core.Groups
{
    public interface IEntityGroup
    {
        int Id { get; }
        Type DescriptorType { get; }
        FasterReadOnlyList<Type> ComponentTypes { get; }
        FasterReadOnlyList<Type> TagTypes { get; }
        ExclusiveGroupStruct ExclusiveGroupStruct { get; }
        static IEntityGroup Instance => throw new NotImplementedException();
    }

    public interface IEntityGroup<TDescriptor> : IEntityGroup
        where TDescriptor : IEntityDescriptor, new()
    {

    }

    public class EntityGroup : IEntityGroup
    {
        public static IEntityGroup Instance => throw new NotImplementedException();

        public int Id { get; }
        public Type DescriptorType { get; }
        public FasterReadOnlyList<Type> ComponentTypes { get; }
        public FasterReadOnlyList<Type> TagTypes { get; }
        public ExclusiveGroupStruct ExclusiveGroupStruct { get; }

        public EntityGroup(int id, Type descriptorType, FasterList<Type> componentTypes, FasterList<Type> tagTypes)
        {
            Id = id;
            DescriptorType = descriptorType;
            ComponentTypes = new FasterReadOnlyList<Type>(componentTypes);
            TagTypes = new FasterReadOnlyList<Type>(tagTypes);
            ExclusiveGroupStruct = ExclusiveGroupStruct.Generate();
        }

        private static readonly FasterDictionary<int, EntityGroup> _groups = new FasterDictionary<int, EntityGroup>();
        private static EntityGroup GetOrCreate(Type descriptorType, Type[] componentTypes, Type[] tagTypes)
        {
            int id = componentTypes.Concat(tagTypes).Concat(new[] { descriptorType }).SortAndHash();
            if (_groups.TryGetValue(id, out EntityGroup group) == true)
            { // Create a new group with the specified descriptor type, component types, and tag types,
                return group;
            }

            group = new EntityGroup(id, descriptorType, new FasterList<Type>(componentTypes), new FasterList<Type>(tagTypes));
            _groups.Add(id, group);

            return group;
        }

        #region Begin EntityGroup Builder Helper Classes

        /// <summary>
        /// Define an EntityGroup with an associated <see cref="TDescriptor"/>. All components within the
        /// descriptor will automatically be associated with the group as well.
        /// </summary>
        /// <typeparam name="TDescriptor"></typeparam>
        public abstract class WithDescriptor<TDescriptor> : IEntityGroup, IEntityGroup<TDescriptor>
            where TDescriptor : IEntityDescriptor, new()
        {
            private static readonly IEntityGroup _instance = GetOrCreate(
                descriptorType: typeof(TDescriptor),
                componentTypes: new TDescriptor().componentsToBuild.Select(x => x.GetEntityComponentType()).ToArray(),
                tagTypes: Array.Empty<Type>());

            public static IEntityGroup Instance => _instance;

            public int Id => _instance.Id;
            public Type DescriptorType => _instance.DescriptorType;
            public FasterReadOnlyList<Type> ComponentTypes => _instance.ComponentTypes;
            public FasterReadOnlyList<Type> TagTypes => _instance.TagTypes;
            public ExclusiveGroupStruct ExclusiveGroupStruct => _instance.ExclusiveGroupStruct;

            /// <summary>
            /// Define an EntityGroup with an associated <see cref="TDescriptor"/> and tags.
            /// </summary>
            /// <typeparam name="TDescriptor"></typeparam>
            public abstract class WithTags<TTag1> : IEntityGroup, IEntityGroup<TDescriptor>
                where TTag1 : IGroupTag
            {
                private static readonly IEntityGroup _instance = GetOrCreate(
                    descriptorType: typeof(TDescriptor),
                    componentTypes: new TDescriptor().componentsToBuild.Select(x => x.GetEntityComponentType()).ToArray(),
                    tagTypes: new[] { typeof(TTag1) });

                public static IEntityGroup Instance => _instance;

                public int Id => _instance.Id;
                public Type DescriptorType => _instance.DescriptorType;
                public FasterReadOnlyList<Type> ComponentTypes => _instance.ComponentTypes;
                public FasterReadOnlyList<Type> TagTypes => _instance.TagTypes;
                public ExclusiveGroupStruct ExclusiveGroupStruct => _instance.ExclusiveGroupStruct;
            }

            /// <summary>
            /// Define an EntityGroup with an associated <see cref="TDescriptor"/> and tags.
            /// </summary>
            /// <typeparam name="TDescriptor"></typeparam>
            public abstract class WithTags<TTag1, TTag2, TTag3, TTag4> : IEntityGroup
                where TTag1 : IGroupTag
                where TTag2 : IGroupTag
                where TTag3 : IGroupTag
                where TTag4 : IGroupTag
            {
                private static readonly IEntityGroup _instance = GetOrCreate(
                    descriptorType: typeof(TDescriptor),
                    componentTypes: new TDescriptor().componentsToBuild.Select(x => x.GetEntityComponentType()).ToArray(),
                    tagTypes: new[] { typeof(TTag1), typeof(TTag2), typeof(TTag3), typeof(TTag4) });

                public static IEntityGroup Instance => _instance;

                public int Id => _instance.Id;
                public Type DescriptorType => _instance.DescriptorType;
                public FasterReadOnlyList<Type> ComponentTypes => _instance.ComponentTypes;
                public FasterReadOnlyList<Type> TagTypes => _instance.TagTypes;
                public ExclusiveGroupStruct ExclusiveGroupStruct => _instance.ExclusiveGroupStruct;
            }
        }
        #endregion
    }
}
