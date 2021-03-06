﻿using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Inflatable.QueryProvider;
using Inflatable.QueryProvider.Providers.SQLServer;
using Inflatable.Tests.BaseClasses;
using Inflatable.Tests.MockClasses;
using Inflatable.Tests.TestDatabases.ComplexGraph;
using Inflatable.Tests.TestDatabases.ComplexGraph.Mappings;
using Serilog;
using System.Data.SqlClient;
using Xunit;

namespace Inflatable.Tests.QueryProvider.Providers.SQLServer
{
    public class SQLServerQueryProviderTests : TestingFixture
    {
        [Fact]
        public void Batch()
        {
            var TestObject = new SQLServerQueryProvider(Configuration, ObjectPool);
            var Result = TestObject.Batch(new TestDatabases.Databases.TestDatabase2Mapping(), DynamoFactory);
            Assert.NotNull(Result);
        }

        [Fact]
        public void CreateGenerator()
        {
            var TestObject = new SQLServerQueryProvider(Configuration, ObjectPool);
            var Mappings = new MappingSource(new IMapping[] {
                new BaseClass1Mapping(),
                new ConcreteClass1Mapping(),
                new ConcreteClass2Mapping(),
                new ConcreteClass3Mapping(),
                new IInterface1Mapping(),
                new IInterface2Mapping()
            },
                new MockDatabaseMapping(),
                new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration, ObjectPool) }, Logger),
            Canister.Builder.Bootstrapper.Resolve<ILogger>(),
               ObjectPool);
            var Result = TestObject.CreateGenerator<ConcreteClass1>(Mappings);
            Assert.Equal(typeof(ConcreteClass1), Result.AssociatedType);
        }

        [Fact]
        public void Creation()
        {
            var TestObject = new SQLServerQueryProvider(Configuration, ObjectPool);
            Assert.Equal(Configuration, TestObject.Configuration);
            Assert.Equal(SqlClientFactory.Instance, TestObject.Provider);
        }
    }
}