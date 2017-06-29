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

using BigBook.Queryable.BaseClasses;
using Inflatable.ClassMapper;
using Inflatable.LinqExpression;
using Inflatable.Sessions;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Inflatable
{
    /// <summary>
    /// Db context
    /// </summary>
    /// <typeparam name="TObject">The type of the object.</typeparam>
    /// <seealso cref="QueryProviderBase"/>
    public class DbContext<TObject> : QueryProviderBase
        where TObject : class
    {
        /// <summary>
        /// Executes the query represented by a specified expression tree.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        public override object Execute(Expression expression)
        {
            var TempSession = Canister.Builder.Bootstrapper.Resolve<Session>();
            var Results = Translate(expression);
            return TempSession.ExecuteAsync<TObject>(Results).Result;
        }

        /// <summary>
        /// Gets the query text.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>The query as a string</returns>
        public override string GetQueryText(Expression expression)
        {
            return Translate(expression).First().Value.ToString();
        }

        /// <summary>
        /// Translates the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        private IDictionary<MappingSource, QueryData<TObject>> Translate(Expression expression)
        {
            return Canister.Builder.Bootstrapper.Resolve<QueryTranslator<TObject>>().Translate(expression);
        }
    }
}