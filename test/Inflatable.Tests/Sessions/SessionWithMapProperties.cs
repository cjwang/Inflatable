﻿using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Inflatable.QueryProvider;
using Inflatable.QueryProvider.Providers.SQLServer;
using Inflatable.Schema;
using Inflatable.Sessions;
using Inflatable.Tests.BaseClasses;
using Inflatable.Tests.TestDatabases.Databases;
using Inflatable.Tests.TestDatabases.MapProperties;
using Inflatable.Tests.TestDatabases.SimpleTest;
using Inflatable.Tests.TestDatabases.SimpleTestWithDatabase;
using Serilog;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Inflatable.Tests.Sessions
{
    public class SessionWithMapProperties : TestingFixture
    {
        public SessionWithMapProperties()
        {
            InternalMappingManager = new MappingManager(new IMapping[] {
                new AllReferencesAndIDMappingWithDatabase(),
                new MapPropertiesMapping()
            },
            new IDatabase[]{
                new TestDatabaseMapping()
            },
            new QueryProviderManager(new[] { new SQLServerQueryProvider(Configuration) }, Logger),
            Canister.Builder.Bootstrapper.Resolve<ILogger>());
            InternalSchemaManager = new SchemaManager(InternalMappingManager, Configuration, Logger);

            var TempQueryProvider = new SQLServerQueryProvider(Configuration);
            InternalQueryProviderManager = new QueryProviderManager(new[] { TempQueryProvider }, Logger);

            CacheManager = Canister.Builder.Bootstrapper.Resolve<BigBook.Caching.Manager>();
            CacheManager.Cache().Clear();
        }

        public Aspectus.Aspectus AOPManager => Canister.Builder.Bootstrapper.Resolve<Aspectus.Aspectus>();
        public BigBook.Caching.Manager CacheManager { get; set; }
        public MappingManager InternalMappingManager { get; set; }

        public QueryProviderManager InternalQueryProviderManager { get; set; }
        public SchemaManager InternalSchemaManager { get; set; }

        [Fact]
        public void AllNoParametersWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, AOPManager, CacheManager);
            SetupData();
            var Results = DbContext<MapProperties>.CreateQuery().ToArray();
            Assert.Equal(3, Results.Count());
        }

        [Fact]
        public async Task DeleteMultipleWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, AOPManager, CacheManager);
            SetupData();
            var Result = await TestObject.ExecuteAsync<MapProperties>("SELECT TOP 2 ID_ as [ID] FROM MapProperties_", CommandType.Text, "Default");
            await TestObject.DeleteAsync(Result.ToArray());
            var Results = await TestObject.ExecuteAsync<MapProperties>("SELECT ID_ as [ID] FROM MapProperties_", CommandType.Text, "Default");
            Assert.Equal(1, Results.Count());
            var Results2 = await TestObject.ExecuteAsync<AllReferencesAndID>("SELECT ID_ as [ID] FROM AllReferencesAndID_", CommandType.Text, "Default");
            Assert.Equal(3, Results2.Count());
        }

        [Fact]
        public async Task DeleteWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, AOPManager, CacheManager);
            SetupData();
            var Result = await TestObject.ExecuteAsync<MapProperties>("SELECT TOP 1 ID_ as [ID] FROM MapProperties_", CommandType.Text, "Default");
            await TestObject.DeleteAsync(Result.ToArray());
            var Results = await TestObject.ExecuteAsync<MapProperties>("SELECT ID_ as [ID] FROM MapProperties_", CommandType.Text, "Default");
            Assert.Equal(2, Results.Count());
        }

        [Fact]
        public async Task DeleteWithNoDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, AOPManager, CacheManager);
            var Result = await TestObject.ExecuteAsync<MapProperties>("SELECT TOP 1 ID_ as [ID] FROM MapProperties_", CommandType.Text, "Default");
            await TestObject.DeleteAsync(Result.ToArray());
            var Results = await TestObject.ExecuteAsync<MapProperties>("SELECT ID_ as [ID] FROM MapProperties_", CommandType.Text, "Default");
            Assert.Empty(Results);
        }

        [Fact]
        public async Task InsertMultipleObjects()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, AOPManager, CacheManager);
            SetupData();
            var Result1 = new MapProperties
            {
                BoolValue = false,
                MappedClass = new AllReferencesAndID
                {
                    ByteArrayValue = new byte[] { 1, 2, 3, 4 },
                    ByteValue = 34,
                    CharValue = 'a',
                    DateTimeValue = new DateTime(2000, 1, 1)
                }
            };
            var Result2 = new MapProperties
            {
                BoolValue = false,
                MappedClass = new AllReferencesAndID
                {
                    ByteArrayValue = new byte[] { 5, 6, 7, 8 },
                    ByteValue = 34,
                    CharValue = 'b',
                    DateTimeValue = new DateTime(2000, 1, 1)
                }
            };
            var Result3 = new MapProperties
            {
                BoolValue = false,
                MappedClass = new AllReferencesAndID
                {
                    ByteArrayValue = new byte[] { 9, 10, 11, 12 },
                    ByteValue = 34,
                    CharValue = 'c',
                    DateTimeValue = new DateTime(2000, 1, 1)
                }
            };
            await TestObject.InsertAsync(Result1, Result2, Result3);
            var Results = await TestObject.ExecuteAsync<MapProperties>("SELECT ID_ as [ID], BoolValue_ as [BoolValue] FROM MapProperties_", CommandType.Text, "Default");
            Assert.Equal(6, Results.Count());
            Assert.True(Results.Any(x => x.ID == Result1.ID
            && !x.BoolValue
            && x.MappedClass.ByteValue == 34
            && x.MappedClass.CharValue == 'a'
            && x.MappedClass.DateTimeValue == new DateTime(2000, 1, 1)));
            Assert.True(Results.Any(x => x.ID == Result2.ID
            && !x.BoolValue
            && x.MappedClass.ByteValue == 34
            && x.MappedClass.CharValue == 'b'
            && x.MappedClass.DateTimeValue == new DateTime(2000, 1, 1)));
            Assert.True(Results.Any(x => x.ID == Result3.ID
            && !x.BoolValue
            && x.MappedClass.ByteValue == 34
            && x.MappedClass.CharValue == 'c'
            && x.MappedClass.DateTimeValue == new DateTime(2000, 1, 1)));
        }

        [Fact]
        public void LoadMapPropertyWithDataInDatabase()
        {
            var TestObject = new Session(InternalMappingManager, InternalSchemaManager, InternalQueryProviderManager, AOPManager, CacheManager);
            SetupData();
            var Result = DbContext<MapProperties>.CreateQuery().Where(x => x.ID == 1).First();
            Assert.NotNull(Result.MappedClass);
            Assert.Equal(1, Result.MappedClass.ID);
        }

        private void SetupData()
        {
            new SQLHelper.SQLHelper(Configuration, SqlClientFactory.Instance)
                .CreateBatch()
                .AddQuery(@"INSERT INTO [dbo].[AllReferencesAndID_]
           ([BoolValue_]
           ,[ByteArrayValue_]
           ,[ByteValue_]
           ,[CharValue_]
           ,[DateTimeValue_]
           ,[DecimalValue_]
           ,[DoubleValue_]
           ,[FloatValue_]
           ,[GuidValue_]
           ,[IntValue_]
           ,[LongValue_]
           ,[SByteValue_]
           ,[ShortValue_]
           ,[StringValue1_]
           ,[StringValue2_]
           ,[TimeSpanValue_]
           ,[UIntValue_]
           ,[ULongValue_]
           ,[UShortValue_])
     VALUES
           (1
           ,1
           ,1
           ,'a'
           ,'1/1/2008'
           ,13.2
           ,423.12341234
           ,1243.1
           ,'ad0d39ad-6889-4ab3-965d-3d4042344ee6'
           ,12
           ,2
           ,1
           ,2
           ,'asdfvzxcv'
           ,'qwerertyizjgposgj'
           ,'January 1, 1900 00:00:00.100'
           ,12
           ,5342
           ,1234)", CommandType.Text)
                .AddQuery(@"INSERT INTO [dbo].[AllReferencesAndID_]
           ([BoolValue_]
           ,[ByteArrayValue_]
           ,[ByteValue_]
           ,[CharValue_]
           ,[DateTimeValue_]
           ,[DecimalValue_]
           ,[DoubleValue_]
           ,[FloatValue_]
           ,[GuidValue_]
           ,[IntValue_]
           ,[LongValue_]
           ,[SByteValue_]
           ,[ShortValue_]
           ,[StringValue1_]
           ,[StringValue2_]
           ,[TimeSpanValue_]
           ,[UIntValue_]
           ,[ULongValue_]
           ,[UShortValue_])
     VALUES
           (1
           ,1
           ,2
           ,'a'
           ,'1/1/2008'
           ,13.2
           ,423.12341234
           ,1243.1
           ,'ad0d39ad-6889-4ab3-965d-3d4042344ee6'
           ,13
           ,2
           ,1
           ,2
           ,'asdfvzxcv'
           ,'qwerertyizjgposgj'
           ,'January 1, 1900 00:00:00.100'
           ,12
           ,5342
           ,1234)", CommandType.Text)
                .AddQuery(@"INSERT INTO [dbo].[AllReferencesAndID_]
           ([BoolValue_]
           ,[ByteArrayValue_]
           ,[ByteValue_]
           ,[CharValue_]
           ,[DateTimeValue_]
           ,[DecimalValue_]
           ,[DoubleValue_]
           ,[FloatValue_]
           ,[GuidValue_]
           ,[IntValue_]
           ,[LongValue_]
           ,[SByteValue_]
           ,[ShortValue_]
           ,[StringValue1_]
           ,[StringValue2_]
           ,[TimeSpanValue_]
           ,[UIntValue_]
           ,[ULongValue_]
           ,[UShortValue_])
     VALUES
           (1
           ,1
           ,3
           ,'a'
           ,'1/1/2008'
           ,13.2
           ,423.12341234
           ,1243.1
           ,'ad0d39ad-6889-4ab3-965d-3d4042344ee6'
           ,14
           ,2
           ,1
           ,2
           ,'asdfvzxcv'
           ,'qwerertyizjgposgj'
           ,'January 1, 1900 00:00:00.100'
           ,12
           ,5342
           ,1234)", CommandType.Text)
           .AddQuery(@"INSERT INTO [dbo].[MapProperties_]
           ([BoolValue_],
           [AllReferencesAndID_MappedClass_ID_])
     VALUES
           (1
           ,1)", CommandType.Text)
           .AddQuery(@"INSERT INTO [dbo].[MapProperties_]
           ([BoolValue_],
           [AllReferencesAndID_MappedClass_ID_])
     VALUES
           (0
           ,2)", CommandType.Text)
           .AddQuery(@"INSERT INTO [dbo].[MapProperties_]
           ([BoolValue_],
           [AllReferencesAndID_MappedClass_ID_])
     VALUES
           (1
           ,3)", CommandType.Text)
                .ExecuteScalar<int>();
        }
    }
}