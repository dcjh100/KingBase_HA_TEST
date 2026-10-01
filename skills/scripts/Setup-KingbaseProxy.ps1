<#
.SYNOPSIS
    Configures Windows Netsh Interface PortProxy for Docker connectivity.
    Requires Administrator privileges.

.DESCRIPTION
    Forwards local ports to a remote IP to allow Docker containers (and local apps) 
    to connect via host.docker.internal or localhost.
    Defaults to forwarding ports 14321, 14322, 14323 to 192.0.2.10.

.PARAMETER TargetIp
    Target database IP address. Default: 192.0.2.10

.PARAMETER Ports
    List of ports to forward. Default: 14321, 14322, 14323

.PARAMETER Action
    Action to perform: "add", "delete", "show". Default: "show"

.EXAMPLE
    .\Setup-KingbaseProxy.ps1 -Action add
    .\Setup-KingbaseProxy.ps1 -TargetIp "192.0.2.10" -Action add
    .\Setup-KingbaseProxy.ps1 -Action delete
#>

param(
    [string]$TargetIp = "192.0.2.10",
    [int[]]$Ports = @(14321, 14322, 14323),
    [ValidateSet("add", "delete", "show")]
    [string]$Action = "show"
)

# Check for Administrator privileges
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Warning "Administrator privileges required for netsh commands."
    Write-Warning "Please run PowerShell as Administrator."
    # Continue to allow 'show' command which might work in read-only mode or if user just wants to check script
}

function Show-Rules {
    Write-Host "--- Current PortProxy Rules ---" -ForegroundColor Cyan
    netsh interface portproxy show all
    Write-Host "-------------------------------" -ForegroundColor Cyan
}

function Add-Rules {
    Write-Host "Adding/Updating rules -> Target: $TargetIp" -ForegroundColor Green
    foreach ($port in $Ports) {
        $cmd = "netsh interface portproxy add v4tov4 listenport=$port listenaddress=0.0.0.0 connectport=$port connectaddress=$TargetIp"
        Write-Host "Exec: $cmd" -ForegroundColor Gray
        Invoke-Expression $cmd
    }
    Write-Host "Done." -ForegroundColor Green
    Show-Rules
}

function Delete-Rules {
    Write-Host "Deleting rules..." -ForegroundColor Yellow
    foreach ($port in $Ports) {
        $cmd = "netsh interface portproxy delete v4tov4 listenport=$port listenaddress=0.0.0.0"
        Write-Host "Exec: $cmd" -ForegroundColor Gray
        Invoke-Expression $cmd
    }
    Write-Host "Done." -ForegroundColor Yellow
    Show-Rules
}

# Main logic
switch ($Action) {
    "show" { Show-Rules }
    "add" { Add-Rules }
    "delete" { Delete-Rules }
}
