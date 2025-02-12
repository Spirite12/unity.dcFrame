using System.Collections.Generic;

namespace DCFrame {
    public class TextTrieNode {
        public bool IsWord { get; set; }

        public Dictionary<char, TextTrieNode> Children { get; set; } = new();
    }
}

