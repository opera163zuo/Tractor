#!/usr/bin/env python3
from __future__ import annotations

import argparse
from pathlib import Path

TEXT_EXTENSIONS = {'.cs', '.csproj', '.sln', '.resx', '.config'}
SKIP_DIRS = {'.git', '.vs', 'bin', 'obj', 'packages'}
UTF8_BOM = b'\xef\xbb\xbf'


def should_skip(path: Path) -> bool:
    return any(part in SKIP_DIRS for part in path.parts)


def iter_targets(root: Path):
    for path in root.rglob('*'):
        if not path.is_file() or should_skip(path):
            continue
        if path.suffix.lower() in TEXT_EXTENSIONS:
            yield path


def decode_source(data: bytes) -> tuple[str, str]:
    payload = data[3:] if data.startswith(UTF8_BOM) else data
    for enc in ('utf-8', 'gb18030'):
        try:
            text = payload.decode(enc)
            return text, enc
        except UnicodeDecodeError:
            pass
    raise UnicodeDecodeError('unknown', b'', 0, 1, 'unsupported text encoding')


def normalize_newlines(text: str) -> str:
    text = text.replace('\r\n', '\n').replace('\r', '\n')
    return text.replace('\n', '\r\n')


def rewrite(path: Path, dry_run: bool) -> str:
    raw = path.read_bytes()
    text, detected = decode_source(raw)
    normalized = UTF8_BOM + normalize_newlines(text).encode('utf-8')
    changed = raw != normalized
    if changed and not dry_run:
        path.write_bytes(normalized)
    return f"{'CHANGED' if changed else 'OK'}\t{detected}\t{path}"


def main() -> int:
    parser = argparse.ArgumentParser(description='Normalize .NET text files to UTF-8 BOM + CRLF.')
    parser.add_argument('root', nargs='?', default='.', help='repository root')
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()

    root = Path(args.root).resolve()
    failures = []
    for path in iter_targets(root):
        try:
            print(rewrite(path, args.dry_run))
        except Exception as exc:
            failures.append((path, exc))
            print(f'ERROR\t{path}\t{exc}')
    return 1 if failures else 0


if __name__ == '__main__':
    raise SystemExit(main())
