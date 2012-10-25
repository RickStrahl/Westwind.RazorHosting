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
    public class FolderHostTests
    {
        public FolderHostTests()
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

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void BasicFolderTest()
        {
            var host = new RazorFolderHostContainer();

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));

            host.UseAppDomain = true;
            //host.Configuration.CompileToMemory = true;
            //host.Configuration.TempAssemblyPath = Environment.CurrentDirectory;

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

            string result = host.RenderTemplate("~/HelloWorld.cshtml", person);

            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            host.Stop();


            if (result == null)
                Assert.Fail(host.ErrorMessage);

            Assert.IsTrue(result.Contains("West Wind"));
        }


        /// <summary>
        /// Demonstrates using @model syntax in the template
        /// Note:
        /// @model Person is turned to 
        /// @inherits RazorTemplateFolderHost<Person>
        /// 
        /// @model syntax is easier to write (and compatible with MVC), 
        /// but doesn't not provide Intellisense inside of Visual Studio. 
        /// </summary>
        [TestMethod]
        public void BasicFolderHostWithModelSyntaxTest()
        {
            var host = new RazorFolderHostContainer();

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));

            host.UseAppDomain = true;
            //host.Configuration.CompileToMemory = true;
            //host.Configuration.TempAssemblyPath = Environment.CurrentDirectory;

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

            string result = host.RenderTemplate("~/HelloWorldWithModelSyntax.cshtml", person);

            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            host.Stop();


            if (result == null)
                Assert.Fail(host.ErrorMessage);

            Assert.IsTrue(result.Contains("West Wind"));
        }

        [TestMethod]
        public void FolderWithPartialTest()
        {
            var host = new RazorFolderHostContainer();

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));

            host.UseAppDomain = true;
            //host.Configuration.CompileToMemory = true;
            //host.Configuration.TempAssemblyPath = Environment.CurrentDirectory;

            host.Start();

            Person person = new Person()
            {
                Name = "John Doe",
                Company = "Doeboy Incorporated",
                Entered = DateTime.Now,
                Address = new Address()
                {
                    Street = "32 Kaiea",
                    City = "Paia"
                }
            };

            string result = host.RenderTemplate("~/TestPartial.cshtml", person);

            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            if (result == null)
                Assert.Fail(host.ErrorMessage);

            // run again
            person.Name = "Billy Bobb";
            result = host.RenderTemplate("~/TestPartial.cshtml", person);

            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            if (result == null)
                Assert.Fail(host.ErrorMessage);


            Assert.IsTrue(result.Contains("Billy Bobb"));

            host.Stop();
        }


        /// <summary>
        /// Renders a page that contains a RenderTemplate() call used to
        /// render nested content. Useful to render user entered content
        /// that might need to contain dynamic expressions
        /// </summary>
        [TestMethod]
        public void StringTemplateTest()
        {
            var host = new RazorFolderHostContainer();

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));

            host.UseAppDomain = true;
            //host.Configuration.CompileToMemory = false;
            //host.Configuration.TempAssemblyPath = Environment.CurrentDirectory;

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

            string result = null;
            for (int i = 0; i < 10; i++)
            {
                result = host.RenderTemplate("~/TestRenderTemplate.cshtml", person);
            }
            
            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            host.Stop();
            
            if (result == null)
                Assert.Fail(host.ErrorMessage);

            Assert.IsTrue(result.Contains("West Wind"));
        }
   
    }
}
