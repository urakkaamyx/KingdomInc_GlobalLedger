"""Package the exact installed and verified Global Ledger DLL."""
from pathlib import Path, PurePosixPath
import hashlib
import json
import re
import tempfile
import xml.etree.ElementTree as ET
import zipfile

PROJECT = Path(__file__).resolve().parents[1]
GAME = PROJECT.parents[1]
OUT = PROJECT / 'Releases'
VERSION = ET.parse(PROJECT / 'GlobalLedger.csproj').getroot().findtext('PropertyGroup/Version')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def archive(path, entries):
    with zipfile.ZipFile(path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as output:
        for name, data in sorted(entries.items()):
            parts = PurePosixPath(name)
            assert not parts.is_absolute() and '..' not in parts.parts and ':' not in name
            output.writestr(name, data)
    with zipfile.ZipFile(path) as check:
        assert check.testzip() is None and set(check.namelist()) == set(entries)
        with tempfile.TemporaryDirectory(prefix='PackageExtract-', dir=PROJECT / 'Verification') as tmp:
            root = Path(tmp).resolve()
            assert root.parent == (PROJECT / 'Verification').resolve()
            check.extractall(root)
            assert {p.relative_to(root).as_posix(): p.read_bytes() for p in root.rglob('*') if p.is_file()} == entries


def main():
    dll = (PROJECT / 'bin/Release/netstandard2.1/GlobalLedger.dll').read_bytes()
    installed_dll = GAME / 'BepInEx/plugins/GlobalLedger/GlobalLedger.dll'
    built_foundation = PROJECT.parent / 'Foundation/bin/Release/netstandard2.1/Foundation.dll'
    foundation_path = built_foundation if built_foundation.exists() else (GAME / 'BepInEx/plugins/Foundation/Foundation.dll')
    foundation_sha = digest(foundation_path.read_bytes())
    reports = [json.loads((PROJECT / 'Verification' / name).read_text(encoding='utf-8-sig')) for name in ('runtime-tests.json', 'compatibility-tests.json')]
    for report in reports:
        assert report['passed'] and not report['error'] and report['count'] >= 99
        assert report['version'] == VERSION and report['dllSha256'] == digest(dll)
        assert any(p.startswith('Foundation ') for p in report['plugins'])
        assert any('Basic mode sorts active items by Tier then A-Z and hides zero-production items by default' in check for check in report['checks'])
        assert any('Container rows display [ContainerIcon (IconDecoRect)] | [ProductionIcon] [ContainerName]' in check for check in report['checks'])
        assert any('Container and building rows with live targets show a dedicated View button on the right' in check for check in report['checks'])
        assert any('In Adv mode, all currency roots start collapsed by default without dumping child rows' in check for check in report['checks'])
        assert any('Gold rows display XX[GoldCoin] XX[SilverCoin] XX[CopperCoin] with native coin symbols' in check for check in report['checks'])
        assert any('Workspace camera pans and zooms normally while Global Ledger stays open' in check for check in report['checks'])
    for log in ('unity-isolated.log', 'unity-companion.log'):
        text = (PROJECT / 'Verification' / log).read_text(encoding='utf-8', errors='replace')
        assert f"Global Ledger passed {reports[0]['count']} runtime checks." in text
        assert 'NullReferenceException' not in text and '[Error  :Global Ledger]' not in text
    forbidden = r'verification|runtime|diagnostic|isolated|companion|Sharable Blueprints|Party[ -]?(?:Up|Node)|Leyline|Path[ -]?Finder|Factory API'
    for name in ('README.md', 'Nexus/README.txt', 'Nexus/NEXUS_DESCRIPTION.bbcode.txt'):
        assert not re.search(forbidden, (PROJECT / name).read_text(encoding='utf-8'), re.I), name
    OUT.mkdir(exist_ok=True)
    player = {'BepInEx/plugins/GlobalLedger/GlobalLedger.dll': dll}
    player.update({'Global Ledger ' + name: (PROJECT / 'Nexus' / name).read_bytes() for name in ('README.txt', 'CHANGELOG.txt', 'REQUIREMENTS.txt')})
    player_zip = OUT / f'KingdomInc-GlobalLedger-{VERSION}.zip'
    archive(player_zip, player)
    names = [
        'GlobalLedger.csproj', 'GlobalLedgerPlugin.cs', 'LedgerBook.cs', 'LedgerLocations.cs',
        'LedgerNavigation.cs', 'LedgerObserver.cs', 'LedgerPanel.cs', 'LedgerUi.cs',
        'Diagnostics.cs', 'NavigationChecks.cs', 'README.md', 'REQUIREMENTS.txt', '.gitignore',
        'install.ps1', 'verify-isolated.ps1', 'Tools/package_nexus.py',
        'Nexus/README.txt', 'Nexus/CHANGELOG.txt', 'Nexus/REQUIREMENTS.txt',
        'Nexus/SUMMARY.txt', 'Nexus/NEXUS_DESCRIPTION.bbcode.txt'
    ]
    source_zip = OUT / f'KingdomInc-GlobalLedger-{VERSION}-source.zip'
    archive(source_zip, {'_Mods/GlobalLedger/' + name: (PROJECT / name).read_bytes() for name in names})
    result = {
        'name': 'Global Ledger', 'version': VERSION,
        'dllSha256': digest(dll), 'gameVersion': reports[0]['gameVersion'], 'unityVersion': reports[0]['unityVersion'],
        'runtimeChecks': reports[0]['count'], 'companionChecks': reports[1]['count'],
        'requiredFramework': 'Foundation 0.6.0+', 'foundationSha256': foundation_sha,
        'cleanExtractionPassed': True, 'files': sorted(player),
        'archives': {p.name: {'bytes': p.stat().st_size, 'sha256': digest(p.read_bytes())} for p in (player_zip, source_zip)}
    }
    (OUT / 'RELEASE_VERIFICATION.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
