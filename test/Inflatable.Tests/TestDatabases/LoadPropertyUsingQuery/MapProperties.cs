﻿using Inflatable.Tests.TestDatabases.SimpleTest;
using Mirage.Generators;

namespace Inflatable.Tests.TestDatabases.LoadPropertyUsingQuery
{
    public class MapPropertiesCustomLoad
    {
        [BoolGenerator]
        public bool BoolValue { get; set; }

        [IntGenerator]
        public int ID { get; set; }

        public virtual AllReferencesAndID MappedClass { get; set; }
    }
}