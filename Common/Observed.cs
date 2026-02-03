using System;
using System.Collections.Generic;

namespace Common {
    public class Observed<T> {
        public bool Changed;

        private T? val;

        public T Value {
            get => val ?? throw new InvalidOperationException("Observed value is uninitialized.");
            set {
                Changed = val is null || !EqualityComparer<T>.Default.Equals(val, value);
                val = value;
            }
        }

        public bool HasValue() => val is not null;
    }
}