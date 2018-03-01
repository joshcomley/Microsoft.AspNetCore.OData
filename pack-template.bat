call prep
call dotnet pack Code/ --output "%~dp0Packaged" --include-symbols --include-source -c %1
call clean
