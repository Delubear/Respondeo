# Imports the public-domain Catholic Public Domain Version (Ronald L. Conte Jr.) from the
# yoarikso/cpdvbible dataset into src/Respondeo.Bible/wwwroot/cpdv/{slug}.json, using the same
# schema as the Douay-Rheims files, plus a manifest.json keyed by the same CPDV (modern) book slugs.
# Source files are numbered in canonical order, so they map to slugs by index.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$out = Join-Path $root 'src/Respondeo.Bible/wwwroot/cpdv'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$api = 'https://api.github.com/repos/yoarikso/cpdvbible/contents/cpdv-json?ref=master'
$files = (Invoke-RestMethod $api) | Where-Object { $_.name -match '^(OT|NT)-(\d+)_' }

$oldBooks = @('Genesis', 'Exodus', 'Leviticus', 'Numbers', 'Deuteronomy', 'Joshua', 'Judges', 'Ruth', '1 Samuel', '2 Samuel',
	'1 Kings', '2 Kings', '1 Chronicles', '2 Chronicles', 'Ezra', 'Nehemiah', 'Tobit', 'Judith', 'Esther', 'Job',
	'Psalms', 'Proverbs', 'Ecclesiastes', 'Song of Songs', 'Wisdom', 'Sirach', 'Isaiah', 'Jeremiah',
	'Lamentations', 'Baruch', 'Ezekiel', 'Daniel', 'Hosea', 'Joel', 'Amos', 'Obadiah', 'Jonah', 'Micah', 'Nahum', 'Habakkuk',
	'Zephaniah', 'Haggai', 'Zechariah', 'Malachi', '1 Maccabees', '2 Maccabees')
$newBooks = @('Matthew', 'Mark', 'Luke', 'John', 'Acts', 'Romans', '1 Corinthians', '2 Corinthians', 'Galatians', 'Ephesians',
	'Philippians', 'Colossians', '1 Thessalonians', '2 Thessalonians', '1 Timothy', '2 Timothy', 'Titus', 'Philemon', 'Hebrews',
	'James', '1 Peter', '2 Peter', '1 John', '2 John', '3 John', 'Jude', 'Revelation')

function Get-Slug($name) { $name.ToLowerInvariant().Replace(' ', '-') }
$imported = @{}

$client = New-Object Net.WebClient
$client.Encoding = [Text.Encoding]::UTF8

function Get-Numbered($object) {
	$object.PSObject.Properties | Where-Object { $_.Name -match '^\d+$' } | Sort-Object { [int]$_.Name }
}

foreach ($file in $files) {
	$null = $file.name -match '^(OT|NT)-(\d+)_'
	$index = [int]$Matches[2] - 1
	$slug = Get-Slug $(if ($Matches[1] -eq 'OT') { $oldBooks[$index] } else { $newBooks[$index] })
	$book = $client.DownloadString($file.download_url) | ConvertFrom-Json

	$chapters = foreach ($c in (Get-Numbered $book)) {
		$verses = foreach ($v in (Get-Numbered $c.Value)) {
			' ' -replace '\s+', ' ').Trim()
						# The source appends Hebrew psalm numbers such as "(23)" or "(116A)" to some verses; drop them.
						$text = ($text -replace '\s*\(\d+[AB]?\)$', '').Trim()
			if ($text) { [ordered]@{ verse = [int]$v.Name; text = $text } }
		}
		[ordered]@{ chapter = [int]$c.Name; verses = @($verses) }
	}

	$json = [ordered]@{ chapters = @($chapters) } | ConvertTo-Json -Depth 5 -Compress
	[IO.File]::WriteAllText((Join-Path $out "$slug.json"), $json, (New-Object Text.UTF8Encoding $false))
	$imported[$slug] = $true
	Write-Host "$($file.name) -> $slug.json ($(@($chapters).Count) chapters)"
}

# Manifest in canonical order, keyed by CPDV slug and listing only books present in the source dataset.
$books = [ordered]@{}
foreach ($name in $oldBooks + $newBooks) {
	$slug = Get-Slug $name
	if ($imported.ContainsKey($slug)) { $books[$slug] = [ordered]@{ file = "$slug.json"; name = $name } }
}
$manifest = [ordered]@{ books = $books } | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText((Join-Path $out 'manifest.json'), $manifest, (New-Object Text.UTF8Encoding $false))
Write-Host "manifest.json ($($books.Count) books)"
