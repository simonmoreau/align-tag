using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Align.Test
{
    [TestFixture]
    public class SampleTest
    {
        [SetUp]
        public void RunBeforeTest()
        {
            Console.WriteLine($"Run 'SetUp' in {GetType().Name}");
        }

        [TearDown]
        public void RunAfterTest()
        {
            Console.WriteLine($"Run 'TearDown' in {GetType().Name}");
        }

        [Test]
        public void PassTest()
        {
            Assert.True(true);
        }

        [Test]
        public void FailTest()
        {
            Assert.True(false, "This Test should fail!");
        }
    }
}
