using System;
using Xunit;

namespace wfc.Tests {
    public class ClassSetTests {
        [Fact]
        public void ManyInOneClass() {
            ClassSet<int> cs = new ClassSet<int>();

            cs.Add(0, 5);
            cs.Add(0, 6);

            Assert.Equal(2, cs.ClassSize(0));
        }
    }
}