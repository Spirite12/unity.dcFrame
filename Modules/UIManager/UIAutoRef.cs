using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    public class UIAutoRef : ScriptableObject {

        public List<MatchRef> uIPrefixMatchList = new List<MatchRef>();

        [System.Serializable]
        public class MatchRef {
            public string componentName;
            public string prefix;
        }
    }
}
