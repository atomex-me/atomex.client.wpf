echo "Executable files signing..."

setlocal
call "tools\get_cert_password.bat"
set PATH="C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x64";%PATH%
cd %1

if exist Atomex.Client.Wpf.exe signtool sign /f "..\..\..\certificate_key.pfx" /p %CERTPWD% /d "Atomex Client" /t http://timestamp.verisign.com/scripts/timstamp.dll /v Atomex.Client.Wpf.exe
	
if exist Atomex.Client.Core.dll signtool sign /f "..\..\..\certificate_key.pfx" /p %CERTPWD% /d "Atomex Client" /t http://timestamp.verisign.com/scripts/timstamp.dll /v Atomex.Client.Core.dll

::for %%f in (*.dll *.exe) do (
::	echo %%f
::	signtool sign /f "..\..\..\certificate_key.pfx" /p %CERTPWD% /d "Atomex Client" /t http://timestamp.verisign.com/scripts/timstamp.dll /v %%f
::)

endlocal

pause Press Any Key
exit