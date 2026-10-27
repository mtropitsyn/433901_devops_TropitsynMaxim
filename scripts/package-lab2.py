"""Export only the calculator lab sources; never include local .env, reports or databases."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

root = Path(__file__).resolve().parents[1]
files = [
    '.dockerignore', '.env.example', '.gitattributes', '.gitignore',
    'CalculatorDocker.sln', 'Dockerfile', 'compose.yaml', 'README.md',
    'database/prepare.sql', 'database/migrations.sql',
    'docs/SERVER_DEPLOYMENT.md', 'docs/SERVER_COMMANDS.txt', 'docs/VERIFICATION.md',
    'scripts/init-server-env.sh', 'scripts/install-docker-server.sh',
    'scripts/prepare-database.sh', 'scripts/Prepare-Database.ps1',
    'scripts/Initialize-Database.ps1', 'scripts/Show-Calculations.ps1',
    'scripts/Test-Web.ps1', 'scripts/Test-Database.ps1', 'scripts/package-lab2.py',
]
for directory in ('CalculatorWeb', 'CalculatorWeb.Checks', '.config', '.github'):
    for path in (root / directory).rglob('*'):
        relative = path.relative_to(root)
        if path.is_file() and not any(part in ('bin', 'obj', '.vs', '__pycache__') for part in relative.parts):
            files.append(relative.as_posix())

sources = sorted(set(files))
for name in sources:
    path = root / name
    if not path.resolve().is_relative_to(root) or not path.is_file():
        raise ValueError(f'Missing or invalid project file: {name}')
    if path.name == '.env' or (path.name.startswith('.env.') and path.name != '.env.example'):
        raise ValueError(f'Private settings must not be exported: {name}')

output = root / 'output' / 'Calculator_MariaDB_Lab2.zip'
output.parent.mkdir(exist_ok=True)
with ZipFile(output, 'w', ZIP_DEFLATED) as archive:
    for name in sources:
        archive.write(root / name, arcname=name)
print(f'Created {output.name}: {len(sources)} files; no .env, build outputs or old reports.')
