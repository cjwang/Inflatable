﻿using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Inflatable.QueryProvider;
using Inflatable.QueryProvider.Enums;
using Inflatable.QueryProvider.Providers.SQLServer;
using Inflatable.QueryProvider.Providers.SQLServer.QueryGenerators;
using Inflatable.Tests.BaseClasses;
using Inflatable.Tests.MockClasses;
using Inflatable.Tests.TestDatabases.ComplexGraph;
using Inflatable.Tests.TestDatabases.ComplexGraph.Mappings;
using Inflatable.Tests.TestDatabases.ManyToManyProperties;
using Inflatable.Tests.TestDatabases.ManyToOneProperties;
using Inflatable.Tests.TestDatabases.ManyToOneProperties.Mappings;
using Inflatable.Tests.TestDatabases.MapProperties;
using Inflatable.Tests.TestDatabases.SimpleTest;
using Inflatable.Tests.TestDatabases.SimpleTestWithDatabase;
using Serilog;
using System.Data;
using System.Linq;
using Xunit;

namespace Inflatable.Tests.QueryProvider.Providers.SQLServer.QueryGenerators
{
    public class SavePropertiesQueryTests : TestingFixture
    {
        [Fact]
        public void Creation()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new BaseClass1Mapping(),
                new ConcreteClass1Mapping(),
                new ConcreteClass2Mapping(),
                new ConcreteClass3Mapping(),
                new IInterface1Mapping(),
                new IInterface2Mapping()
            },
                new MockDatabaseMapping(),
                new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
            Canister.Builder.Bootstrapper.Resolve<ILogger>());
            var TestObject = new SavePropertiesQuery<ConcreteClass1>(Mappings);
            Assert.Equal(typeof(ConcreteClass1), TestObject.AssociatedType);
            Assert.Same(Mappings, TestObject.MappingInformation);
            Assert.Equal(QueryType.JoinsSave, TestObject.QueryType);
        }

        [Fact]
        public void GenerateDeclarations()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new BaseClass1Mapping(),
                new ConcreteClass1Mapping(),
                new ConcreteClass2Mapping(),
                new ConcreteClass3Mapping(),
                new IInterface1Mapping(),
                new IInterface2Mapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());
            var TestObject = new SavePropertiesQuery<ConcreteClass1>(Mappings);
            var Result = TestObject.GenerateDeclarations();
            Assert.Equal(CommandType.Text, Result[0].DatabaseCommandType);
            Assert.Empty(Result[0].Parameters);
            Assert.Equal("", Result[0].QueryString);
            Assert.Equal(QueryType.JoinsSave, Result[0].QueryType);
        }

        [Fact]
        public void GenerateQuery()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new BaseClass1Mapping(),
                new ConcreteClass1Mapping(),
                new ConcreteClass2Mapping(),
                new ConcreteClass3Mapping(),
                new IInterface1Mapping(),
                new IInterface2Mapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());
            var TestObject = new SavePropertiesQuery<ConcreteClass1>(Mappings);
            var Result = TestObject.GenerateQueries(new ConcreteClass1 { ID = 10, BaseClassValue1 = 1, Value1 = 2 })[0];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(0, Result.Parameters.Length);
            Assert.Equal("", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
        }

        [Fact]
        public void GenerateQueryWithManyToManyProperties()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new AllReferencesAndIDMappingWithDatabase(),
                new ManyToManyPropertiesMapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());

            var ManyToManyProperty = Mappings.Mappings[typeof(ManyToManyProperties)].ManyToManyProperties.First();
            ManyToManyProperty.Setup(Mappings, new Inflatable.Schema.DataModel(Mappings, Configuration, Logger));

            var TestObject = new SavePropertiesQuery<ManyToManyProperties>(Mappings);
            var TempManyToMany = new ManyToManyProperties { ID = 10, BoolValue = true };
            TempManyToMany.ManyToManyClass.Add(new TestDatabases.SimpleTest.AllReferencesAndID { ID = 1 });
            TempManyToMany.ManyToManyClass.Add(new TestDatabases.SimpleTest.AllReferencesAndID { ID = 2 });

            var Result = TestObject.GenerateQueries(TempManyToMany, ManyToManyProperty)[0];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(2, Result.Parameters.Length);
            Assert.Equal(1, Result.Parameters[0].InternalValue);
            Assert.Equal(10, Result.Parameters[1].InternalValue);
            Assert.Equal("AllReferencesAndID_ID_", Result.Parameters[0].ID);
            Assert.Equal("ManyToManyProperties_ID_", Result.Parameters[1].ID);
            Assert.Equal("IF NOT EXISTS (SELECT * FROM [dbo].[AllReferencesAndID_ManyToManyProperties] WHERE [dbo].[AllReferencesAndID_ManyToManyProperties].[AllReferencesAndID_ID_] = @AllReferencesAndID_ID_ AND [dbo].[AllReferencesAndID_ManyToManyProperties].[ManyToManyProperties_ID_] = @ManyToManyProperties_ID_) BEGIN INSERT INTO [dbo].[AllReferencesAndID_ManyToManyProperties]([dbo].[AllReferencesAndID_ManyToManyProperties].[AllReferencesAndID_ID_],[dbo].[AllReferencesAndID_ManyToManyProperties].[ManyToManyProperties_ID_]) VALUES (@AllReferencesAndID_ID_,@ManyToManyProperties_ID_) END;", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
        }

        [Fact]
        public void GenerateQueryWithManyToOneManyProperties()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new ManyToOneManyPropertiesMapping(),
                new ManyToOneOnePropertiesMapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());

            var ManyToOneManyProperty = Mappings.Mappings[typeof(ManyToOneManyProperties)].ManyToOneProperties.First();
            ManyToOneManyProperty.Setup(Mappings, new Inflatable.Schema.DataModel(Mappings, Configuration, Logger));

            var TestObject = new SavePropertiesQuery<ManyToOneManyProperties>(Mappings);
            var TempManyToOneMany = new ManyToOneManyProperties { ID = 10, BoolValue = true };
            TempManyToOneMany.ManyToOneClass.Add(new ManyToOneOneProperties { ID = 1 });
            TempManyToOneMany.ManyToOneClass.Add(new ManyToOneOneProperties { ID = 2 });

            var Result = TestObject.GenerateQueries(TempManyToOneMany, ManyToOneManyProperty)[0];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(2, Result.Parameters.Length);
            Assert.Equal(10, Result.Parameters[0].InternalValue);
            Assert.Equal(1, Result.Parameters[1].InternalValue);
            Assert.Equal("ManyToOneManyProperties_ID_", Result.Parameters[0].ID);
            Assert.Equal("ID", Result.Parameters[1].ID);
            Assert.Equal("UPDATE [dbo].[ManyToOneOneProperties_] SET [dbo].[ManyToOneOneProperties_].[ManyToOneManyProperties_ID_] = @ManyToOneManyProperties_ID_ WHERE [dbo].[ManyToOneOneProperties_].[ID_] = @ID;", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
            Result = TestObject.GenerateQueries(TempManyToOneMany, ManyToOneManyProperty)[1];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(2, Result.Parameters.Length);
            Assert.Equal(10, Result.Parameters[0].InternalValue);
            Assert.Equal(2, Result.Parameters[1].InternalValue);
            Assert.Equal("ManyToOneManyProperties_ID_", Result.Parameters[0].ID);
            Assert.Equal("ID", Result.Parameters[1].ID);
            Assert.Equal("UPDATE [dbo].[ManyToOneOneProperties_] SET [dbo].[ManyToOneOneProperties_].[ManyToOneManyProperties_ID_] = @ManyToOneManyProperties_ID_ WHERE [dbo].[ManyToOneOneProperties_].[ID_] = @ID;", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
        }

        [Fact]
        public void GenerateQueryWithManyToOneSingleProperties()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new ManyToOneOnePropertiesMapping(),
                new ManyToOneManyPropertiesMapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());

            var ManyToOneOneProperty = Mappings.Mappings[typeof(ManyToOneOneProperties)].ManyToOneProperties.First();
            ManyToOneOneProperty.Setup(Mappings, new Inflatable.Schema.DataModel(Mappings, Configuration, Logger));

            var TestObject = new SavePropertiesQuery<ManyToOneOneProperties>(Mappings);
            var TempManyToOneOne = new ManyToOneOneProperties { ID = 10, BoolValue = true };
            TempManyToOneOne.ManyToOneClass = new ManyToOneManyProperties { ID = 1 };

            var Result = TestObject.GenerateQueries(TempManyToOneOne, ManyToOneOneProperty)[0];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(2, Result.Parameters.Length);
            Assert.Equal(1, Result.Parameters[0].InternalValue);
            Assert.Equal(10, Result.Parameters[1].InternalValue);
            Assert.Equal("ManyToOneManyProperties_ID_", Result.Parameters[0].ID);
            Assert.Equal("ID", Result.Parameters[1].ID);
            Assert.Equal("UPDATE [dbo].[ManyToOneOneProperties_] SET [dbo].[ManyToOneOneProperties_].[ManyToOneManyProperties_ID_] = @ManyToOneManyProperties_ID_ WHERE [dbo].[ManyToOneOneProperties_].[ID_] = @ID;", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
        }

        [Fact]
        public void GenerateQueryWithMapPropertiesNullValue()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new AllReferencesAndIDMappingWithDatabase(),
                new MapPropertiesMapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());
            var MapProperty = Mappings.Mappings[typeof(MapProperties)].MapProperties.First();
            MapProperty.Setup(Mappings);
            var TestObject = new SavePropertiesQuery<MapProperties>(Mappings);
            var Result = TestObject.GenerateQueries(new MapProperties { ID = 10, BoolValue = true }, MapProperty)[0];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(2, Result.Parameters.Length);
            Assert.Equal(10, Result.Parameters[1].InternalValue);
            Assert.Equal(null, Result.Parameters[0].InternalValue);
            Assert.Equal("ID", Result.Parameters[1].ID);
            Assert.Equal("AllReferencesAndID_MappedClass_ID_", Result.Parameters[0].ID);
            Assert.Equal("UPDATE [dbo].[MapProperties_] SET [dbo].[MapProperties_].[AllReferencesAndID_MappedClass_ID_] = @AllReferencesAndID_MappedClass_ID_ WHERE [dbo].[MapProperties_].[ID_] = @ID;", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
        }

        [Fact]
        public void GenerateQueryWithMapPropertiesWithValue()
        {
            var Mappings = new MappingSource(new IMapping[] {
                new AllReferencesAndIDMappingWithDatabase(),
                new MapPropertiesMapping()
            },
                   new MockDatabaseMapping(),
                   new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
               Canister.Builder.Bootstrapper.Resolve<ILogger>());
            var MapProperty = Mappings.Mappings[typeof(MapProperties)].MapProperties.First();
            MapProperty.Setup(Mappings);
            var TestObject = new SavePropertiesQuery<MapProperties>(Mappings);
            var Result = TestObject.GenerateQueries(new MapProperties { ID = 10, BoolValue = true, MappedClass = new AllReferencesAndID { ID = 1 } }, MapProperty)[0];
            Assert.Equal(CommandType.Text, Result.DatabaseCommandType);
            Assert.Equal(2, Result.Parameters.Length);
            Assert.Equal(10, Result.Parameters[1].InternalValue);
            Assert.Equal(1, Result.Parameters[0].InternalValue);
            Assert.Equal("ID", Result.Parameters[1].ID);
            Assert.Equal("AllReferencesAndID_MappedClass_ID_", Result.Parameters[0].ID);
            Assert.Equal("UPDATE [dbo].[MapProperties_] SET [dbo].[MapProperties_].[AllReferencesAndID_MappedClass_ID_] = @AllReferencesAndID_MappedClass_ID_ WHERE [dbo].[MapProperties_].[ID_] = @ID;", Result.QueryString);
            Assert.Equal(QueryType.JoinsSave, Result.QueryType);
        }
    }
}