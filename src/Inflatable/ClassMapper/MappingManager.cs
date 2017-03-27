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
using Inflatable.ClassMapper.TypeGraph;
using Inflatable.Interfaces;
using Inflatable.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Inflatable.ClassMapper
{
    /// <summary>
    /// Mapping manager
    /// </summary>
    public class MappingManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingManager"/> class.
        /// </summary>
        /// <param name="mappings">The mappings.</param>
        public MappingManager(IEnumerable<IMapping> mappings)
        {
            mappings = mappings ?? new List<IMapping>();
            Mappings = new ConcurrentDictionary<Type, IMapping>();
            TypeGraphs = new ConcurrentDictionary<Type, Tree<Type>>();
            ChildTypes = new ListMapping<Type, Type>();
            ParentTypes = new ListMapping<Type, Type>();
            AddMappings(mappings);
            SetupTypeGraphs();
            IEnumerable<Type> ConcreteTypes = SetupChildTypes();
            MergeMappings();
            SetupParentTypes(ConcreteTypes);
            ReduceMappings();
        }

        /// <summary>
        /// Gets the child types.
        /// </summary>
        /// <value>The child types.</value>
        public ListMapping<Type, Type> ChildTypes { get; private set; }

        /// <summary>
        /// Gets or sets the mappings.
        /// </summary>
        /// <value>The mappings.</value>
        public IDictionary<Type, IMapping> Mappings { get; private set; }

        /// <summary>
        /// Gets the parent types.
        /// </summary>
        /// <value>The parent types.</value>
        public ListMapping<Type, Type> ParentTypes { get; private set; }

        /// <summary>
        /// Gets or sets the type graph.
        /// </summary>
        /// <value>The type graph.</value>
        public IDictionary<Type, Tree<Type>> TypeGraphs { get; private set; }

        /// <summary>
        /// Adds the mappings.
        /// </summary>
        /// <param name="mappings">The mappings.</param>
        private void AddMappings(IEnumerable<IMapping> mappings)
        {
            foreach (var Mapping in mappings)
            {
                Mappings.Add(Mapping.ObjectType, Mapping);
            }
        }

        /// <summary>
        /// Merges the mappings.
        /// </summary>
        private void MergeMappings()
        {
            var MappingMerger = new MergeMappings(Mappings);
            foreach (var TempTypeGraph in TypeGraphs.Values)
            {
                MappingMerger.Merge(TempTypeGraph);
            }
        }

        /// <summary>
        /// Reduces the mappings.
        /// </summary>
        private void ReduceMappings()
        {
            var ReduceMapping = new ReduceMappings(Mappings);
            foreach (var TempTypeGraph in TypeGraphs.Values)
            {
                ReduceMapping.Reduce(TempTypeGraph);
            }
        }

        /// <summary>
        /// Sets up the child types.
        /// </summary>
        /// <returns>The concrete types found</returns>
        private IEnumerable<Type> SetupChildTypes()
        {
            var TempConcreteDiscoverer = new DiscoverConcreteTypes(TypeGraphs);
            var ConcreteTypes = TempConcreteDiscoverer.FindConcreteTypes();
            foreach (var ConcreteType in ConcreteTypes)
            {
                var Parents = TypeGraphs[ConcreteType].ToList();
                foreach (var Parent in Parents)
                {
                    ChildTypes.Add(Parent, ConcreteType);
                }
            }

            return ConcreteTypes;
        }

        /// <summary>
        /// Sets up the parent types.
        /// </summary>
        /// <param name="ConcreteTypes">The concrete types.</param>
        private void SetupParentTypes(IEnumerable<Type> ConcreteTypes)
        {
            foreach (var ConcreteType in ConcreteTypes)
            {
                var Parents = TypeGraphs[ConcreteType].ToList();
                foreach (var Parent in Parents)
                {
                    ParentTypes.Add(ConcreteType, Parent);
                }
            }
        }

        /// <summary>
        /// Sets up the type graphs.
        /// </summary>
        private void SetupTypeGraphs()
        {
            var TempGenerator = new Generator(Mappings);
            foreach (var Key in Mappings.Keys)
            {
                TypeGraphs.Add(Key, TempGenerator.Generate(Key));
            }
        }
    }
}