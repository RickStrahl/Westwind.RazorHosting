using System;

namespace Westwind.RazorHosting
{

    /// <summary>
    /// Configuration objects that can be passed to templates to pass additional 
    /// information down to the templates from a Host container
    /// </summary>
    [Serializable]
    public class RazorTemplateConfiguration
    {        
        /// <summary>
        /// Use this object to pass configuration data to the template
        /// </summary>
        public object ConfigData;
    }

    /// <summary>
    /// Folder Host specific configuration object
    /// </summary>
    [Serializable]
    public class RazorFolderHostTemplateConfiguration : RazorTemplateConfiguration
    {
        public string TemplatePath = string.Empty;
        public string TemplateRelativePath = string.Empty;
        public string PhysicalPath = string.Empty;
    }
    

}
