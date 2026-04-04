"""Compare v1 translated entries against beta ExampleLanguage entries.

Reports covered/missing/orphaned IDs by matching v1 *.jp.xml files against
beta *.example.xml files and comparing their extracted entry keys.

Usage:
    python scripts/diff_beta_strings.py <v1_dir> <beta_dir> [--format text|json|csv] [-o output]
"""

import argparse
import csv
import json
import sys
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import TextIO

from scripts.inventory_beta_strings import (
    TRIANGLE_RIGHT,
    extract_strings_from_file,
    extract_xml_entries,
)

# ---------------------------------------------------------------------------
# Data structures
# ---------------------------------------------------------------------------


@dataclass
class DiffEntry:
    """A single diff result for one translatable entry."""

    file: str  # beta example file name (or v1 file name for orphaned)
    root: str  # XML root tag
    key: str  # (Context,ID) for strings root; element_path+attribute for others
    status: str  # "covered" | "missing" | "orphaned"


@dataclass
class DiffReport:
    """Aggregated diff results."""

    entries: list[DiffEntry]

    @property
    def covered(self) -> list[DiffEntry]:
        """Entries present in both v1 and beta."""
        return [e for e in self.entries if e.status == "covered"]

    @property
    def missing(self) -> list[DiffEntry]:
        """Entries in beta but absent from v1."""
        return [e for e in self.entries if e.status == "missing"]

    @property
    def orphaned(self) -> list[DiffEntry]:
        """Entries in v1 but absent from beta (or no matching beta file)."""
        return [e for e in self.entries if e.status == "orphaned"]


# ---------------------------------------------------------------------------
# Loaders
# ---------------------------------------------------------------------------


def _make_key_strings(context: str, string_id: str) -> str:
    return f"{context}\x00{string_id}"


def _make_key_xml(element_path: str, attribute: str) -> str:
    return f"{element_path.replace(TRIANGLE_RIGHT, '')}\x00{attribute}"


def _load_beta_file_entries(filepath: Path) -> tuple[str, dict[str, str]]:
    """Load entries from a single beta XML file.

    Beta files use triangle markers to identify translatable content;
    this function only handles those properly-marked files.
    Returns a tuple of (root_tag, {key: value}).
    """
    # Strings files: use the dedicated extractor
    string_entries = extract_strings_from_file(filepath)
    if string_entries:
        keys = {_make_key_strings(e.context, e.string_id): e.value for e in string_entries}
        return "strings", keys

    # LanguageXml files: beta has triangle-marked attrs
    xml_entries = extract_xml_entries(filepath)
    if xml_entries:
        keys = {_make_key_xml(e.element_path, e.attribute): e.value for e in xml_entries}
        return xml_entries[0].root, keys

    print(f"WARNING: no entries extracted from {filepath}", file=sys.stderr)  # noqa: T201
    return "unknown", {}


def load_v1_entries(v1_dir: Path) -> dict[str, Path]:
    """Enumerate v1 *.jp.xml files from v1_dir.

    Returns a mapping: stem -> Path, deferring key extraction to
    match_beta_keys_in_v1 so that beta keys drive the comparison.
    """
    return {filepath.name[: -len(".jp.xml")]: filepath for filepath in sorted(v1_dir.glob("*.jp.xml"))}


def load_beta_entries(beta_dir: Path) -> dict[str, tuple[str, dict[str, str]]]:
    """Load all *.example.xml files from beta_dir.

    Returns a mapping: stem -> (root_tag, {key: value}).
    """
    result: dict[str, tuple[str, dict[str, str]]] = {}
    for filepath in sorted(beta_dir.glob("*.example.xml")):
        stem = filepath.name[: -len(".example.xml")]
        root_tag, keys = _load_beta_file_entries(filepath)
        result[stem] = (root_tag, keys)
    return result


# ---------------------------------------------------------------------------
# v1 key matching
# ---------------------------------------------------------------------------

# Identifier attributes used by the beta path-qualification logic.
_KEY_ATTRS = frozenset(("Name", "ID", "Command"))
# Attributes that are structural/metadata and not translatable content.
_SKIP_STRUCTURAL = frozenset(("Lang", "Encoding", "Load"))


def match_beta_keys_in_v1(beta_keys: dict[str, str], v1_filepath: Path, *, beta_root: str) -> set[str]:
    """Return the subset of beta_keys whose entries exist in the v1 file.

    For <strings> files (beta_root == "strings"), checks that Context+ID pairs
    appear in the v1 overlay.  For LanguageXml files, checks that the
    element_path+attribute qualified key resolves to an element with a matching
    identifier in the v1 tree.

    The comparison is purely structural (element presence) — it does not require
    the translated value to match.
    """
    if not beta_keys:
        return set()

    is_strings = beta_root == "strings"

    try:
        tree = ET.parse(v1_filepath)  # noqa: S314
    except ET.ParseError as exc:
        print(f"WARNING: failed to parse {v1_filepath}: {exc}", file=sys.stderr)  # noqa: T201
        return set()

    root = tree.getroot()

    if is_strings:
        # Build a set of (Context, ID) pairs from the v1 strings file
        v1_pairs: set[tuple[str, str]] = set()
        for elem in root.iter("string"):
            ctx = elem.get("Context", "")
            sid = elem.get("ID", "")
            if sid:
                v1_pairs.add((ctx, sid))

        matched: set[str] = set()
        for key in beta_keys:
            ctx, sid = key.split("\x00", 1)
            if (ctx, sid) in v1_pairs:
                matched.add(key)
        return matched
    # LanguageXml: walk the v1 tree and collect (element_path, attribute) pairs
    # using the same path-qualification as the beta extractor.
    v1_paths: set[tuple[str, str]] = _collect_xml_paths(root)
    matched = set()
    for key in beta_keys:
        path_part, attr = key.split("\x00", 1)
        if (path_part, attr) in v1_paths:
            matched.add(key)
    return matched


def _qualify_segment(tag: str, attrib: dict[str, str], *, sibling_index: int | None = None) -> str:
    """Build a qualified path segment from tag and key attributes."""
    keys = {k: v for k, v in attrib.items() if k in _KEY_ATTRS and v}
    if keys:
        qualifier = ",".join(f"{k}={v}" for k, v in sorted(keys.items()))
        return f"{tag}[{qualifier}]"
    if sibling_index is not None:
        return f"{tag}[{sibling_index}]"
    return tag


def _collect_xml_paths(root: ET.Element) -> set[tuple[str, str]]:
    """Walk a v1 XML tree and return a set of (element_path, attribute_name) pairs.

    Uses the same path-qualification logic as inventory_beta_strings so that
    keys produced here align with keys extracted from beta example files.
    Only non-structural, non-empty attributes are included.
    """
    result: set[tuple[str, str]] = set()

    def _walk(elem: ET.Element, parent_segments: list[str], *, sibling_index: int | None = None) -> None:
        my_segment = _qualify_segment(elem.tag, elem.attrib, sibling_index=sibling_index)
        current_segments = [*parent_segments, my_segment]
        current_path = ".".join(current_segments)

        for attr_name, attr_value in elem.attrib.items():
            if attr_name in _SKIP_STRUCTURAL:
                continue
            if attr_value:
                result.add((current_path, attr_name))

        if elem.text and elem.text.strip():
            result.add((current_path, "(text)"))

        children = list(elem)
        tag_totals: dict[str, int] = {}
        for c in children:
            tag_totals[c.tag] = tag_totals.get(c.tag, 0) + 1
        tag_idx: dict[str, int] = {}
        for child in children:
            idx = tag_idx.get(child.tag, 0)
            tag_idx[child.tag] = idx + 1
            child_key_attrs = {k: v for k, v in child.attrib.items() if k in _KEY_ATTRS and v}
            needs_index = not child_key_attrs and tag_totals[child.tag] > 1
            _walk(child, current_segments, sibling_index=idx if needs_index else None)

    _walk(root, [])
    return result


# ---------------------------------------------------------------------------
# Diff logic
# ---------------------------------------------------------------------------


def diff_entries(
    v1_map: dict[str, Path],
    beta_map: dict[str, tuple[str, dict[str, str]]],
) -> DiffReport:
    """Match v1 files to beta files by stem, compare entry keys.

    - covered: beta key present in v1 file
    - missing: beta key absent from v1 file
    - orphaned: v1 file has no matching beta file (reported at file level)
    """
    entries: list[DiffEntry] = []

    v1_stems = set(v1_map.keys())
    beta_stems = set(beta_map.keys())

    # Stems present in both: use beta keys as ground truth
    for stem in sorted(v1_stems & beta_stems):
        v1_filepath = v1_map[stem]
        beta_root, beta_keys = beta_map[stem]
        beta_file = f"{stem}.example.xml"

        covered_keys = match_beta_keys_in_v1(beta_keys, v1_filepath, beta_root=beta_root)

        for key in sorted(beta_keys):
            status = "covered" if key in covered_keys else "missing"
            entries.append(DiffEntry(file=beta_file, root=beta_root, key=key, status=status))

    # Beta-only stems: all entries are missing
    for stem in sorted(beta_stems - v1_stems):
        beta_root, beta_keys = beta_map[stem]
        beta_file = f"{stem}.example.xml"
        entries.extend(
            DiffEntry(file=beta_file, root=beta_root, key=key, status="missing") for key in sorted(beta_keys)
        )

    # V1-only stems: report as orphaned (one entry per file)
    for stem in sorted(v1_stems - beta_stems):
        v1_filepath = v1_map[stem]
        v1_file = f"{stem}.jp.xml"
        entries.append(DiffEntry(file=v1_file, root="unknown", key=v1_file, status="orphaned"))

    return DiffReport(entries=entries)


# ---------------------------------------------------------------------------
# Output formatters
# ---------------------------------------------------------------------------


def diff_to_json(report: DiffReport) -> str:
    """Serialize DiffReport to JSON."""
    data = {
        "summary": {
            "covered": len(report.covered),
            "missing": len(report.missing),
            "orphaned": len(report.orphaned),
            "total": len(report.entries),
        },
        "entries": [asdict(e) for e in report.entries],
    }
    return json.dumps(data, ensure_ascii=False, indent=2)


def diff_to_csv(report: DiffReport, stream: TextIO) -> None:
    """Write diff entries to CSV on the given stream."""
    writer = csv.writer(stream)
    writer.writerow(["file", "root", "key", "status"])
    for e in report.entries:
        writer.writerow([e.file, e.root, e.key, e.status])


def diff_to_text(report: DiffReport, stream: TextIO) -> None:
    """Write human-readable diff summary to stream."""
    total = len(report.entries)
    n_covered = len(report.covered)
    n_missing = len(report.missing)
    n_orphaned = len(report.orphaned)

    stream.write(f"Diff summary: {total} entries total\n")
    stream.write(f"  covered : {n_covered}\n")
    stream.write(f"  missing : {n_missing}\n")
    stream.write(f"  orphaned: {n_orphaned}\n")

    if n_missing:
        stream.write("\n[MISSING] In beta but not in v1:\n")
        stream.writelines(f"  {e.file}  {e.key}\n" for e in report.missing)

    if n_orphaned:
        stream.write("\n[ORPHANED] In v1 but not in beta:\n")
        stream.writelines(f"  {e.file}  {e.key}\n" for e in report.orphaned)


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def main(argv: list[str] | None = None) -> int:
    """Run diff from CLI arguments."""
    parser = argparse.ArgumentParser(description="Diff v1 jp.xml entries against beta example.xml entries")
    parser.add_argument("v1_dir", type=Path, help="Directory containing *.jp.xml files")
    parser.add_argument("beta_dir", type=Path, help="Directory containing *.example.xml files")
    parser.add_argument(
        "--format",
        choices=["text", "json", "csv"],
        default="text",
        help="Output format (default: text)",
    )
    parser.add_argument("-o", "--output", type=Path, help="Output file (default: stdout)")
    args = parser.parse_args(argv)

    if not args.v1_dir.is_dir():
        print(f"Error: {args.v1_dir} is not a directory", file=sys.stderr)  # noqa: T201
        return 1
    if not args.beta_dir.is_dir():
        print(f"Error: {args.beta_dir} is not a directory", file=sys.stderr)  # noqa: T201
        return 1

    v1_map = load_v1_entries(args.v1_dir)
    beta_map = load_beta_entries(args.beta_dir)
    report = diff_entries(v1_map, beta_map)

    if args.format == "json":
        text = diff_to_json(report)
        if args.output:
            args.output.write_text(text, encoding="utf-8")
        else:
            print(text)  # noqa: T201
    elif args.format == "csv":
        if args.output:
            with args.output.open("w", newline="", encoding="utf-8") as f:
                diff_to_csv(report, f)
        else:
            diff_to_csv(report, sys.stdout)
    elif args.output:
        with args.output.open("w", encoding="utf-8") as f:
            diff_to_text(report, f)
    else:
        diff_to_text(report, sys.stdout)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
