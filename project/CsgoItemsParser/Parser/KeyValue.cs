using System;
using System.Collections.Generic;

namespace CsgoItemsParser.Parser
{
    /// <summary>
    /// A single node of a parsed Valve KeyValues (VDF) document.
    /// A node is either a leaf ("key" "value") or a block ("key" { ... }), never both.
    /// </summary>
    public class KeyValue
    {
        public string Key;
        public string Value;
        public List<KeyValue> Children;

        public bool IsBlock => Children != null;

        public KeyValue FindChild(string key)
        {
            if (Children == null) return null;

            foreach (var child in Children)
                if (string.Equals(child.Key, key, StringComparison.OrdinalIgnoreCase))
                    return child;

            return null;
        }

        public IEnumerable<KeyValue> FindChildren(string key)
        {
            if (Children == null) yield break;

            foreach (var child in Children)
                if (string.Equals(child.Key, key, StringComparison.OrdinalIgnoreCase))
                    yield return child;
        }
    }
}
