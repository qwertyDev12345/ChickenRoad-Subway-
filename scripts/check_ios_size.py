"""Check exported IPA and uncompressed Payload against a decimal MB budget."""
import argparse
import json
from pathlib import Path
import zipfile


def measure(ipa):
    with zipfile.ZipFile(ipa) as archive:
        entries = [entry for entry in archive.infolist()
                   if entry.filename.startswith("Payload/") and not entry.is_dir()]
        if not entries:
            raise ValueError("IPA contains no Payload files")
        # Include embedded frameworks, extensions and signatures in the app budget.
        return {
            "ipa_bytes": ipa.stat().st_size,
            "app_bytes": sum(entry.file_size for entry in entries),
            "largest_files": [{"path": e.filename, "bytes": e.file_size}
                              for e in sorted(entries, key=lambda e: e.file_size, reverse=True)[:20]],
        }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("ipa", type=Path)
    parser.add_argument("--limit-mb", type=float, default=95,
                        help="Default leaves 5 MB below the 100 MB product limit for store processing")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    result = measure(args.ipa)
    result["limit_bytes"] = int(args.limit_mb * 1_000_000)
    result["passed"] = max(result["ipa_bytes"], result["app_bytes"]) < result["limit_bytes"]
    output = json.dumps(result, indent=2)
    print(output)
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(output + "\n", encoding="utf-8")
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
