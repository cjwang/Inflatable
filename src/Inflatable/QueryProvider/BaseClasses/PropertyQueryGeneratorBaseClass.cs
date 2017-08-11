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
using Inflatable.QueryProvider.Interfaces;

namespace Inflatable.QueryProvider.BaseClasses
{
    /// <summary>
    /// Property query generator base class
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <seealso cref="Inflatable.QueryProvider.BaseClasses.QueryGeneratorBaseClass{TObject}"/>
    /// <seealso cref="ILinqQueryGenerator{TObject}"/>
    public abstract class PropertyQueryGeneratorBaseClass<TObject> : QueryGeneratorBaseClass<TObject>, IPropertyQueryGenerator<TObject>
        where TObject : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyQueryGeneratorBaseClass{TObject}"/> class.
        /// </summary>
        /// <param name="mappingInformation">Mapping information</param>
        protected PropertyQueryGeneratorBaseClass(MappingSource mappingInformation)
            : base(mappingInformation)
        {
        }

        /// <summary>
        /// Generates the query.
        /// </summary>
        /// <param name="queryObject">The object to generate the queries from.</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>The resulting query</returns>
        public abstract IQuery GenerateQuery(TObject queryObject, string propertyName);

        /// <summary>
        /// Generates the query.
        /// </summary>
        /// <param name="queryObject">The object to generate the queries from.</param>
        /// <returns>The resulting query</returns>
        public override IQuery GenerateQuery(TObject queryObject)
        {
            return new Query(System.Data.CommandType.Text, "", QueryType);
        }
    }
}