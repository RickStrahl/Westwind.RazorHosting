using System;
using System.IO;

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
            Html = new HtmlHelper();

            if (configurationData == null)
            {
                if (context is TModel)
                    Model = context as TModel;
                return;
            }

            // Pick up configuration data and stuff into Request object
            RazorFolderHostTemplateConfiguration config = configurationData as RazorFolderHostTemplateConfiguration;

            Request.TemplatePath = config.TemplatePath;
            Request.TemplateRelativePath = config.TemplateRelativePath;
            

            // Just use the entire ConfigData as the model, but in theory 
            // configData could contain many objects or values to set on
            // template properties
            Model = config.ModelData as TModel;
        }
    }


    /// <summary>
    /// Custom template implementation for the FolderHostContainer that supports 
    /// relative path based partial rendering.    
    /// </summary>
    public class RazorTemplateFolderHost : RazorTemplateBase        
    {
        private string _layout;

        /// <summary>
        /// The layout page for this template
        /// </summary>
        public string Layout
        {
            get { return _layout; }
            set
            {
                _layout = value;

                dynamic engine = Engine;
                var config = engine?.TemplatePerRequestConfigurationData as RazorTemplateConfiguration;
                if (config != null)
                {
                    ((RazorFolderHostTemplateConfiguration) config).LayoutPage = value;
                }
            }
        }

        /// <summary>
        /// Hold template configuration data. for this implementation the
        /// Layout page and template paths are important.
        /// </summary>
        public new RazorFolderHostTemplateConfiguration TemplateConfigData
        {
            get
            {
                if (_templateConfigData != null)
                    return _templateConfigData;

                var engine = Engine as RazorEngine;
                var config = engine?.TemplatePerRequestConfigurationData as RazorFolderHostTemplateConfiguration;
                _templateConfigData = config;
                return config;
            }
        }
        private RazorFolderHostTemplateConfiguration _templateConfigData;


        public override void InitializeTemplate(object model, object configurationData)
        {
            Html = new HtmlHelper();

            // Pick up configuration data and stuff into Request object
            RazorFolderHostTemplateConfiguration config = configurationData as RazorFolderHostTemplateConfiguration;

            Request.TemplatePath = config.TemplatePath;
            Request.TemplateRelativePath = config.TemplateRelativePath;
        }        


        /// <summary>
        /// Render a partial view based on a Web relative path
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="model"></param>
        /// <returns>HtmlString that indicates this string should not be escaped</returns>
        public RawString RenderPartial(string relativePath, object model)
        {
            if (HostContainer == null)
                return null;

            if(!Path.HasExtension(relativePath))
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
            return new RawString(output);
        }
        
        /// <summary>
        /// Overridden so that we don't fail if this encountered
        /// in the body. Echo'd back out by default. HostContainers
        /// may do something withe @RenderBody() result.
        /// </summary>
        /// <returns></returns>
        public virtual RawString RenderBody()
        {
            return new RawString("@RenderBody()");
        }        
    }    
}
