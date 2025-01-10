using NUnit.Framework;
using System;

namespace Unity.Entities.Tests
{
    partial class EntityManagerSafetyTests : ECSTestsFixture
    {
        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Test requires entity data access safety checks")]
        public void RemoveEntityComponentThrows()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            Assert.Throws<ArgumentException>(() => { m_Manager.RemoveComponent(entity, typeof(Entity)); });
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Test requires entity data access safety checks")]
        public void GetSetComponentThrowsIfNotExist()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var destroyedEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.DestroyEntity(destroyedEntity);

            Assert.Throws<System.ArgumentException>(() => { m_Manager.SetComponentData(entity, new EcsTestData2()); });
            Assert.Throws<System.ArgumentException>(() => { m_Manager.SetComponentData(destroyedEntity, new EcsTestData2()); });

            Assert.Throws<System.ArgumentException>(() => { m_Manager.GetComponentData<EcsTestData2>(entity); });
            Assert.Throws<System.ArgumentException>(() => { m_Manager.GetComponentData<EcsTestData2>(destroyedEntity); });
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Test requires entity data access safety checks")]
        public void ComponentDataArrayFromEntityThrowsIfNotExist()
        {
            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            var destroyedEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.DestroyEntity(destroyedEntity);

            var data = EmptySystem.GetComponentLookup<EcsTestData2>();

            Assert.Throws<System.ArgumentException>(() => { data[entity] = new EcsTestData2(); });
            Assert.Throws<System.ArgumentException>(() => { data[destroyedEntity] = new EcsTestData2(); });

            Assert.Throws<System.ArgumentException>(() => { var p = data[entity]; });
            Assert.Throws<System.ArgumentException>(() => { var p = data[destroyedEntity]; });
        }

        [Test]
        public void AddComponentTwiceIgnored()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.AddComponentData(entity, new EcsTestData(1));
            m_Manager.AddComponentData(entity, new EcsTestData(2));

            var testData = m_Manager.GetComponentData<EcsTestData>(entity);
            Assert.AreEqual(testData.value, 2);
        }

        [Test]
        public void RemoveComponentTwiceIgnored()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.RemoveComponent<Simulate>(entity);

            m_Manager.AddComponent<EcsTestData>(entity);

            EntitiesAssert.ContainsOnly(m_Manager, EntityMatch.Exact<EcsTestData>(entity));
            var removed0 = m_Manager.RemoveComponent<EcsTestData>(entity);
            EntitiesAssert.ContainsOnly(m_Manager, EntityMatch.Exact(entity));
            var removed1 = m_Manager.RemoveComponent<EcsTestData>(entity);
            EntitiesAssert.ContainsOnly(m_Manager, EntityMatch.Exact(entity));

            Assert.That(removed0, Is.True);
            Assert.That(removed1, Is.False);
        }

        [Test]
        public void RemoveSharedComponentTwiceIgnored()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.AddSharedComponentManaged(entity, new EcsTestSharedComp());

            var removed0 = m_Manager.RemoveComponent<EcsTestSharedComp>(entity);
            var removed1 = m_Manager.RemoveComponent<EcsTestSharedComp>(entity);

            Assert.That(removed0, Is.True);
            Assert.That(removed1, Is.False);
        }

        [Test]
        public void RemoveChunkComponentTwiceIgnored()
        {
            var entity = m_Manager.CreateEntity();

            m_Manager.AddChunkComponentData(m_Manager.UniversalQuery, new EcsTestData());

            var removed0 = m_Manager.RemoveChunkComponent<EcsTestData>(entity);
            var removed1 = m_Manager.RemoveChunkComponent<EcsTestData>(entity);

            Assert.That(removed0, Is.True);
            Assert.That(removed1, Is.False);
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Test requires entity data access safety checks")]
        public void AddComponentOnDestroyedEntityThrows()
        {
            var destroyedEntity = m_Manager.CreateEntity();
            m_Manager.DestroyEntity(destroyedEntity);
            Assert.Throws<System.InvalidOperationException>(() => { m_Manager.AddComponentData(destroyedEntity, new EcsTestData(1)); });
        }

        [Test]
        public void RemoveComponentOnDestroyedEntityIsIgnored()
        {
            var destroyedEntity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.DestroyEntity(destroyedEntity);
            m_Manager.RemoveComponent<EcsTestData>(destroyedEntity);
        }

        [Test]
        public void RemoveComponentOnEntityIsIgnored()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.RemoveComponent<EcsTestData>(entity);
        }

        [Test]
        public void RemoveChunkComponentOnEntityWithoutChunkComponentIsIgnored()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.RemoveChunkComponent<EcsTestData>(entity);
        }

        [Test]
        public void CreateDestroyEmptyEntity()
        {
            var entity = m_Manager.CreateEntity();
            Assert.IsTrue(m_Manager.Exists(entity));
            m_Manager.DestroyEntity(entity);
            Assert.IsFalse(m_Manager.Exists(entity));
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Test requires entity data access safety checks")]
        public void NotYetCreatedEntityWithSameVersionThrows()
        {
            var notYetCreatedEntitySameVersion = new Entity() {Index = 0, Version = 1};
            Assert.IsFalse(m_Manager.Exists(notYetCreatedEntitySameVersion));
            Assert.Throws<InvalidOperationException>(() => m_Manager.AddComponentData(notYetCreatedEntitySameVersion , new EcsTestData()));
        }

        [Test]
        public void CreateEntityWithNullTypeThrows()
        {
            Assert.Throws<System.NullReferenceException>(() => m_Manager.CreateEntity(null));
        }

        [Test]
        public void CreateEntityWithOneNullTypeThrows()
        {
            Assert.Throws<System.ArgumentException>(() => m_Manager.CreateEntity(null, typeof(EcsTestData)));
        }

        unsafe struct BigComponentData1 : IComponentData
        {
            public fixed int BigArray[10000];
        }

        unsafe struct BigComponentData2 : IComponentData
        {
            public fixed float BigArray[10000];
        }

        [Test]
        [TestRequiresDotsDebugOrCollectionChecks("Test requires entity data access safety checks")]
        public void CreateTooBigArchetypeThrows()
        {
            Assert.Throws<System.ArgumentException>(() =>
            {
                m_Manager.CreateArchetype(typeof(BigComponentData1), typeof(BigComponentData2));
            });
        }
    }
}
