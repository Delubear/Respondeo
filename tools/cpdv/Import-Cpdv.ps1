# Imports the public-domain Catholic Public Domain Version (Ronald L. Conte Jr.) from the
# yoarikso/cpdvbible dataset into src/Respondeo.Bible/wwwroot/cpdv/{slug}.json, using the same
# schema as the Douay-Rheims files. CPDV book order matches the Douay-Rheims order, so files map by index.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$out = Join-Path $root 'src/Respondeo.Bible/wwwroot/cpdv'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$api = 'https://api.github.com/repos/yoarikso/cpdvbible/contents/cpdv-json?ref=master'
$files = (Invoke-RestMethod $api) | Where-Object { $_.name -match '^(OT|NT)-(\d+)_' }

$oldSlugs = @('genesis', 'exodus', 'leviticus', 'numbers', 'deuteronomy', 'josue', 'judges', 'ruth', '1-kings', '2-kings',
	'3-kings', '4-kings', '1-paralipomenon', '2-paralipomenon', '1-esdras', '2-esdras', 'tobias', 'judith', 'esther', 'job',
	'psalms', 'proverbs', 'ecclesiastes', 'canticle-of-canticles', 'wisdom', 'ecclesiasticus', 'isaie', 'jeremie',
	'lamentations', 'baruch', 'ezechiel', 'daniel', 'osee', 'joel', 'amos', 'abdias', 'jonas', 'micheas', 'nahum', 'habacuc',
	'sophonias', 'aggeus', 'zacharias', 'malachie', '1-machabees', '2-machabees')
$newSlugs = @('matthew', 'mark', 'luke', 'john', 'acts', 'romans', '1-corinthians', '2-corinthians', 'galatians', 'ephesians',
	'philippians', 'colossians', '1-thessalonians', '2-thessalonians', '1-timothy', '2-timothy', 'titus', 'philemon', 'hebrews',
	'james', '1-peter', '2-peter', '1-john', '2-john', '3-john', 'jude', 'apocalypse')

$client = New-Object Net.WebClient
$client.Encoding = [Text.Encoding]::UTF8

function Get-Numbered($object) {
	$object.PSObject.Properties | Where-Object { $_.Name -match '^\d+$' } | Sort-Object { [int]$_.Name }
}

foreach ($file in $files) {
	$null = $file.name -match '^(OT|NT)-(\d+)_'
	$index = [int]$Matches[2] - 1
	$slug = if ($Matches[1] -eq 'OT') { $oldSlugs[$index] } else { $newSlugs[$index] }
	$book = $client.DownloadString($file.download_url) | ConvertFrom-Json

	$chapters = foreach ($c in (Get-Numbered $book)) {
		$verses = foreach ($v in (Get-Numbered $c.Value)) {
			$text = (([string]$v.Value) -replace [char]0x00A0, ' ' -replace '\s+', ' ').Trim()
			if ($text) { [ordered]@{ verse = [int]$v.Name; text = $text } }
		}
		[ordered]@{ chapter = [int]$c.Name; verses = @($verses) }
	}

	$json = [ordered]@{ chapters = @($chapters) } | ConvertTo-Json -Depth 5 -Compress
	[IO.File]::WriteAllText((Join-Path $out "$slug.json"), $json, (New-Object Text.UTF8Encoding $false))
	Write-Host "$($file.name) -> $slug.json ($(@($chapters).Count) chapters)"
}
