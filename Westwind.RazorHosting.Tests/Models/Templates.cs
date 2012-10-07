using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorHostingTests
{
    public class Templates
    {
       public static string BasicTemplateStringWithPersonModel =
@"inherits Westwind.RazorTemplateBase<RazorHostingTests.Person>
Hello Current time is: @DateTime.Now.

Current time is: @DateTime.Now

<b class=""@(Environment.UserName)"">BOLD</b>

This Template runs in its own AppDomain which can be unloaded.
AppDomain: @AppDomain.CurrentDomain.FriendlyName
Assembly: @System.Reflection.Assembly.GetExecutingAssembly().FullName

<b>@Model.Name of @Model.Company entered on @Model.Entered

Entered on @Model.Entered

@this.GetType().BaseType.ToString()

helper HelloWorld(this HtmlHelper helper, string name) {
  <div class=""errordisplay"">Helloname</div>
}
";
    }
}
