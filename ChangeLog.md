#Westwind.RazorHosting Change Log
* * * 

##Version 3.0
November 15th, 2013

* **Update to Razor 3.0**
  Updated to the latest version of the Razor Engine distributed
  with MVC 5. 

* **Require .NET 4.5 to run**
  The Razor Libraries version 3 require .NET 4.5 so this library
  is updating to version 3 as well.

* **Version numbers in Sync with Razor Version**
  Decided to keep the library version in sync with Razor version.

##Version 1.01
*December 20th, 2012*

* **Changed default bin folder location**  
  Default folder location is now loaded from GetEntryAssembly() location by default and falls back to current directory if that can't resolve. BaseBinary folder can still be used to explicitly override the path.

* **Removed System.Web dependency**  
  Removed dependency on System.Web for client applications. One drawback: 
  No access to HtmlString(), for raw result values, so templates use a custom
  RawString format.

* **RazorEngine.LastResultData property**  
   to hold response data retrieved from last request

* **Added non-generic RazorEngine class**  
  To simplify basic pages that don't require strongly typed ViewModels the non-generic RazorEngine class can now be used. When used only dynamic models are available.

* **Nested template Rendering**  
  Added support to allow nested template rendering within expressions. Allows to have RazorExpressions to be evaluated in the result of a parsed expression. Useful for dynamic content systems that can contain Razor content as part of the application stored data (like a CMS or help system)
  
