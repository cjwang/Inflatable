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
using SQLHelperDB.HelperClasses.Interfaces;
using System;
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
            Queries = new ListMapping<string, QueryGeneratorData>();
            SetupQueries();
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
        private IEnumerable<IIDProperty> IDProperties { get; }

        /// <summary>
        /// Gets or sets the queries.
        /// </summary>
        /// <value>The queries.</value>
        private ListMapping<string, QueryGeneratorData> Queries { get; }

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
        /// <param name="property">The property.</param>
        /// <returns>The resulting query</returns>
        public override IQuery[] GenerateQueries(TMappedClass queryObject, IClassProperty property)
        {
            switch (property)
            {
                case IMapProperty TempMapProperty:
                    return MapProperty(TempMapProperty, queryObject);

                case IManyToManyProperty Property:
                    return ManyToManyProperty(Property, queryObject);

                case IManyToOneListProperty ManyToOne:
                    return ManyToOneProperty(ManyToOne, queryObject);

                case IManyToOneProperty ManyToOne:
                    return ManyToOneProperty(ManyToOne, queryObject);
            }

            return Array.Empty<IQuery>();
        }

        /// <summary>
        /// Generates the parameters.
        /// </summary>
        /// <param name="queryObject">The query object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The parameters</returns>
        private static IParameter[] GenerateParameters(TMappedClass queryObject, IPropertyColumns property)
        {
            var ColumnInfos = property.GetColumnInfo();
            var ReturnValues = new IParameter[ColumnInfos.Length];
            for (int x = 0; x < ColumnInfos.Length; ++x)
            {
                ReturnValues[x] = ColumnInfos[x].GetAsParameter(queryObject);
            }
            return ReturnValues;
        }

        /// <summary>
        /// Generates the parameters.
        /// </summary>
        /// <param name="queryObject">The query object.</param>
        /// <param name="property">The property.</param>
        /// <param name="propertyItem">The property item.</param>
        /// <returns>The parameters</returns>
        private static IParameter[] GenerateParameters(TMappedClass queryObject, IPropertyColumns property, object propertyItem)
        {
            var ColumnInfos = property.GetColumnInfo();
            var ReturnValues = new IParameter[ColumnInfos.Length];
            for (int x = 0; x < ColumnInfos.Length; ++x)
            {
                ReturnValues[x] = ColumnInfos[x].GetAsParameter(queryObject, propertyItem);
            }
            return ReturnValues;
        }

        /// <summary>
        /// Generates from clause.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns></returns>
        private string GenerateFromClause(Utils.TreeNode<Type> node)
        {
            var Result = new StringBuilder();
            var Mapping = MappingInformation.Mappings[node.Data];
            for (int x = 0, nodeNodesCount = node.Nodes.Count; x < nodeNodesCount; x++)
            {
                var ParentNode = node.Nodes[x];
                var ParentMapping = MappingInformation.Mappings[ParentNode.Data];
                var TempIDProperties = new StringBuilder();
                string Separator = "";
                foreach (var IDProperty in ParentMapping.IDProperties)
                {
                    TempIDProperties.AppendFormat("{0}{1}={2}", Separator, GetParentColumnName(Mapping, IDProperty), GetColumnName(IDProperty));
                    Separator = " AND ";
                }
                foreach (var IDProperty in ParentMapping.AutoIDProperties)
                {
                    TempIDProperties.AppendFormat("{0}{1}={2}", Separator, GetParentColumnName(Mapping, IDProperty), GetColumnName(IDProperty));
                    Separator = " AND ";
                }
                Result.AppendFormat(" INNER JOIN {0} ON {1}", GetTableName(ParentMapping), TempIDProperties);
                Result.Append(GenerateFromClause(ParentNode));
            }

            return Result.ToString();
        }

        /// <summary>
        /// Generates the join save query.
        /// </summary>
        /// <param name="foreignIDProperties">The foreign identifier properties.</param>
        /// <param name="mapProperty">The map property.</param>
        /// <returns></returns>
        private string GenerateJoinSaveQuery(IEnumerable<IIDProperty> foreignIDProperties, IMapProperty mapProperty)
        {
            var Builder = new StringBuilder();
            var WhereList = new StringBuilder();
            var ParametersList = new StringBuilder();
            var FromList = new StringBuilder();
            string Splitter2 = "";
            foreach (var ForeignID in foreignIDProperties)
            {
                ParametersList
                    .Append(Splitter2)
                    .Append(GetTableName(mapProperty.ParentMapping))
                    .Append(".[")
                    .Append(mapProperty.ForeignMapping.TableName)
                    .Append(mapProperty.ParentMapping.Prefix)
                    .Append(mapProperty.Name)
                    .Append(mapProperty.ParentMapping.Suffix)
                    .Append(ForeignID.ColumnName)
                    .Append("] = @")
                    .Append(mapProperty.ForeignMapping.TableName)
                    .Append(mapProperty.ParentMapping.Prefix)
                    .Append(mapProperty.Name)
                    .Append(mapProperty.ParentMapping.Suffix)
                    .Append(ForeignID.ColumnName);
                Splitter2 = " AND ";
            }
            Splitter2 = "";
            foreach (var IDProperty in IDProperties)
            {
                WhereList.Append(Splitter2).Append(GetColumnName(IDProperty)).Append(" = ").Append(GetParameterName(IDProperty));
                Splitter2 = " AND ";
            }
            FromList.Append(GetTableName(mapProperty.ParentMapping));
            FromList.Append(GenerateFromClause(MappingInformation.TypeGraphs[mapProperty.ParentMapping.ObjectType].Root));

            Builder.AppendFormat("UPDATE {0} SET {1} FROM {2} WHERE {3};", GetTableName(mapProperty.ParentMapping), ParametersList, FromList, WhereList);
            return Builder.ToString();
        }

        /// <summary>
        /// Generates the join save query.
        /// </summary>
        /// <param name="foreignIDProperties">The foreign identifier properties.</param>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        private string GenerateJoinSaveQuery(IEnumerable<IIDProperty> foreignIDProperties, IManyToManyProperty property)
        {
            var Builder = new StringBuilder();
            var PropertyNames = new StringBuilder();
            var PropertyValues = new StringBuilder();
            var ParametersList = new StringBuilder();
            string Splitter = "";
            foreach (var ForeignID in foreignIDProperties)
            {
                PropertyNames.Append(Splitter).Append("[").Append(property.ParentMapping.SchemaName).Append("].[").Append(property.TableName).Append("].[").Append(ForeignID.ParentMapping.TableName).Append(ForeignID.ColumnName).Append("]");
                PropertyValues.Append(Splitter).Append("@").Append(ForeignID.ParentMapping.TableName).Append(ForeignID.ColumnName);
                Splitter = ",";
            }
            string Prefix = "";
            if (IDProperties.Any(x => x.ParentMapping == property.ForeignMapping))
            {
                Prefix = "Parent_";
            }

            foreach (var IDProperty in IDProperties)
            {
                PropertyNames.Append(Splitter).Append("[").Append(property.ParentMapping.SchemaName).Append("].[").Append(property.TableName).Append("].[").Append(Prefix).Append(IDProperty.ParentMapping.TableName).Append(IDProperty.ColumnName).Append("]");
                PropertyValues.Append(Splitter).Append("@").Append(Prefix).Append(IDProperty.ParentMapping.TableName).Append(IDProperty.ColumnName);
                Splitter = ",";
            }
            Builder.AppendFormat("INSERT INTO {0}({1}) VALUES ({2});", GetTableName(property), PropertyNames, PropertyValues);
            return Builder.ToString();
        }

        private string GenerateJoinSaveQuery(IEnumerable<IIDProperty> foreignIDProperties, IManyToOneProperty manyToOne)
        {
            var Builder = new StringBuilder();
            var WhereList = new StringBuilder();
            var ParametersList = new StringBuilder();
            var FromList = new StringBuilder();
            string TableName = "";
            if (manyToOne is IManyToOneListProperty)
            {
                GenerateJoinSaveQueryMultiple(manyToOne, WhereList, ParametersList, FromList);
                TableName = GetTableName(manyToOne.ForeignMapping);
            }
            else
            {
                GenerateJoinSaveQuerySingle(foreignIDProperties, manyToOne, WhereList, ParametersList, FromList);
                TableName = GetTableName(manyToOne.ParentMapping);
            }
            Builder.AppendFormat("UPDATE {0} SET {1} FROM {2} WHERE {3};", TableName, ParametersList, FromList, WhereList);
            return Builder.ToString();
        }

        /// <summary>
        /// Generates the join save query multiple.
        /// </summary>
        /// <param name="manyToOne">The many to one.</param>
        /// <param name="whereList">The where list.</param>
        /// <param name="parametersList">The parameters list.</param>
        /// <param name="fromList">From list.</param>
        private void GenerateJoinSaveQueryMultiple(IManyToOneProperty manyToOne,
                    StringBuilder whereList,
                    StringBuilder parametersList,
                    StringBuilder fromList)
        {
            string Splitter = "";
            foreach (var ForeignIDs in manyToOne.GetColumnInfo().Where(x => x.IsForeign))
            {
                parametersList.Append(Splitter).Append("[").Append(ForeignIDs.SchemaName).Append("].[").Append(ForeignIDs.TableName).Append("].[").Append(ForeignIDs.ColumnName).Append("] = @").Append(ForeignIDs.ColumnName);
                Splitter = " AND ";
            }
            Splitter = "";
            foreach (var ForeignIDs in manyToOne.GetColumnInfo().Where(x => !x.IsForeign))
            {
                whereList.Append(Splitter).Append("[").Append(ForeignIDs.SchemaName).Append("].[").Append(ForeignIDs.TableName).Append("].[").Append(ForeignIDs.ColumnName).Append("] = @").Append(ForeignIDs.ColumnName);
                Splitter = " AND ";
            }
            fromList.Append(GetTableName(manyToOne.ForeignMapping));
            fromList.Append(GenerateFromClause(MappingInformation.TypeGraphs[manyToOne.ForeignMapping.ObjectType].Root));
        }

        /// <summary>
        /// Generates the join save query single.
        /// </summary>
        /// <param name="foreignIDProperties">The foreign identifier properties.</param>
        /// <param name="manyToOne">The many to one.</param>
        /// <param name="whereList">The where list.</param>
        /// <param name="parametersList">The parameters list.</param>
        /// <param name="fromList">From list.</param>
        private void GenerateJoinSaveQuerySingle(IEnumerable<IIDProperty> foreignIDProperties,
                    IManyToOneProperty manyToOne,
                    StringBuilder whereList,
                    StringBuilder parametersList,
                    StringBuilder fromList)
        {
            string Splitter = "";
            foreach (var ForeignID in foreignIDProperties)
            {
                parametersList.Append(Splitter).Append(GetTableName(manyToOne.ParentMapping)
).Append(".["
).Append(manyToOne.ColumnName
).Append(manyToOne.ForeignMapping.TableName
).Append(ForeignID.ColumnName
).Append("] = @"
).Append(manyToOne.ColumnName
).Append(manyToOne.ForeignMapping.TableName
).Append(ForeignID.ColumnName);
                Splitter = " AND ";
            }
            Splitter = "";
            foreach (var IDProperty in IDProperties)
            {
                whereList.Append(Splitter).Append(GetColumnName(IDProperty)).Append(" = ").Append(GetParameterName(IDProperty));
                Splitter = " AND ";
            }
            fromList.Append(GetTableName(manyToOne.ParentMapping));
            fromList.Append(GenerateFromClause(MappingInformation.TypeGraphs[manyToOne.ParentMapping.ObjectType].Root));
        }

        /// <summary>
        /// Manies to many property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="queryObject">The query object.</param>
        /// <returns></returns>
        private IQuery[] ManyToManyProperty(IManyToManyProperty property, TMappedClass queryObject)
        {
            if (!Queries.ContainsKey(property.Name))
            {
                var ForeignMappings = MappingInformation.GetChildMappings(property.PropertyType)
                                            .SelectMany(x => MappingInformation.GetParentMapping(x.ObjectType))
                                            .Distinct();
                var ForeignIDProperties = ForeignMappings.SelectMany(x => x.IDProperties);

                foreach (var ForeignMapping in ForeignMappings)
                {
                    Queries.Add(property.Name, new QueryGeneratorData
                    {
                        AssociatedMapping = MappingInformation.Mappings[ForeignMapping.ObjectType],
                        IDProperties = IDProperties,
                        QueryText = GenerateJoinSaveQuery(ForeignIDProperties, property)
                    });
                }
            }
            if (!(property.GetValue(queryObject) is IEnumerable ItemList))
            {
                return Array.Empty<IQuery>();
            }

            var ReturnValue = new List<IQuery>();
            foreach (var TempQuery in Queries[property.Name])
            {
                foreach (var Item in ItemList)
                {
                    ReturnValue.Add(new Query(TempQuery.AssociatedMapping.ObjectType,
                        CommandType.Text,
                        TempQuery.QueryText,
                        QueryType,
                        GenerateParameters(queryObject, property, Item)));
                }
            }
            return ReturnValue.ToArray();
        }

        /// <summary>
        /// Manies to one property.
        /// </summary>
        /// <param name="manyToOne">The many to one.</param>
        /// <param name="queryObject">The query object.</param>
        /// <returns></returns>
        private IQuery[] ManyToOneProperty(IManyToOneListProperty manyToOne, TMappedClass queryObject)
        {
            if (!Queries.ContainsKey(manyToOne.Name))
            {
                var ForeignMappings = MappingInformation.GetChildMappings(manyToOne.PropertyType)
                                            .SelectMany(x => MappingInformation.GetParentMapping(x.ObjectType))
                                            .Distinct();
                var ForeignIDProperties = ForeignMappings.SelectMany(x => x.IDProperties);

                foreach (var ForeignMapping in ForeignMappings)
                {
                    Queries.Add(manyToOne.Name, new QueryGeneratorData
                    {
                        AssociatedMapping = MappingInformation.Mappings[ForeignMapping.ObjectType],
                        IDProperties = IDProperties,
                        QueryText = GenerateJoinSaveQuery(ForeignIDProperties, manyToOne)
                    });
                }
            }
            if (!(manyToOne.GetValue(queryObject) is IEnumerable ItemList))
            {
                return Array.Empty<IQuery>();
            }

            var ReturnValue = new List<IQuery>();
            foreach (var TempQuery in Queries[manyToOne.Name])
            {
                foreach (var Item in ItemList)
                {
                    ReturnValue.Add(new Query(TempQuery.AssociatedMapping.ObjectType,
                        CommandType.Text,
                        TempQuery.QueryText,
                        QueryType,
                        GenerateParameters(queryObject, manyToOne, Item)));
                }
            }
            return ReturnValue.ToArray();
        }

        /// <summary>
        /// Manies to one property.
        /// </summary>
        /// <param name="manyToOne">The many to one.</param>
        /// <param name="queryObject">The query object.</param>
        /// <returns></returns>
        private IQuery[] ManyToOneProperty(IManyToOneProperty manyToOne, TMappedClass queryObject)
        {
            var ItemValue = manyToOne.GetValue(queryObject);

            if (!Queries.ContainsKey(manyToOne.Name))
            {
                var ForeignMappings = MappingInformation.GetChildMappings(manyToOne.PropertyType)
                                            .SelectMany(x => MappingInformation.GetParentMapping(x.ObjectType))
                                            .Distinct();
                var ForeignIDProperties = ForeignMappings
                                            .SelectMany(x => x.IDProperties);

                foreach (var ForeignMapping in ForeignMappings)
                {
                    Queries.Add(manyToOne.Name, new QueryGeneratorData
                    {
                        AssociatedMapping = MappingInformation.Mappings[ForeignMapping.ObjectType],
                        IDProperties = IDProperties,
                        QueryText = GenerateJoinSaveQuery(ForeignIDProperties, manyToOne)
                    });
                }
            }
            var ReturnValue = new List<IQuery>();
            foreach (var TempQuery in Queries[manyToOne.Name])
            {
                ReturnValue.Add(new Query(TempQuery.AssociatedMapping.ObjectType,
                    CommandType.Text,
                    TempQuery.QueryText,
                    QueryType,
                    GenerateParameters(queryObject, manyToOne, ItemValue)));
            }
            return ReturnValue.ToArray();
        }

        /// <summary>
        /// Maps the property.
        /// </summary>
        /// <param name="mapProperty">The map property.</param>
        /// <param name="queryObject">The query object.</param>
        /// <returns></returns>
        private IQuery[] MapProperty(IMapProperty mapProperty, TMappedClass queryObject)
        {
            var ItemValue = mapProperty.GetValue(queryObject);

            if (!Queries.ContainsKey(mapProperty.Name))
            {
                var ForeignMappings = MappingInformation.GetChildMappings(mapProperty.PropertyType)
                                            .SelectMany(x => MappingInformation.GetParentMapping(x.ObjectType))
                                            .Distinct();
                var ForeignIDProperties = ForeignMappings
                                            .SelectMany(x => x.IDProperties);

                foreach (var ForeignMapping in ForeignMappings)
                {
                    Queries.Add(mapProperty.Name, new QueryGeneratorData
                    {
                        AssociatedMapping = MappingInformation.Mappings[ForeignMapping.ObjectType],
                        IDProperties = IDProperties,
                        QueryText = GenerateJoinSaveQuery(ForeignIDProperties, mapProperty)
                    });
                }
            }
            var ReturnValue = new List<IQuery>();
            foreach (var TempQuery in Queries[mapProperty.Name])
            {
                ReturnValue.Add(new Query(TempQuery.AssociatedMapping.ObjectType,
                    CommandType.Text,
                    TempQuery.QueryText,
                    QueryType,
                    GenerateParameters(queryObject, mapProperty)));
            }
            return ReturnValue.ToArray();
        }

        /// <summary>
        /// Sets up the queries.
        /// </summary>
        private void SetupQueries()
        {
            foreach (var ParentMapping in MappingInformation.GetParentMapping(typeof(TMappedClass)))
            {
                foreach (var Property in ParentMapping.ManyToManyProperties)
                {
                    ManyToManyProperty(Property, default(TMappedClass));
                }
                foreach (var Property in ParentMapping.ManyToOneProperties)
                {
                    switch (Property)
                    {
                        case IManyToOneListProperty ManyToOne:
                            ManyToOneProperty(ManyToOne, default(TMappedClass));
                            break;

                        case IManyToOneProperty ManyToOne:
                            ManyToOneProperty(ManyToOne, default(TMappedClass));
                            break;
                    }
                }
                foreach (var Property in ParentMapping.MapProperties)
                {
                    MapProperty(Property, default(TMappedClass));
                }
            }
        }
    }
}