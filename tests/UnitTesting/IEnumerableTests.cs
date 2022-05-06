using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace Extensions
{
    public class IEnumerableTests
    {
        [Fact]
        public void Test1()
        {
            IEnumerable<string> test = Array.Empty<string>();
            Assert.True(!test?.Any() ?? true);
        }
    }
}