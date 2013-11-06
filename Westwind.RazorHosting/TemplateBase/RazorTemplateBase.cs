using System;
using System.Diagnostics;
using System.Net;

namespace Westwind.RazorHosting
{

    /// <summary>
    /// Base class used for Razor Page Templates - Razor generates
    /// a class from the parsed Razor markup and this class is the
    /// base class. Class must implement an Execute() method that 
    /// is overridden by the parser and contains the code that generates
    /// the markup.  Write() and WriteLiteral() must be implemented
    /// to handle output generation inside of the Execute() generated
    /// code.
    /// 
    /// This class can be subclassed to provide custom functionality.
    /// One common feature likely will be to provide Context style properties
    /// that are application specific (ie. HelpBuilderContext) and strongly
    /// typed and easily accesible in Razor markup code.   
    /// </summary>
    public class RazorTemplateBase<TModel> : RazorTemplateBase
        where TModel : class, new()
    {
        /// <summary>
        /// Create a strongly typed model
        /// </summary>
        public new TModel Model { get; set; }

        /// <summary>
        /// This method is called upon instantiation
        /// and allows passing custom configuration
        /// data to the template from the Engine.
        /// 
        /// This method can then be overridden        
        /// </summary>
        /// <param name="configurationData"></param>
        public override void InitializeTemplate(object model, object configurationData = null)
        {
            Html = new HtmlHelper();

            if (model is TModel)
                Model = model as TModel;           
        }
    }

    /// <summary>
    /// Base class used for Razor Page Templates - Razor generates
    /// a class from the parsed Razor markup and this class is the
    /// base class. Class must implement an Execute() method that 
    /// is overridden by the parser and contains the code that generates
    /// the markup.  Write() and WriteLiteral() must be implemented
    /// to handle output generation inside of the Execute() generated
    /// code.
    /// 
    /// This class can be subclassed to provide custom functionality.
    /// One common feature likely will be to provide Context style properties
    /// that are application specific (ie. HelpBuilderContext) and strongly
    /// typed and easily accesible in Razor markup code.   
    /// </summary>
    public class RazorTemplateBase : MarshalByRefObject, IDisposable
    {
        /// <summary>
        /// You can pass in a generic context object
        /// to use in your template code
        /// </summary>
        public dynamic Model { get; set; }


        /// <summary>
        /// Simplistic Html Helper implementation
        /// </summary>
        public HtmlHelper Html { get; set; }

        /// <summary>
        /// An optional result property that can receive a 
        /// a processing result that can be passed back to the
        /// the caller.
        /// </summary>
        public dynamic ResultData { get; set; }
        
        /// <summary>
        /// Class that generates output. Currently ultra simple
        /// with only Response.Write() implementation.
        /// </summary>
        public RazorResponse Response { get; set; }


        /// <summary>
        /// Class that provides request specific information.
        /// May or may not have its member data set.
        /// </summary>
        public RazorRequest Request { get; set; }


        /// <summary>
        /// Instance of the HostContainer that is hosting
        /// this Engine instance. Note that this may be null
        /// if no HostContainer is used.
        /// 
        /// Note this object needs to be cast to the 
        /// the appropriate Host Container
        /// </summary>
        public object HostContainer { get; set; }

        /// <summary>
        /// Instance of the RazorEngine object.
        /// </summary>
        public object Engine { get; set; }        


        /// <summary>
        /// This method is called upon instantiation
        /// and allows passing custom configuration
        /// data to the template from the Engine.
        /// 
        /// This method can then be overridden        
        /// </summary>
        /// <param name="configurationData"></param>
        public virtual void InitializeTemplate(object model = null, object configurationData = null)
        {
            Html = new HtmlHelper();
            Model = model;
        }

        public RazorTemplateBase()
        {
            Response = new RazorResponse();
            Request = new RazorRequest();
        }

        /// <summary>
        /// Writes a literal string. Used to write generic text
        /// from the page markup (ie. non-expression text)
        /// </summary>
        /// <param name="value"></param>
        public virtual void WriteLiteral(object value)
        {
            Response.Write(value);
        }

        /// <summary>
        /// Writes an expression value. This value is HtmlEncoded always
        /// </summary>
        /// <param name="value"></param>
        public virtual void Write(object value = null)
        {
            if (value is RawString)
            {
                // Write as raw string without encoding
                WriteLiteral(value.ToString());
            }
            else
            {
                // For HTML output we'd probably want to HTMLEncode everything            
                // But not for plain text templating
                WriteLiteral(Utilities.HtmlEncode(value));
            }
        }

        /// <summary>
        /// WriteAttribute implementation lifted from ANurse's MicroRazor Implementation
        /// and the AspWebStack source.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="prefix"></param>
        /// <param name="suffix"></param>
        /// <param name="values"></param>
        public virtual void WriteAttribute(string name, PositionTagged<string> prefix, 
                                           PositionTagged<string> suffix, params AttributeValue[] values)
        {
            bool first = true;
            bool wroteSomething = false;
            if (values.Length == 0)
            {
                // Explicitly empty attribute, so write the prefix and suffix
                WritePositionTaggedLiteral(prefix);
                WritePositionTaggedLiteral(suffix);
            }
            else
            {
                for (int i = 0; i < values.Length; i++)
                {
                    AttributeValue attrVal = values[i];
                    PositionTagged<object> val = attrVal.Value;
                    PositionTagged<string> next = i == values.Length - 1 ?
                        suffix : // End of the list, grab the suffix
                        values[i + 1].Prefix; // Still in the list, grab the next prefix

                    bool? boolVal = null;
                    if (val.Value is bool)
                    {
                        boolVal = (bool)val.Value;
                    }

                    if (val.Value != null && (boolVal == null || boolVal.Value))
                    {
                        string valStr = val.Value as string;
                        if (valStr == null)
                        {
                            valStr = val.Value.ToString();
                        }
                        if (boolVal != null)
                        {
                            Debug.Assert(boolVal.Value);
                            valStr = name;
                        }

                        if (first)
                        {
                            WritePositionTaggedLiteral(prefix);
                            first = false;
                        }
                        else
                        {
                            WritePositionTaggedLiteral(attrVal.Prefix);
                        }

                        // Calculate length of the source span by the position of the next value (or suffix)
                        int sourceLength = next.Position - attrVal.Value.Position;

                        if (attrVal.Literal)
                        {
                            WriteLiteral(valStr);
                        }
                        else
                        {
                            Write(valStr); // Write value
                        }
                        wroteSomething = true;
                    }
                }
                if (wroteSomething)
                    WritePositionTaggedLiteral(suffix);
            }
        }

        private void WritePositionTaggedLiteral(string value, int position)
        {
            WriteLiteral(value);
        }

        private void WritePositionTaggedLiteral(PositionTagged<string> value)
        {
            WritePositionTaggedLiteral(value.Value, value.Position);
        }


        public virtual string HtmlEncode(string input)
        {
            return WebUtility.HtmlEncode(input);
        }   

        /// <summary>
        /// Allows rendering a dynamic template from within the
        /// running template. The template passed must be a string
        /// and you can pass a model for rendering.
        /// 
        /// This is useful to support nested templating for allowing
        /// rendered values to contain embedded Razor template expressions
        /// which is useful where user generated content may contain
        /// Razor template logic.
        /// </summary>
        /// <param name="template"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual string RenderTemplate(string template,object model)
        {            
            if (template == null)
                return string.Empty;
            
            if(!template.Contains("@"))
                return template;

            // use dynamic to get around generic type casting
            dynamic engine = Engine;
            string result = engine.RenderTemplate(template, model);
            if (result == null)
                throw new ApplicationException("RenderTemplate failed: " + engine.ErrorMessage);
                       
            return result;
        }

        /// <summary>
        /// Razor Parser overrides this method
        /// </summary>
        public virtual void Execute() { }


        public virtual void Dispose()
        {
            if (Response != null)
            {
                Response.Dispose();
                Response = null;
            }
        }

        /// <summary>
        /// Force this host to stay alive indefinitely
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService()
        {
            return null;
        }

        #region Old_WriteAttribute_Implementations
        #if false
        /// <summary>
        /// This method is used to write out attribute values using
        /// some funky nested tuple storage.
        /// 
        /// Handles situations like href="@Model.Entry.Id"
        /// 
        /// This call comes in from the Razor runtime parser
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="tokens"></param>
        public virtual void WriteAttribute(string attr,
            //params object[] parms)
                                           Tuple<string, int> token1,
                                           Tuple<string, int> token2,
                                           Tuple<Tuple<string, int>,
                                           Tuple<object, int>, bool> token3)
        {
            object value = null;
        
            if (token3 != null)
                value = token3.Item2.Item1;
            else
                value = string.Empty;
        
            var output = token1.Item1 + value.ToString() + token2.Item1;
        
            Response.Write(output);
        }

        /// <summary>
        /// This method is used to write out attribute values using
        /// some funky nested tuple storage.
        /// 
        /// Handles situations like href="@(Model.Url)?parm1=1"
        /// where text and expressions mix in the attribute
        /// 
        /// This call comes in from the Razor runtime parser
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="tokens"></param>
        public virtual void WriteAttribute(string attr,
            //params object[] parms)
                                   Tuple<string, int> token1,
                                   Tuple<string, int> token2,
                                   Tuple<Tuple<string, int>,
                                         Tuple<object, int>, bool> token3,
                                   Tuple<Tuple<string, int>,
                                         Tuple<string, int>, bool> token4)
        {
            //            WriteAttribute("href", 
            //                Tuple.Create(" href=\"", 395), 
            //                Tuple.Create("\"", 452), 
            //                Tuple.Create(Tuple.Create("", 402), Tuple.Create<System.Object, System.Int32>("Value", 402), false),
            //                Tuple.Create(Tuple.Create("", 439), Tuple.Create("?action=login", 439), true)            
            object value = null;
            object textval = null;
            if (token3 != null)
                value = token3.Item2.Item1;
            else
                value = string.Empty;

            if (token4 != null)
                textval = token4.Item2.Item1;
            else
                textval = string.Empty;

            var output = token1.Item1 + value.ToString() + textval.ToString() + token2.Item1;

            Response.Write(output);
        }
        #endif
        #endregion
    }
}

