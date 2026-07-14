<#
.SYNOPSIS
    Deployment Logging module for Wdrozyciel Integration

.DESCRIPTION
    Comprehensive logging system for deployment operations
    Tracks all deployment activities and provides diagnostics
#>

class DeploymentLogger {
    [string]$LogPath
    [string]$LogFile
    [int]$MaxLogSize = 10MB
    [System.Collections.Generic.List[string]]$LogEntries
    
    DeploymentLogger([string]$LogDirectory) {
        $this.LogPath = $LogDirectory
        $this.LogFile = Join-Path $LogDirectory "deployment-$(Get-Date -Format 'yyyy-MM-dd').log"
        $this.LogEntries = [System.Collections.Generic.List[string]]::new()
        
        if (-not (Test-Path $this.LogPath)) {
            New-Item -ItemType Directory -Path $this.LogPath -Force | Out-Null
        }
    }
    
    [void]Log([string]$Message, [string]$Level = "INFO") {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $entry = "[$timestamp] [$Level] $Message"
        
        $this.LogEntries.Add($entry)
        Add-Content -Path $this.LogFile -Value $entry -Encoding UTF8
    }
    
    [void]LogInfo([string]$Message) {
        $this.Log($Message, "INFO")
    }
    
    [void]LogWarning([string]$Message) {
        $this.Log($Message, "WARNING")
    }
    
    [void]LogError([string]$Message) {
        $this.Log($Message, "ERROR")
    }
    
    [void]LogDebug([string]$Message) {
        $this.Log($Message, "DEBUG")
    }
    
    [string]GetLogFilePath() {
        return $this.LogFile
    }
    
    [void]RotateLogs() {
        $logSize = (Get-Item $this.LogFile -ErrorAction SilentlyContinue).Length
        if ($logSize -gt $this.MaxLogSize) {
            $backupName = "$($this.LogFile).backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Rename-Item $this.LogFile $backupName -Force
        }
    }
}

Export-ModuleMember -Class DeploymentLogger
