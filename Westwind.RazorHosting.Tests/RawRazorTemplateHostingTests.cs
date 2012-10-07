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
    /// This example demonstrates 
    /// </summary>
    [TestClass]
    public class RawRazorTemplateHostingTests
    {
   
     

        [TestMethod]
        public void RawRazorTest()
        {
            string generatedNamespace = "__RazorHosting";
            string generatedClassname = "RazorTest";

            Type baseClassType = typeof(RazorTemplateBase);

            // Create an instance of the Razor Engine for a given 
            // template type
            RazorEngineHost host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            host.DefaultBaseClass = baseClassType.FullName;
           
            host.DefaultClassName = generatedClassname;
            host.DefaultNamespace = generatedNamespace;
            
            host.NamespaceImports.Add("System");
            host.NamespaceImports.Add("System.Text");
            host.NamespaceImports.Add("System.Collections.Generic");
            host.NamespaceImports.Add("System.Linq");
            host.NamespaceImports.Add("System.IO");
            
            // add the library namespace
            host.NamespaceImports.Add("Westwind.RazorHosting"); 
           
            var engine = new RazorTemplateEngine(host);
           
            // Create and compile Code from the template 
            var reader = new StringReader(Templates.BasicTemplateStringWithPersonModel);

            // Generate the template class as CodeDom from reader
            GeneratorResults razorResults = engine.GenerateCode(reader);

            // Create code from the codeDom and compile
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CodeGeneratorOptions options = new CodeGeneratorOptions();

            // Capture Code Generated as a string for error info
            // and debugging
            string LastGeneratedCode = null;
            using (StringWriter writer = new StringWriter())
            {
                codeProvider.GenerateCodeFromCompileUnit(razorResults.GeneratedCode, writer, options);
                LastGeneratedCode = writer.ToString();
            }

            CompilerParameters compilerParameters = new CompilerParameters();

            // Always add Standard Assembly References
            compilerParameters.ReferencedAssemblies.Add("System.dll");
            compilerParameters.ReferencedAssemblies.Add("System.Core.dll");
            compilerParameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");   // dynamic support!  
            compilerParameters.ReferencedAssemblies.Add("System.Web.Razor.dll"); 

            // Must add Razorhosting or whatever assembly holds the template 
            // engine can do this automatically but here we have to do manually
            compilerParameters.ReferencedAssemblies.Add("Westwind.RazorHosting.dll");

            // Add this assembly so model can be found
            var razorAssembly = Assembly.GetExecutingAssembly().Location;
            compilerParameters.ReferencedAssemblies.Add(razorAssembly);

            compilerParameters.GenerateInMemory = true;

            CompilerResults compilerResults = codeProvider.CompileAssemblyFromDom(compilerParameters, razorResults.GeneratedCode);
            if (compilerResults.Errors.HasErrors)
            {
                var compileErrors = new StringBuilder();
                foreach (System.CodeDom.Compiler.CompilerError compileError in compilerResults.Errors)
                    compileErrors.Append(String.Format("Line: {0}\t Col: {1}\t Error: {2}", compileError.Line, compileError.Column, compileError.ErrorText));
                

                Assert.Fail(compileErrors.ToString());
            }

            string name = compilerResults.CompiledAssembly.FullName;



            // Instantiate the template
            Assembly generatedAssembly = compilerResults.CompiledAssembly;
            if (generatedAssembly == null)
                Assert.Fail("Assembly generation failed.");

            // find the generated type to instantiate
            Type type = null;
            var types = generatedAssembly.GetTypes() as Type[];
            // there's only 1 per razor assembly
            if (types.Length > 0)
                type = types[0];

            object inst = Activator.CreateInstance(type);
            RazorTemplateBase instance = inst as RazorTemplateBase;

            if (instance == null)
            {
                Assert.Fail("Couldn't activate template: " +  type.FullName);
                return;
            }

           
            // Configure the instance 
            StringWriter outputWriter = new StringWriter();

           // Template contains a Response object that writes to the writer
           instance.Response.SetTextWriter(outputWriter);

           Person person = new Person()
           {
               Name = "Rick Strahl",
               Company = "West Wind",
               Entered = DateTime.Now,
               Address = new Address()
               {
                   Street = "32 Kaiea",
                   City = "Paia"
               }
           };

           instance.InitializeTemplate(person);
           
            
     
           // Execute the template  and clean up
           instance.Execute();
           instance.Dispose();


           // read the result from the writer passed in
           var result = outputWriter.ToString();           
           Console.WriteLine(result); 
        }


    
    }
}