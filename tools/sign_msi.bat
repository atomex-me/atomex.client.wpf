echo "MSI signing..."

setlocal
call "tools\get_cert_password.bat"

set PATH="C:\Program Files (x86)\Windows Kits\10\bin\10.0.17134.0\x64";%PATH%
signtool sign /f "certificate_key.pfx" /p %CERTPWD% /d "Atomex Client" /t http://timestamp.verisign.com/scripts/timstamp.dll /v %1

endlocal

pause Press Any Key
exit