﻿using System;

namespace todofuentes.Test.Helpers
{
    public class NullScope : IDisposable
    {

        public static NullScope Instance { get; } = new NullScope();
        public void Dispose() { }

        private NullScope() { }

    }
}
