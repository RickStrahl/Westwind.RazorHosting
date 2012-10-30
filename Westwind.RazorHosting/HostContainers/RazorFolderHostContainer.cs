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
    /// This class is a caching directory based host wrapper around
    /// the RazorHost classes to provide directory based Razor
    /// template execution. Templates are compiled on 
    /// the fly, and cached unless the templates on disk are changed.
    /// 
    /// Runs Razor Templates in a seperate AppDomain
    /// 
    /// Uses the RazorTemplateFolderHost base template by default.
    /// For any other template implementation use the generic parameter
    /// to specify the template type.
    /// </summary>
    public class RazorFolderHostContainer : RazorFolderHostContainer<RazorTemplateFolderHost>
    {
    }

    /// <summary>
    /// This class is a caching directory based host wrapper around
    /// the RazorHost classes to provide directory based Razor
    /// template execution. Templates are compiled on 
    /// the fly, and cached unless the templates on disk are changed.
    /// 
    /// Runs Razor Templates in a seperate AppDomain
    /// </summary>
    /// <typeparam name="TBaseTemplate">The type of the base template to use</typeparam>
    public class RazorFolderHostContainer<TBaseTemplate> : RazorBaseHostContainer<TBaseTemplate>
        where TBaseTemplate : RazorTemplateFolderHost, new()
    {
        /// <summary>
        /// The Path where templates live
        /// </summary>
        public string TemplatePath { get; set; }

        /// <summary>
        /// When rendering to a file render output to this
        /// file.
        /// </summary>
        public string RenderingOutputFile { get; set; }

        public RazorFolderHostContainer()
        {
            BaseBinaryFolder = Environment.CurrentDirectory;

            // Default the template path underneath the binary folder as \templates
            TemplatePath = Path.Combine(BaseBinaryFolder, "templates");
        }

        /// <summary>
        /// Renders a template to a TextWriter. Useful to write output into a stream or
        /// the Response object. Used for partial rendering.
        /// </summary>
        /// <param name="relativePath">Relative path to the file in the folder structure</param>
        /// <param name="context">Optional context object or null</param>
        /// <param name="model">Optional parameter that is set as the Model property in generic versions</param>
        /// <param name="writer">The textwriter to write output into</param>
        /// <returns></returns>
        public string RenderTemplate(string relativePath, object model = null, TextWriter writer = null)
        {
            CompiledAssemblyItem item = GetAssemblyFromFileAndCache(relativePath);
            if (item == null)
            {
                if (writer != null)
                    writer.Close();
                return null;
            }

            // Set configuration data that is to be passed to the template (any object) 
            Engine.TemplatePerRequestConfigurationData = new RazorFolderHostTemplateConfiguration()
            {
                TemplatePath = Path.Combine(this.TemplatePath, relativePath),
                TemplateRelativePath = relativePath
            };

            string result = null;
            try
            {
                // String result will be empty as output will be rendered into the
                // Response object's stream output. However a null result denotes
                // an error 
                result = Engine.RenderTemplateFromAssembly(item.AssemblyId, model, writer);

                if (result == null)
                    this.SetError(Engine.ErrorMessage);
            }
            catch (Exception ex)
            {
                this.SetError(ex.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Close();

                // Clear out the per request cache
                Engine.TemplatePerRequestConfigurationData = null;
            }

            return result;
        }

   
        /// <summary>
        /// Internally checks if a cached assembly exists and if it does uses it
        /// else creates and compiles one. Returns an assembly Id to be 
        /// used with the LoadedAssembly list.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        protected virtual CompiledAssemblyItem GetAssemblyFromFileAndCache(string relativePath)
        {
            var path = relativePath.Replace("/", "\\").Replace("~\\", "");

            string fileName = Path.Combine(TemplatePath,path).ToLower();
            int fileNameHash = fileName.GetHashCode();
            if (!File.Exists(fileName))
            {
                this.SetError(Westwind.RazorHosting.Properties.Resources.TemplateFileDoesnTExist + fileName);
                return null;
            }

            CompiledAssemblyItem item = null;
            this.LoadedAssemblies.TryGetValue(fileNameHash, out item);

            string assemblyId = null;

            // Check for cached instance
            if (item != null)
            {
                var fileTime = File.GetLastWriteTimeUtc(fileName);
                if (fileTime <= item.CompileTimeUtc)
                    assemblyId = item.AssemblyId;
            }
            else
                item = new CompiledAssemblyItem();

            // No cached instance - create assembly and cache
            if (assemblyId == null)
            {
                string safeClassName = GetSafeClassName(fileName);

                string template = null;
                try
                {
                    template = File.ReadAllText(fileName);
                }
                catch
                {
                    this.SetError(Westwind.RazorHosting.Properties.Resources.ErrorReadingTemplateFile + fileName);
                    return null;
                }
                assemblyId = Engine.CompileTemplate(template);

                // need to ensure reader is closed
                //if (reader != null)
                //    reader.Close();

                if (assemblyId == null)
                {
                    this.SetError(Engine.ErrorMessage);
                    return null;
                }

                item.AssemblyId = assemblyId;
                item.CompileTimeUtc = DateTime.UtcNow;
                item.FileName = fileName;
                item.SafeClassName = safeClassName;

                this.LoadedAssemblies[fileNameHash] = item;
            }

            return item;
        }


        /// <summary>
        /// Determine if a file has been changed since a known date.
        /// Dates are specified in UTC format.
        /// </summary>
        /// <param name="relativePath">relative path to the template root.</param>
        /// <param name="originalTimeUtc"></param>
        /// <returns></returns>
        protected virtual bool HasFileChanged(string relativePath, DateTime originalTimeUtc)
        {
            string fileName = Path.Combine(this.TemplatePath, relativePath);
            DateTime lastWriteTime = File.GetLastWriteTimeUtc(fileName);

            if (lastWriteTime > originalTimeUtc)
                return true;

            return false;
        }

        /// <summary>
        /// Overridden to return a unique name based on the filename
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected override string GetSafeClassName(object objectId)
        {
            string fileName = objectId as string;
            fileName = Utilities.GetRelativePath(fileName, TemplatePath);
            return Path.GetFileNameWithoutExtension(fileName).Replace("\\", "_");
        }

     
     
    }

    /// <summary>
    /// Item that stores information about a cached assembly
    /// that keeps track of templates that have been compiled
    /// and cached.
    /// </summary>
    public class CompiledAssemblyItem
    {
        public string AssemblyId { get; set; }
        public DateTime CompileTimeUtc { get; set; }
        public string FileName { get; set; }
        public string SafeClassName { get; set; }
        public string Namespace { get; set; }
    }
}
