param(
    [string]$framework = 'all',
    [string]$target = 'all'
)

$csproj = 'app.csproj'

function build($buildFramework, $buildTarget) {
    try {
        if ($buildFramework -eq 'mono') {
            if (!(which mono)) { throw "install mono" }
            $env:FrameworkPathOverride = "$(split-path $(which mono))/../lib/mono/4.5"
        }
        elseif (!(which dotnet)) { throw "install dotnet" }

        write-host "Building $buildFramework/$buildTarget..."
        dotnet publish -o "build\$buildFramework\$buildTarget" -c Release -r win10-x64 -f $buildTarget -v m --nologo $csproj
    }
    finally {
        del -ea:silent env:FrameworkPathOverride
    }
}

foreach ($csprojTarget in ([xml](type $csproj)).project.propertygroup.targetframeworks -split ';') {
    if ('all', $csprojTarget -notcontains $target) { continue }

    if ('all', 'dotnet' -contains $framework) {
        build dotnet $csprojTarget
    }
    if ('all', 'mono' -contains $framework -and $csprojTarget -notmatch 'netcore') {
        build mono $csprojTarget
    }
}
