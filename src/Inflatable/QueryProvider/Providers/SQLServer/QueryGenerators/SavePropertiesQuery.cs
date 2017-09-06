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
using Inflatable.ClassMapper;
using Inflatable.ClassMapper.Interfaces;
using Inflatable.QueryProvider.BaseClasses;
using Inflatable.QueryProvider.Enums;
using Inflatable.QueryProvider.Interfaces;
using Inflatable.QueryProvider.Providers.SQLServer.QueryGenerators.HelperClasses;
using SQLHelper.HelperClasses.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Inflatable.QueryProvider.Providers.SQLServer.QueryGenerators
{
    /// <summary>
    /// Save properties query generator
    /// </summary>
    /// <typeparam name="TMappedClass">The type of the mapped class.</typeparam>
    /// <seealso cref="BaseClasses.PropertyQueryGeneratorBaseClass{TMappedClass}"/>
    public class SavePropertiesQuery<TMappedClass> : PropertyQueryGeneratorBaseClass<TMappedClass>
        where TMappedClass : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SavePropertiesQuery{TMappedClass}"/> class.
        /// </summary>
        /// <param name="mappingInformation">Mapping information</param>
        public SavePropertiesQuery(MappingSource mappingInformation)
            : base(mappingInformation)
        {
            IDProperties = MappingInformation.GetChildMappings(typeof(TMappedClass))
                                             .SelectMany(x => MappingInformation.GetParentMapping(x.ObjectType))
                                             .Distinct()
                                             .SelectMany(x => x.IDProperties);
            Queries = new Dictionary<string, List<QueryGeneratorData>>();
        }

        /// <summary>
        /// Gets the type of the query.
        /// </summary>
        /// <value>The type of the query.</value>
        public override QueryType QueryType => QueryType.JoinsSave;

        /// <summary>
        /// Gets the identifier properties.
        /// </summary>
        /// <value>The identifier properties.</value>
        private IEnumerable<IIDProperty> IDProperties { get; set; }

        /// <summary>
        /// Gets or sets the queries.
        /// </summary>
        /// <value>The queries.</value>
        private IDictionary<string, List<QueryGeneratorData>> Queries { get; set; }

        /// <summary>
        /// Generates the declarations needed for the query.
        /// </summary>
        /// <returns>The resulting declarations.</returns>
        public override IQuery[] GenerateDeclarations()
        {
            return new IQuery[] { new Query(AssociatedType, CommandType.Text, "", QueryType) };
        }

        /// <summary>
        /// Generates the query.
        /// </summary>
        /// <param name="queryObject">The object to generate the queries from.</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>The resulting query</returns>
        public override IQuery[] GenerateQueries(TMappedClass queryObject, string propertyName)
        {
            var ParentMappings = MappingInformation.GetParentMapping(AssociatedType);
            var Property = ParentMappings.SelectMany(x => x.ManyToManyProperties).FirstOrDefault(x => x.Name == propertyName);

            if (Property != null)
                return ManyToManyProperty(Property, queryObject);

            return new IQuery[0];
        }

        private string GenerateJoinSaveQuery(IEnumerable<IIDProperty> foreignIDProperties, IManyToManyProperty property)
        {
            StringBuilder Builder = new StringBuilder();
            StringBuilder PropertyNames = new StringBuilder();
            StringBuilder PropertyValues = new StringBuilder();
            StringBuilder ParametersList = new StringBuilder();
            string Splitter = "";
            string Splitter2 = "";
            foreach (var ForeignID in foreignIDProperties)
            {
                PropertyNames.Append(Splitter).Append("[" + property.ParentMapping.SchemaName + "].[" + property.TableName + "].[" + ForeignID.ParentMapping.TableName + ForeignID.ColumnName + "]");
                PropertyValues.Append(Splitter).Append("@" + ForeignID.ParentMapping.TableName + ForeignID.ColumnName);
                ParametersList.Append(Splitter2).Append("[" + property.ParentMapping.SchemaName + "].[" + property.TableName + "].[" + ForeignID.ParentMapping.TableName + ForeignID.ColumnName + "] = @" + ForeignID.ParentMapping.TableName + ForeignID.ColumnName);
                Splitter = ",";
                Splitter2 = " AND ";
            }
            foreach (var IDProperty in IDProperties)
            {
                PropertyNames.Append(Splitter).Append("[" + property.ParentMapping.SchemaName + "].[" + property.TableName + "].[" + IDProperty.ParentMapping.TableName + IDProperty.ColumnName + "]");
                PropertyValues.Append(Splitter).Append("@" + IDProperty.ParentMapping.TableName + IDProperty.ColumnName);
                ParametersList.Append(Splitter2).Append("[" + property.ParentMapping.SchemaName + "].[" + property.TableName + "].[" + IDProperty.ParentMapping.TableName + IDProperty.ColumnName + "] = @" + IDProperty.ParentMapping.TableName + IDProperty.ColumnName);
                Splitter = ",";
                Splitter2 = " AND ";
            }
            Builder.AppendFormat("IF NOT EXISTS (SELECT * FROM {0} WHERE {3}) BEGIN INSERT INTO {0}({1}) VALUES ({2}) END;", GetTableName(property), PropertyNames, PropertyValues, ParametersList);
            return Builder.ToString();
        }

        private IParameter[] GenerateParameters(TMappedClass queryObject, IManyToManyProperty property, object propertyItem)
        {
            List<IParameter> ReturnValues = new List<IParameter>();
            ReturnValues.AddRange(property.GetAsParameter(queryObject, propertyItem));
            return ReturnValues.ToArray();
        }

        private IQuery[] ManyToManyProperty(IManyToManyProperty property, TMappedClass queryObject)
        {
            var ForeignIDProperties = MappingInformation.GetChildMappings(property.PropertyType)
                                            .SelectMany(x => MappingInformation.GetParentMapping(x.ObjectType))
                                            .Distinct()
                                            .SelectMany(x => x.IDProperties);
            var ItemList = property.GetValue(queryObject) as IEnumerable;

            if (!Queries.ContainsKey(property.Name))
            {
                var ChildMappings = MappingInformation.GetChildMappings(property.PropertyType);

                Queries.Add(property.Name, new List<QueryGeneratorData>());
                foreach (var ChildMapping in ChildMappings)
                {
                    Queries[property.Name].Add(new QueryGeneratorData
                    {
                        AssociatedMapping = ChildMapping,
                        IDProperties = IDProperties,
                        QueryText = GenerateJoinSaveQuery(ForeignIDProperties, property)
                    });
                }
            }
            if (ItemList == null)
                return new IQuery[0];

            List<IQuery> ReturnValue = new List<IQuery>();
            foreach (var TempQuery in Queries[property.Name])
            {
                foreach (var Item in ItemList)
                {
                    ReturnValue.Add(new Query(TempQuery.AssociatedMapping.ObjectType,
                        CommandType.Text,
                        TempQuery.QueryText,
                        QueryType.JoinsSave,
                        GenerateParameters(queryObject, property, Item)));
                }
            }
            return ReturnValue.ToArray();
        }
    }
}