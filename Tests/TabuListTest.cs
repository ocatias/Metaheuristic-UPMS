using CO1;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Tests
{
    public class TabuListTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            TabuList t1 = new TabuList();
            t1.addPairing(new List<int> { 2, 3 });
            t1.addPairing(new List<int> { 1 });
            t1.addPairing(new List<int> { 1, 2, 3, 4});

            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 1 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 2 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 3 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 4 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 2, 3 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 1, 2 ,3 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> {2, 4 }));


            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 5 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 6 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 2 , 3, 4, 5 }));

            t1.removePairings(new List<int> { 1 });
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 4 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 2, 3 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 2, 4 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 2 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 3 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 2 }));
            Assert.IsFalse(t1.isNotATabuPairing(new List<int> { 2, 3 }));

            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 5 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 6 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 2, 3, 4, 5 }));

            t1.removePairings(new List<int> { 2, 3 });
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 4 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 2, 3 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 2, 4 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 2 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 3 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 2 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 2, 3 }));

            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 5 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 6 }));
            Assert.IsTrue(t1.isNotATabuPairing(new List<int> { 1, 2, 3, 4, 5 }));
        }
    }
}
