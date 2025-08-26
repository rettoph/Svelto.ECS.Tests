using Svelto.DataStructures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Svelto.ECS
{
    interface ITouchedByReflection { }

    internal sealed class GroupCompound
    {
        private readonly int _Id;
        private readonly string _Name;
        private readonly Type[] _GroupTagTypes;
        private readonly FasterList<ExclusiveGroupStruct> _Groups;
        private readonly HashSet<ExclusiveGroupStruct> _GroupsHashSet;

        public FasterReadOnlyList<ExclusiveGroupStruct> Groups
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(this._Groups);
        }

        public ExclusiveBuildGroup BuildGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(this._Groups[0], 1);
        }

        private GroupCompound(int id, string name, Type[] groupTagTypes, ExclusiveGroupBitmask buildGroupBitmask)
        {
            this._Id = id;
            this._Name = name;
            this._GroupTagTypes = groupTagTypes;
            this._Groups = new FasterList<ExclusiveGroupStruct>(1);
            this._GroupsHashSet = new HashSet<ExclusiveGroupStruct>();

            var group = new ExclusiveGroup(buildGroupBitmask);
            this.Add(group);
            this.PopulateExistingGroups();

#if DEBUG
            var groupName =
                    $"Compound: {this._Name} ID {group.id}";
            GroupNamesMap.idToName[group] = groupName;
#endif

            //The hashname is independent from the actual group ID. this is fundamental because it is want
            //guarantees the hash to be the same across different machines
            GroupHashMap.RegisterGroup(group, this._Name);
        }

        internal void Add(ExclusiveGroupStruct group)
        {
#if DEBUG && !PROFILE_SVELTO
            for (var i = 0; i < _Groups.count; ++i)
                if (_Groups[i] == group)
                    throw new System.Exception("this test must be transformed in unit test");
#endif

            this._Groups.Add(group);
            this._GroupsHashSet.Add(group);
        }

        internal bool Contains(ExclusiveGroupStruct group)
        {
            return this._GroupsHashSet.Contains(group);
        }

        private void PopulateExistingGroups()
        {
            // Since GroupCompunds are no longer hotloaded statically we need to ensure 
            // all existing groups get defined (and add the new group to all existing definitions)
            // This involves adding the new group into lower GroupCompounds
            // and adding existing groups from higher GroupCompounds

            foreach (var (id, existingGroupCompound) in _GroupCompounds)
            { // Iterate through all existing GroupCompounds
                if (existingGroupCompound == this)
                { // Ignore self
                    continue;
                }

                if (this.ContainsGroupTagTypes(existingGroupCompound._GroupTagTypes) == true)
                { // If all GroupTags within the existingGroupCompound exist in the current groupCompound...
                  // This selects GroupCompound<XYZ> when creating GroupCompound<ABC, XYZ> but not the other way around
                  // We want to add the new group into the existing groupCompound
                    existingGroupCompound.Add(this.BuildGroup);
                }

                if (existingGroupCompound.ContainsGroupTagTypes(this._GroupTagTypes) == true)
                { // If all GroupTags within the current groupCompount exist in the existingGroupCompound...
                  // This selects GroupCompound<ABC, XYZ> when creating GroupCompound<XYZ> but not the other way around
                  // We want to add the existing group into the current groupCompound
                    this.Add(existingGroupCompound.BuildGroup);
                }
            }
        }

        private bool ContainsGroupTagTypes(params Type[] groupTagTypes)
        {
            if (groupTagTypes.Length > this._GroupTagTypes.Length)
            {
                return false;
            }

            foreach (Type groupTagType in groupTagTypes)
            {
                if (this._GroupTagTypes.Contains(groupTagType) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private static readonly ConcurrentDictionary<int, GroupCompound> _GroupCompounds = new ConcurrentDictionary<int, GroupCompound>();
        internal static GroupCompound GetOrCreateGroupCompoundByGroupTagTypes(Type[] groupTagTypes, ExclusiveGroupBitmask buildGroupBitmask)
        {
            // Sort the tag types
            Type[] groupTagTypesSorted = groupTagTypes.OrderBy(x => x.AssemblyQualifiedName).ThenBy(x => x.GetHashCode()).ToArray();

            // Generate unique id from sorted types
            int id = 0;
            for (var i = 0; i < groupTagTypesSorted.Length; i++)
            {
                id = HashCode.Combine(groupTagTypesSorted[i], id);
            }

            if (_GroupCompounds.TryGetValue(id, out GroupCompound groupCompound) == true)
            { // An instance already exists
                return groupCompound;
            }

            // Need to create and store a new instance
            string name = groupTagTypesSorted.Select((x, _) => x.FullName).Aggregate((s1, s2) => $"{s1}-{s2}");
            groupCompound = new GroupCompound(id, name, groupTagTypesSorted, buildGroupBitmask);
            _GroupCompounds.TryAdd(id, groupCompound);

            return groupCompound;
        }
    }

    public abstract class GroupCompound<G1, G2, G3, G4> : ITouchedByReflection
            where G1 : GroupTag<G1>
            where G2 : GroupTag<G2>
            where G3 : GroupTag<G3>
            where G4 : GroupTag<G4>
    {
        private static readonly GroupCompound _GroupCompoundInstance = GroupCompound.GetOrCreateGroupCompoundByGroupTagTypes(
            groupTagTypes: new[] { typeof(G1), typeof(G2), typeof(G3), typeof(G4) },
            buildGroupBitmask: GroupTag<G1>.bitmask | GroupTag<G2>.bitmask | GroupTag<G3>.bitmask | GroupTag<G4>.bitmask);

        public static FasterReadOnlyList<ExclusiveGroupStruct> Groups
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.Groups;
        }

        public static ExclusiveBuildGroup BuildGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.BuildGroup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Includes(ExclusiveGroupStruct group)
        {
            DBC.ECS.Check.Require(group != ExclusiveGroupStruct.Invalid, "invalid group passed");

            return _GroupCompoundInstance.Contains(group);
        }
    }

    public abstract class GroupCompound<G1, G2, G3> : ITouchedByReflection
            where G1 : GroupTag<G1>
            where G2 : GroupTag<G2>
            where G3 : GroupTag<G3>
    {
        private static readonly GroupCompound _GroupCompoundInstance = GroupCompound.GetOrCreateGroupCompoundByGroupTagTypes(
            groupTagTypes: new[] { typeof(G1), typeof(G2), typeof(G3) },
            buildGroupBitmask: GroupTag<G1>.bitmask | GroupTag<G2>.bitmask | GroupTag<G3>.bitmask);

        public static FasterReadOnlyList<ExclusiveGroupStruct> Groups
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.Groups;
        }

        public static ExclusiveBuildGroup BuildGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.BuildGroup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Includes(ExclusiveGroupStruct group)
        {
            DBC.ECS.Check.Require(group != ExclusiveGroupStruct.Invalid, "invalid group passed");

            return _GroupCompoundInstance.Contains(group);
        }
    }

    public abstract class GroupCompound<G1, G2> : ITouchedByReflection
        where G1 : GroupTag<G1>
        where G2 : GroupTag<G2>
    {
        private static readonly GroupCompound _GroupCompoundInstance = GroupCompound.GetOrCreateGroupCompoundByGroupTagTypes(
            groupTagTypes: new[] { typeof(G1), typeof(G2) },
            buildGroupBitmask: GroupTag<G1>.bitmask | GroupTag<G2>.bitmask);

        public static FasterReadOnlyList<ExclusiveGroupStruct> Groups
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.Groups;
        }

        public static ExclusiveBuildGroup BuildGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.BuildGroup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Includes(ExclusiveGroupStruct group)
        {
            DBC.ECS.Check.Require(group != ExclusiveGroupStruct.Invalid, "invalid group passed");

            return _GroupCompoundInstance.Contains(group);
        }
    }

    /// <summary>
    /// GroupTags are just GroupCompounds with a single type associated
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GroupTag<T> : ITouchedByReflection
        where T : GroupTag<T>
    {
        private static readonly GroupCompound _GroupCompoundInstance = GroupCompound.GetOrCreateGroupCompoundByGroupTagTypes(
            groupTagTypes: new[] { typeof(T) },
            buildGroupBitmask: bitmask);

        public static FasterReadOnlyList<ExclusiveGroupStruct> Groups
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.Groups;
        }

        public static ExclusiveBuildGroup BuildGroup
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _GroupCompoundInstance.BuildGroup;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Includes(ExclusiveGroupStruct group)
        {
            DBC.ECS.Check.Require(group != ExclusiveGroupStruct.Invalid, "invalid group passed");

            return _GroupCompoundInstance.Contains(group);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Offset(ExclusiveGroupStruct group)
        {
            return BuildGroup.Offset(group);
        }

        //special group attributes, at the moment of writing this comment, only the disabled group has a special attribute

        //Allow to call GroupTag static constructors like
        //                public class Dead: GroupTag<Dead>
        //                {
        //                    static Dead()
        //                    {
        //                        bitmask = ExclusiveGroupBitmask.DISABLED_BIT;
        //                    }
        //                };

        protected internal static ExclusiveGroupBitmask bitmask;
        //set a number different than 0 to create a range of groups instead of a single group
        //example of usage:
        //        public class VehicleGroup:GroupTag<VehicleGroup>
        //        {
        //            static VehicleGroup()
        //            {
        //                range = (ushort)Data.MaxTeamCount;
        //            }
        //        }

        protected internal static ushort range;
    }
}