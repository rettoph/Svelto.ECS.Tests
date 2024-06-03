﻿using System;
using System.ComponentModel;
using DBC.ECS;
using NUnit.Framework;
using Svelto.DataStructures;
using Svelto.ECS.Hybrid;
using Svelto.ECS.Internal;
using Svelto.ECS.Schedulers;
using Assert = NUnit.Framework.Assert;

namespace Svelto.ECS.Tests.Messy
{
    public static class Groups
    {
        public static readonly ExclusiveGroup group1 = new ExclusiveGroup();
        public static readonly ExclusiveGroup group2 = new ExclusiveGroup();
        public static readonly ExclusiveGroup group3 = new ExclusiveGroup();
        public static readonly ExclusiveGroup group0 = new ExclusiveGroup();
        public static readonly ExclusiveGroup groupR4 = new ExclusiveGroup(4);
    }

    [TestFixture]
    public partial class TestSveltoECS
    {
        [SetUp]
        public void Init()
        {
            _simpleSubmissionEntityViewScheduler = new SimpleEntitiesSubmissionScheduler();
            _enginesRoot                         = new EnginesRoot(_simpleSubmissionEntityViewScheduler);
            _mockEngine         = new TestEngine();

            _enginesRoot.AddEngine(_mockEngine);

            _entityFactory   = _enginesRoot.GenerateEntityFactory();
            _entityFunctions = _enginesRoot.GenerateEntityFunctions();
        }

        [TearDown]
        public void Dispose()
        {
            _enginesRoot.Dispose();
        }

        [TestCase]
        public void TestBuildEntityViewComponentWithoutImplementors()
        {
            void CheckFunction()
            {
                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(1, Groups.group1));
                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            }

            Assert.Throws(typeof(PreconditionException), CheckFunction);
        }

        [TestCase]
        public void TestBuildEntityViewComponentWithWrongImplementors()
        {
            void CheckFunction()
            {
                _entityFactory.BuildEntity<TestDescriptorWrongEntityViewInterface>(new EGID(1, Groups.group1),
                    new[] { new TestIt(2) });
                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            }

            try
            {
                CheckFunction();
            }
            catch
            {
                Assert.Pass();
                return;
            }
            
            Assert.Fail();
        }

        [TestCase]
        public void TestWrongEntityViewComponent()
        {
            void CheckFunction()
            {
                _entityFactory.BuildEntity<TestDescriptorWrongEntityView>(new EGID(1, Groups.group1), new[] { new TestIt(2) });
                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            }

            Assert.Throws<TypeInitializationException>(
                CheckFunction); //it's TypeInitializationException because the Type is not being constructed due to the ECSException
        }

        [TestCase]
        public void TestWrongEntityViewComponent2()
        {
            void CheckFunction()
            {
                _entityFactory.BuildEntity<TestDescriptorWrongEntityView>(new EGID(1, Groups.group1), new[] { new TestIt(2) });
                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            }

            Assert.Throws<TypeInitializationException>(CheckFunction);
        }

        [TestCase]
        public void TestRemoveMultipleTimesOnTheSameEntity()
        {
            //must test swap and remove on the same frame, remove will supersede swap but not fail
          //  Assert.Fail();
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestExceptionTwoEntitiesCannotHaveTheSameIDInTheSameGroupInterleaved(uint id)
        {
            void CheckFunction()
            {
                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
                _simpleSubmissionEntityViewScheduler.SubmitEntities();

                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            }

            Assert.Throws(typeof(ECSException), CheckFunction);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestCreationAndRemovalOfDynamicEntityDescriptors(uint id)
        {
            var ded = new DynamicEntityDescriptor<TestDescriptorEntityView>(new IComponentBuilder[]
            {
                new ComponentBuilder<TestEntityComponent>()
            });

            bool hasit;
            //Build Entity id, group0
            {
                _entityFactory.BuildEntity(new EGID(id, Groups.group0), ded, new[] { new TestIt(2) });

                _simpleSubmissionEntityViewScheduler.SubmitEntities();

                hasit = _mockEngine.HasEntity<TestEntityComponent>(new EGID(id, Groups.group0));

                Assert.IsTrue(hasit);
            }

            //Swap Entity id, group0 to group 3
            {
                _entityFunctions.SwapEntityGroup<TestDescriptorEntityView>(new EGID(id, Groups.group0), Groups.group3);

                _simpleSubmissionEntityViewScheduler.SubmitEntities();

                hasit = _mockEngine.HasEntity<TestEntityComponent>(new EGID(id, Groups.group3));

                Assert.IsTrue(hasit);
            }

            _entityFunctions.RemoveEntity<TestDescriptorEntityView>(new EGID(id, Groups.group3));

            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            hasit = _mockEngine.HasEntity<TestEntityComponent>(new EGID(id, Groups.group3));

            Assert.IsFalse(hasit);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestExceptionTwoDifferentEntitiesCannotHaveTheSameIDInTheSameGroupInterleaved(uint id)
        {
            void CheckFunction()
            {
                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group0), new[] { new TestIt(2) });

                _simpleSubmissionEntityViewScheduler.SubmitEntities();

                _entityFactory.BuildEntity<TestDescriptorEntityView2>(new EGID(id, Groups.group0), new[] { new TestIt(2) });

                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            }

            Assert.That(CheckFunction, Throws.TypeOf<ECSException>());
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestExceptionTwoDifferentEntitiesCannotHaveTheSameIDInTheSameGroup(uint id)
        {
            bool crashed = false;

            try
            {
                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group0), new[] { new TestIt(2) });
                _entityFactory.BuildEntity<TestDescriptorEntityView2>(new EGID(id, Groups.group0), new[] { new TestIt(2) });
            }
            catch
            {
                crashed = true;
            }

            Assert.IsTrue(crashed);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestExceptionTwoEntitiesCannotHaveTheSameIDInTheSameGroup(uint id)
        {
            bool crashed = false;

            try
            {
                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group0), new[] { new TestIt(2) });
                _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group0), new[] { new TestIt(2) });
            }
            catch
            {
                crashed = true;
            }

            Assert.IsTrue(crashed);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestTwoEntitiesWithSameIDWorksOnDifferentGroups(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group0), new[] { new TestIt(2) });
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group0)));
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestRemove(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            _entityFunctions.RemoveEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1));
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsFalse(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntity(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityWithImplementor(uint id)
        {
            _entityFactory.BuildEntity<TestEntityWithComponentViewAndComponent>(new EGID(id, Groups.group1),
                new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));

            var entityView =
                _mockEngine.entitiesDB.QueryEntity<TestEntityViewComponent>(new EGID(id, Groups.group1));
            Assert.AreEqual(entityView.TestIt.value, 2);

            uint index;
            Assert.AreEqual(
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityViewComponent>(
                    new EGID(id, Groups.group1), out index)[index].TestIt.value, 2);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityViewComponent(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntitytruct(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntity>(new EGID(id, Groups.group1));
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityComponentWithInitializer(uint id)
        {
            var init = _entityFactory.BuildEntity<TestDescriptorEntity>(new EGID(id, Groups.group1));
            init.Init(new TestEntityComponent(3));
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityComponent>(Groups.group1));
            uint index;
            Assert.IsTrue(
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityComponent>(new EGID(id, Groups.group1),
                    out index)[index].value == 3);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityMixed(uint id)
        {
            TestIt testIt = new TestIt(2);
            _entityFactory.BuildEntity<TestEntityWithComponentViewAndComponent>(new EGID(id, Groups.group1), new[] { testIt });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityComponent>(Groups.group1));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
            var (entityCollection, count) =
                _mockEngine.entitiesDB.QueryEntities<TestEntityViewComponent>(Groups.group1);
            Assert.AreSame(entityCollection[0].TestIt, testIt);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityWithViewStructWithImplementorAndTestQueryEntitiesAndIndex(uint id)
        {
            var testIt = new TestIt(2);
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { testIt });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));

            uint index;
            var testEntityView2 =
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityViewComponent>(
                    new EGID(id, Groups.group1), out index)[index];

            Assert.AreEqual(testEntityView2.TestIt, testIt);
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityToGroupWithDescriptorInfo(uint id)
        {
            _entityFactory.BuildEntity(new EGID(id, Groups.group1), new TestDescriptorEntityView(), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestBuildEntityInAddFunction(uint id)
        {
            _enginesRoot.AddEngine(new TestEngineAdd(_entityFactory));
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities(); //submit the entities
            _simpleSubmissionEntityViewScheduler.SubmitEntities(); //now submit the entities added by the engines
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityComponent>(new EGID(100, Groups.group0)));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestRemoveFromGroup(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));

            _entityFunctions.RemoveEntity<TestDescriptorEntityView>(id, Groups.group1);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsFalse(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsFalse(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestRemoveGroup(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));

            _entityFunctions.RemoveEntitiesFromGroup(Groups.group1);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsFalse(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsFalse(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestRemoveAndAddAgainEntity(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            _entityFunctions.RemoveEntity<TestDescriptorEntityView>(id, Groups.group1);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group1), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group1)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group1));
        }

        [TestCase((uint)0)]
        [TestCase((uint)1)]
        [TestCase((uint)2)]
        public void TestSwapGroup(uint id)
        {
            _entityFactory.BuildEntity<TestDescriptorEntityView>(new EGID(id, Groups.group0), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            _entityFunctions.SwapEntityGroup<TestDescriptorEntityView>(id, Groups.group0, Groups.group3);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsFalse(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group0)));
            Assert.IsFalse(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group0));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group3));
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group3)));
#if SLOW_SVELTO_SUBMISSION
            Assert.AreEqual(
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityViewComponent>(
                    new EGID(id, Groups.group3), out var index)[index].ID.entityID, id);
            Assert.AreEqual(
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityViewComponent>(
                    new EGID(id, Groups.group3), out index)[index].ID.groupID.id, (Groups.group3.id));
#endif

            _entityFunctions.SwapEntityGroup<TestDescriptorEntityView>(id, Groups.group3, Groups.group0);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group0)));
            Assert.IsTrue(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group0));
            Assert.IsFalse(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group3));
            Assert.IsFalse(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(id, Groups.group3)));
#if SLOW_SVELTO_SUBMISSION
            Assert.AreEqual(
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityViewComponent>(
                    new EGID(id, Groups.group0), out index)[index].ID.entityID, id);
            Assert.AreEqual(
                _mockEngine.entitiesDB.QueryEntitiesAndIndex<TestEntityViewComponent>(
                    new EGID(id, Groups.group0), out index)[index].ID.groupID.id, Groups.group0.id);
#endif                    
        }

        [TestCase((uint)0, (uint)1, (uint)2, (uint)3)]
        [TestCase((uint)4, (uint)5, (uint)6, (uint)7)]
        [TestCase((uint)8, (uint)9, (uint)10, (uint)11)]
        public void TestExecuteOnAllTheEntities(uint id, uint id2, uint id3, uint id4)
        {
            _entityFactory.BuildEntity<TestEntityWithComponentViewAndComponent>(new EGID(id, Groups.groupR4),
                new[] { new TestIt(2) });
            _entityFactory.BuildEntity<TestEntityWithComponentViewAndComponent>(new EGID(id2, Groups.groupR4 + 1),
                new[] { new TestIt(2) });
            _entityFactory.BuildEntity<TestEntityWithComponentViewAndComponent>(new EGID(id3, Groups.groupR4 + 2),
                new[] { new TestIt(2) });
            _entityFactory.BuildEntity<TestEntityWithComponentViewAndComponent>(new EGID(id4, Groups.groupR4 + 3),
                new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            AllGroupsEnumerable<TestEntityViewComponent> allGroupsEnumerable = _mockEngine.entitiesDB.QueryEntities<TestEntityViewComponent>();
            foreach (var ((entity, count), group) in allGroupsEnumerable)
            {
                for (int i = 0; i < count; i++)
                    entity[i].TestIt.value = entity[i].ID.entityID;
            }

            foreach (var ((entity, groupCount), _) in _mockEngine.entitiesDB
                        .QueryEntities<TestEntityComponent>())
            {
                for (int i = 0; i < groupCount; i++)
                    entity[i].value = entity[i].ID.entityID;
            }

            for (uint i = 0; i < 4; i++)
            {
                var (buffer1, count) =
                    _mockEngine.entitiesDB.QueryEntities<TestEntityComponent>(Groups.groupR4 + i);
                var (buffer2, count2) =
                    _mockEngine.entitiesDB.QueryEntities<TestEntityViewComponent>(Groups.groupR4 + i);

                Assert.AreEqual(count, 1);
                Assert.AreEqual(count2, 1);

                for (int j = 0; j < count; j++)
                {
                    Assert.AreEqual(buffer1[j].value, buffer1[j].ID.entityID);
                    Assert.AreEqual(buffer2[j].TestIt.value, buffer2[j].ID.entityID);
                }
            }

            _entityFunctions.RemoveEntity<TestEntityWithComponentViewAndComponent>(new EGID(id, Groups.groupR4));
            _entityFunctions.RemoveEntity<TestEntityWithComponentViewAndComponent>(new EGID(id2, Groups.groupR4 + 1));
            _entityFunctions.RemoveEntity<TestEntityWithComponentViewAndComponent>(new EGID(id3, Groups.groupR4 + 2));
            _entityFunctions.RemoveEntity<TestEntityWithComponentViewAndComponent>(new EGID(id4, Groups.groupR4 + 3));
            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            foreach (var (_, _) in allGroupsEnumerable)
            {
                Assert.Fail();
            }

            foreach (var (_, _) in _mockEngine.entitiesDB.QueryEntities<TestEntityComponent>())
            {
                Assert.Fail();
            }
        }

        [TestCase]
        public void QueryingNotExistingViewsInAnExistingGroupMustNotCrash()
        {
            Assert.IsFalse(_mockEngine.HasAnyEntityInGroup<TestEntityViewComponent>(Groups.group0));
            Assert.IsFalse(_mockEngine.HasAnyEntityInGroupArray<TestEntityViewComponent>(Groups.group0));
        }

        [TestCase]
        public void TestExtendibleDescriptor()
        {
            _entityFactory.BuildEntity<B>(new EGID(1, Groups.group0));
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            _entityFunctions.SwapEntityGroup<A>(new EGID(1, Groups.group0), Groups.group1);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            Assert.IsFalse(_mockEngine.HasEntity<EVS2>(new EGID(1, Groups.group0)));
            Assert.IsTrue(_mockEngine.HasEntity<EVS2>(new EGID(1, Groups.group1)));
            Assert.IsFalse(_mockEngine.HasEntity<EVS1>(new EGID(1, Groups.group0)));
            Assert.IsTrue(_mockEngine.HasEntity<EVS1>(new EGID(1, Groups.group1)));
        }

        [TestCase]
        public void TestExtendibleDescriptor2()
        {
            _entityFactory.BuildEntity<B2>(new EGID(1, Groups.group0), new[] { new TestIt(2) });
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            _entityFunctions.SwapEntityGroup<A2>(new EGID(1, Groups.group0), Groups.group1);
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            Assert.IsFalse(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(1, Groups.group0)));
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityViewComponent>(new EGID(1, Groups.group1)));
            Assert.IsFalse(_mockEngine.HasEntity<TestEntityComponent>(new EGID(1, Groups.group0)));
            Assert.IsTrue(_mockEngine.HasEntity<TestEntityComponent>(new EGID(1, Groups.group1)));
        }

        [TestCase]
        public void TestQueryEntitiesWithMultipleParamsTwoStructs()
        {
            for (int i = 0; i < 100; i++)
            {
                var init = _entityFactory.BuildEntity<TestDescriptorWith2Components>(new EGID((uint)i, Groups.group0));
                init.Init(new TestEntityComponent((uint)(i)));
                init.Init(new TestEntityComponent2((uint)(i + 100)));
            }

            for (int i = 0; i < 100; i++)
            {
                var init = _entityFactory.BuildEntity<TestDescriptorWith2Components>(new EGID((uint)i, Groups.group1));
                init.Init(new TestEntityComponent((uint)(i + 200)));
                init.Init(new TestEntityComponent2((uint)(i + 300)));
            }

            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            uint index = 0;

            var groupsEnumerable = _mockEngine.entitiesDB.QueryEntities<TestEntityComponent, TestEntityComponent2>
                    (new FasterList<ExclusiveGroupStruct>(Groups.group0, Groups.group1));
            
            foreach (var ((iteratorentityComponentA, iteratorentityComponentB, count), exclusiveGroupStruct) in
                     groupsEnumerable)
            {
                for (int i = 0; i < count; i++)
                {
                    if (exclusiveGroupStruct == Groups.group0)
                    {
                        Assert.AreEqual(iteratorentityComponentA[i].value, index);
                        Assert.AreEqual(iteratorentityComponentB[i].value, index + 100);
                    }
                    else
                    {
                        Assert.AreEqual(iteratorentityComponentA[i].value, index + 200);
                        Assert.AreEqual(iteratorentityComponentB[i].value, index + 300);
                    }

                    index = ++index % 100;
                }
            }
        }

        [TestCase]
        public void TestQueryEntitiesWithMultipleParamsOneStruct()
        {
            for (int i = 0; i < 100; i++)
            {
                var init = _entityFactory.BuildEntity<TestDescriptorWith2Components>(new EGID((uint)i, Groups.group0));
                init.Init(new TestEntityComponent((uint)(i)));
                init.Init(new TestEntityComponent2((uint)(i + 100)));
            }

            for (int i = 0; i < 100; i++)
            {
                var init = _entityFactory.BuildEntity<TestDescriptorWith2Components>(new EGID((uint)i, Groups.group1));
                init.Init(new TestEntityComponent((uint)(i + 200)));
                init.Init(new TestEntityComponent2((uint)(i + 300)));
            }

            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            FasterList<ExclusiveGroupStruct> groupStructId =
                new FasterList<ExclusiveGroupStruct>(new ExclusiveGroupStruct[] { Groups.group0, Groups.group1 });
            var iterators = _mockEngine.entitiesDB.QueryEntities<TestEntityComponent>(groupStructId);

            uint index = 0;

            foreach (var ((iterator, count), exclusiveGroupStruct) in iterators)
            {
                for (int i = 0; i < count; i++)
                {
                    if (exclusiveGroupStruct == Groups.group0)
                        Assert.IsTrue(iterator[i].value == index);
                    else
                        Assert.That(iterator[i].value, Is.EqualTo(index + 200));

                    index = ++index % 100;
                }
            }
        }
        
//        [Test]
//        public void EntityCollectionBenchmark()
//        {
//            Assert.DoesNotThrow(
//                () =>
//                {
//                    var simpleEntitiesSubmissionScheduler = new SimpleEntitiesSubmissionScheduler();
//                    var _enginesroot = new EnginesRoot(simpleEntitiesSubmissionScheduler);
//                    var factory = _enginesroot.GenerateEntityFactory();
//
//                    for (uint i = 0 ; i < 1_000_000; i++)
//                        factory.BuildEntity<TestDescriptorEntity>(new EGID(i, Groups.group1));
//
//                    simpleEntitiesSubmissionScheduler.SubmitEntities();
//                    _enginesroot.Dispose();
//                });
//        }

        [Test]
        public void TestConcreteGenericDescriptor()
        {
            Assert.DoesNotThrow(()
            =>
            {
                _entityFactory.BuildEntity<ConcreteGenericDescriptor>(0, Groups.group1);
                
                _simpleSubmissionEntityViewScheduler.SubmitEntities();
            });
        }
        
        [Test]
        public void TestDynamicEntityDescriptor()
        {
            var testBuildOnSwapEngine = new TestDynamicDescriptorEngine(_entityFactory, _entityFunctions);
            _enginesRoot.AddEngine(testBuildOnSwapEngine);

            var descriptor = DynamicEntityDescriptor<BaseWidgetDescriptor>.CreateDynamicEntityDescriptor();
            descriptor.Add<GUIWidgetEventsComponent1>();
            
            _entityFactory.BuildEntity(0, Groups.group1, descriptor);

            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            
            _entityFunctions.SwapEntityGroup<BaseWidgetDescriptor>(0, Groups.group1, Groups.group2);
            
            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            
            Assert.That(testBuildOnSwapEngine.workedA, Is.True);
            Assert.That(testBuildOnSwapEngine.workedB, Is.True);
        }

        [Test]
        public void TestEntityBuildInSubmission()
        {
            var testBuildOnSwapEngine = new TestBuildOnSwapEngine(_entityFactory);
            _enginesRoot.AddEngine(testBuildOnSwapEngine);

            var testSwapAfterBuildEngine = new TestSwapAfterBuildEngine(_entityFunctions);
            _enginesRoot.AddEngine(testSwapAfterBuildEngine);

            _entityFactory.BuildEntity<TestDescriptorEntity>(0, Groups.group1);

            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            _entityFunctions.SwapEntityGroup<TestDescriptorEntity>(0, Groups.group1, Groups.group2);

            _simpleSubmissionEntityViewScheduler.SubmitEntities();

            Assert.DoesNotThrow(() => testSwapAfterBuildEngine.Step());
        }
        
        [Test]
        public void PreallocateEntitySpace_ShouldPreallocateCorrectNumberOfEntities_WhenCalledWithValidParameters()
        {
            // Arrange
            var groupStructId = new ExclusiveGroupStruct();

            // Act
            Assert.DoesNotThrow(() => _entityFactory.PreallocateEntitySpace<TestDescriptorEntity>(Groups.group1, 10));
        }
        
        [Test]
        public void PreallocateEntitySpace_ShouldNotPreallocateWhenEntitiesAreAlreadyPresent()
        {
            // Arrange
            var groupStructId = new ExclusiveGroupStruct();
            _entityFactory.BuildEntity<TestDescriptorEntity>(0, Groups.group1);

            _simpleSubmissionEntityViewScheduler.SubmitEntities();
            // Act
            Assert.Throws<ECSException>(() => _entityFactory.PreallocateEntitySpace<TestDescriptorEntity>(Groups.group1, 10));
        }
        
        [Test]
        public void TestRemoveEntityTwice()
        {
            var simpleSubmissionEntityViewScheduler = new SimpleEntitiesSubmissionScheduler();
            var enginesRoot = new EnginesRoot(simpleSubmissionEntityViewScheduler);

            var entityFactory = enginesRoot.GenerateEntityFactory();
            var entityFunctions = enginesRoot.GenerateEntityFunctions();

            var r = entityFactory.BuildEntity<TestDescriptorEntity>(2, Groups.group1);
            simpleSubmissionEntityViewScheduler.SubmitEntities();
            entityFunctions.RemoveEntity<TestDescriptorEntity>(r.EGID);
            entityFunctions.RemoveEntity<TestDescriptorEntity>(r.EGID);
            simpleSubmissionEntityViewScheduler.SubmitEntities();
        }
        
        [Test]
        public void TestRemoveEntityTwiceMustFailIfInvalidGroupFound()
        {
            var simpleSubmissionEntityViewScheduler = new SimpleEntitiesSubmissionScheduler();
            var enginesRoot = new EnginesRoot(simpleSubmissionEntityViewScheduler);

            var entityFactory = enginesRoot.GenerateEntityFactory();
            var entityFunctions = enginesRoot.GenerateEntityFunctions();

            var r = entityFactory.BuildEntity<TestDescriptorEntity>(2, Groups.group1);
            simpleSubmissionEntityViewScheduler.SubmitEntities();
            entityFunctions.RemoveEntity<TestDescriptorEntity>(2, Groups.group1);
            Assert.Throws<ECSException>(() => entityFunctions.RemoveEntity<TestDescriptorEntity>(2, Groups.group2));
        }

        EnginesRoot                       _enginesRoot;
        IEntityFactory                    _entityFactory;
        IEntityFunctions                  _entityFunctions;
        SimpleEntitiesSubmissionScheduler _simpleSubmissionEntityViewScheduler;
        TestEngine                        _mockEngine;

        class TestBuildOnSwapEngine : IReactOnSwapEx<TestEntityComponent>, IQueryingEntitiesEngine
        {
            readonly IEntityFactory _entityFactory;

            public TestBuildOnSwapEngine(IEntityFactory entityFactory)
            {
                _entityFactory = entityFactory;
            }

            public void MovedTo(
                (uint start, uint end) rangeOfEntities,
                in EntityCollection<TestEntityComponent> collection,
                ExclusiveGroupStruct fromGroup,
                ExclusiveGroupStruct toGroup)
            {
                _entityFactory.BuildEntity<TestDescriptorEntity>(1, Groups.group1);
            }

            public void Ready() { }

            public EntitiesDB entitiesDB { get; set; }
        }

        
        class TestDynamicDescriptorEngine : IReactOnSwapEx<GUIWidgetEventsComponent1>, IQueryingEntitiesEngine, IReactOnRemoveEx<GUIWidgetEventsComponent1>, IReactOnSwap<GUIWidgetEventsComponent1>
        {
            public bool workedA;
            public bool workedB;
            
            readonly IEntityFactory _entityFactory;
            readonly IEntityFunctions _functions;

            public TestDynamicDescriptorEngine(IEntityFactory entityFactory, IEntityFunctions functions)
            {
                _entityFactory = entityFactory;
                _functions = functions;
            }

            public void MovedTo(
                (uint start, uint end) rangeOfEntities,
                in EntityCollection<GUIWidgetEventsComponent1> collection,
                ExclusiveGroupStruct fromGroup,
                ExclusiveGroupStruct toGroup)
            {
                for (uint i = rangeOfEntities.start; i < rangeOfEntities.end; i++)
                {
                    _functions.RemoveEntity<BaseWidgetDescriptor>(i, toGroup);
                }
            }

            public void Ready() { }

            public EntitiesDB entitiesDB { get; set; }
            
            public void Remove((uint start, uint end) rangeOfEntities, in EntityCollection<GUIWidgetEventsComponent1> entities, ExclusiveGroupStruct groupID)
            {
                workedB = true;
            }

            public void MovedTo(ref GUIWidgetEventsComponent1 entityComponent, ExclusiveGroupStruct previousGroup, EGID egid)
            {
                workedA = true;
            }
        }

        class TestSwapAfterBuildEngine : IStepEngine, IQueryingEntitiesEngine
        {
            readonly IEntityFunctions _entityFunctions;
            public           string           name => nameof(TestSwapAfterBuildEngine);

            public TestSwapAfterBuildEngine(IEntityFunctions entityFunctions)
            {
                _entityFunctions = entityFunctions;
            }

            public void Step()
            {
                var (_, entityIDs, count) = entitiesDB.QueryEntities<TestEntityComponent>(Groups.group1);

                for (int i = 0; i < count; i++)
                {
                    _entityFunctions.SwapEntityGroup<TestDescriptorEntity>(entityIDs[i], Groups.group1, Groups.group2);
                }
            }

            public EntitiesDB entitiesDB { get; set; }

            public void Ready() { }
        }

        class TestEngineAdd : IReactOnAddAndRemove<TestEntityViewComponent>
        {
            public TestEngineAdd(IEntityFactory entityFactory)
            {
                _entityFactory = entityFactory;
            }

            public void Add(ref TestEntityViewComponent entityView, EGID egid)
            {
                _entityFactory.BuildEntity<TestDescriptorEntity>(new EGID(100, Groups.group0));
            }

            public void Remove(ref TestEntityViewComponent entityView, EGID egid)
            {
                // Svelto.ECS.Tests\Svelto.ECS\DataStructures\TypeSafeDictionary.cs:line 196
                // calls Remove - throwing NotImplementedException here causes test host to
                // crash in Visual Studio or when using "dotnet test" from the command line
                // throw new NotImplementedException();
            }

            readonly IEntityFactory _entityFactory;
        }

        internal class TestEngine : IQueryingEntitiesEngine
        {
            public EntitiesDB entitiesDB { get; set; }

            public void Ready()
            {
            }

            public bool HasEntity<T>(EGID ID) where T : struct, _IInternalEntityComponent
            {
                return entitiesDB.Exists<T>(ID);
            }

            public bool HasAnyEntityInGroup<T>(ExclusiveGroup groupID) where T : struct, _IInternalEntityComponent
            {
                return entitiesDB.QueryEntities<T>(groupID).count > 0;
            }

            public bool HasAnyEntityInGroupArray<T>(ExclusiveGroup groupID) where T : struct, _IInternalEntityComponent
            {
                return entitiesDB.QueryEntities<T>(groupID).count > 0;
            }
        }
    }

    struct EVS1 : IEntityComponent
    {
    }

    struct EVS2 : IEntityComponent
    {
    }

    class A : GenericEntityDescriptor<EVS1>
    {
    }

    class B : ExtendibleEntityDescriptor<A>
    {
        static readonly IComponentBuilder[] _nodesToBuild;

        static B()
        {
            _nodesToBuild = new IComponentBuilder[] { new ComponentBuilder<EVS2>(), };
        }

        public B() : base(_nodesToBuild)
        {
        }
    }

    class A2 : GenericEntityDescriptor<TestEntityViewComponent>
    {
    }

    class B2 : ExtendibleEntityDescriptor<A2>
    {
        static readonly IComponentBuilder[] _nodesToBuild;

        static B2()
        {
            _nodesToBuild = new IComponentBuilder[] { new ComponentBuilder<TestEntityComponent>(), };
        }

        public B2() : base(_nodesToBuild)
        {
        }
    }

    class TestDescriptorEntityView : GenericEntityDescriptor<TestEntityViewComponent>
    {
    }

    class TestDescriptorEntityView2 : GenericEntityDescriptor<TestEntityViewComponent>
    {
    }

    class TestDescriptorEntity : GenericEntityDescriptor<TestEntityComponent>
    {
    }

    class TestEntityWithComponentViewAndComponent : GenericEntityDescriptor<TestEntityViewComponent,
        TestEntityComponent>
    {
    }

    class TestDescriptorWith2Components : GenericEntityDescriptor<TestEntityComponent, TestEntityComponent2>
    {
    }

    class TestDescriptorWrongEntityView : GenericEntityDescriptor<TestWrongComponent>
    {
    }

    class TestDescriptorWrongEntityViewInterface : GenericEntityDescriptor<TestEntityViewComponentWrongInterface>
    {
    }

    struct TestWrongComponent : IEntityViewComponent
    {
        public EGID ID { get; set; }
    }

    struct TestEntityComponent : IEntityComponent
#if SLOW_SVELTO_SUBMISSION            
          , INeedEGID
#endif            
    {
        public uint value;

        public TestEntityComponent(uint value) : this()
        {
            this.value = value;
        }

        public EGID ID { get; set; }
    }

    struct TestEntityComponent2 : IEntityComponent
    {
        public uint value;

        public TestEntityComponent2(uint value) : this()
        {
            this.value = value;
        }
    }

    struct TestEntityViewComponent : IEntityViewComponent
#if SLOW_SVELTO_SUBMISSION            
          , INeedEGID
#endif            
            
    {
#pragma warning disable 649
        public ITestIt TestIt;
#pragma warning restore 649

        public EGID ID { get; set; }
    }

    struct TestEntityViewComponentWrongInterface : IEntityViewComponent
    {
#pragma warning disable 649
        public ITestItWrong TestIt;
#pragma warning restore 649
        public EGID ID { get; set; }
    }

    interface ITestItWrong
    {
    }

    interface ITestIt
    {
        float value { get; set; }
    }

    class TestIt : ITestIt, IImplementor
    {
        public TestIt(int i)
        {
            value = i;
        }

        public float value     { get; set; }
        public int   testValue { get; }
    }
    
    //test building a dynamic entity descriptor inline doesn't fail
    
    //test the warmup of the following doesn't fail
    public interface IGUIEntityDescriptor: IEntityDescriptor { }

    public class GUIExtendibleEntityDescriptor: ExtendibleEntityDescriptor<GUIExtendibleEntityDescriptor.GuiEntityDescriptor>, IGUIEntityDescriptor
    {
        public GUIExtendibleEntityDescriptor(IComponentBuilder[] extraEntities): base(extraEntities) { }

        public class
                GuiEntityDescriptor: GenericEntityDescriptor<EGIDComponent> { }
    }
    
    public class BaseWidgetDescriptor : GUIExtendibleEntityDescriptor
    {
        public BaseWidgetDescriptor() : base(new IComponentBuilder[]
        {
            new ComponentBuilder<GUIWidgetEventsComponent>()
        }) { }
    }

    public struct GUIWidgetEventsComponent: _IInternalEntityComponent { }
    public struct GUIWidgetEventsComponent1: _IInternalEntityComponent { }

    public class GenericDescriptor<T> : IEntityDescriptor
    {
        private IComponentBuilder[] _components;

        protected GenericDescriptor(IComponentBuilder[] components)
        {
            _components = components;
        }

#region Implementation of IEntityDescriptor

        public IComponentBuilder[] componentsToBuild => _components;

#endregion
    }

    public class ConcreteGenericDescriptor : GenericDescriptor<GUIWidgetEventsComponent>
    {
        public ConcreteGenericDescriptor() : base(new IComponentBuilder[]
        {
            new ComponentBuilder<GUIWidgetEventsComponent>()
        }) { }
    }
}