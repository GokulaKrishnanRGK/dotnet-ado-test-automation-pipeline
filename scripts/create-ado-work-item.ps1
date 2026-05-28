[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$OrganizationUrl,
    [Parameter(Mandatory = $true)]
    [string]$Project,
    [Parameter(Mandatory = $true)]
    [string]$ResultsRoot,
    [Parameter(Mandatory = $true)]
    [string]$Repository,
    [Parameter(Mandatory = $true)]
    [string]$CommitSha,
    [Parameter(Mandatory = $true)]
    [string]$GithubRunUrl,
    [Parameter(Mandatory = $true)]
    [string]$ArtifactName,
    [Parameter(Mandatory = $true)]
    [string]$PipelineRunUrl,
    [string]$WorkItemType = "Issue",
    [string]$AreaPath = "",
    [string]$AssignedTo = "",
    [string]$AccessToken = $env:SYSTEM_ACCESSTOKEN,
    [string]$DedupeSalt = $env:ADO_WORK_ITEM_DEDUPE_SALT,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-AuthorizationHeader {
    param([string]$BearerToken)

    if (-not [string]::IsNullOrWhiteSpace($BearerToken)) {
        return @{ Authorization = "Bearer $BearerToken" }
    }

    throw "Work item automation requires Azure Pipelines System.AccessToken."
}

function Get-NormalizedOrganizationUrl {
    param([string]$Url)

    if ([string]::IsNullOrWhiteSpace($Url)) {
        throw "OrganizationUrl is required."
    }

    return $Url.TrimEnd("/")
}

function Get-ShortCommitSha {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "unknown"
    }

    [int]$length = [Math]::Min(12, $Value.Length)
    return $Value.Substring(0, $length)
}

function Get-Sha256Hash {
    param([string]$Value)

    [byte[]]$bytes = [Text.Encoding]::UTF8.GetBytes($Value)
    [byte[]]$hashBytes = [Security.Cryptography.SHA256]::HashData($bytes)
    return [Convert]::ToHexString($hashBytes).ToLowerInvariant()
}

function ConvertTo-PlainText {
    param([object]$Value)

    if ($null -eq $Value) {
        return ""
    }

    return [string]$Value
}

function ConvertTo-HtmlEncodedText {
    param([string]$Value)

    return [System.Net.WebUtility]::HtmlEncode($Value)
}

function Get-FailedTestResults {
    param([string]$Root)

    if (-not (Test-Path -LiteralPath $Root -PathType Container)) {
        throw "ResultsRoot '$Root' was not found."
    }

    [System.IO.FileInfo[]]$trxFiles = @(Get-ChildItem -LiteralPath $Root -Filter "*.trx" -Recurse -File)
    [object[]]$failures = @()

    foreach ($trxFile in $trxFiles) {
        [xml]$trx = Get-Content -LiteralPath $trxFile.FullName -Raw
        [System.Xml.XmlNamespaceManager]$namespaceManager = New-Object System.Xml.XmlNamespaceManager($trx.NameTable)
        $namespaceManager.AddNamespace("trx", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

        [System.Xml.XmlNodeList]$failedResults = $trx.SelectNodes("//trx:UnitTestResult[@outcome='Failed']", $namespaceManager)

        foreach ($failedResult in $failedResults) {
            [string]$testName = ConvertTo-PlainText $failedResult.testName
            [string]$message = ConvertTo-PlainText $failedResult.Output.ErrorInfo.Message
            [string]$stackTrace = ConvertTo-PlainText $failedResult.Output.ErrorInfo.StackTrace

            $failures += [pscustomobject]@{
                TestName = $testName
                Message = $message
                StackTrace = $stackTrace
                TrxPath = $trxFile.FullName
            }
        }
    }

    return $failures
}

function Invoke-AzureDevOpsJsonApi {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers,
        [object]$Body = $null,
        [string]$ContentType = "application/json"
    )

    if ($null -eq $Body) {
        return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $Headers
    }

    return Invoke-RestMethod `
        -Method $Method `
        -Uri $Uri `
        -Headers $Headers `
        -ContentType $ContentType `
        -Body ($Body | ConvertTo-Json -Depth 20)
}

function Find-ExistingWorkItem {
    param(
        [string]$Organization,
        [string]$ProjectName,
        [string]$Title,
        [hashtable]$Headers
    )

    [string]$escapedTitle = $Title.Replace("'", "''")
    [string]$escapedProjectName = $ProjectName.Replace("'", "''")
    [string]$wiql = @"
SELECT [System.Id]
FROM WorkItems
WHERE [System.TeamProject] = '$escapedProjectName'
  AND [System.Title] = '$escapedTitle'
  AND [System.WorkItemType] = '$WorkItemType'
  AND [System.State] <> 'Closed'
  AND [System.State] <> 'Removed'
ORDER BY [System.ChangedDate] DESC
"@

    [string]$uri = "$Organization/$ProjectName/_apis/wit/wiql?api-version=7.1"
    $response = Invoke-AzureDevOpsJsonApi -Method Post -Uri $uri -Headers $Headers -Body @{ query = $wiql }
    [object[]]$workItems = @($response.workItems)

    if ($workItems.Count -eq 0) {
        return $null
    }

    return $workItems[0].id
}

function New-WorkItemPatch {
    param(
        [string]$Operation,
        [string]$Path,
        [object]$Value
    )

    return [ordered]@{
        op = $Operation
        path = $Path
        value = $Value
    }
}

function Get-ReproStepsHtml {
    param(
        [object]$Failure,
        [string]$DedupeKey,
        [string]$ShortSha
    )

    [string]$message = ConvertTo-HtmlEncodedText $Failure.Message
    [string]$stackTrace = ConvertTo-HtmlEncodedText $Failure.StackTrace
    [string]$trxPath = ConvertTo-HtmlEncodedText $Failure.TrxPath

    return @"
<div>
  <p><strong>Scenario:</strong> $(ConvertTo-HtmlEncodedText $Failure.TestName)</p>
  <p><strong>Commit:</strong> $(ConvertTo-HtmlEncodedText $CommitSha)</p>
  <p><strong>Short commit:</strong> $(ConvertTo-HtmlEncodedText $ShortSha)</p>
  <p><strong>Artifact:</strong> $(ConvertTo-HtmlEncodedText $ArtifactName)</p>
  <p><strong>Repository:</strong> $(ConvertTo-HtmlEncodedText $Repository)</p>
  <p><strong>GitHub run:</strong> <a href="$(ConvertTo-HtmlEncodedText $GithubRunUrl)">$(ConvertTo-HtmlEncodedText $GithubRunUrl)</a></p>
  <p><strong>Azure DevOps run:</strong> <a href="$(ConvertTo-HtmlEncodedText $PipelineRunUrl)">$(ConvertTo-HtmlEncodedText $PipelineRunUrl)</a></p>
  <p><strong>Deduplication key:</strong> $(ConvertTo-HtmlEncodedText $DedupeKey)</p>
  <p><strong>TRX path:</strong> $trxPath</p>
  <h3>Failure message</h3>
  <pre>$message</pre>
  <h3>Stack trace</h3>
  <pre>$stackTrace</pre>
</div>
"@
}

[string]$normalizedOrganizationUrl = Get-NormalizedOrganizationUrl -Url $OrganizationUrl
[object[]]$failedTests = @(Get-FailedTestResults -Root $ResultsRoot)
[string]$shortCommitSha = Get-ShortCommitSha -Value $CommitSha

if ($failedTests.Count -eq 0) {
    Write-Host "No failed BDD tests found under '$ResultsRoot'."
    return
}

Write-Host "Found $($failedTests.Count) failed BDD test result(s)."

[hashtable]$headers = @{}
if (-not $DryRun) {
    $headers = Get-AuthorizationHeader -BearerToken $AccessToken
}

foreach ($failure in $failedTests) {
    [string]$dedupeInput = "$Repository|$CommitSha|$ArtifactName|$($failure.TestName)|$DedupeSalt"
    [string]$dedupeKey = (Get-Sha256Hash -Value $dedupeInput).Substring(0, 16)
    [string]$title = "[OpsLedger BDD] $($failure.TestName) failed at $shortCommitSha [$dedupeKey]"
    [string]$reproSteps = Get-ReproStepsHtml -Failure $failure -DedupeKey $dedupeKey -ShortSha $shortCommitSha
    [string]$tags = "OpsLedger; BDD; AutomatedFailure; $shortCommitSha; $dedupeKey"

    if ($DryRun) {
        Write-Host "Dry run: would create or update $WorkItemType '$title'."
        continue
    }

    $existingWorkItemId = Find-ExistingWorkItem `
        -Organization $normalizedOrganizationUrl `
        -ProjectName $Project `
        -Title $title `
        -Headers $headers

    if ($null -eq $existingWorkItemId) {
        [object[]]$createPatch = @(
            (New-WorkItemPatch -Operation "add" -Path "/fields/System.Title" -Value $title),
            (New-WorkItemPatch -Operation "add" -Path "/fields/System.Tags" -Value $tags),
            (New-WorkItemPatch -Operation "add" -Path "/fields/Microsoft.VSTS.TCM.ReproSteps" -Value $reproSteps)
        )

        if (-not [string]::IsNullOrWhiteSpace($AreaPath)) {
            $createPatch += New-WorkItemPatch -Operation "add" -Path "/fields/System.AreaPath" -Value $AreaPath
        }

        if (-not [string]::IsNullOrWhiteSpace($AssignedTo)) {
            $createPatch += New-WorkItemPatch -Operation "add" -Path "/fields/System.AssignedTo" -Value $AssignedTo
        }

        [string]$createUri = "$normalizedOrganizationUrl/$Project/_apis/wit/workitems/`$${WorkItemType}?api-version=7.1"
        $created = Invoke-AzureDevOpsJsonApi `
            -Method Patch `
            -Uri $createUri `
            -Headers $headers `
            -Body $createPatch `
            -ContentType "application/json-patch+json"

        Write-Host "Created $WorkItemType $($created.id) for '$($failure.TestName)'."
        continue
    }

    [object[]]$updatePatch = @(
        (New-WorkItemPatch -Operation "add" -Path "/fields/System.History" -Value "Repeated BDD failure observed for commit $CommitSha and artifact $ArtifactName. Pipeline run: $PipelineRunUrl"),
        (New-WorkItemPatch -Operation "add" -Path "/fields/Microsoft.VSTS.TCM.ReproSteps" -Value $reproSteps)
    )

    [string]$updateUri = "$normalizedOrganizationUrl/$Project/_apis/wit/workitems/$existingWorkItemId?api-version=7.1"
    $updated = Invoke-AzureDevOpsJsonApi `
        -Method Patch `
        -Uri $updateUri `
        -Headers $headers `
        -Body $updatePatch `
        -ContentType "application/json-patch+json"

    Write-Host "Updated existing $WorkItemType $($updated.id) for '$($failure.TestName)'."
}
