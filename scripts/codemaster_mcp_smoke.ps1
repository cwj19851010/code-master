param(
    [string]$ServerDll = "D:\MyHomeWorks\CodeMaster\CodeMaster.McpServer\bin\Debug\net10.0\CodeMaster.McpServer.dll",
    [switch]$Mutate,
    [string]$ProjectName = "McpSmokeProject",
    [string]$ProjectPath = "D:\CodeMasterMcpSmoke",
    [int]$FrontendPort = 5188,
    [int]$BackendPort = 5088,
    [switch]$Initialize,
    [switch]$Generate,
    [ValidateSet("full", "incremental")]
    [string]$GenerationMode = "full",
    [switch]$ValidateFrontendBuild,
    [switch]$StartServices
)

$ErrorActionPreference = "Stop"

function New-McpRequest {
    param(
        [int]$Id,
        [string]$Method,
        [object]$Params = @{}
    )

    return @{
        jsonrpc = "2.0"
        id = $Id
        method = $Method
        params = $Params
    } | ConvertTo-Json -Depth 50 -Compress
}

function New-ToolCall {
    param(
        [int]$Id,
        [string]$Name,
        [object]$Arguments = @{}
    )

    return New-McpRequest -Id $Id -Method "tools/call" -Params @{
        name = $Name
        arguments = $Arguments
    }
}

function Send-McpJson {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Json,
        [switch]$NoResponse
    )

    $Process.StandardInput.WriteLine($Json)
    if ($NoResponse) {
        return $null
    }

    $line = $Process.StandardOutput.ReadLine()
    if ([string]::IsNullOrWhiteSpace($line)) {
        throw "MCP server returned an empty response for request: $Json"
    }

    $response = $line | ConvertFrom-Json
    if ($response.error) {
        throw "MCP error $($response.error.code): $($response.error.message)"
    }

    return $response
}

function Get-ToolText {
    param([object]$Response)

    $text = $Response.result.content[0].text
    return $text | ConvertFrom-Json
}

if (-not (Test-Path -LiteralPath $ServerDll)) {
    throw "MCP server DLL not found: $ServerDll. Build CodeMaster.McpServer first."
}

$process = [System.Diagnostics.Process]::new()
$process.StartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$process.StartInfo.FileName = "dotnet"
$process.StartInfo.Arguments = '"' + $ServerDll + '"'
$process.StartInfo.WorkingDirectory = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $ServerDll)))
$process.StartInfo.RedirectStandardInput = $true
$process.StartInfo.RedirectStandardOutput = $true
$process.StartInfo.RedirectStandardError = $false
$process.StartInfo.UseShellExecute = $false
$process.StartInfo.CreateNoWindow = $true

[void]$process.Start()

$results = [ordered]@{}
$nextId = 1

try {
    $init = Send-McpJson -Process $process -Json (New-McpRequest -Id ($nextId++) -Method "initialize" -Params @{
        protocolVersion = "2024-11-05"
        capabilities = @{}
        clientInfo = @{ name = "codemaster-mcp-smoke"; version = "1" }
    })
    $results.Initialize = $init.result.serverInfo.name

    Send-McpJson -Process $process -NoResponse -Json (@{
        jsonrpc = "2.0"
        method = "notifications/initialized"
        params = @{}
    } | ConvertTo-Json -Depth 10 -Compress)

    $toolsResponse = Send-McpJson -Process $process -Json (New-McpRequest -Id ($nextId++) -Method "tools/list")
    $toolNames = @($toolsResponse.result.tools | ForEach-Object { $_.name })
    $requiredTools = @(
        "query_project",
        "analyze_requirements",
        "save_project",
        "save_module",
        "create_or_update_entity",
        "run_project_operation",
        "generate_code",
        "get_project_structure"
    )
    $missing = @($requiredTools | Where-Object { $_ -notin $toolNames })
    if ($missing.Count -gt 0) {
        throw "Missing MCP tools: $($missing -join ', ')"
    }
    $results.ToolCount = $toolNames.Count

    $projects = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "query_project" -Arguments @{ level = "projects" }))
    $results.ProjectCountBefore = $projects.count

    if ($Mutate) {
        $project = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "save_project" -Arguments @{
            projectName = $ProjectName
            displayName = "MCP Smoke Project"
            databaseType = "SQLite"
            connectionString = "Data Source=${ProjectName}.db"
            projectPath = $ProjectPath
            projectType = "Server"
            frontendPort = $FrontendPort
            backendPort = $BackendPort
        }))
        $projectId = [string]$project.projectId
        if ([string]::IsNullOrWhiteSpace($projectId)) {
            throw "save_project did not return projectId."
        }
        $results.ProjectId = $projectId

        $module = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "save_module" -Arguments @{
            projectId = $projectId
            moduleName = "SmokeManagement"
            moduleDescription = "Smoke management"
            icon = "Document"
            orderNum = 99
        }))
        $results.ModuleId = [string]$module.moduleId

        $customer = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "create_or_update_entity" -Arguments @{
            projectId = $projectId
            moduleName = "SmokeManagement"
            entityName = "Customer"
            description = "Customer"
            generateFrontend = $true
            fields = @(
                @{ name = "Name"; dataType = "string"; description = "Name"; isRequired = $true; showInList = $true; showInSearch = $true; orderNum = 1 },
                @{ name = "Phone"; dataType = "string"; description = "Phone"; formControlType = "input"; isPhone = $true; showInList = $true; orderNum = 2 }
            )
        }))
        $results.CustomerEntityId = [string]$customer.entityId

        $order = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "create_or_update_entity" -Arguments @{
            projectId = $projectId
            moduleName = "SmokeManagement"
            entityName = "Order"
            description = "Order"
            generateFrontend = $true
            fields = @(
                @{ name = "OrderNo"; dataType = "string"; description = "Order No"; isRequired = $true; showInList = $true; showInSearch = $true; orderNum = 1 },
                @{
                    name = "CustomerId"
                    dataType = "long"
                    description = "Customer"
                    formControlType = "select-table"
                    relatedEntityName = "Customer"
                    relatedEntityIdField = "Id"
                    relatedDisplayFields = @("Name", "Phone")
                    showInList = $true
                    showInSearch = $true
                    orderNum = 2
                },
                @{ name = "Amount"; dataType = "decimal"; description = "Amount"; formControlType = "number"; precision = 18; scale = 2; showInList = $true; orderNum = 3 },
                @{ name = "Status"; dataType = "string"; description = "Status"; formControlType = "select"; enumValues = "Pending,Paid,Closed"; showInList = $true; showInSearch = $true; orderNum = 4 },
                @{ name = "OrderTime"; dataType = "DateTime"; description = "Order Time"; formControlType = "datetime"; showInList = $true; showInSearch = $true; orderNum = 5 }
            )
        }))
        $results.OrderEntityId = [string]$order.entityId

        $structure = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "get_project_structure" -Arguments @{
            projectId = $projectId
            includeGeneratedFiles = $false
        }))
        $results.StructureModules = @($structure.modules).Count
        $results.StructureEntities = @($structure.modules | ForEach-Object { $_.entities }).Count

        if ($Initialize) {
            $initProject = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "run_project_operation" -Arguments @{
                operation = "initialize"
                projectId = $projectId
                targetPath = $ProjectPath
            }))
            $results.ProjectInitialized = [bool]$initProject.success
            if (-not $initProject.success) {
                throw "Project initialization failed: $($initProject.message)"
            }
        }

        if ($Generate) {
            if ($Initialize) {
                $stop = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "run_project_operation" -Arguments @{
                    operation = "stop_all"
                    projectId = $projectId
                }))
                $results.ServicesStoppedBeforeGenerate = [bool]$stop.success
            }

            $generation = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "generate_code" -Arguments @{
                projectId = $projectId
                mode = $GenerationMode
                validateBuild = $true
                validateFrontendBuild = [bool]$ValidateFrontendBuild
            }))
            $results.Generated = [bool]$generation.success
            $results.GeneratedEntityCount = $generation.entityCount
            if (-not $generation.success) {
                throw "Code generation failed: $($generation.message)"
            }
        }

        if ($StartServices) {
            $start = Get-ToolText (Send-McpJson -Process $process -Json (New-ToolCall -Id ($nextId++) -Name "run_project_operation" -Arguments @{
                operation = "start_all"
                projectId = $projectId
            }))
            $results.ServicesStarted = [bool]$start.success
            if (-not $start.success) {
                throw "Service startup failed."
            }
        }
    }
}
finally {
    try {
        $process.StandardInput.Close()
    }
    catch {
    }

    if (-not $process.WaitForExit(30000)) {
        $process.Kill($true)
        $process.WaitForExit()
    }
    else {
        $process.WaitForExit()
    }

    $results.ExitCode = $process.ExitCode
    $results.StderrTail = "(stderr inherited by the current console)"
}

[pscustomobject]$results
