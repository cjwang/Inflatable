﻿/*
Copyright 2017 James Craig

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Inflatable.QueryProvider.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Data.SqlClient;

namespace Inflatable.QueryProvider.Providers.SQLServer
{
    /// <summary>
    /// SQL Server query provider
    /// </summary>
    /// <seealso cref="IQueryProvider"/>
    public class SQLServerQueryProvider : IQueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SQLServerQueryProvider"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="ArgumentNullException">configuration</exception>
        /// <exception cref="System.ArgumentNullException">configuration</exception>
        public SQLServerQueryProvider(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            CachedResults = new ConcurrentDictionary<Tuple<Type, MappingSource>, IGenerator>();
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Provider name associated with the query provider
        /// </summary>
        public DbProviderFactory Provider => SqlClientFactory.Instance;

        /// <summary>
        /// Gets or sets the dictionary.
        /// </summary>
        /// <value>The dictionary.</value>
        private ConcurrentDictionary<Tuple<Type, MappingSource>, IGenerator> CachedResults { get; }

        /// <summary>
        /// Creates a batch for running commands
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>A batch object</returns>
        public SQLHelper.SQLHelper Batch(IDatabase source)
        {
            return new SQLHelper.SQLHelper(Configuration, Provider, source.Name);
        }

        /// <summary>
        /// Creates a generator object
        /// </summary>
        /// <typeparam name="TMappedClass">Class type to create the generator for</typeparam>
        /// <param name="mappingInformation">The mapping information.</param>
        /// <returns>Generator object</returns>
        public IGenerator<TMappedClass> CreateGenerator<TMappedClass>(MappingSource mappingInformation)
            where TMappedClass : class
        {
            var Key = new Tuple<Type, MappingSource>(typeof(TMappedClass), mappingInformation);
            if (CachedResults.ContainsKey(Key))
            {
                return (IGenerator<TMappedClass>)CachedResults[Key];
            }

            var ReturnValue = new SQLServerGenerator<TMappedClass>(mappingInformation);
            CachedResults.AddOrUpdate(Key, ReturnValue, (x, y) => y);
            return ReturnValue;
        }
    }
}