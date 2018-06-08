using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Westwind.RazorHosting;

namespace RazorHostingTests
{
    /// <summary>
    /// Summary description for FolderHostTests
    /// </summary>
    [TestClass]
    public class FolderHostTests
    {
        [TestMethod]
        public void BasicFolderTest()
        {

            var host = new RazorFolderHostContainer();
            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));
            host.UseAppDomain = false;

            // these are implicitly set
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
            Console.WriteLine(host.ErrorMessage);
            Console.WriteLine(host.Engine.LastGeneratedCode);

            host.Stop();

            if (result == null)
                Assert.Fail(host.ErrorMessage);

            Assert.IsTrue(result.Contains("West Wind"));
        }


        /// <summary>
        /// In order for this to work you need to add:
        /// * Add Microsoft.CodeDom.Providers.DotNetCompilerPlatform to your project
        /// * add CodeDom section from this projects app.config into an app.config for your app
        /// </summary>
        [TestMethod]
        public void BasicFolderWithRoslynCompilerTest()
        {

            var host = new RazorFolderHostContainer();
            host.CodeProvider = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider();

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));
            host.UseAppDomain = false;

            // these are implicitly set
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

            string result = host.RenderTemplate("~/HelloWorldCSharpLatest.cshtml", person);



            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.ErrorMessage);
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

        [TestMethod]
        public void FolderHostWithLayoutPageTest()
        {
            using (var host = new RazorFolderHostContainer())
            {

                host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
                host.BaseBinaryFolder = Environment.CurrentDirectory;
                Console.WriteLine(host.TemplatePath);

                // point at the folder where dependent assemblies can be found
                // this applies only to separate AppDomain hosting
                host.BaseBinaryFolder = Environment.CurrentDirectory;

                // add model assembly - ie. this assembly
                host.AddAssemblyFromType(typeof(Person));

                // NOTE: If you use AppDomains you will need to add a /bin folder
                //       with all dependencies OR run out of the current folder
                //       and all models have to be serializable or MarshalByRefObj
                host.UseAppDomain = false;

                //host.Configuration.CompileToMemory = true;
                //host.Configuration.TempAssemblyPath = Environment.CurrentDirectory;

                // Always must start the host
                host.Start();

                // create a model to pass
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

                Console.WriteLine("-----Layout page only (rendered)");
                // just show what a layout template looks like on its own
                string layout = host.RenderTemplate("~/_Layout.cshtml", person);
                Console.WriteLine(layout);


                Console.WriteLine("----- Content page In Layout Container");
                
                // render a template and pass the model
                string result = host.RenderTemplate("~/LayoutPageExample.cshtml", person);

                //result = layout.Replace("@RenderBody", result);

                //Assert.True(result != null, "Template didn't return any data: " + host.ErrorMessage);

                Console.WriteLine("---");
                Console.WriteLine(result);
                Console.WriteLine("---");
                
                Assert.IsNotNull(result, host.ErrorMessage);
            }
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

        [TestMethod]
        public void MissingTemplateTest()
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

            string result = host.RenderTemplate("~/NotThere.cshtml", person);

            Assert.IsTrue(host.ErrorMessage.Contains("Template File doesn't exist"));

            Console.WriteLine(host.ErrorMessage);

            host.Stop();
            
        }

        [TestMethod]
        public void RuntimeErrorTest()
        {
            var host = new RazorFolderHostContainer();

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));
            host.UseAppDomain = false;

            // these are implicitly set
            host.Configuration.CompileToMemory = false;
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

            string result = host.RenderTemplate("~/RuntimeError.cshtml", person);

            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.ErrorMessage);
            Console.WriteLine("---"); 
            Console.WriteLine(host.Engine.LastGeneratedCode);

            host.Stop();

            Assert.IsNull(result);
            Assert.IsTrue(!string.IsNullOrEmpty(host.ErrorMessage));
        }

        [TestMethod]
        public void RuntimeErrorWithExceptionTest()
        {
            var host = new RazorFolderHostContainer();
            host.ThrowExceptions = true;

            host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
            host.BaseBinaryFolder = Environment.CurrentDirectory;

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(typeof(Person));
            host.UseAppDomain = false;

            // these are implicitly set
            host.Configuration.CompileToMemory = false;
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

            bool exception = false;
            try
            {
                string result = host.RenderTemplate("~/RuntimeError.cshtml", person);
            }
            catch(RazorHostContainerException ex)
            {
                Console.WriteLine(ex.InnerException.Message);
                Console.WriteLine(ex.InnerException.Source);
                Console.WriteLine(ex.InnerException.StackTrace);
                Console.WriteLine(ex.GeneratedSourceCode);
                var config = ex.RequestConfigurationData as RazorFolderHostTemplateConfiguration;
                if (config != null)
                {
                    Console.WriteLine(config.PhysicalPath);
                    Console.WriteLine(config.TemplatePath);
                    Console.WriteLine(config.TemplateRelativePath);
                    Console.WriteLine(config.LayoutPage);
                    
                }


                exception = true;
            } 

            Assert.IsTrue(exception, "Exception should have been thrown.");

            host.Stop();


        }

    }
}
