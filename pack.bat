echo off
call del "%~dp0Code\Microsoft.AspNetCore.OData\bin" /Q
call del "%~dp0Code\Microsoft.AspNetCore.OData.EntityFramework\bin" /Q
call del "%~dp0Code\Microsoft.AspNetCore.OData.Extensions\bin" /Q
call del "%~dp0Code\Microsoft.AspNetCore.OData\obj" /Q
call del "%~dp0Code\Microsoft.AspNetCore.OData.EntityFramework\obj" /Q
call del "%~dp0Code\Microsoft.AspNetCore.OData.Extensions\obj" /Q
echo on
call %NuGetSynchroniser% -p Microsoft.AspNetCore.OData.csproj,Microsoft.AspNetCore.OData.EntityFramework.csproj,Microsoft.AspNetCore.OData.Extensions.csproj