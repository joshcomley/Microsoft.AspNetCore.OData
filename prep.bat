call lprun "D:\Code\PowerZero\Code\Projects\DeveloperBox\LINQPad\Queries\Brandless Project Tools\Increment version.xml.linq" "%~dp0version.xml"
call del Packaged\* /Q
call del "Code\Microsoft.AspNetCore.OData\bin" /Q
call del "Code\Microsoft.AspNetCore.OData.EntityFramework\bin" /Q
call del "Code\Microsoft.AspNetCore.OData.Extensions\bin" /Q
call del "Code\Microsoft.AspNetCore.OData\obj" /Q
call del "Code\Microsoft.AspNetCore.OData.EntityFramework\obj" /Q
call del "Code\Microsoft.AspNetCore.OData.Extensions\obj" /Q
call dotnet restore