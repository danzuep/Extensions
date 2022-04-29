using System;
using System.Collections.Generic;
using Xunit;
using Extensions.IEnumerables;

namespace Extensions
{
    public class IEnumerableTests
    {
        [Fact]
        public void Test1()
        {
            IEnumerable<string> test = Array.Empty<string>();
            Assert.True(test.IsNullOrEmpty());
        }
    }
}