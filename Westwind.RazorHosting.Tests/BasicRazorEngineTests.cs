using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.RazorHosting;
using System.IO;
using System.Web.Razor;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Dynamic;

namespace RazorHostingTests
{
    
    /// <summary>
    /// These tests use the basic RazorEngine rendering without
    /// any of the host containers that provide for caching and starting
    /// and stopping.
    /// 
    /// These examples all run in the host process and compile
    /// templates into new temporary in memory assemblies.
    /// </summary>
    [TestClass]
    public class BasicRazorEngineTests
    {
        RazorEngine<RazorTemplateBase> Engine = null;

        private RazorEngine<RazorTemplateBase> CreateHost()
        {
            if (this.Engine != null)
                return this.Engine;

            this.Engine = new RazorEngine<RazorTemplateBase>();

            // Use Static Methods - no error message if host doesn't load                       
            //this.Host = RazorEngineFactory<RazorTemplateBase>.CreateRazorHostInAppDomain();

            if (this.Engine == null)
                throw new ApplicationException("Unable to load Razor Template Host");
            
            return this.Engine;
        }

        [TestMethod]
        public void SimplestRazorEngineTest()
        {
            string template = @"Hello World @Model.Name. Time is: @DateTime.Now";
	        var host = new RazorEngine<RazorTemplateBase>();
            string result = host.RenderTemplate(template, new Person { Name = "Joe Doe" });

            Assert.IsNotNull(result,host.ErrorMessage);
            Assert.IsTrue(result.Contains("Joe Doe"));

            Console.WriteLine(result);
        }

        /// <summary>
        /// Test Special support for anonymous types in models.
        /// 
        /// Note this will only work with full Reflection permissions
        /// as anonymous types are marked as internal.
        /// </summary>
        [TestMethod]
        public void SimplestRazorEngineWithAnonymousModelTest()
        {
            var model = new { Name = "Joe Doe", Company = "West Wind" };

            var type = model.GetType();

            string template = @"Hello World @Model.Name of @Model.Company. Time is: @DateTime.Now";
            var host = new RazorEngine<RazorTemplateBase>();
            string cid = host.CompileTemplate(template);            
            string result = host.RenderTemplateFromAssembly(cid, model);
            Console.WriteLine(result + "\r\n" + host.ErrorMessage + "\r\n" + host.LastGeneratedCode);

            Assert.IsNotNull(result, host.ErrorMessage + "\r\n" + host.LastGeneratedCode);
            Assert.IsTrue(result.Contains("Joe Doe"));
        }

        [TestMethod]
        public void SimplestRazorEngineWithExplicitCompilationTest()
        {
            string template = @"Hello World @Model.Name. Time is: @DateTime.Now";
            var host = new RazorEngine<RazorTemplateBase>();
            string compiledId = host.CompileTemplate(template);
            
            string result = host.RenderTemplateFromAssembly(compiledId,
                                                            new Person() { Name = "Joe Doe" });

            Assert.IsNotNull(result, host.ErrorMessage);
            Assert.IsTrue(result.Contains("Joe Doe"));
        }

        /// <summary>
        /// Demonstrates using @model syntax in the template
        /// 
        /// Note:
        /// @model Person is turned to 
        /// @inherits RazorTemplateFolderHost<Person>
        /// 
        /// @model syntax is easier to write (and compatible with MVC), 
        /// but doesn't not provide Intellisense inside of Visual Studio. 
        /// </summary>
        [TestMethod]
        public void SimplestRazorEngineWithModelTest()
        {
            string template = @"@model Person
Hello World @Model.Name. Time is: @DateTime.Now";

            var host = new RazorEngine<RazorTemplateBase>();
            host.AddNamespace("RazorHostingTests");

            string result = host.RenderTemplate(template, new Person { Name = "Joe Doe" });

            Assert.IsNotNull(result, host.ErrorMessage);
            Assert.IsTrue(result.Contains("Joe Doe"));

            Console.WriteLine(result);
        }

        [TestMethod]
        public void BasicRazorEngineStringRenderingTest()
        {
            RazorEngine<RazorTemplateBase> host = CreateHost();
            if (host == null)
                Assert.Fail("Unable to create RazorEngine " + host.ErrorMessage);


            Person person = new Person()
            {
                Name = "Rick",
                Company = "West WInd",
                Entered = DateTime.Now,
                Address = new Address()
                {
                    Street = "32 Kaiea",
                    City = "Paia"
                }
            };

            string result = host.RenderTemplate(
                    // template to render
                    Templates.BasicTemplateStringWithPersonModel,
                    // Model
                    person);                   

            if (result == null)
            {
                Assert.Fail(host.ErrorMessage);
                return;
            }

            Console.WriteLine(result);
            Console.WriteLine("--- Source Code ---");
            Console.WriteLine(host.LastGeneratedCode);
        }


        [TestMethod]
        public void BasicRazorEngineTextReaderRenderingTest()
        {
            RazorEngine<RazorTemplateBase> host = CreateHost();
            if (host == null)
                Assert.Fail("Unable to create RazorEngine " + host.ErrorMessage);

            Person person = new Person()
            {
                Name = "Rick",
                Company = "West WInd",
                Entered = DateTime.Now
            };

            var curAssemlblyPath = Path.GetFileName(typeof(Person).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\"));

            TextReader reader = new StringReader(Templates.BasicTemplateStringWithPersonModel);

            string result = host.RenderTemplate(
                // template to render
                    reader,
                // Model
                    person                
               );

            if (result == null)
            {
                Assert.Fail(host.ErrorMessage);
                return;
            }

            Console.WriteLine(result);
            Console.WriteLine("--- Source Code ---");
            Console.WriteLine(host.LastGeneratedCode);
        }

        [TestMethod]
        public void BasicRazorEngineToTextWriterTest()
        {
            RazorEngine<RazorTemplateBase> host = CreateHost();
            if (host == null)
                Assert.Fail("Unable to create RazorEngine " + host.ErrorMessage);

            Person person = new Person()
            {
                Name = "Rick",
                Company = "West Wind",
                Entered = DateTime.Now
            };

            var curAssemlblyPath = Path.GetFileName(typeof(Person).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\"));
            string outputFile = Path.Combine(Environment.CurrentDirectory,"templateoutput.txt");
            File.Delete(outputFile);


            using (TextWriter writer = new StreamWriter(outputFile, false, Encoding.UTF8))
            {
                // when rendering to writer result is empty (success) or null (failure)
                string result = host.RenderTemplate(
                    // template to render
                        Templates.BasicTemplateStringWithPersonModel,
                    // Model
                        person,                  
                    // write to textwriter
                        writer
                    );

                if (result == null)
                {
                    Assert.Fail(host.ErrorMessage);
                    return;
                }
            }

            Assert.IsTrue(File.Exists(outputFile),"Template output not created");
            string text = File.ReadAllText(outputFile);
            Console.WriteLine(text); 
            Assert.IsTrue(text.Contains("West Wind"), "Text not found in generated template");

            File.Delete(outputFile);
            
            Console.WriteLine("--- Source Code ---");
            Console.WriteLine(host.LastGeneratedCode);
        }

        [TestMethod]
        public void ManualCompileAndRunTest()
        {
            RazorEngine<RazorTemplateBase> host = CreateHost();
            if (host == null)
                Assert.Fail("Unable to create RazorEngine " + host.ErrorMessage);

            Person person = new Person()
            {
                Name = "Rick",
                Company = "West WInd",
                Entered = DateTime.Now,
                Address = new Address()
                {
                    Street = "32 Kaiea",
                    City = "Paia"
                }
            };

            // we have to explicitly add a reference to the model's assembly
            // if we're compiling manually - the compiler doesn't know about
            // the model.
            host.AddAssemblyFromType(person);
            
            string templateId = host.CompileTemplate(
                            Templates.BasicTemplateStringWithPersonModel,
                            "__RazorHost", "ManualCompileAndRun");

            if (templateId == null)
                Assert.Fail("Unable to compile Template: " + host.ErrorMessage);
            
            string result = host.RenderTemplateFromAssembly(templateId, person);

            if (result == null)
            {
                Assert.Fail(host.ErrorMessage);
                return;
            }

            Console.WriteLine(result);
            Console.WriteLine("--- Source Code ---");
            Console.WriteLine(host.LastGeneratedCode);
        }
    }
}