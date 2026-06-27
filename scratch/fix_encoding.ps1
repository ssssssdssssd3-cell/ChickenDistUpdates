# fix_encoding.ps1
$path = "ChickenDist\Core\UpdateManager.cs"
$content = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

# Get the Windows-1252 encoding
$enc1252 = [System.Text.Encoding]::GetEncoding(1252)

# Convert the string to bytes using Windows-1252
$bytes = $enc1252.GetBytes($content)

# Re-interpret these bytes as UTF-8
$decoded = [System.Text.Encoding]::UTF8.GetString($bytes)

# Write it back using UTF-8 WITH BOM (default for [System.IO.File]::WriteAllText with UTF8 encoding)
[System.IO.File]::WriteAllText($path, $decoded, [System.Text.Encoding]::UTF8)
Write-Output "Successfully decoded UpdateManager.cs"

# Also do the other UpdateManager.cs in Core/UpdateManager.cs if it exists
$path2 = "Core\UpdateManager.cs"
if (Test-Path $path2) {
    $content2 = [System.IO.File]::ReadAllText($path2, [System.Text.Encoding]::UTF8)
    $bytes2 = $enc1252.GetBytes($content2)
    $decoded2 = [System.Text.Encoding]::UTF8.GetString($bytes2)
    [System.IO.File]::WriteAllText($path2, $decoded2, [System.Text.Encoding]::UTF8)
    Write-Output "Successfully decoded Core\UpdateManager.cs"
}
