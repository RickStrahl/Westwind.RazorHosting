using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Westwind.RazorHosting;

namespace RazorHostingTests
{
    /// <summary>
    /// Summary description for FolderHostTests
    /// </summary>
    [TestClass]
    public class StringHostTests
    {
        public StringHostTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

      

        [TestMethod]
        public void BasicStringHostTest()
        {
            var host = new RazorStringHostContainer();
            
            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(this);

            host.UseAppDomain = true;

            host.Start();
              
            Person person = new Person()
            {
                Name = "Rick",
                Company = "West Wind",
                Entered = DateTime.Now,
                Address = new Address()
                {
                    Street = "32 Kaiea",
                    City = "Paia"
                }
            };
            
            string result = host.RenderTemplate(Templates.BasicTemplateStringWithPersonModel,person);
            
            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            if (result == null)
                Assert.Fail(host.ErrorMessage);
            
            host.Stop();
        }
    }
}
