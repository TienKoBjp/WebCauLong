$key = "YOUR_GEMINI_API_KEY"
$uri = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key=$key"
$body = @{
    contents = @(
        @{
            parts = @(
                @{ text = "Hello" }
            )
        }
    )
} | ConvertTo-Json -Depth 5

try {
    $res = Invoke-RestMethod -Method Post -Uri $uri -ContentType "application/json" -Body $body
    Write-Host "Success!"
    $res | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Error status code: $($_.Exception.Response.StatusCode)"
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $errorJson = $reader.ReadToEnd()
    Write-Host "Response body: $errorJson"
}

