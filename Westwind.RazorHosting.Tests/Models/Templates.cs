using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RazorHostingTests
{
    public class Templates
    {
       public static string BasicTemplateStringWithPersonModel =
@"@inherits Westwind.RazorHosting.RazorTemplateBase<RazorHostingTests.Person>
Hello @Model.Name,

Current time is: @DateTime.Now.
User Name: @Environment.UserName

<b class=""@(Environment.UserName)"">BOLD</b>

AppDomain: @AppDomain.CurrentDomain.FriendlyName
Assembly: @System.Reflection.Assembly.GetExecutingAssembly().FullName

<b>@Model.Name of @Model.Company entered on @Model.Entered

Entered on @Model.Entered

Base Type:
@this.GetType().BaseType.ToString()

@{ ResultData = ""Hello from the template""; }
";
    }
}
