call del Packaged\* /Q
call lprun "D:\Code\PowerZero\Code\Projects\DeveloperBox\LINQPad\Queries\Brandless Project Tools\Increment version.xml.linq" "%~dp0version.xml"
call dotnet pack Code/ --output "%~dp0Packaged" --include-symbols --include-source -c Release