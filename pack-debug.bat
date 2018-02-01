call del Packaged\* /Q
call lprun "D:\Code\PowerZero\Code\Projects\DeveloperBox\LINQPad\Queries\Brandless Project Tools\Increment version.xml.linq" "%~dp0version.xml"
REM dotnet pack Code/ --output "%~dp0Packaged" --include-symbols --include-source -c Debug
dotnet pack "%~dp0Code/Microsoft.AspNetCore.OData/Microsoft.AspNetCore.OData.csproj" --output "%~dp0Packaged" --include-symbols --include-source -c Debug
dotnet pack "%~dp0Code\Microsoft.AspNetCore.OData.EntityFramework\Microsoft.AspNetCore.OData.EntityFramework.csproj" --output "%~dp0Packaged" --include-symbols --include-source -c Debug
dotnet pack "%~dp0Code/Microsoft.AspNetCore.OData.Extensions/Microsoft.AspNetCore.OData.Extensions.csproj" --output "%~dp0Packaged" --include-symbols --include-source -c Debug