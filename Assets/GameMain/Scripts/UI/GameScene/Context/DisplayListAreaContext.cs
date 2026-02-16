using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class DisplayListAreaContext : UIContext
    {
        public string Title;
        public int CurrentCount;
        public int MaxCount = -1;
        public DisplayItemContext[] ItemContexts;
    }
}