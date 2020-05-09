﻿using BigBook;
using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Inflatable.QueryProvider;
using Inflatable.QueryProvider.Providers.SQLServer;
using Inflatable.Schema;
using Inflatable.Sessions;
using Inflatable.Tests.BaseClasses;
using Inflatable.Tests.TestDatabases.Databases;
using Inflatable.Tests.TestDatabases.ManyToManyProperties;
using Inflatable.Tests.TestDatabases.ManyToManyProperties.Mappings;
using Serilog;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inflatable.Tests.Sessions
{
    public class SessionWithManyToManyPropertiesSelfReferencing : TestingFixture
    {
        public SessionWithManyToManyPropertiesSelfReferencing()
        {
            InternalMappingManager = new MappingManager(new IMapping[] {
                new ManyToManyPropertySelfReferencingMapping()
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
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Results = DbContext<ManyToManyPropertySelfReferencing>.CreateQuery().ToArray();
            Assert.Equal(6, Results.Length);
        }

        [Fact]
        public async Task DeleteMultipleWithDataInDatabase()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Result = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT TOP 2 ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            await TestObject.Delete(Result.ToArray()).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Equal(2, Results.Count());
        }

        [Fact]
        public async Task DeleteMultipleWithDataInDatabaseAndCascade()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Result = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT TOP 2 ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            await TestObject.Delete(Result.ToArray()).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Equal(2, Results.Count());
        }

        [Fact]
        public async Task DeleteWithDataInDatabase()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Result = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT TOP 1 ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            await TestObject.Delete(Result.ToArray()).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Equal(4, Results.Count());
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
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            var Result = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT TOP 1 ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            await TestObject.Delete(Result.ToArray()).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Empty(Results);
        }

        [Fact]
        public async Task InsertMultipleObjectsWithCascade()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Result1 = new ManyToManyPropertySelfReferencing
            {
                BoolValue = false
            };
            Result1.Children.Add(new ManyToManyPropertySelfReferencing
            {
                BoolValue = true
            });
            var Result2 = new ManyToManyPropertySelfReferencing
            {
                BoolValue = false
            };
            Result2.Children.Add(new ManyToManyPropertySelfReferencing
            {
                BoolValue = true
            });
            var Result3 = new ManyToManyPropertySelfReferencing
            {
                BoolValue = false
            };
            Result3.Children.Add(new ManyToManyPropertySelfReferencing
            {
                BoolValue = true
            });
            await TestObject.Save(Result1, Result2, Result3).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID], BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Equal(12, Results.Count());
            Assert.Contains(Results, x => x.ID == Result1.ID
            && !x.BoolValue
            && x.Children.Count == 1
            && x.Children.All(y => y.BoolValue));
            Assert.Contains(Results, x => x.ID == Result2.ID
            && !x.BoolValue
            && x.Children.Count == 1
            && x.Children.All(y => y.BoolValue));
            Assert.Contains(Results, x => x.ID == Result3.ID
            && !x.BoolValue
            && x.Children.Count == 1
            && x.Children.All(y => y.BoolValue));
        }

        [Fact]
        public async Task LoadMapPropertyWithDataInDatabase()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Result = DbContext<ManyToManyPropertySelfReferencing>.CreateQuery().Where(x => x.ID == 1).First();
            Assert.NotNull(Result.Children);
            Assert.Equal(4, Result.Children[0].ID);
        }

        [Fact]
        public async Task UpdateMultipleCascadeWithDataInDatabase()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            var UpdatedResults = Results.ForEach(x =>
            {
                x.BoolValue = false;
                x.Children.ForEach(y => y.BoolValue = false);
                x.Children.Add(new ManyToManyPropertySelfReferencing
                {
                    BoolValue = false
                });
            }).ToArray();
            await TestObject.Save(UpdatedResults).ExecuteAsync().ConfigureAwait(false);
            Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.True(Results.All(x => !x.BoolValue));
            Results = Results.Where(x => x.Children.Count > 0);
            Assert.Equal(9, Results.Max(x => x.Children.Max(y => y.ID)));
        }

        [Fact]
        public async Task UpdateMultipleWithDataInDatabase()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            var UpdatedResults = Results.ForEach(x =>
            {
                x.BoolValue = false;
                x.Children.Add(new ManyToManyPropertySelfReferencing
                {
                    BoolValue = true
                });
                var Result = TestObject.Save(x.Children).ExecuteAsync().GetAwaiter().GetResult();
            }).ToArray();
            await TestObject.Save(UpdatedResults).ExecuteAsync().ConfigureAwait(false);
            Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.False(Results.All(x => !x.BoolValue));
            Results = Results.Where(x => x.Children.Count > 0);
            Assert.Equal(9, Results.Max(x => x.Children.Max(y => y.ID)));
        }

        [Fact]
        public async Task UpdateMultipleWithDataInDatabaseToNull()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();

            await SetupDataAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            var UpdatedResults = Results.ForEach(x =>
            {
                x.BoolValue = false;
                x.Children.Clear();
            }).ToArray();
            Assert.Equal(9, await TestObject.Save(UpdatedResults).ExecuteAsync().ConfigureAwait(false));
            Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID],BoolValue_ as [BoolValue] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.True(Results.All(x => !x.BoolValue));
            Assert.True(Results.All(x => x.Children.Count == 0));
        }

        [Fact]
        public async Task UpdateNullWithDataInDatabase()
        {
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();
            await SetupDataAsync().ConfigureAwait(false);
            Assert.Equal(0, await TestObject.Save<ManyToManyPropertySelfReferencing>(null).ExecuteAsync().ConfigureAwait(false));
        }

        [Fact]
        public async Task UpdateWithNoDataInDatabase()
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
            var TestObject = Canister.Builder.Bootstrapper.Resolve<ISession>();
            var Result = new ManyToManyPropertySelfReferencing
            {
                BoolValue = false
            };
            Result.Children.Add(new ManyToManyPropertySelfReferencing
            {
                BoolValue = true
            });
            await TestObject.Save(Result).ExecuteAsync().ConfigureAwait(false);
            var Results = await TestObject.ExecuteAsync<ManyToManyPropertySelfReferencing>("SELECT ID_ as [ID] FROM ManyToManyPropertySelfReferencing_", CommandType.Text, "Default").ConfigureAwait(false);
            Assert.Equal(2, Results.Count());
        }

        private async Task SetupDataAsync()
        {
            var TestObject = new SchemaManager(MappingManager, Configuration, Logger, DataModeler, Sherlock, Helper);
            await Helper
                .CreateBatch()
                .AddQuery(CommandType.Text, @"INSERT INTO [dbo].[ManyToManyPropertySelfReferencing_]([BoolValue_]) VALUES (1)
INSERT INTO [dbo].[ManyToManyPropertySelfReferencing_]([BoolValue_]) VALUES (1)
INSERT INTO [dbo].[ManyToManyPropertySelfReferencing_]([BoolValue_]) VALUES (1)")
                .AddQuery(CommandType.Text, @"INSERT INTO [dbo].[ManyToManyPropertySelfReferencing_]([BoolValue_]) VALUES (1)
INSERT INTO [dbo].[ManyToManyPropertySelfReferencing_]([BoolValue_]) VALUES (1)
INSERT INTO [dbo].[ManyToManyPropertySelfReferencing_]([BoolValue_]) VALUES (1)")
                .AddQuery(CommandType.Text, @"INSERT INTO [dbo].[Parent_Child]([Parent_ManyToManyPropertySelfReferencing_ID_],[ManyToManyPropertySelfReferencing_ID_]) VALUES (1,4)
INSERT INTO [dbo].[Parent_Child]([Parent_ManyToManyPropertySelfReferencing_ID_],[ManyToManyPropertySelfReferencing_ID_]) VALUES (2,5)
INSERT INTO [dbo].[Parent_Child]([Parent_ManyToManyPropertySelfReferencing_ID_],[ManyToManyPropertySelfReferencing_ID_]) VALUES (3,6)")
                .ExecuteScalarAsync<int>().ConfigureAwait(false);
        }
    }
}