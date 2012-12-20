#Westwind.RazorHosting Change Log
* * * 

##Version 1.01

* **Changed default bin folder location**
  Default folder location is now loaded from GetEntryAssembly() location by default and falls back to current directory if that can't resolve. BaseBinary folder can still be used to explicitly override the path.

* **Removed System.Web dependency**

* **RazorEngine.LastResultData property**
   to hold response data retrieved from last request

* **Added non-generic RazorEngine class**
  To simplify basic pages that don't require strongly typed ViewModels the non-generic RazorEngine class can now be used. When used only dynamic models are available.

* **Nested template Rendering**
  Added support to allow nested template rendering within expressions. Allows to have RazorExpressions to be evaluated in the result of a parsed expression. Useful for dynamic content systems that can contain Razor content as part of the application stored data (like a CMS or help system)
  