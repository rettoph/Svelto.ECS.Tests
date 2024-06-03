using NUnit.Framework;
using Svelto.DataStructures;
using Svelto.ECS.Schedulers;

namespace Svelto.ECS.Tests.ECS
{
    public class GenericTestsBaseClass
    {
        [SetUp]
        public void Init()
        {
            _scheduler   = new SimpleEntitiesSubmissionScheduler();
            _enginesRoot = new EnginesRoot(_scheduler);
            _factory     = _enginesRoot.GenerateEntityFactory();
            _functions   = _enginesRoot.GenerateEntityFunctions();
            _entitiesDB  = _enginesRoot;
        }

        [TearDown]
        public void Cleanup()
        {
            
        }

        protected EntityInitializer CreateTestEntity(uint entityId, ExclusiveGroupStruct group, int value = 1)
        {
            var initializer = _factory.BuildEntity<EntityDescriptorWithComponentAndViewComponent>
                (entityId, group, new object[] {new TestFloatValue(value), new TestIntValue(value)});
            initializer.Init(new TestEntityComponent(value, value));
            return initializer;
        }

        protected SimpleEntitiesSubmissionScheduler _scheduler;
        protected EnginesRoot                                 _enginesRoot;
        protected IEntityFactory                    _factory;
        protected IEntityFunctions                  _functions;
        protected IUnitTestingInterface             _entitiesDB;

        protected static readonly FasterList<ExclusiveGroupStruct> GroupAB = new FasterList<ExclusiveGroupStruct>().Add(Groups.GroupA).Add(Groups.GroupB);
    }
}