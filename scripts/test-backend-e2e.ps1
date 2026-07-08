param(
    [string]$BaseUrl = "http://127.0.0.1:8081",
    [string]$Username = "teacher",
    [string]$Password = "teacher123",
    [int]$SessionId = 1,
    [string]$StudentCode = "SV001"
)

$ErrorActionPreference = "Stop"

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)] $Actual,
        [Parameter(Mandatory = $true)] $Expected,
        [Parameter(Mandatory = $true)] [string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message. Expected '$Expected' but got '$Actual'."
    }
}

function Assert-True {
    param(
        [Parameter(Mandatory = $true)] [bool]$Condition,
        [Parameter(Mandatory = $true)] [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$loginBody = @{
    username = $Username
    password = $Password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType "application/json" -Body $loginBody
Assert-True ($null -ne $loginResponse.token -and $loginResponse.token.Length -gt 20) "Login did not return a usable bearer token"

$headers = @{ Authorization = "Bearer $($loginResponse.token)" }

$sessionList = Invoke-RestMethod -Uri "$BaseUrl/api/exam-sessions" -Headers $headers
Assert-True ($sessionList.Count -ge 1) "Exam session list is empty"

$policy = Invoke-RestMethod -Uri "$BaseUrl/api/exam-sessions/$SessionId/policy" -Headers $headers
Assert-Equal $policy.sessionId $SessionId "Policy returned the wrong session id"
Assert-True ($policy.blockedProcesses -contains "zalo") "Policy is missing the zalo block rule"
Assert-True ($policy.screenIntervalMs -ge 250) "Policy is missing the screen interval"

$policyUpdateBody = @{
    blockedProcesses = @("zalo", "chatgpt", "claude")
    blockedWindowKeywords = @("ChatGPT", "Claude")
    screenIntervalMs = 2500
    screenJpegQuality = 42
    webcamEnabled = $true
    webcamSnapshotOnConnect = $true
    webcamIntervalSeconds = 20
    webcamJpegQuality = 60
    blockClipboardShortcuts = $true
    websitePolicyMode = "allowlist"
    allowedWebsiteHosts = @("dntu.edu.vn", "exam.dntu.edu.vn")
} | ConvertTo-Json -Depth 5

$updatedPolicy = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/exam-sessions/$SessionId/policy" -Headers $headers -ContentType "application/json" -Body $policyUpdateBody
Assert-Equal $updatedPolicy.screenIntervalMs 2500 "Policy update did not persist screen interval"
Assert-Equal $updatedPolicy.webcamIntervalSeconds 20 "Policy update did not persist webcam interval"
Assert-Equal $updatedPolicy.blockClipboardShortcuts $true "Policy update did not persist clipboard blocking"
Assert-True ($updatedPolicy.allowedWebsiteHosts -contains "dntu.edu.vn") "Policy update did not persist website allowlist"
Assert-True ($updatedPolicy.blockedProcesses -contains "chatgpt") "Policy update did not persist blocked process rules"

$start = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/exam-sessions/$SessionId/start" -Headers $headers
Assert-Equal $start.status "running" "Session did not transition to running"

$eventBody = @{
    student_code = $StudentCode
    event_type = "process_violation"
    machine_name = "LAB-PC-01"
    ip_address = "192.168.1.10"
    payload_json = @{
        processName = "chatgpt"
        windowTitle = "ChatGPT"
        action = "killed"
    }
} | ConvertTo-Json -Depth 6

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/exam-sessions/$SessionId/events" -Headers $headers -ContentType "application/json" -Body $eventBody | Out-Null

$chatBody = @{
    sender_role = "student"
    sender_code = $StudentCode
    target_code = "teacher"
    message = "Em can ho tro."
    scope = "student_to_teacher"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/exam-sessions/$SessionId/chat-messages" -Headers $headers -ContentType "application/json" -Body $chatBody | Out-Null

$submissionBody = @{
    student_code = $StudentCode
    file_name = "$StudentCode.zip"
    storage_path = "Submissions/EXAM-001/$StudentCode/$StudentCode.zip"
    sha256 = "abc123def456"
    file_size = 1024
    status = "submitted"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/exam-sessions/$SessionId/submissions" -Headers $headers -ContentType "application/json" -Body $submissionBody | Out-Null

$report = Invoke-RestMethod -Uri "$BaseUrl/api/exam-sessions/$SessionId/report" -Headers $headers
Assert-Equal $report.status "running" "Report did not reflect the running session state"
Assert-True ($report.totalEvents -ge 1) "Report did not record the posted event"
Assert-True ($report.totalSubmissions -ge 1) "Report did not record the posted submission"
Assert-Equal $report.recentEvents[0].studentCode $StudentCode "Recent event did not resolve the student code"
Assert-Equal $report.recentSubmissions[0].studentCode $StudentCode "Recent submission did not resolve the student code"

$finish = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/exam-sessions/$SessionId/finish" -Headers $headers
Assert-Equal $finish.status "finished" "Session did not transition to finished"

[pscustomobject]@{
    result = "passed"
    baseUrl = $BaseUrl
    sessionId = $SessionId
    studentCode = $StudentCode
    totalEvents = $report.totalEvents
    totalSubmissions = $report.totalSubmissions
} | ConvertTo-Json -Depth 4
