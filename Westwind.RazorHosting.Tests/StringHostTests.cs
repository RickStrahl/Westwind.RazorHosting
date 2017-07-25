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


        [TestMethod]
        public void BasicStringHostTest()
        {
            var host = new RazorStringHostContainer();

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(this);

            host.UseAppDomain = false;

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

            string result = host.RenderTemplate(Templates.BasicTemplateStringWithPersonModel, person);

            Console.WriteLine(result);
            Console.WriteLine("---");
            Console.WriteLine(host.Engine.LastGeneratedCode);

            if (result == null)
                Assert.Fail(host.ErrorMessage);

            host.Stop();
        }


        /// <summary>
        /// Renders a LINQ expression of the Model which requires a strongly
        /// typed model, but no @model or @inherits is used. The model is
        /// inferred in this case.
        /// </summary>
        [TestMethod]
        public void BasicStringHostWithInferredModelTest()
        {
            var host = new RazorStringHostContainer();

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(this);

            host.UseAppDomain = false;

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


            string template = @"
<div>@Model.Name
<div>
@foreach (var addr in Model.Addresses.OrderBy( ad=> ad.Street))
{
        <div>@addr.Street, @addr.Phone</div>    
}
</div>
";

            string result = host.RenderTemplate(template, person, inferModelType: true);

            Console.WriteLine(result);
            Console.WriteLine("---");

            Assert.IsNotNull(result, "Result shouldn't be null: " + host.ErrorMessage);

            host.Stop();
        }

        /// <summary>
        /// Explicit template failure when a runtime error occurs
        /// </summary>
        [TestMethod]
        public void BasicStringHostRuntimeErrorlTest()
        {
            var host = new RazorStringHostContainer();

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(this);

            host.UseAppDomain = false;

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


            string template = @"
@{
   Model.Name = null;   
}
<div>
    Fail here with Null exception: 
    @Model.Name.ToLower()
<div>
";

            string result = host.RenderTemplate(template, person, inferModelType: true);

            Assert.IsNull(result, "Result should have failed with a runtime error.");
            Console.WriteLine(result);
            Console.WriteLine(host.ErrorMessage);

            host.Stop();
        }




        /// <summary>
        /// Explicit template failure when a runtime error occurs
        /// </summary>
        [TestMethod]
        public void BasicStringHostRuntimeErrorExceptionTest()
        {
            var host = new RazorStringHostContainer()
            {
                ThrowExceptions = true,
                UseAppDomain = false
            };

            // add model assembly - ie. this assembly
            host.AddAssemblyFromType(this);



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


            string template = @"
@{
   Model.Name = null;   
}
<div>
    Fail here with Null exception: 
    @Model.Name.ToLower()
<div>
";
            bool isException = false;
            string result = null;
            try
            {
                result = host.RenderTemplate(template, person, inferModelType: true);
            }
            catch (RazorHostContainerException ex)
            {
                isException = true;
                Assert.IsNull(result, "Result should have failed with a runtime error.");
                Console.WriteLine(ex.InnerException.Message);
                Console.WriteLine(ex.InnerException.Source);
                Console.WriteLine(ex.InnerException.StackTrace);
                Console.WriteLine(ex.GeneratedSourceCode);

            }
            Console.WriteLine(result);
            Console.WriteLine(host.ErrorMessage);

            host.Stop();
        }
    }
}
