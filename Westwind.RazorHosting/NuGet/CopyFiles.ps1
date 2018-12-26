copy ..\Westwind.RazorHosting\bin\Release\Westwind.RazorHosting.dll lib\net45
copy ..\Westwind.RazorHosting\bin\Release\Westwind.RazorHosting.xml lib\net45
     
.\signtool.exe sign /v /n "West Wind Technologies" /sm /s MY /tr "http://timestamp.digicert.com" /td SHA256 /fd SHA256 ".\lib\net45\Westwind.RazorHosting.dll"
