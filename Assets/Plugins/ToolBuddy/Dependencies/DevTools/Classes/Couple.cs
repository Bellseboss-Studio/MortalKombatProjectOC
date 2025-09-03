using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluffyUnderware.DevTools
{
    public class Couple<T1, T2>
    {
        public T1 First { get; set; }
        public T2 Second { get; set; }
        public Couple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }
}
