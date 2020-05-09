﻿using BigBook;
using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Inflatable.QueryProvider;
using Inflatable.QueryProvider.Providers.SQLServer;
using Inflatable.Schema;
using Inflatable.Sessions;
using Inflatable.Tests.BaseClasses;
using Inflatable.Tests.TestDatabases.Databases;
using Inflatable.Tests.TestDatabases.MapProperties;
using Inflatable.Tests.TestDatabases.MapProperties.Mappings;
using Inflatable.Tests.TestDatabases.SimpleTestWithDatabase;
using Serilog;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inflatable.Tests.Sessions
{
    public class SessionWithMapBaseClassProperties : TestingFixture
    {
        public SessionWithMapBaseClassProperties()
        {
            InternalMappingManager = new MappingManager(new IMapping[] {
                new AllReferencesAndIDMappingWithDatabase(),
                new MapPropertiesWithBaseClassesMapping(),
                new MapProperty2Mapping(),
                new MapProperty1Mapping(),
                new MapPropertyBaseClassMapping(),
                new IMapPropertyInterfaceMapping()
            },
            new IDatabase[]{
                new TestDatabaseMapping()
            },
            new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration, ObjectPool) }, Logger),
            Canister.Builder.Bootstrapper.Resolve<ILogger>(),
            ObjectPool);
            InternalSchemaManager = new SchemaManager(InternalMappingManager, Configuration, Logger, DataModeler, Sherlock, Helper);

            var TempQueryProvider = new SQLServerQueryProvider(Configuration, ObjectPool);
            InternalQueryProviderManager = new QueryProviderManager(new[] { TempQueryProvider }, Logger);

            CacheManager = Canister.Builder.Bootstrapper.Resolve<BigBook.Caching.Manager>();
            CacheManager.Cache().Clear();
        }

        public static Aspectus.Aspectus AOPManager => Canister.Builder.Bootstrapper.Resolve<Aspectus.Aspectus>();
        public BigBook.Caching.Manager CacheManager { get; set; }
        public MappingManager InternalMappingManager { get; set; }

        public QueryProviderManager InternalQueryProviderManager { get; set; }
        public SchemaManager InternalSchemaManager { get; set; }

        [Fact]
        public async Task AllNoParametersWithDataInDatabase()
        {
            _ = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            await SetupDataAsync().ConfigureAwait(false);
            var Results = DbContext<MapPropertiesWithBaseClasses>.CreateQuery().ToArray();
            Assert.Equal(3, Results.Length);
        }

        [Fact]
        public async Task DeleteMultipleWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            await SetupDataAsync().ConfigureAwait(false);
            var Result = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT TOP 2 ID_ as [ID] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            await TestObject.Delete(Result.ToArray()).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Single(Results);
            var Results2 = await TestObject.ExecuteAsync<IMapPropertyInterface>("SELECT ID_ as [ID] FROM IMapPropertyInterface_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Single(Results2);
        }

        [Fact]
        public async Task DeleteWithNoDataInDatabase()
        {
            try
            {
                await Helper.CreateBatch(SqlClientFactory.Instance, "Data Source=localhost;Initial Catalog=master;Integrated Security=SSPI;Pooling=false")
                    .AddQuery(CommandType.Text, "ALTER DATABASE TestDatabase SET OFFLINE WITH ROLLBACK IMMEDIATE\r\nALTER DATABASE TestDatabase SET ONLINE\r\nDROP DATABASE TestDatabase")
                    .AddQuery(CommandType.Text, "ALTER DATABASE TestDatabase2 SET OFFLINE WITH ROLLBACK IMMEDIATE\r\nALTER DATABASE TestDatabase2 SET ONLINE\r\nDROP DATABASE TestDatabase2")
                    .AddQuery(CommandType.Text, "ALTER DATABASE MockDatabase SET OFFLINE WITH ROLLBACK IMMEDIATE\r\nALTER DATABASE MockDatabase SET ONLINE\r\nDROP DATABASE MockDatabase")
                    .AddQuery(CommandType.Text, "ALTER DATABASE MockDatabaseForMockMapping SET OFFLINE WITH ROLLBACK IMMEDIATE\r\nALTER DATABASE MockDatabaseForMockMapping SET ONLINE\r\nDROP DATABASE MockDatabaseForMockMapping")
                    .ExecuteScalarAsync<int>().ConfigureAwait(false);
            }
            catch { }
            _ = new SchemaManager(MappingManager, Configuration, Logger, DataModeler, Sherlock, Helper);
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            var Result = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT TOP 1 ID_ as [ID] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            await TestObject.Delete(Result.ToArray()).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Empty(Results);
        }

        [Fact]
        public async Task InsertMultipleObjectsWithCascade()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            await SetupDataAsync().ConfigureAwait(false);
            var Result1 = new MapPropertiesWithBaseClasses
            {
                BoolValue = false,
                MappedClass = new MapProperty1
                {
                    BaseValue1 = 1,
                    ChildValue1 = 1
                }
            };
            var Result2 = new MapPropertiesWithBaseClasses
            {
                BoolValue = true,
                MappedClass = new MapProperty2
                {
                    BaseValue1 = 2,
                    ChildValue2 = 2
                }
            };
            var Result3 = new MapPropertiesWithBaseClasses
            {
                BoolValue = false,
                MappedClass = new MapProperty1
                {
                    BaseValue1 = 3,
                    ChildValue1 = 3
                }
            };
            await TestObject.Save(Result1, Result2, Result3).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID], BoolValue_ as [BoolValue] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Equal(6, Results.Count());
            Assert.Contains(Results, x => x.ID == Result1.ID
            && !x.BoolValue
            && x.MappedClass.BaseValue1 == 1
            && ((MapProperty1)x.MappedClass).ChildValue1 == 1);
            Assert.Contains(Results, x => x.ID == Result2.ID
            && x.BoolValue
            && x.MappedClass.BaseValue1 == 2
            && ((MapProperty2)x.MappedClass).ChildValue2 == 2);
            Assert.Contains(Results, x => x.ID == Result3.ID
            && !x.BoolValue
            && x.MappedClass.BaseValue1 == 3
            && ((MapProperty1)x.MappedClass).ChildValue1 == 3);
        }

        [Fact]
        public async Task LoadMapPropertyWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            await SetupDataAsync().ConfigureAwait(false);
            var Result = DbContext<MapPropertiesWithBaseClasses>.CreateQuery().Where(x => x.ID == 1).First();
            Assert.NotNull(Result.MappedClass);
            Assert.Equal(1, Result.MappedClass.ID);
        }

        [Fact]
        public async Task UpdateMultipleWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            await SetupDataAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            var UpdatedResults = Results.ForEach(x =>
            {
                x.BoolValue = false;
                x.MappedClass = new MapProperty1
                {
                    ChildValue1 = 11,
                    BaseValue1 = 10
                };
            }).ToArray();
            await TestObject.Save(UpdatedResults).ExecuteAsync().ConfigureAwait(false);
            Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.True(Results.All(x => !x.BoolValue));
            Assert.True(Results.All(x => x.MappedClass.ID > 3));
            Assert.True(Results.All(x => x.MappedClass is MapProperty1));
        }

        [Fact]
        public async Task UpdateMultipleWithDataInDatabaseToNull()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, Logger, CacheManager, DynamoFactory);
            await SetupDataAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            var UpdatedResults = Results.ForEach(x =>
            {
                x.BoolValue = false;
                x.MappedClass = null;
            }).ToArray();
            Assert.Equal(6, await TestObject.Save(UpdatedResults).ExecuteAsync().ConfigureAwait(false));
            Results = await TestObject.ExecuteAsync<MapPropertiesWithBaseClasses>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM MapPropertiesWithBaseClasses_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.True(Results.All(x => !x.BoolValue));
            Assert.True(Results.All(x => x.MappedClass == null));
        }

        private async Task SetupDataAsync()
        {
            _ = new SchemaManager(MappingManager, Configuration, Logger, DataModeler, Sherlock, Helper);
            await Helper
                .CreateBatch()
                .AddQuery(CommandType.Text, @"INSERT INTO [dbo].[IMapPropertyInterface_] DEFAULT VALUES;
INSERT INTO [dbo].[MapPropertyBaseClass_]([BaseValue1_],[IMapPropertyInterface_ID_]) VALUES (1,1);
INSERT INTO [dbo].[MapProperty1_]([ChildValue1_],[MapPropertyBaseClass_ID_]) VALUES (2,1);

INSERT INTO [dbo].[IMapPropertyInterface_] DEFAULT VALUES;
INSERT INTO [dbo].[MapPropertyBaseClass_]([BaseValue1_],[IMapPropertyInterface_ID_]) VALUES (1,2);
INSERT INTO [dbo].[MapProperty2_]([ChildValue2_],[MapPropertyBaseClass_ID_]) VALUES (2,2);

INSERT INTO [dbo].[IMapPropertyInterface_] DEFAULT VALUES;
INSERT INTO [dbo].[MapPropertyBaseClass_]([BaseValue1_],[IMapPropertyInterface_ID_]) VALUES (1,3);
INSERT INTO [dbo].[MapProperty1_]([ChildValue1_],[MapPropertyBaseClass_ID_]) VALUES (2,3);

INSERT INTO [dbo].[MapPropertiesWithBaseClasses_]([BoolValue_],[IMapPropertyInterface_MappedClass_ID_]) VALUES (1,1)
INSERT INTO [dbo].[MapPropertiesWithBaseClasses_]([BoolValue_],[IMapPropertyInterface_MappedClass_ID_]) VALUES (0,2)
INSERT INTO [dbo].[MapPropertiesWithBaseClasses_]([BoolValue_],[IMapPropertyInterface_MappedClass_ID_]) VALUES (1,3)")
                .ExecuteScalarAsync<int>().ConfigureAwait(false);
        }
    }
}