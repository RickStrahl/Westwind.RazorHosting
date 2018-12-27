#pragma warning disable CS1591
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
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using Westwind.RazorHosting.Properties;

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
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
                BaseBinaryFolder = Environment.CurrentDirectory;
            else
                BaseBinaryFolder = assembly.Location;


            // Default the template path underneath the binary folder as \templates
            TemplatePath = Path.Combine(BaseBinaryFolder, "templates");
        }

        /// <summary>
        /// Renders a template to a TextWriter. Useful to write output into a stream or
        /// the Response object. Used for partial rendering.
        /// </summary>
        /// <param name="relativePath">Relative path to the file in the folder structure</param>
        /// <param name="model">Optional parameter that is set as the Model property in generic versions</param>
        /// <param name="writer">The textwriter to write output into</param>
        /// <param name="isLayoutPage"></param>
        /// <returns></returns>
        public string RenderTemplate(string relativePath, object model = null, TextWriter writer = null, bool isLayoutPage = false)
        {
            LastException = null;

            CompiledAssemblyItem item = GetAssemblyFromFileAndCache(relativePath);
            if (item == null)
            {
                if (writer != null)
                    writer.Close();
                return null;
            }

            var previousConfigurationData = Engine.TemplatePerRequestConfigurationData;

            // Set configuration data that is to be passed to the template (any object) 
            Engine.TemplatePerRequestConfigurationData = new RazorFolderHostTemplateConfiguration()
            {
                TemplatePath = Path.Combine(TemplatePath, relativePath.Replace("~/","").Replace("~","")),
                TemplateRelativePath = relativePath,
                LayoutPage = null,
                IsLayoutPage = isLayoutPage
            };

            string result = null;
            try
            {
                // String result will be empty as output will be rendered into the
                // Response object's stream output. However a null result denotes
                // an error 
                result = Engine.RenderTemplateFromAssembly(item.AssemblyId, model, writer);

                var config = Engine.TemplatePerRequestConfigurationData as RazorFolderHostTemplateConfiguration;
                
                if (result == null)
                {
                    LastException = new RazorHostContainerException($"Failed to render Page {relativePath}: " +
                                                                Engine.ErrorMessage,
                        Engine.LastGeneratedCode,
                        Engine.LastException,
                        config.TemplatePath,
                        config);
                    return null;
                }


                if (config != null && !config.IsLayoutPage  && !string.IsNullOrEmpty(config.LayoutPage))
                {
                    string layoutTemplate = config.LayoutPage;                    
                    config.IsLayoutPage = false;
                    
                    string layout = RenderTemplate(layoutTemplate, model, writer,isLayoutPage: true);
                    if (layout == null)
                    {
                        LastException = new RazorHostContainerException($"Failed to render Layout Page {layoutTemplate}: " +
                                                        Engine.ErrorMessage, 
                            Engine.LastGeneratedCode,
                            Engine.LastException, 
                            Path.Combine(TemplatePath,layoutTemplate),
                            config);                        
                        result = null;
                    }
                    else
                    {
                        result = layout.Replace("@RenderBody()", result);
                    }
                }
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
            }
            finally
            {                
                writer?.Close();

                // reset per request cache
                Engine.TemplatePerRequestConfigurationData = previousConfigurationData;
            }

            return result;
        }


        /// <summary>
        /// Renders an HTML 
        /// </summary>
        /// <param name="noTemplateSourceCode"></param>
        /// <returns></returns>
        public override string RenderHtmlErrorPage(bool noTemplateSourceCode = false)
        {
            string result = base.RenderHtmlErrorPage(noTemplateSourceCode);

            if (LastException != null)
            {
                if (!string.IsNullOrEmpty(LastException.ActiveTemplate))
                    result = result.Replace("</body></html>",
                        "<hr />" +
                        "<small>Source template: <b><a target='_newwindow' href='file:///" +
                        LastException.ActiveTemplate + "'>" + LastException.ActiveTemplate + "</a></b>" +
                        "</small></body></html>");
            }

            return result;
        }


        /// <summary>
        /// Internally checks if a cached assembly exists and if it does uses it
        /// else creates and compiles one. Returns an assembly Id to be 
        /// used with the LoadedAssembly list.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        protected virtual CompiledAssemblyItem GetAssemblyFromFileAndCache(string relativePath)
        {
            LastException = null;

            var path = relativePath.Replace("/", "\\").Replace("~\\", "");

            string filename = Path.Combine(TemplatePath,path).ToLower();
            int fileNameHash = filename.GetHashCode();
            if (!File.Exists(filename))
            {
                SetErrorException(new RazorHostContainerException(Westwind.RazorHosting.Properties.Resources.TemplateFileDoesnTExist + filename,
                    null,
                    null,
                    filename,
                    null));             
                return null;
            }

            CompiledAssemblyItem item = null;
            LoadedAssemblies.TryGetValue(fileNameHash, out item);

            string assemblyId = null;

            // Check for cached instance
            if (item != null)
            {
                // Check against file time - if file is newer we need to recompile the template
                var fileTime = File.GetLastWriteTimeUtc(filename);
                if (fileTime <= item.CompileTimeUtc)
                    assemblyId = item.AssemblyId;   // no change, used cached
            }
            else
                item = new CompiledAssemblyItem();

            // No cached instance - create assembly and cache
            if (assemblyId == null)
            {
                string safeClassName = GetSafeClassName(filename);

                string template = null;
                try
                {
                    template = File.ReadAllText(filename);
                }
                catch
                {
                    this.SetErrorException(new RazorHostContainerException(Resources.ErrorReadingTemplateFile + filename,
                        null,
                        null,
                        filename,
                        null));                    
                    return null;
                }
                assemblyId = Engine.CompileTemplate(template);

                // need to ensure reader is closed
                //if (reader != null)
                //    reader.Close();

                if (assemblyId == null)
                {
                    SetError();
                    this.SetErrorException(new RazorHostContainerException(Engine.ErrorMessage,
                        Engine.LastGeneratedCode,
                        Engine.LastException,
                        filename,
                        null));
                    return null;
                }

                //ExtractLayoutPage(template, item);

                item.AssemblyId = assemblyId;
                item.CompileTimeUtc = DateTime.UtcNow;
                item.FileName = filename;
                item.SafeClassName = safeClassName;

                LoadedAssemblies[fileNameHash] = item;
            }

            return item;
        }


        private static Regex LayoutRegEx = new Regex("@{.*?Layout = \"(.*?)\";.*?}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        //private static void ExtractLayoutPage(string template, CompiledAssemblyItem item)
        //{

        //    if (string.IsNullOrEmpty(template))
        //        return;

        //    var matches = LayoutRegEx.Match(template);
        //    if (matches.Length > 0)
        //    {
        //        if (matches.Groups.Count > 1)
        //            item.LayoutPage = matches.Groups[1].Value;
        //    }
        //}




        /// <summary>
        /// Determine if a file has been changed since a known date.
        /// Dates are specified in UTC format.
        /// </summary>
        /// <param name="relativePath">relative path to the template root.</param>
        /// <param name="originalTimeUtc"></param>
        /// <returns></returns>
        protected virtual bool HasFileChanged(string relativePath, DateTime originalTimeUtc)
        {
            string fileName = Path.Combine(TemplatePath, relativePath);
            DateTime lastWriteTime = File.GetLastWriteTimeUtc(fileName);

            if (lastWriteTime > originalTimeUtc)
                return true;

            return false;
        }

        /// <summary>
        /// Overridden to return a unique name based on the filename
        /// </summary>
        /// <param name="objectId">Filename</param>
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

        public string LayoutPage { get; set;  }
    }
}
