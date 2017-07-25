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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Westwind.RazorHosting
{
    
    /// <summary>
    /// Razor Host container to execute Razor Templates from string input.
    /// 
    /// Can run templates in a separate AppDomain and caches templates
    /// to avoid re-compilation and allocation of new resources for 
    /// each template.
    /// </summary>
    public class RazorStringHostContainer : RazorBaseHostContainer<RazorTemplateBase>
    {        

        public RazorStringHostContainer()
        {
            BaseBinaryFolder = Environment.CurrentDirectory;            
        }

        /// <summary>
        /// Call this method to actually render a template to the specified outputfile
        /// </summary>"
        /// <param name="templateText">The template text to parse and render</param>        
        /// <param name="model">
        /// Any object that will be available in the template as a dynamic of this.Context or
        /// if the type matches the template type this.Model.
        /// </param>
        /// <param name="writer">Optional textwriter that output is written to</param>
        /// <param name="inferModelType">If true infers the model type if no @model or @inherits tag is provided</param>
        /// <returns>rendering results or null on failure. If a writer is a passed string.Empty is returned or null for failure</returns>
        public string RenderTemplate(string templateText, 
                                        object model = null, 
                                        TextWriter writer = null, 
                                        bool inferModelType = false) 
        {
            if (inferModelType && model != null &&
                !templateText.Trim().StartsWith("@model ") &&
                !templateText.Trim().StartsWith("@inherits "))
                templateText = "@model " + model.GetType().FullName + "\r\n" + templateText;

            CompiledAssemblyItem assItem = GetAssemblyFromStringAndCache(templateText);
            if (assItem == null)
                return null;

            // String result will be empty as output will be rendered into the
            // Response object's stream output. However a null result denotes
            // an error 
            string result = Engine.RenderTemplateFromAssembly(assItem.AssemblyId, model, writer);

            if (result == null)
            {
                SetError(Engine.ErrorMessage);
                return null;     
            }               

            return result;
        }


        /// <summary>
        /// Renders a template from a string input to a file output.
        /// Same text templates are compiled and cached for re-use.
        /// </summary>
        /// <param name="templateText">Text of the template to run</param>
        /// <param name="model">Optional model to pass</param>
        /// <param name="outputFile">Output file where output is sent to</param>
        /// <param name="inferModelType">If true infers the model type if no @model or @inherits tag is provided</param>
        /// <returns></returns>
        public bool RenderTemplateToFile(string templateText, object model, string outputFile, bool inferModelType = false) 
        {

            if (inferModelType && model != null &&
                !templateText.Trim().StartsWith("@model ") &&
                !templateText.Trim().StartsWith("@inherits "))
                            templateText = "@model " + model.GetType().FullName + "\r\n" + templateText;

            CompiledAssemblyItem assItem = GetAssemblyFromStringAndCache(templateText);
            if (assItem == null)
                return false;
            
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(outputFile, false, 
                    Engine.Configuration.OutputEncoding, 
                    Engine.Configuration.StreamBufferSize);
            }
            catch(Exception ex)
            {
                SetError("Unable to write template output to " + outputFile + ": " + ex.Message);
                return false;
            }

            return RenderTemplateFromAssembly(assItem.AssemblyId, model, writer);
        }

        /// <summary>
        /// Internally tries to retrieve a previously compiled template from cache
        /// if not found compiles a template into an assembly
        /// always returns an assembly id as a string.
        /// </summary>
        /// <param name="templateText">The text to parse</param>
        /// <returns>assembly id as a string or null on error</returns>
        protected virtual CompiledAssemblyItem GetAssemblyFromStringAndCache(string templateText)
        {
            int hash = templateText.GetHashCode();

            CompiledAssemblyItem item = null;
            LoadedAssemblies.TryGetValue(hash, out item);

            string assemblyId = null;

            // Check for cached instance
            if (item != null)
                assemblyId = item.AssemblyId;
            else
                item = new CompiledAssemblyItem();

            // No cached instance - create assembly and cache
            if (assemblyId == null)
            {
                string safeClassName = GetSafeClassName(null);
                assemblyId = Engine.CompileTemplate(templateText, GeneratedNamespace, safeClassName);

                if (assemblyId == null)
                {
                    SetError(Engine.ErrorMessage);
                    return null;
                }

                item.AssemblyId = assemblyId;
                item.CompileTimeUtc = DateTime.UtcNow;
                item.SafeClassName = safeClassName;

                LoadedAssemblies[hash] = item;
            }

            return item;
        }
    }
}
