using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Westwind.RazorHosting;
using System.IO;
using System.Web.Razor;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

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

        RazorEngine<RazorTemplateBase> Host = null;


        private RazorEngine<RazorTemplateBase> CreateHost()
        {
            if (this.Host != null)
                return this.Host;

            //this.Host = new RazorEngine<RazorTemplateBase>();

            // Use Static Methods - no error message if host doesn't load                       
            this.Host = RazorEngineFactory<RazorTemplateBase>.CreateRazorHostInAppDomain();

            if (this.Host == null)
                throw new ApplicationException("Unable to load Razor Template Host");
            
            return this.Host;
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
            host.AddReferencedAssemblyFromInstance(person);
            
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