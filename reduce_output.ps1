$targetPath = $args[0]
echo "Try to delete redundant CEF files from build: $targetPath"

$files = @(
	'chrome_100_percent.pak'
	'chrome_200_percent.pak'
	'd3dcompiler_47.dll'
	'libEGL.dll'
	'libGLESv2.dll'
	'swiftshader\libEGL.dll'
	'swiftshader\libGLESv2.dll'
)

Foreach ($i in $files)
{
	$pathToFile = $targetPath+$i
	echo "Try to delete: $pathToFile"
	Remove-Item -Path $pathToFile
}

$localesPath = $targetPath+"locales\*.*"
echo $localesPath
Remove-Item $localesPath -Exclude "en-US.pak"