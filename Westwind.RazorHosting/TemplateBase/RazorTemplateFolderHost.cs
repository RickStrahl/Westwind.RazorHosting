using Westwind.RazorHosting.Properties;
using System;
using System.IO;
using System.Web;

namespace Westwind.RazorHosting
{
    /// <summary>
    /// Custom template implementation for the FolderHostContainer that supports 
    /// relative path based partial rendering.    
    /// 
    /// </summary>
    ///<typeparam name="TModel">Type parameter that determines the type of the Model
    /// property on this template. Note that the host container is responsible for
    /// assigning the model property in the ConfigData passed to the RenderTemplate.
    /// </typeparam>
    public class RazorTemplateFolderHost<TModel> : RazorTemplateFolderHost
        where TModel : class
    {
        /// <summary>
        /// Create a strongly typed model
        /// </summary>
        public new TModel Model { get; set; }

        public override void InitializeTemplate(object context, object configurationData)
        {
            if (configurationData == null)
            {
                if (context is TModel)
                    Model = context as TModel;
                return;
            }

            // Pick up configuration data and stuff into Request object
            RazorFolderHostTemplateConfiguration config = configurationData as RazorFolderHostTemplateConfiguration;

            this.Request.TemplatePath = config.TemplatePath;
            this.Request.TemplateRelativePath = config.TemplateRelativePath;

            // Just use the entire ConfigData as the model, but in theory 
            // configData could contain many objects or values to set on
            // template properties
            this.Model = config.ConfigData as TModel;
        }
    }


    /// <summary>
    /// Custom template implementation for the FolderHostContainer that supports 
    /// relative path based partial rendering.    
    /// </summary>
    public class RazorTemplateFolderHost : RazorTemplateBase        
    {                
      
        public override void InitializeTemplate(object model, object configurationData)
        {
            // Pick up configuration data and stuff into Request object
            RazorFolderHostTemplateConfiguration config = configurationData as RazorFolderHostTemplateConfiguration;

            this.Request.TemplatePath = config.TemplatePath;
            this.Request.TemplateRelativePath = config.TemplateRelativePath;
        }        


        /// <summary>
        /// Render a partial view based on a Web relative path
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="model"></param>
        /// <returns>HtmlString that indicates this string should not be escaped</returns>
        public HtmlString RenderPartial(string relativePath, object model)
        {
            if (this.HostContainer == null)
                return null;

            relativePath = Path.ChangeExtension(relativePath,"cshtml");

            // we don't know the exact type since it can be generic so make dynamic
            // execution possible with dynamic type
            dynamic hostContainer = HostContainer;

            // We need another configuration object in order to create 
            RazorFolderHostTemplateConfiguration config = new RazorFolderHostTemplateConfiguration()
            {
                TemplatePath = Path.GetFullPath(relativePath),
                TemplateRelativePath = relativePath                        
            };
                
            string output = null;
            Exception ex = null;
            // now execute the child request to a string
            try
            {                
                output = hostContainer.RenderTemplate(relativePath, model);
            }
            catch(Exception renderException)
            {
                ex = renderException;
            }

            if (output == null)
            {
                string error = Path.GetFileName(relativePath) + ": ";
                if(ex == null)
                    error += hostContainer.ErrorMessage;
                else
                    error += ex.GetBaseException().Message;                

                throw new ApplicationException(error, ex);
            }

            // return result as raw output
            return new HtmlString(output);
        }
    }    
}
