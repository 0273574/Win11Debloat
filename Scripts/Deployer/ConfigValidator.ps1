<#
.SYNOPSIS
    Configuration Validator module for Wdrozyciel Integration

.DESCRIPTION
    Validates deployment profiles and system configurations
    Ensures all required settings are present before deployment
#>

function Test-DeploymentProfileValidity {
    param(
        [Parameter(Mandatory=$true)]
        [object]$Profile
    )
    
    $validation = @{
        IsValid = $true
        Errors = @()
        Warnings = @()
    }
    
    # Check profile name
    if ([string]::IsNullOrWhiteSpace($Profile.name)) {
        $validation.IsValid = $false
        $validation.Errors += "Profile name is required"
    }
    
    # Check description
    if ([string]::IsNullOrWhiteSpace($Profile.description)) {
        $validation.Warnings += "Profile description is recommended"
    }
    
    # Check system settings
    if ($null -eq $Profile.systemSettings -or $Profile.systemSettings.PSObject.Properties.Count -eq 0) {
        $validation.Warnings += "No system settings defined"
    }
    
    # Check applications
    if ($null -eq $Profile.applications -or $Profile.applications.Count -eq 0) {
        $validation.Warnings += "No applications defined"
    }
    
    # Validate required applications
    foreach ($app in $Profile.applications) {
        if ([string]::IsNullOrWhiteSpace($app.name)) {
            $validation.IsValid = $false
            $validation.Errors += "Application name is required"
        }
        if ($app.required -and [string]::IsNullOrWhiteSpace($app.version)) {
            $validation.Warnings += "Required application '$($app.name)' should specify version"
        }
    }
    
    return $validation
}

function Test-SystemRequirements {
    $requirements = @{
        OSVersion = [System.Environment]::OSVersion.VersionString
        PowerShellVersion = $PSVersionTable.PSVersion.ToString()
        IsAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        DiskSpaceAvailable = (Get-Item (Split-Path $env:WINDIR)).PSDrive.Free / 1GB
    }
    
    return $requirements
}

Export-ModuleMember -Function @(
    'Test-DeploymentProfileValidity',
    'Test-SystemRequirements'
)
