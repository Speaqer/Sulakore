﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace Sulakore.Modules
{
    public sealed class HashResolvingException : Exception
    {
        public string Revision { get; }
        public Dictionary<string, string[]> Unresolved { get; }

        public HashResolvingException(string revision, IDictionary<string, IList<string>> unresolved)
            : base($"Failed to resolve '{unresolved.Count}' hash value(s) from revision '{revision}'.")
        {
            Unresolved = Unresolved
                .ToDictionary(u => u.Key, u => u.Value.ToArray());
        }
    }
}