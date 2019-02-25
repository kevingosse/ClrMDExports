xcopy /R /Y "..\src\ClrMDExports\Init.cs" ".\contentFiles\cs\net471\"
xcopy /R /Y "..\src\ClrMDExports\Init.cs" ".\contentFiles\cs\netstandard2.0\"
xcopy /R /Y "..\src\ClrMDExports\Init.cs" ".\content\"

nuget pack