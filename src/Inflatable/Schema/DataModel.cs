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

using BigBook;
using Data.Modeler;
using Data.Modeler.Providers.Interfaces;
using Holmes;
using Inflatable.ClassMapper;
using Inflatable.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using SQLHelperDB;
using SQLHelperDB.HelperClasses;
using SQLHelperDB.HelperClasses.Interfaces;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Inflatable.Schema
{
    /// <summary>
    /// Data model class
    /// </summary>
    public class DataModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataModel"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="dataModeler">The data modeler.</param>
        /// <param name="sherlock">The sherlock.</param>
        /// <param name="batch">The batch.</param>
        /// <exception cref="ArgumentNullException">source or config or logger</exception>
        public DataModel(IMappingSource source, IConfiguration config, ILogger logger, DataModeler dataModeler, Sherlock sherlock, SQLHelper batch)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            logger ??= Log.Logger ?? new LoggerConfiguration().CreateLogger() ?? throw new ArgumentNullException(nameof(logger));

            var sourceConnection = new Connection(config, source.Source.Provider, source.Source.Name);
            var sourceSpec = DataModeler.CreateSource(sourceConnection.DatabaseName ?? string.Empty);
            SourceSpec = sourceSpec;
            GeneratedSchemaChanges = Task.Run(async () => await GenerateSchemaAsync(source, logger, dataModeler, sourceConnection, sourceSpec).ConfigureAwait(false)).GetAwaiter().GetResult();
            Task.Run(async () => await AnalyzeSchemaAsync(sherlock, logger, source, sourceConnection, batch).ConfigureAwait(false)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The default schemas
        /// </summary>
        private static readonly string[] DefaultSchemas = {
            "dbo",
            "guest",
            "INFORMATION_SCHEMA",
            "sys",
            "db_owner",
            "db_accessadmin",
            "db_securityadmin",
            "db_ddladmin",
            "db_backupoperator",
            "db_datareader",
            "db_datawriter",
            "db_denydatareader",
            "db_denydatawriter"
        };

        /// <summary>
        /// Gets the generated schema changes.
        /// </summary>
        /// <value>The generated schema changes.</value>
        public string[] GeneratedSchemaChanges { get; }

        /// <summary>
        /// Gets the source spec.
        /// </summary>
        /// <value>The source spec.</value>
        public ISource SourceSpec { get; }

        /// <summary>
        /// Analyze the schema.
        /// </summary>
        /// <param name="sherlock">The sherlock.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="source">The source.</param>
        /// <param name="sourceConnection">The source connection.</param>
        /// <param name="batch">The batch.</param>
        private static async Task AnalyzeSchemaAsync(Sherlock sherlock, ILogger logger, IMappingSource source, IConnection sourceConnection, SQLHelper batch)
        {
            if (!source.ApplyAnalysis
                && !source.GenerateAnalysis)
            {
                return;
            }

            logger.Information("Analyzing {Info:l} for suggestions.", sourceConnection.DatabaseName);
            var Results = await sherlock.AnalyzeAsync(sourceConnection).ConfigureAwait(false);
            batch.CreateBatch();
            foreach (var Result in Results)
            {
                logger.Information("Finding: {Info:l}", Result.Text);
                logger.Information("Metrics: {Data:l}", Result.Metrics);
                logger.Information("Suggested Fix: {Fix:l}", Result.Fix);
                if (source.ApplyAnalysis && string.IsNullOrEmpty(Result.Fix))
                {
                    batch.AddQuery(CommandType.Text, Result.Fix);
                }
            }
            if (source.ApplyAnalysis)
            {
                logger.Information("Applying fixes for {Info:l}.", sourceConnection.DatabaseName);
                await batch.ExecuteScalarAsync<int>().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Generates the schema.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="dataModeler">The data modeler.</param>
        /// <param name="sourceConnection">The source connection.</param>
        /// <param name="sourceSpec">The source spec.</param>
        /// <returns>The generated schema changes.</returns>
        private static async Task<string[]> GenerateSchemaAsync(IMappingSource source, ILogger logger, DataModeler dataModeler, IConnection sourceConnection, ISource sourceSpec)
        {
            if (!source.UpdateSchema
                && !source.GenerateSchema)
            {
                return Array.Empty<string>();
            }

            var Debug = logger.IsEnabled(LogEventLevel.Debug);

            var Generator = dataModeler.GetSchemaGenerator(source.Source.Provider);
            if (Generator is null)
                return Array.Empty<string>();

            logger.Information("Getting structure for {Info:l}", sourceConnection.DatabaseName);
            var OriginalSource = !string.IsNullOrEmpty(sourceConnection.DatabaseName) ? (await Generator.GetSourceStructureAsync(sourceConnection).ConfigureAwait(false)) : null;

            SetupTableStructures(logger, sourceConnection, source, sourceSpec);

            logger.Information("Generating schema changes for {Info:l}", sourceConnection.DatabaseName);
            var GeneratedSchemaChanges = Generator.GenerateSchema(sourceSpec, OriginalSource!) ?? Array.Empty<string>();
            if (Debug)
            {
                logger.Debug("Schema changes generated: {GeneratedSchemaChanges}", GeneratedSchemaChanges);
            }

            if (!source.UpdateSchema)
            {
                return GeneratedSchemaChanges;
            }

            logger.Information("Applying schema changes for {Info:l}", sourceConnection.DatabaseName);
            await Generator.SetupAsync(GeneratedSchemaChanges, sourceConnection).ConfigureAwait(false);
            return GeneratedSchemaChanges;
        }

        /// <summary>
        /// Sets up the foreign keys.
        /// </summary>
        private static void SetupForeignKeys(ILogger logger, IConnection sourceConnection, ISource sourceSpec)
        {
            logger.Information("Setting up foreign keys for {Info:l}", sourceConnection.DatabaseName);
            sourceSpec.Tables.ForEach(x => x.SetupForeignKeys());
        }

        /// <summary>
        /// Sets up the tables.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sourceConnection">The source connection.</param>
        /// <param name="source">The source.</param>
        /// <param name="sourceSpec">The source spec.</param>
        private static void SetupTables(ILogger logger, IConnection sourceConnection, IMappingSource source, ISource sourceSpec)
        {
            logger.Information("Setting up table structure for {Info:l}", sourceConnection.DatabaseName);
            foreach (var Mapping in source.Mappings.Values.OrderBy(x => x.Order))
            {
                if (!DefaultSchemas.Contains(Mapping.SchemaName))
                {
                    sourceSpec.Schemas.AddIfUnique(Mapping.SchemaName);
                }

                var Table = sourceSpec.AddTable(Mapping.TableName, Mapping.SchemaName);
                var Tree = source.TypeGraphs[Mapping.ObjectType];
                var ParentMappings = Tree?.Root.Nodes.ForEach(x => source.Mappings[x.Data]) ?? Array.Empty<IMapping>();
                foreach (var ID in Mapping.IDProperties)
                {
                    ID.Setup();
                    ID.AddToTable(Table);
                }
                foreach (var ID in Mapping.AutoIDProperties)
                {
                    ID.Setup();
                    ID.AddToTable(Table);
                }
                foreach (var Reference in Mapping.ReferenceProperties)
                {
                    Reference.Setup();
                    Reference.AddToTable(Table);
                }
                foreach (var Map in Mapping.MapProperties)
                {
                    Map.Setup(source);
                    Map.AddToTable(Table);
                }
                foreach (var Map in Mapping.ManyToManyProperties)
                {
                    Map.Setup(source, sourceSpec);
                }
                foreach (var ParentMapping in ParentMappings)
                {
                    foreach (var ID in ParentMapping.IDProperties)
                    {
                        ID.Setup();
                        ID.AddToChildTable(Table);
                    }
                    foreach (var ID in ParentMapping.AutoIDProperties)
                    {
                        ID.Setup();
                        ID.AddToChildTable(Table);
                    }
                }
            }
            foreach (var Mapping in source.Mappings.Values.OrderBy(x => x.Order))
            {
                foreach (var Map in Mapping.ManyToOneProperties)
                {
                    Map.Setup(source, sourceSpec);
                }
            }
            foreach (var Mapping in source.Mappings.Values.OrderBy(x => x.Order))
            {
                foreach (var Map in Mapping.ManyToOneProperties)
                {
                    Map.SetColumnInfo(source);
                }
                foreach (var Map in Mapping.ManyToManyProperties)
                {
                    Map.SetColumnInfo(source);
                }
                foreach (var Map in Mapping.MapProperties)
                {
                    Map.SetColumnInfo(source);
                }
            }
        }

        /// <summary>
        /// Sets up the table structures.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="sourceConnection">The source connection.</param>
        /// <param name="mappingSource">The mapping source.</param>
        /// <param name="sourceSpec">The source spec.</param>
        private static void SetupTableStructures(ILogger logger, IConnection sourceConnection, IMappingSource mappingSource, ISource sourceSpec)
        {
            SetupTables(logger, sourceConnection, mappingSource, sourceSpec);
            SetupForeignKeys(logger, sourceConnection, sourceSpec);
        }
    }
}