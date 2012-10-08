#Westwind.RazorHosting
**Hosting the Razor Runtime outside of ASP.NET MVC/WebPages**
* * * 

This library allows you to host the Razor template engine found in ASP.NET MVC and ASP.NET WebPages 
in your own applications and ASP.NET applications outside of MVC and Web Pages.

Razor is a great tool for text templating using HTML like syntax mixed with C# code. The engine can
be used for things like text merging for things like Mail Merge operations, HTML reports or HTML
displays in desktop applications, code generation and much more. You can use it in Desktop applications
as well as in any ASP.NET application that doesn't already have ready access to the Razor View engine.

* [http://nuget.org/packages/Westwind.RazorHosting](Install from NuGet (Westwind.RazorHosting) )
* [http://www.west-wind.com/wwThreads/default.asp?Forum=West+Wind+.NET+Tools+and+Demos](Questions and Discussion of Westwind.RazorHosting)

> **Note!**
> The RazorHosting engine provides core templating functionality of the Razor sytnax engine. This means that
> all C# language features and all of Razor's basic expression and logic parsing features work.
> However, it does not provide full parity with either the MVC or WebPages implementation, since both
> of these engines are closely tied to ASP.NET semantics. Things like HTML and URL Helpers, Sections and Layout
> pages are not supported. Partials are supported only in the Folder Host implementation
>
> RazorHosting only supports C# - there's no support for Visual Basic.

##Basic Usage
This library provides both a basic Razor Engine implementation and a couple of Razor Host implementations. 
The host implementations provide an environment for caching templates and hosting the Razor runtime in
an alternate AppDomain to control memory usage. The folder host container also supports partial templates
and accessing templates via virtual path syntax (~/folder/page.cshtml).

###Plain RazorEngine Usage
Plain RazorEngine usage is easiest, but it also provides no caching so everytime you evaluate
a template, the template is reparsed, recompiled and reloaded unless you explicitly compile
a template and cache it yourself.

To execute a template:

    string template = @"Hello World @Model.Name. Time is: @DateTime.Now";
	var host = new RazorEngine<RazorTemplateBase>();
    string result = host.RenderTemplate(template,new Person() { Name="Joe Doe" }");

You can also create a template and cache it:

    string template = @"Hello World @Model.Name. Time is: @DateTime.Now";
	var host = new RazorEngine<RazorTemplateBase>();
	host.AddAssembly("System.Data.dll");  // add any assemblies you need in templates            
    
    string compiledId = host.CompileTemplate(template);    
	string result = host.RenderTemplateFromAssembly(compiledId,
													new Person() { Name = "Joe Doe" });

    // Run again later without recompilation
	string result = host.RenderTemplateFromAssembly(compiledId,
													new Person() { Name = "Rick Strahl" });

The latter allows you to capture the compiled id which points to a cached assembly instance
in the current RazorEngine instance. This avoids having to reparse and recompile the template
each time it's executed and doesn't generate a new assembly in memory (or on disk) each time
which saves memory.

All templates include a model property and the RenderTemplate method can pass in a model.
By default models are of type _dynamic_, but the model can also be explicitly typed by
using the Razor @inherits tag:

	@inherits RazorTemplateBase<RazorHostingTests.Person>

This is equivalent to MVC's @model property and represents the native Razor syntax for
determining the base class, which is RazorTemplateBase<T> here (or your custom subclass thereof).
If no @inherits is specified RazorTemplateBase is used with a dynamic Model.

###Using Host Containers
Host Containers wrap the basic RazorEngine described above by providing the abililty to load
into a seperate AppDomain (optionally) and to provide automatic caching services so you don't
have to track compiled assemblies. Host containers also figure out whether a template needs
to be recreated if the template is changed. 

HostContainers are meant to be instantiated once and then left running for the duration
of an application or long running operation, processing many template requests during
their lifecycle.

There are two HostContainers:

####RazorStringHostContainer####
StringHostContainer executes templates from string, but caches compiled templates based on
the template's content. IOW, running the same exact template twice will automatically compile
on the first run, and use the cached version on the second and subsequent runs. As long as the
the template string is identical the cached assembly is used.

To run a String Template Host:

    var host = new RazorStringHostContainer();
            
    // add model assembly - ie. this assembly
    host.AddAssemblyFromType(this);
    host.AddAssembly("System.Data.dll");

    host.UseAppDomain = true;

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
	string template = @"@inherits Westwind.RazorTemplateBase<RazorHostingTests.Person>
	<b>@Model.Name of @Model.Company entered on @Model.Entered";
    
    string result = host.RenderTemplate(string,person);
    	
    Console.WriteLine(result);
    Console.WriteLine("---");
    Console.WriteLine(host.Engine.LastGeneratedCode);

    if (result == null)
        Assert.Fail(host.ErrorMessage);
            
    host.Stop();

With a host container you typically will run many requests between the Start() and Stop() operations.

####RazorFolderHostContainer####
FolderHost container can be used to point to a folder on disk and treat it like a virtual directory
for rendering templates from disk. Templates are loaded based on a virtual path (~/sub/page.cshtml)
and support the abililty to use @RenderPartial() to render partials.

To run folder host templates:

	var host = new RazorFolderHostContainer();

	host.TemplatePath = Path.GetFullPath(@"..\..\FileTemplates\");
	host.BaseBinaryFolder = Environment.CurrentDirectory;

	// add model assembly - ie. this assembly
	host.AddAssemblyFromType(typeof(Person));

	host.UseAppDomain = true;
	//host.Configuration.CompileToMemory = true;
	//host.Configuration.TempAssemblyPath = Environment.CurrentDirectory;

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

	string result = host.RenderTemplate("~/HelloWorld.cshtml", person);

	Console.WriteLine(result);
	Console.WriteLine("---");
	Console.WriteLine(host.Engine.LastGeneratedCode);

	host.Stop();


	if (result == null)
		Assert.Fail(host.ErrorMessage);

	Assert.IsTrue(result.Contains("West Wind"));
   
where the template might look like this:

	@inherits RazorTemplateFolderHost<RazorHostingTests.Person>
	<!DOCTYPE html>
	<html>
	<head>
		<meta charset="utf-8" />
		<title></title>
	</head>
	<body>    
        @RenderPartial("~/Header_Partial.cshtml",Model)

		@Model.Name @Model.Company @Model.Address.City @Model.Entered 
    
		@{for (int i = 0; i < 10; i++)
		  {
			  Response.WriteLine(i + ".");
		  }   
	</body>
	</html>

Note that you can render partials, by specifying the virtual path for the partial.

###Limitations
Unlike MVC and/or WebPages the RazorHosting engine only supports core Razor functinonality
so it's not equivalent to the feature set provided by MVC or WebPages. As such many common
features like HTML and URL helpers, @section and @Layout are not available in this implementation
since these features are specific to MVC/WebPages and their close ties to the HTTP features
provided in ASP.NET.

##License
This library is published under MIT license terms:

Copyright © 2012 Rick Strahl, West Wind Technologies

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial 
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.