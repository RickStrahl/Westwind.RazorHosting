using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.RazorHosting;

namespace RazorHosting.Tests
{
    [TestClass]
    public class UtilitiesTests
    {
        [TestMethod]
        public void GetTextWithLineNumbers()
        {
            var lines = @"
int x = 1;
x++;
line[x] = 1;

int x = 1;
x++;
line[x] = 1;

int x = 1;
x++;
line[x] = 1;

int x = 1;
x++;
line[x] = 1;";


            string res = Utilities.GetTextWithLineNumbers(lines);
            Console.WriteLine(res);
            Assert.IsTrue(res.Contains("14.  "));


            res = Utilities.GetTextWithLineNumbers(lines,"{0}:  {1}");
            Console.WriteLine(res);
            Assert.IsTrue(res.Contains("14:  "));



        }
    }
}
