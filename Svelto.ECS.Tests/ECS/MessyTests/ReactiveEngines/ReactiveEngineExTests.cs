using NUnit.Framework;
using System.Collections.Generic;

namespace Svelto.ECS.Tests.ECS
{
    [TestFixture]
    public class ReactiveEngineExTests : GenericTestsBaseClass
    {
        [TestCase]
        public void Test_ReactiveEngineEx_AddCallback()
        {
            var engine = new ReactOnAddExEngine();

            _enginesRoot.AddEngine(engine);

            for (uint i = 0; i < 10; i++)
                CreateTestEntity(i, Groups.GroupA, (int) i);

            for (uint i = 0; i < 5; i++)
                CreateTestEntity(i, Groups.GroupB, (int) i);

            _scheduler.SubmitEntities();

            Assert.AreEqual(15, engine.addedCount);
        }

        [TestCase]
        public void Test_ReactiveEngineEx_MoveCallback()
        {
            var engine = new ReactOnMoveExEngine();

            _enginesRoot.AddEngine(engine);

            for (uint i = 0; i < 10; i++)
                CreateTestEntity(i, Groups.GroupA, (int) i);

            for (uint i = 10; i < 20; i++)
                CreateTestEntity(i, Groups.GroupB, (int) i);

            _scheduler.SubmitEntities();

            for (uint i = 0; i < 5; ++i)
            {
                _functions.SwapEntityGroup<EntityDescriptorWithComponentAndViewComponent>(
                    i, Groups.GroupA, Groups.GroupB);
            }

            for (uint i = 10; i < 12; ++i)
            {
                _functions.SwapEntityGroup<EntityDescriptorWithComponentAndViewComponent>(
                    i, Groups.GroupB, Groups.GroupA);
            }

            _scheduler.SubmitEntities();

            Assert.AreEqual(7, engine.movedCount);
        }

        [TestCase]
        public void Test_ReactiveEngineEx_RemoveCallback()
        {
            var engine = new ReactOnRemoveExEngine();

            _enginesRoot.AddEngine(engine);

            for (uint i = 0; i < 10; i++)
                CreateTestEntity(i, Groups.GroupA, (int) i);

            for (uint i = 10; i < 20; i++)
                CreateTestEntity(i, Groups.GroupB, (int) i);

            _scheduler.SubmitEntities();

            for (uint i = 0; i < 5; ++i)
            {
                _functions.RemoveEntity<EntityDescriptorWithComponentAndViewComponent>(
                    i, Groups.GroupA);
            }

            for (uint i = 10; i < 12; ++i)
            {
                _functions.RemoveEntity<EntityDescriptorWithComponentAndViewComponent>(
                    i, Groups.GroupB);
            }

            _scheduler.SubmitEntities();

            Assert.AreEqual(7, engine.removedCount);
        }

        [TestCase]
        public void Test_ReactiveEngineEx_DisposeCallback()
        {
            var engine = new ReactOnDisposeExEngine();

            _enginesRoot.AddEngine(engine);

            for (uint i = 0; i < 10; i++)
                CreateTestEntity(i, Groups.GroupA, (int) i);

            for (uint i = 10; i < 20; i++)
                CreateTestEntity(i, Groups.GroupB, (int) i);

            _scheduler.SubmitEntities();

            _enginesRoot.Dispose();

            Assert.AreEqual(20, engine.removedCount);
        }


        [TestCase]
        public void Test_ReactiveEngineEx_RemoveCallback_CheckEntityIDs()
        {
            var engine = new ReactOnRemoveEx_CheckEntityIDs_Engine();

            _enginesRoot.AddEngine(engine);

            //create 10 entitites in groupA
            for (uint i = 0; i < 10; i++)
                CreateTestEntity(i, Groups.GroupA);

            //create 10 entities in groupB
            for (uint i = 10; i < 20; i++)
                CreateTestEntity(i, Groups.GroupB);

            _scheduler.SubmitEntities();

            //remove the first 5 entities from group A
            for (uint i = 0; i < 5; ++i)
            {
                _functions.RemoveEntity<EntityDescriptorWithComponentAndViewComponent>(
                    i, Groups.GroupA);
            }

            //remove the first 2 entities from group B
            for (uint i = 10; i < 12; ++i)
            {
                _functions.RemoveEntity<EntityDescriptorWithComponentAndViewComponent>(
                    i, Groups.GroupB);
            }

            _scheduler.SubmitEntities();

            //the IDs removed from groupA
            Assert.Contains(0, engine.removedEntityIDs);
            Assert.Contains(1, engine.removedEntityIDs);
            Assert.Contains(2, engine.removedEntityIDs);
            Assert.Contains(3, engine.removedEntityIDs);
            Assert.Contains(4, engine.removedEntityIDs);
            
            //the IDs removed from groupB
            Assert.Contains(10, engine.removedEntityIDs);
            Assert.Contains(11, engine.removedEntityIDs);
        }

        public class ReactOnAddExEngine : IReactOnAddEx<TestEntityComponent>
        {
            public uint addedCount = 0;

            public void Add((uint start, uint end) rangeOfEntities, in EntityCollection<TestEntityComponent> collection, ExclusiveGroupStruct groupID)
            {
                addedCount += rangeOfEntities.end - rangeOfEntities.start;
            }
        }

        public class ReactOnMoveExEngine : IReactOnSwapEx<TestEntityComponent>
        {
            public uint movedCount = 0;

            public void MovedTo((uint start, uint end) rangeOfEntities, in EntityCollection<TestEntityComponent> collection, ExclusiveGroupStruct fromGroup, ExclusiveGroupStruct toGroup)
            {
                movedCount += rangeOfEntities.end - rangeOfEntities.start;
            }
        }

        public class ReactOnRemoveExEngine : IReactOnRemoveEx<TestEntityComponent>
        {
            public uint removedCount = 0;

            public void Remove((uint start, uint end) rangeOfEntities, in EntityCollection<TestEntityComponent> collection, ExclusiveGroupStruct groupID)
            {
                removedCount += rangeOfEntities.end - rangeOfEntities.start;
            }
        }

        public class ReactOnDisposeExEngine : IReactOnDisposeEx<TestEntityComponent>
        {
            public uint removedCount = 0;

            public void Remove((uint start, uint end) rangeOfEntities, in EntityCollection<TestEntityComponent> collection, ExclusiveGroupStruct groupID)
            {
                removedCount += rangeOfEntities.end - rangeOfEntities.start;
            }
        }

        public class ReactOnRemoveEx_CheckEntityIDs_Engine : IReactOnRemoveEx<TestEntityComponent>
        {
            public List<uint> removedEntityIDs = new List<uint>();

            public void Remove((uint start, uint end) rangeOfEntities, in EntityCollection<TestEntityComponent> collection, ExclusiveGroupStruct groupID)
            {
                var (_, entityIDs, _) = collection;
                for (uint index = rangeOfEntities.start; index < rangeOfEntities.end; index++)
                {
                    removedEntityIDs.Add(entityIDs[index]);
                }
            }
        }
    }
}
