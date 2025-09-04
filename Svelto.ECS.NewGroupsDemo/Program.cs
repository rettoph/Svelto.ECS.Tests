using Svelto.ECS;
using Svelto.ECS.Core.Groups;
using Svelto.ECS.Schedulers;
using System.Numerics;

var entitiesSubmissionScheduler = new EntitiesSubmissionScheduler();
var enginesRoot = new EnginesRoot(entitiesSubmissionScheduler);
var factory = enginesRoot.GenerateEntityFactory();

factory.BuildEntity<PlayerDescriptor, RedTeamPlayerGroup>(entityID: 0);
factory.BuildEntity<WallDescriptor, RedTeamPlayerGroup>(entityID: 0); // Invalid group!

foreach (var group in RedTeamHealthGroupsQuery.Groups)
{
    // Do something with confidence this group contains entities having a health component
    // on the red team
}

// BEGIN COMPONENTS
internal struct Health : IEntityComponent
{
    private int Value { get; set; }
}

internal struct Position : IEntityComponent
{
    public Vector2 Value { get; set; }
}
// END COMPONENTS

// BEGIN DESCRIPTORS
internal class PlayerDescriptor : IEntityDescriptor
{
    private static readonly IComponentBuilder[] _componentsToBuild = [new ComponentBuilder<Health>(), new ComponentBuilder<Position>()];
    public IComponentBuilder[] componentsToBuild => _componentsToBuild;
}
internal class MonsterDescriptor : IEntityDescriptor
{
    private static readonly IComponentBuilder[] _componentsToBuild = [new ComponentBuilder<Health>(), new ComponentBuilder<Position>()];
    public IComponentBuilder[] componentsToBuild => _componentsToBuild;
}

internal class WallDescriptor : IEntityDescriptor
{
    private static readonly IComponentBuilder[] _componentsToBuild = [new ComponentBuilder<Position>()];
    public IComponentBuilder[] componentsToBuild => _componentsToBuild;
}
// END DESCRIPTORS


// BEGIN GROUPTAGS
internal class RedTeam : GroupTag<RedTeam> { }
internal class BlueTeam : GroupTag<BlueTeam> { }
// END GROUP TAGS

// BEGIN GROUPS
internal class RedTeamPlayerGroup : EntityGroup.WithDescriptor<PlayerDescriptor>.WithTags<RedTeam> { }
internal class RedTeamMonsterGroup : EntityGroup.WithDescriptor<MonsterDescriptor>.WithTags<RedTeam> { }
internal class RedTeamWallGroup : EntityGroup.WithDescriptor<WallDescriptor>.WithTags<RedTeam> { }
internal class BlueTeamPlayerGroup : EntityGroup.WithDescriptor<PlayerDescriptor>.WithTags<BlueTeam> { }
internal class BlueTeamMonsterGroup : EntityGroup.WithDescriptor<MonsterDescriptor>.WithTags<BlueTeam> { }
internal class BlueTeamWallGroup : EntityGroup.WithDescriptor<WallDescriptor>.WithTags<BlueTeam> { }
// END GROUPS

// BEGIN QUERIES
internal class RedTeamHealthGroupsQuery : EntityGroupQuery.WithTags<RedTeam>.WithComponents<Health> { }
// END QUERIES