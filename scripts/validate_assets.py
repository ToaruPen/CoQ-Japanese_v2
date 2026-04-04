"""Validate localization XML assets for the Caves of Qud beta pipeline.

Checks:
    1. XML well-formedness
    2. UTF-8 encoding without BOM
    3. Placeholder preservation between source and translation
    4. Structural consistency of overlay files against their schema surface

Supported placeholder patterns:
    - {{...}}   template variables
    - &X        color/markup codes (single char after &)
    - ^x        case/formatting codes (single char after ^)
    - &&        literal ampersand escape
    - ^^        literal caret escape
    - =token=   replacement tokens

Usage:
    python scripts/validate_assets.py <file_or_dir> [--overlay-source <source_dir>]
"""

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from dataclasses import dataclass, field
from enum import StrEnum
from pathlib import Path


class Severity(StrEnum):
    """Issue severity level."""

    ERROR = "error"
    WARNING = "warning"


@dataclass
class ValidationIssue:
    """A single validation finding."""

    file: str
    severity: Severity
    check: str
    message: str
    line: int | None = None


@dataclass
class ValidationResult:
    """Aggregated validation result."""

    issues: list[ValidationIssue] = field(default_factory=list)

    @property
    def error_count(self) -> int:
        """Count of error-severity issues."""
        return sum(1 for i in self.issues if i.severity == Severity.ERROR)

    @property
    def warning_count(self) -> int:
        """Count of warning-severity issues."""
        return sum(1 for i in self.issues if i.severity == Severity.WARNING)

    @property
    def ok(self) -> bool:
        """True when no errors are present."""
        return self.error_count == 0


# ---------------------------------------------------------------------------
# Placeholder extraction
# ---------------------------------------------------------------------------

# Order matters: match && and ^^ before the single-char variants
PLACEHOLDER_PATTERNS: list[tuple[str, re.Pattern[str]]] = [
    ("double_ampersand", re.compile(r"&&")),
    ("double_caret", re.compile(r"\^\^")),
    ("template_var", re.compile(r"\{\{[^}]+\}\}")),
    ("replacement_token", re.compile(r"=[a-zA-Z_][a-zA-Z0-9_.]*=")),
    ("color_code", re.compile(r"&[a-zA-Z0-9]")),
    ("format_code", re.compile(r"\^[a-zA-Z0-9]")),
]


def extract_placeholders(text: str) -> list[str]:
    """Extract all placeholder tokens from a text string.

    Returns a sorted list of placeholder strings found in the text.
    The extraction is order-aware: && and ^^ are consumed before &X and ^x
    so that escaped literals are not double-counted.
    """
    result: list[str] = []
    remaining = text
    for _name, pattern in PLACEHOLDER_PATTERNS:
        matches = pattern.findall(remaining)
        result.extend(matches)
        # Remove matched spans to avoid double-counting
        remaining = pattern.sub("", remaining)
    return sorted(result)


# ---------------------------------------------------------------------------
# Individual checks
# ---------------------------------------------------------------------------


def check_utf8_no_bom(filepath: Path) -> list[ValidationIssue]:
    """Verify the file is valid UTF-8 without a BOM."""
    issues: list[ValidationIssue] = []
    raw = filepath.read_bytes()

    # BOM check
    if raw.startswith(b"\xef\xbb\xbf"):
        issues.append(
            ValidationIssue(
                file=str(filepath),
                severity=Severity.ERROR,
                check="utf8_bom",
                message="File has UTF-8 BOM; the game expects UTF-8 without BOM",
            )
        )

    # Encoding check
    try:
        raw.decode("utf-8")
    except UnicodeDecodeError as e:
        issues.append(
            ValidationIssue(
                file=str(filepath),
                severity=Severity.ERROR,
                check="utf8_decode",
                message=f"File is not valid UTF-8: {e}",
            )
        )

    return issues


def check_xml_wellformedness(filepath: Path) -> list[ValidationIssue]:
    """Parse the XML and report any well-formedness errors."""
    issues: list[ValidationIssue] = []
    try:
        ET.parse(filepath)  # noqa: S314
    except ET.ParseError as e:
        line = e.position[0] if hasattr(e, "position") and e.position else None
        issues.append(
            ValidationIssue(
                file=str(filepath),
                severity=Severity.ERROR,
                check="xml_parse",
                message=f"XML parse error: {e}",
                line=line,
            )
        )
    return issues


def check_placeholders_in_file(filepath: Path) -> list[ValidationIssue]:
    """Scan all text content and attributes for common placeholder issues.

    This standalone check looks for malformed placeholders (unclosed {{ or =token).
    For cross-file placeholder preservation, use check_overlay_placeholders().
    """
    issues: list[ValidationIssue] = []
    try:
        tree = ET.parse(filepath)  # noqa: S314
    except ET.ParseError:
        return issues  # already caught by wellformedness check

    for elem in tree.iter():
        texts_to_check: list[tuple[str, str]] = []
        for attr_name, attr_value in elem.attrib.items():
            texts_to_check.append((f"@{attr_name}", attr_value))
        if elem.text and elem.text.strip():
            texts_to_check.append(("(text)", elem.text))

        for location, text in texts_to_check:
            # Unclosed template variables
            opens = text.count("{{")
            closes = text.count("}}")
            if opens != closes:
                issues.append(
                    ValidationIssue(
                        file=str(filepath),
                        severity=Severity.ERROR,
                        check="placeholder_balance",
                        message=f"Unbalanced {{{{ }}}} in {elem.tag}/{location}: {opens} opens vs {closes} closes",
                    )
                )
            # Unclosed =token= detection: find =word sequences not closed by a trailing =
            closed_tokens = set(re.findall(r"=[a-zA-Z_][a-zA-Z0-9_.]*=", text))
            stripped = text
            for tok in closed_tokens:
                stripped = stripped.replace(tok, "")
            unclosed = re.findall(r"=[a-zA-Z_][a-zA-Z0-9_.]*(?!=)", stripped)
            if unclosed:
                issues.append(
                    ValidationIssue(
                        file=str(filepath),
                        severity=Severity.WARNING,
                        check="token_unclosed",
                        message=f"Possibly unclosed =token in {elem.tag}/{location}: {unclosed}",
                    )
                )

    return issues


def check_overlay_placeholders(
    overlay_path: Path,
    source_path: Path,
) -> list[ValidationIssue]:
    """Compare placeholder sets between a *.jp.xml overlay and its source.

    For Strings files, compare by Context+ID.
    For other XML files, compare by element path + attribute.
    """
    issues: list[ValidationIssue] = []

    try:
        overlay_tree = ET.parse(overlay_path)  # noqa: S314
        source_tree = ET.parse(source_path)  # noqa: S314
    except ET.ParseError:
        return issues

    overlay_root = overlay_tree.getroot()
    source_root = source_tree.getroot()

    # Root tag consistency
    if overlay_root.tag != source_root.tag:
        issues.append(
            ValidationIssue(
                file=str(overlay_path),
                severity=Severity.ERROR,
                check="root_tag_mismatch",
                message=f"Overlay root <{overlay_root.tag}> does not match source <{source_root.tag}>",
            )
        )
        return issues

    # Strings-style files: <strings> with <string Context="" ID="">
    if source_root.tag == "strings":
        source_map: dict[tuple[str, str], str] = {}
        for elem in source_root.iter("string"):
            ctx = elem.get("Context", "")
            sid = elem.get("ID", "")
            raw_text = (elem.text or "").strip()
            val = raw_text or elem.get("Value", "")
            source_map[(ctx, sid)] = val

        for elem in overlay_root.iter("string"):
            ctx = elem.get("Context", "")
            sid = elem.get("ID", "")
            raw_text = (elem.text or "").strip()
            val = raw_text or elem.get("Value", "")
            key = (ctx, sid)
            if key in source_map:
                src_ph = Counter(extract_placeholders(source_map[key]))
                ovl_ph = Counter(extract_placeholders(val))
                missing = sorted((src_ph - ovl_ph).elements())
                added = sorted((ovl_ph - src_ph).elements())
                if missing:
                    issues.append(
                        ValidationIssue(
                            file=str(overlay_path),
                            severity=Severity.ERROR,
                            check="placeholder_missing",
                            message=(f"String [{ctx}]{sid}: missing placeholders from source: {missing}"),
                        )
                    )
                if added:
                    issues.append(
                        ValidationIssue(
                            file=str(overlay_path),
                            severity=Severity.WARNING,
                            check="placeholder_added",
                            message=(f"String [{ctx}]{sid}: new placeholders not in source: {added}"),
                        )
                    )
    else:
        # XML-attribute style: compare matching elements by key attributes
        _compare_xml_trees(source_root, overlay_root, overlay_path, issues)

    return issues


def _find_overlay_match(
    src_child: ET.Element,
    src_index: int,
    overlay: ET.Element,
) -> ET.Element | None:
    """Find the overlay element matching a source child by tag and key attributes.

    When no key attributes (Name, ID, Command) are present, falls back to
    positional matching among same-tag siblings.
    """
    key_attrs = {k: v for k, v in src_child.attrib.items() if k in {"Name", "ID", "Command"}}
    if key_attrs:
        for ovl_child in overlay:
            if ovl_child.tag != src_child.tag:
                continue
            if all(ovl_child.get(k) == v for k, v in key_attrs.items()):
                return ovl_child
        return None
    # Positional fallback: match the Nth same-tag sibling
    same_tag = [c for c in overlay if c.tag == src_child.tag]
    return same_tag[src_index] if src_index < len(same_tag) else None


def _build_child_path(src_child: ET.Element, parent_path: str) -> str:
    """Build a path string for an element including its key attributes."""
    child_path = f"{parent_path}/{src_child.tag}"
    key_attrs = {k: v for k, v in src_child.attrib.items() if k in {"Name", "ID", "Command"}}
    if key_attrs:
        key_str = ",".join(f"{k}={v}" for k, v in sorted(key_attrs.items()))
        child_path = f"{child_path}[{key_str}]"
    return child_path


def _check_placeholder_diff(
    src_val: str,
    ovl_val: str,
    overlay_path: Path,
    location: str,
) -> list[ValidationIssue]:
    """Return issues for missing or added placeholders between source and overlay."""
    src_ph = Counter(extract_placeholders(src_val))
    ovl_ph = Counter(extract_placeholders(ovl_val))
    result: list[ValidationIssue] = []
    missing = sorted((src_ph - ovl_ph).elements())
    if missing:
        result.append(
            ValidationIssue(
                file=str(overlay_path),
                severity=Severity.ERROR,
                check="placeholder_missing",
                message=f"{location}: missing placeholders: {missing}",
            )
        )
    added = sorted((ovl_ph - src_ph).elements())
    if added:
        result.append(
            ValidationIssue(
                file=str(overlay_path),
                severity=Severity.WARNING,
                check="placeholder_added",
                message=f"{location}: new placeholders not in source: {added}",
            )
        )
    return result


def _compare_xml_trees(
    source: ET.Element,
    overlay: ET.Element,
    overlay_path: Path,
    issues: list[ValidationIssue],
    path: str = "",
) -> None:
    """Recursively compare attributes of matching elements between source and overlay."""
    # Track positional index per tag for keyless sibling matching
    tag_counts: dict[str, int] = {}
    for src_child in source:
        idx = tag_counts.get(src_child.tag, 0)
        tag_counts[src_child.tag] = idx + 1
        match = _find_overlay_match(src_child, idx, overlay)
        if match is None:
            continue

        child_path = _build_child_path(src_child, path)

        # Compare attribute placeholders
        for attr_name in src_child.attrib:
            if attr_name in {"Lang", "Encoding", "Load"}:
                continue
            # Overlay files only specify changed values; skip if overlay has no value
            ovl_val = match.get(attr_name, "")
            if not ovl_val:
                continue
            issues.extend(
                _check_placeholder_diff(
                    src_child.get(attr_name, ""), ovl_val, overlay_path, f"{child_path}/@{attr_name}"
                )
            )

        # Compare text content
        src_text = src_child.text or ""
        ovl_text = match.text or ""
        # Overlay files only specify changed text; skip if empty
        if src_text.strip() and ovl_text.strip():
            issues.extend(_check_placeholder_diff(src_text, ovl_text, overlay_path, f"{child_path}/(text)"))

        _compare_xml_trees(src_child, match, overlay_path, issues, child_path)


# ---------------------------------------------------------------------------
# Top-level validation
# ---------------------------------------------------------------------------


def validate_file(filepath: Path, source_dir: Path | None = None) -> ValidationResult:
    """Run all validation checks on a single XML file."""
    result = ValidationResult()

    result.issues.extend(check_utf8_no_bom(filepath))
    result.issues.extend(check_xml_wellformedness(filepath))
    result.issues.extend(check_placeholders_in_file(filepath))

    # Overlay check: if this looks like a *.jp.xml, find the source
    if source_dir and filepath.name.endswith(".jp.xml"):
        source_name = filepath.name.replace(".jp.xml", ".example.xml")
        source_path = source_dir / source_name
        if source_path.exists():
            result.issues.extend(check_overlay_placeholders(filepath, source_path))
        else:
            result.issues.append(
                ValidationIssue(
                    file=str(filepath),
                    severity=Severity.WARNING,
                    check="source_not_found",
                    message=f"Expected source {source_name} not found in {source_dir}",
                )
            )

    return result


def validate_directory(dirpath: Path, source_dir: Path | None = None) -> ValidationResult:
    """Validate all XML files in a directory."""
    result = ValidationResult()
    for filepath in sorted(dirpath.glob("**/*.xml")):
        file_result = validate_file(filepath, source_dir)
        result.issues.extend(file_result.issues)
    return result


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def main(argv: list[str] | None = None) -> int:
    """Run validation from CLI arguments."""
    parser = argparse.ArgumentParser(description="Validate localization XML assets")
    parser.add_argument("path", type=Path, help="File or directory to validate")
    parser.add_argument(
        "--overlay-source",
        type=Path,
        default=None,
        help="ExampleLanguage directory to compare overlays against",
    )
    args = parser.parse_args(argv)

    target = args.path
    if not target.exists():
        print(f"Error: {target} does not exist", file=sys.stderr)  # noqa: T201
        return 1

    if target.is_file():
        result = validate_file(target, args.overlay_source)
    else:
        result = validate_directory(target, args.overlay_source)

    if not result.issues:
        print("All checks passed.")  # noqa: T201
        return 0

    for issue in result.issues:
        line_str = f":{issue.line}" if issue.line else ""
        print(f"[{issue.severity.value}] {issue.file}{line_str}: ({issue.check}) {issue.message}")  # noqa: T201

    print(f"\n{result.error_count} error(s), {result.warning_count} warning(s)")  # noqa: T201
    return 1 if result.error_count > 0 else 0


if __name__ == "__main__":
    raise SystemExit(main())
