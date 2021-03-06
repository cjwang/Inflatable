﻿using Mirage.Generators;
using System.Collections.Generic;

namespace Inflatable.SpeedTests.ManyToManyProperties
{
    public class ManyToManyProperties
    {
        public ManyToManyProperties()
        {
            ManyToManyClass = new List<AllReferencesAndID>();
        }

        [BoolGenerator]
        public bool BoolValue { get; set; }

        [IntGenerator]
        public int ID { get; set; }

        public virtual IList<AllReferencesAndID> ManyToManyClass { get; set; }
    }
}