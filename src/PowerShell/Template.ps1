function Invoke-BloodHound
{
    [CmdletBinding(PositionalBinding = $false)]
    param(
        [Alias("c")]
        [String[]]
        $CollectionMethods = [String[]]@('Default'),

        [Alias("d")]
        [String]
        $Domain,
        
        [Alias("s")]
        [Switch]
        $SearchForest,

        [Switch]
        $Stealth,

        [String]
        $LdapFilter,

        [String]
        $DistinguishedName,

        [String]
        $ComputerFile,

        [ValidateScript({ Test-Path -Path $_ })]
        [String]
        $OutputDirectory = $( Get-Location ),

        [ValidateNotNullOrEmpty()]
        [String]
        $OutputPrefix,

        [String]
        $CacheName,

        [Switch]
        $MemCache,

        [Switch]
        $RebuildCache,

        [Switch]
        $RandomFilenames,

        [String]
        $ZipFilename,
        
        [Switch]
        $NoZip,
        
        [String]
        $ZipPassword,
        
        [Switch]
        $TrackComputerCalls,
        
        [Switch]
        $PrettyPrint,

        [String]
        $LdapUsername,

        [String]
        $LdapPassword,

        [string]
        $DomainController,

        [ValidateRange(0, 65535)]
        [Int]
        $LdapPort,

        [Switch]
        $SecureLdap,
        
        [Switch]
        $DisableCertVerification,

        [Switch]
        $DisableSigning,

        [Switch]
        $SkipPortCheck,

        [ValidateRange(50, 5000)]
        [Int]
        $PortCheckTimeout = 500,

        [Switch]
        $SkipPasswordCheck,

        [Switch]
        $ExcludeDCs,

        [Int]
        $Throttle,

        [ValidateRange(0, 100)]
        [Int]
        $Jitter,

        [Int]
        $Threads,

        [Switch]
        $SkipRegistryLoggedOn,

        [String]
        $OverrideUsername,

        [String]
        $RealDNSName,

        [Switch]
        $CollectAllProperties,

        [Switch]
        $Loop,

        [String]
        $LoopDuration,

        [String]
        $LoopInterval,

        [ValidateRange(500, 60000)]
        [Int]
        $StatusInterval,
        
        [Alias("v")]
        [ValidateRange(0, 5)]
        [Int]
        $Verbosity,

        [Alias("h")]
        [Switch]
        $Help,

        [Switch]
        $Version
    )

    $vars = New-Object System.Collections.Generic.List[System.Object]
    
    if(!($PSBoundParameters.ContainsKey("help") -or $PSBoundParameters.ContainsKey("version"))){
        $PSBoundParameters.Keys | % {
            if ($_ -notmatch "verbosity"){
                $vars.add("--$_")
                if($PSBoundParameters.item($_).gettype().name -notmatch "switch"){
                    $vars.add($PSBoundParameters.item($_))
                }
            }
            elseif ($_ -match "verbosity") {
                $vars.add("-v")
                $vars.add($PSBoundParameters.item($_))
            }
        }
    }
    else {
        $PSBoundParameters.Keys |? {$_ -match "help" -or $_ -match "version"}| % {
            $vars.add("--$_")
        }
    }
    
    $passed = [string[]]$vars.ToArray()
}
