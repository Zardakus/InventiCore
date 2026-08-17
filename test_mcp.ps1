$tenantId = "7eca4967-f455-464c-bb32-925522806364"
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "run --project src/InventiCore.Mcp --tenant-id $tenantId"
$processInfo.RedirectStandardInput = $true
$processInfo.RedirectStandardOutput = $true
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $processInfo
$process.Start() | Out-Null

$stdin = $process.StandardInput
$stdout = $process.StandardOutput

# 1. Analyze Low Stock
$analyzeReq = '{"jsonrpc":"2.0","id":"1","method":"tools/call","params":{"name":"analyze_low_stock","arguments":{}}}'
$stdin.WriteLine($analyzeReq)
$stdin.Flush()

# Ler stdout até encontrar o jsonrpc response
$analyzeRes = ""
while($true) {
    $line = $stdout.ReadLine()
    if ($line -match "jsonrpc") {
        $analyzeRes = $line
        break
    }
}

Write-Output "ANALYSIS RESULT:"
Write-Output $analyzeRes

$process.Kill()
