﻿using Canister.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Inflatable.Tests.Modules
{
    public class ConfigurationModule : IModule
    {
        public int Order => 1;

        protected static string ConnectionString => "Data Source=localhost;Initial Catalog=TestDatabase;Integrated Security=SSPI;Pooling=false";

        protected static string ConnectionString2 => "Data Source=localhost;Initial Catalog=TestDatabase2;Integrated Security=SSPI;Pooling=false";

        protected static string MockDatabaseConnectionString => "Data Source=localhost;Initial Catalog=MockDatabase;Integrated Security=SSPI;Pooling=false";
        protected static string MockDatabaseForMockMappingConnectionString => "Data Source=localhost;Initial Catalog=MockDatabaseForMockMapping;Integrated Security=SSPI;Pooling=false";

        public void Load(IBootstrapper bootstrapper)
        {
            if (bootstrapper == null)
            {
                return;
            }

            var dict = new Dictionary<string, string>
                {
                    { "ConnectionStrings:Default", ConnectionString },
                    { "ConnectionStrings:Default2", ConnectionString2 },
                    { "ConnectionStrings:MockDatabase",MockDatabaseConnectionString },
                    { "ConnectionStrings:MockDatabaseForMockMapping",MockDatabaseForMockMappingConnectionString }
                };
            var Configuration = new ConfigurationBuilder()
                             .AddInMemoryCollection(dict)
                             .Build();
            bootstrapper.Register<IConfiguration>(Configuration, ServiceLifetime.Singleton);
            bootstrapper.Register(Configuration, ServiceLifetime.Singleton);
        }
    }
}