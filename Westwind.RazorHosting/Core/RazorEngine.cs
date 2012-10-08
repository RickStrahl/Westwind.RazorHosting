#region License
/*
 **************************************************************
 *  Author: Rick Strahl 
 *          © West Wind Technologies, 2010-2011
 *          http://www.west-wind.com/
 * 
 * Created: 1/4/2010
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 **************************************************************  
*/
#endregion


using System;
using System.Text;
using System.Linq;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Westwind.RazorHosting.Properties;
using System.Web.Razor;

namespace Westwind.RazorHosting
{
    /// <summary>
    /// Razor Hosting Engine that allows execution of Razor templates outside of
    /// ASP.NET. You can execute templates from string or a textreader and output
    /// to string or a text reader.
    /// 
    /// This implementation only supports C#.
    /// </summary>
    /// <typeparam name="TBaseTemplateType">RazorTemplateHost based type</typeparam>
    public class RazorEngine<TBaseTemplateType> : MarshalByRefObject
        where TBaseTemplateType : RazorTemplateBase
    {
        /// <summary>
        /// Any errors that occurred during template execution
        /// </summary>
        public string ErrorMessage {get; set; }
        
        /// <summary>
        /// Last generated output
        /// </summary>
        public string LastGeneratedCode { get; set; }

       /// <summary>
       /// Holds Razor Configuration Properties
       /// </summary>
        public RazorEngineConfiguration Configuration { get; set; }

        /// <summary>
        /// Provide a reference to a RazorHost container so that it
        /// can be passed to a template.
        /// 
        /// This may be null, but if a container is available this value
        /// is set and passed on to the template as HostContainer.
        /// </summary>
        public object HostContainer {get; set; }

        /// <summary>
        /// A list of default namespaces to include
        /// 
        /// Defaults already included:
        /// System, System.Text, System.IO, System.Collections.Generic, System.Linq
        /// </summary>
        internal List<string> ReferencedNamespaces { get; set; }

        /// <summary>
        /// A list of default assemblies referenced during compilation
        /// 
        /// Defaults already included:
        /// System, System.Text, System.IO, System.Collections.Generic, System.Linq
        /// </summary>
        internal List<string> ReferencedAssemblies { get; set; }
        

        /// <summary>
        /// Internally cache assemblies loaded with ParseAndCompileTemplate.        
        /// Assemblies are cached in the EngineHost so they don't have
        /// to cross AppDomains for invocation when running in a separate AppDomain
        /// </summary>
        protected Dictionary<string, Assembly> AssemblyCache { get; set; }

        /// <summary>
        /// A property that holds any per request configuration 
        /// data that is to be passed to the template. This object
        /// is passed to InitializeTemplate after the instance was
        /// created.
        /// 
        /// This object must be serializable. 
        /// This object should be set on every request and cleared out after 
        /// each request       
        /// </summary>
        public object TemplatePerRequestConfigurationData { get; set; }

        /// <summary>
        /// Creates an instance of the host and performs basic configuration
        /// Optionally pass in any required namespaces and assemblies by name
        /// </summary>
        public RazorEngine()
        {
            Configuration = new RazorEngineConfiguration();
            AssemblyCache = new Dictionary<string, Assembly>();
            ErrorMessage = string.Empty;

            ReferencedNamespaces = new List<string>();
            ReferencedNamespaces.Add("System");
            ReferencedNamespaces.Add("System.Text");
            ReferencedNamespaces.Add("System.Collections.Generic");
            ReferencedNamespaces.Add("System.Linq");
            ReferencedNamespaces.Add("System.IO");
            ReferencedNamespaces.Add("System.Web");
            ReferencedNamespaces.Add("Westwind.RazorHosting");

            ReferencedAssemblies = new List<string>();
            ReferencedAssemblies.Add("System.dll");
            ReferencedAssemblies.Add("System.Core.dll");
            ReferencedAssemblies.Add("Microsoft.CSharp.dll");   // dynamic support!                         
            ReferencedAssemblies.Add("System.Web.dll");
            ReferencedAssemblies.Add("System.Web.Razor.dll");
            
            ReferencedAssemblies.Add( this.GetType().Assembly.Location);
        }



        /// <summary>
        /// Method to add assemblies to the referenced assembly list.
        /// Each assembly added HAS to be accessible via GAC or in
        /// the applications' bin/bin private path
        /// </summary>
        /// <param name="assemblyName"></param>
        public void AddAssembly(string assemblyName)
        {
            ReferencedAssemblies.Add(assemblyName);
        }

        /// <summary>
        /// Method to add namespaces to the compiled code.
        /// Add namespaces to minimize explicit namespace
        /// requirements in your Razor template code.
        /// 
        /// Make sure that any required assemblies are
        /// loaded first.
        /// </summary>
        /// <param name="namespace"></param>
        public void AddNamespace(string ns)
        {
            ReferencedNamespaces.Add(ns);
        }

        /// <summary>
        /// Execute a template based on a TextReader input into a provided TextWriter object.
        /// </summary>
        /// <param name="mplateSourceReader">A text reader that reads in the markup template</param>
        /// <param name="generatedNamespace">Name of the namespace that is generated</param>
        /// <param name="generatedClass">Name of the class that is generated</param>
        /// <param name="referencedAssemblies">Any assembly references required by template as a DLL names. Must be in execution path or GAC.</param>
        /// <param name="model">Optional context available in the template as this.Context</param>
        /// <param name="outputWriter">
        /// A text writer that receives the rendered template output. 
        /// Writer is closed after rendering. 
        /// When provided the result of this method is string.Empty (success) or null (failure)
        /// </param>
        /// <returns>output from template. If an outputWriter is passed in result is string.Empty on success, null on failure</returns>
        public string RenderTemplate(
                    TextReader templateSourceReader,
                    object model = null,                    
                    TextWriter outputWriter = null)
        {
            this.SetError();
             
            AddReferencedAssemblyFromInstance(model);

            var assemblyId = CompileTemplate(templateSourceReader);

            if (assemblyId == null)
                return null;

            return RenderTemplateFromAssembly(assemblyId, model, outputWriter);
        }

        /// <summary>
        /// Execute a template based on a TextReader input into a provided TextWriter object.
        /// </summary>
        /// <param name="templateText">A string that contains the markup template</param>
        /// <param name="generatedNamespace">Name of the namespace that is generated</param>
        /// <param name="generatedClass">Name of the class that is generated</param>
        /// <param name="referencedAssemblies">Any assembly references required by template as a DLL names. Must be in execution path or GAC.</param>
        /// <param name="model">Optional context available in the template as this.Context</param>
        /// <param name="outputWriter">
        /// A text writer that receives the rendered template output. 
        /// Writer is closed after rendering. 
        /// When provided the result of this method is string.Empty (success) or null (failure)
        /// </param>
        /// <returns>output from template. If an outputWriter is passed in result is string.Empty on success, null on failure</returns>
        public string RenderTemplate(
                    string templateText,
                    object model = null,                    
                    TextWriter outputWriter = null)
        {            
            TextReader templateReader = new StringReader(templateText);
            return RenderTemplate(templateReader, model, outputWriter);
        }

        /// <summary>
        /// Executes a template based on a previously compiled and cached assembly reference.
        /// This effectively allows you to cache an assembly.
        /// </summary>
        /// <param name="generatedAssembly"></param>
        /// <param name="model"></param>
        /// <param name="outputWriter">A text writer that receives output generated by the template. Writer is closed after rendering.</param>
        /// <param name="generatedNamespace"></param>
        /// <param name="generatedClass"></param>
        /// <returns>output from template. If an outputWriter is passed in result is string.Empty on success, null on failure</returns>
        public string RenderTemplateFromAssembly(
            string assemblyId,
            object model,
            TextWriter outputWriter = null,
            string generatedNamespace = null,
            string generatedClass = null)
        {
            this.SetError();

            // Handle anonymous and other non-public types
            if (model != null && model.GetType().IsNotPublic)
                model = new AnonymousDynamicType(model);

            Assembly generatedAssembly = AssemblyCache[assemblyId];
            if (generatedAssembly == null)
            {
                this.SetError(Resources.PreviouslyCompiledAssemblyNotFound);
                return null;
            }

            // find the generated type to instantiate
            Type type = null;

            if (string.IsNullOrEmpty(generatedNamespace) || string.IsNullOrEmpty(generatedClass))
            {
                type = generatedAssembly.GetTypes().FirstOrDefault();
                if (type == null)
                {
                    this.SetError(Resources.UnableToCreateType);
                    return null;
                }
            }
            else
            {
                string className = generatedNamespace + "." + generatedClass;
                try
                {
                    type = generatedAssembly.GetType(className);
                }
                catch (Exception ex)
                {
                    SetError(Resources.UnableToCreateType + className + ": " + ex.Message);
                    return null;
                }
            }

            // Start with empty non-error response (if we use a writer)
            string result = string.Empty;

            using (TBaseTemplateType instance = InstantiateTemplateClass(type))
            {
                //if (TemplatePerRequestConfigurationData != null)
                instance.InitializeTemplate(model, TemplatePerRequestConfigurationData);

                if (instance == null)
                    return null;

                if (outputWriter != null)
                    instance.Response.SetTextWriter(outputWriter);

                if (!InvokeTemplateInstance(instance, model))
                    return null;

                // Capture string output if implemented and return
                // otherwise null is returned
                if (outputWriter == null)
                    result = instance.Response.ToString();
                else
                    // return string.Empty for success if a writer is provided
                    result = string.Empty;
            }

            return result;
        }


        /// <summary>
        /// Parses and compiles a markup template into an assembly and returns
        /// an assembly name. The name is an ID that can be passed to 
        /// ExecuteTemplateByAssembly which picks up a cached instance of the
        /// loaded assembly.
        /// 
        /// </summary>
        /// <param name="referencedAssemblies">Any referenced assemblies by dll name only. Assemblies must be in execution path of host or in GAC.</param>
        /// <param name="templateSourceReader">Textreader that loads the template</param>
        /// <param name="generatedNamespace">The namespace of the class to generate from the template. null generates name.</param>
        /// <param name="generatedClassName">The name of the class to generate from the template. null generates name.</param>
        /// <remarks>
        /// The actual assembly isn't returned here to allow for cross-AppDomain
        /// operation. If the assembly was returned it would fail for cross-AppDomain
        /// calls.
        /// </remarks>
        /// <returns>An assembly Id. The Assembly is cached in memory and can be used with RenderFromAssembly.</returns>
        public string CompileTemplate(
                    TextReader templateSourceReader,                                    
                    string generatedNamespace = null,
                    string generatedClassName = null)
        {
            if (string.IsNullOrEmpty(generatedNamespace))
                generatedNamespace = "__RazorHost";
            if (string.IsNullOrEmpty(generatedClassName))
                generatedClassName = GetSafeClassName(null);
            else
                generatedClassName = GetSafeClassName(generatedClassName);

            RazorTemplateEngine engine = CreateHost(generatedNamespace, generatedClassName);

            // Generate the template class as CodeDom  
            GeneratorResults razorResults = engine.GenerateCode(templateSourceReader);

            // Create code from the codeDom and compile
            CSharpCodeProvider codeProvider = new CSharpCodeProvider();
            CodeGeneratorOptions options = new CodeGeneratorOptions();

            // Capture Code Generated as a string for error info
            // and debugging
            LastGeneratedCode = null;
            using (StringWriter writer = new StringWriter())
            {
                codeProvider.GenerateCodeFromCompileUnit(razorResults.GeneratedCode, writer, options);
                LastGeneratedCode = writer.ToString();
            }

            CompilerParameters compilerParameters = new CompilerParameters(ReferencedAssemblies.ToArray());
           
            compilerParameters.GenerateInMemory = Configuration.CompileToMemory;
            if (!Configuration.CompileToMemory)
                compilerParameters.OutputAssembly = Path.Combine(Configuration.TempAssemblyPath, "_" + Guid.NewGuid().ToString("n") + ".dll");

            CompilerResults compilerResults = codeProvider.CompileAssemblyFromDom(compilerParameters, razorResults.GeneratedCode);
            if (compilerResults.Errors.Count > 0)
            {
                var compileErrors = new StringBuilder();
                foreach (System.CodeDom.Compiler.CompilerError compileError in compilerResults.Errors)
                    compileErrors.Append(String.Format(Resources.LineX0TColX1TErrorX2RN, 
                                        compileError.Line, 
                                        compileError.Column, 
                                        compileError.ErrorText));

                this.SetError(compileErrors.ToString() + "\r\n" + LastGeneratedCode);
                return null;
            }

            AssemblyCache.Add(compilerResults.CompiledAssembly.FullName, compilerResults.CompiledAssembly);

            return compilerResults.CompiledAssembly.FullName;
        }

        /// <summary>
        /// Parses and compiles a markup template into an assembly and returns
        /// an assembly name. The name is an ID that can be passed to 
        /// ExecuteTemplateByAssembly which picks up a cached instance of the
        /// loaded assembly.
        /// 
        /// </summary>
        /// <param name="ReferencedAssemblies">Any referenced assemblies by dll name only. Assemblies must be in execution path of host or in GAC.</param>
        /// <param name="templateSourceReader">Textreader that loads the template</param>
        /// <remarks>
        /// The actual assembly isn't returned here to allow for cross-AppDomain
        /// operation. If the assembly was returned it would fail for cross-AppDomain
        /// calls.
        /// </remarks>
        /// <returns>An assembly Id. The Assembly is cached in memory and can be used with RenderFromAssembly.</returns>
        public string CompileTemplate(string templateText,                    
                    string generatedNamespace = null,
                    string generatedClassName = null)
        {
            using (StringReader reader = new StringReader(templateText))
            {
                return CompileTemplate(reader, generatedNamespace, generatedClassName);
            }
        }


        /// <summary>
        /// Creates an instance of the RazorHost with various options applied.
        /// Applies basic namespace imports and the name of the class to generate
        /// </summary>
        /// <param name="generatedNamespace"></param>
        /// <param name="generatedClass"></param>
        /// <returns></returns>
        protected RazorTemplateEngine CreateHost(string generatedNamespace, string generatedClass)
        {     
            Type baseClassType = typeof(TBaseTemplateType);

            RazorEngineHost host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            host.DefaultBaseClass = baseClassType.FullName;
            host.DefaultClassName = generatedClass;
            host.DefaultNamespace = generatedNamespace;

            foreach (string ns in this.ReferencedNamespaces)
            {
                host.NamespaceImports.Add(ns);
            }            
            
            return new RazorTemplateEngine(host);            
        }
        



        /// <summary>
        /// Allows retrieval of an Assembly cached internally by its id
        /// returned from ParseAndCompileTemplate. Useful if you want
        /// to write an assembly to disk for later activation
        /// </summary>
        /// <param name="assemblyId"></param>
        public Assembly GetAssemblyFromId(string assemblyId)
        {
            Assembly ass = null;
            AssemblyCache.TryGetValue(assemblyId, out ass);
            return ass;            
        }


        /// <summary>
        /// Overridable instance creation routine for the host. 
        /// 
        /// Handle custom template base classes (derived from RazorTemplateBase)
        /// and setting of properties on the instance in subclasses by overriding
        /// this method.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual TBaseTemplateType InstantiateTemplateClass(Type type)
        {
          
            object inst = Activator.CreateInstance(type);
            TBaseTemplateType instance = inst as TBaseTemplateType;

            if (instance == null)
            {
                SetError(Resources.CouldnTActivateTypeInstance + type.FullName);
                return null;
            }

            instance.Engine = this;

            // If a HostContainer was set pass that to the template too
            instance.HostContainer = this.HostContainer;
            
            return instance;
        }

        /// <summary>
        /// Internally executes an instance of the template,
        /// captures errors on execution and returns true or false
        /// </summary>
        /// <param name="instance">An instance of the generated template</param>
        /// <returns>true or false - check ErrorMessage for errors</returns>
        protected virtual bool InvokeTemplateInstance(TBaseTemplateType instance, object context)
        {
            try
            {
                instance.Model = context;
                
                if (context != null)
                {
                    // if there's a model property try to 
                    // assign it from context
                    try
                    {
                        dynamic dynInstance = instance;
                        dynamic dcontext = context;
                        dynInstance.Model = dcontext;
                    }
                    catch(Exception ex) 
                    {
                        var msg = ex.Message;
                    }                    
                }

                instance.Execute();
            }
            catch (Exception ex)
            {
                this.SetError(Resources.TemplateExecutionError + ex.Message);
                return false;
            }
            finally
            {
                // Must make sure Response is closed
                instance.Response.Dispose();
            }
            return true;
        }

        /// <summary>
        /// Override to allow indefinite lifetime - no unloading
        /// </summary>
        /// <returns></returns>
        public override object  InitializeLifetimeService()
        {
 	         return null;
        }

        /// <summary>
        /// Adds an assembly to the ReferenceAssemblies based on an object instance.
        /// Easy way to add a model's assembly.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="referencedAssemblies"></param>
        public void AddReferencedAssemblyFromInstance(object model)
        {
            if (model != null)
            {
                string assemblyFile = model.GetType().Assembly.Location;
                string justFile = Path.GetFileName(assemblyFile).ToLower();
                if (!ReferencedAssemblies.Where(s => s.ToLower().Contains(justFile)).Any())
                        ReferencedAssemblies.Add(assemblyFile);
            }
        }


        /// <summary>
        /// Returns a unique ClassName for a template to execute
        /// Optionally pass in an objectId on which the code is based
        /// or null to get default behavior.
        /// 
        /// Default implementation just returns Guid as string
        /// </summary>
        /// <param name="objectId"></param>
        /// <returns></returns>
        protected virtual string GetSafeClassName(object objectId)
        {
            return "_" + Guid.NewGuid().ToString().Replace("-", "_");
        }

        /// <summary>
        /// Sets error information consistently
        /// </summary>
        /// <param name="message"></param>
        public void SetError(string message)
        {
            if (message == null)
                ErrorMessage = string.Empty;
            else
                ErrorMessage = message;            
        }

        public void SetError()
        {
            SetError(null);
        }
    }

    public enum CodeProvider
    { 
        CSharp,
        VisualBasic
    }
  }
