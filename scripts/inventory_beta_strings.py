"""Inventory beta ExampleLanguage schema and extract translatable entries.

Scans the game's ExampleLanguage/ directory to build a structured inventory
of all Context/ID pairs from Strings files and all translatable attributes
from [LanguageXml] document roots.

Usage:
    python scripts/inventory_beta_strings.py <example_language_dir> [--format json|csv]

The game generates these files under StreamingAssets/Base/ExampleLanguage/:
    - Strings.example.xml          (code-string _S/_T lookups)
    - Strings.appendix.example.xml (XML-defined strings promoted to string table)
    - Strings.Conversations.example.xml (conversation strings)
    - <Root>.example.xml           (per-[LanguageXml] document files)
"""

from __future__ import annotations

import argparse
import csv
import json
import sys
import xml.etree.ElementTree as ET
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import TextIO

# ---------------------------------------------------------------------------
# Schema knowledge: every [LanguageXml] root the beta engine registers
# ---------------------------------------------------------------------------

LANGUAGE_XML_ROOTS: list[dict[str, str]] = [
    {"root": "embarkmodules", "source": "EmbarkBuilderConfiguration", "description": "Character embark module UI text"},
    {"root": "templates", "source": "Templates", "description": "Game text templates (TemplateElement)"},
    {"root": "mutations", "source": "MutationFactory", "description": "Mutation names, descriptions, level text"},
    {"root": "subtypes", "source": "SubtypeFactory", "description": "Subtype/class display names"},
    {
        "root": "genotypes",
        "source": "GenotypeFactory",
        "description": "Genotype display names and chargen descriptions",
    },
    {"root": "skills", "source": "SkillFactory", "description": "Skill/power names, descriptions, snippets"},
    {"root": "naming", "source": "NameStyles", "description": "Name generation prefixes, postfixes, templates"},
    {"root": "help", "source": "XRLManual", "description": "Help topics (TextElement content)"},
    {"root": "objects", "source": "ObjectBlueprintLoader", "description": "Object blueprints with parts/tags/xtags"},
    {"root": "activatedabilities", "source": "ActivatedAbilities", "description": "Activated ability descriptions"},
    {"root": "books", "source": "BookUI", "description": "Book titles and page text"},
    {"root": "options", "source": "Options", "description": "Option display text, categories, help text"},
    {"root": "quests", "source": "QuestLoader", "description": "Quest names, steps, descriptive text"},
    {
        "root": "sparkingbaetyls",
        "source": "RandomAltarBaetylRewardManager",
        "description": "Baetyl reward descriptions",
    },
    {"root": "worlds", "source": "WorldFactory", "description": "World/zone proper names and articles"},
    {"root": "relics", "source": "RelicGenerator", "description": "Relic type name mappings"},
    {"root": "pronounsets", "source": "PronounSet", "description": "Pronoun set display forms"},
    {"root": "factions", "source": "Factions", "description": "Faction display names, water ritual text, interests"},
]

# The processor also generates these special string-table files
GENERATED_STRING_FILES: list[str] = [
    "Strings.example.xml",
    "Strings.appendix.example.xml",
    "Strings.Conversations.example.xml",
]

# Unicode triangle right (U+25B6) used as the translation marker
TRIANGLE_RIGHT = "\u25b6"


@dataclass
class StringEntry:
    """A single translatable string from a Strings file."""

    context: str
    string_id: str
    value: str
    source_file: str


@dataclass
class XmlEntry:
    """A translatable attribute or text element from an example XML file."""

    root: str
    element_path: str
    attribute: str
    value: str
    source_file: str


@dataclass
class Inventory:
    """Complete inventory of translatable content."""

    string_entries: list[StringEntry] = field(default_factory=list)
    xml_entries: list[XmlEntry] = field(default_factory=list)
    files_found: list[str] = field(default_factory=list)
    files_missing: list[str] = field(default_factory=list)
    schema_roots: list[dict[str, str]] = field(default_factory=list)


# ---------------------------------------------------------------------------
# Parsing helpers
# ---------------------------------------------------------------------------


def extract_strings_from_file(filepath: Path) -> list[StringEntry]:
    """Extract Context/ID pairs from a <strings> example XML file."""
    entries: list[StringEntry] = []
    try:
        tree = ET.parse(filepath)  # noqa: S314
    except ET.ParseError:
        return entries

    root = tree.getroot()
    if root.tag != "strings":
        return entries

    for elem in root.iter("string"):
        context = elem.get("Context", "")
        string_id = elem.get("ID", "")
        value = elem.text or elem.get("Value", "")
        value = value.strip()
        # Strip the triangle marker if present
        value = value.removeprefix(TRIANGLE_RIGHT)
        if string_id:
            entries.append(
                StringEntry(
                    context=context,
                    string_id=string_id,
                    value=value,
                    source_file=filepath.name,
                )
            )
    return entries


def _build_path(parents: list[str], tag: str) -> str:
    """Build a dot-separated XML path."""
    parts = [*parents, tag]
    return ".".join(parts)


def extract_xml_entries(filepath: Path) -> list[XmlEntry]:
    """Extract translatable attributes and text content from an example XML file.

    The ExampleLanguage files mark translatable attribute values with the
    triangle-right prefix and translatable text nodes with inner text.
    """
    entries: list[XmlEntry] = []
    try:
        tree = ET.parse(filepath)  # noqa: S314
    except ET.ParseError:
        return entries

    root_tag = tree.getroot().tag

    def _walk(elem: ET.Element, parents: list[str]) -> None:
        current_path = _build_path(parents, elem.tag)
        for attr_name, attr_value in elem.attrib.items():
            if attr_name in {"Lang", "Encoding", "Load"}:
                continue
            if attr_value.startswith(TRIANGLE_RIGHT):
                entries.append(
                    XmlEntry(
                        root=root_tag,
                        element_path=current_path,
                        attribute=attr_name,
                        value=attr_value[len(TRIANGLE_RIGHT) :],
                        source_file=filepath.name,
                    )
                )
        # Text content
        if elem.text and elem.text.strip():
            text = elem.text.strip()
            text = text.removeprefix(TRIANGLE_RIGHT)
            if text:
                entries.append(
                    XmlEntry(
                        root=root_tag,
                        element_path=current_path,
                        attribute="(text)",
                        value=text,
                        source_file=filepath.name,
                    )
                )
        for child in elem:
            _walk(child, [*parents, elem.tag])

    _walk(tree.getroot(), [])
    return entries


def build_inventory(example_dir: Path) -> Inventory:
    """Build a complete inventory from an ExampleLanguage directory."""
    inv = Inventory(schema_roots=list(LANGUAGE_XML_ROOTS))

    # 1) Strings files
    for name in GENERATED_STRING_FILES:
        path = example_dir / name
        if path.exists():
            inv.files_found.append(name)
            inv.string_entries.extend(extract_strings_from_file(path))
        else:
            inv.files_missing.append(name)

    # 2) Per-root example files
    # Per-root example files are scanned by glob below rather than by root name,
    # because the game's naming convention varies across documents.

    for path in sorted(example_dir.glob("*.example.xml")):
        if path.name in set(GENERATED_STRING_FILES):
            continue
        if path.name not in inv.files_found:
            inv.files_found.append(path.name)
        xml_entries = extract_xml_entries(path)
        inv.xml_entries.extend(xml_entries)

    return inv


# ---------------------------------------------------------------------------
# Output formatters
# ---------------------------------------------------------------------------


def inventory_to_json(inv: Inventory) -> str:
    """Serialize inventory to JSON."""
    return json.dumps(asdict(inv), ensure_ascii=False, indent=2)


def inventory_to_csv(inv: Inventory, stream: TextIO) -> None:
    """Write string entries to CSV on the given stream."""
    writer = csv.writer(stream)
    writer.writerow(["type", "source_file", "context_or_root", "id_or_path", "attribute", "value"])
    for e in inv.string_entries:
        writer.writerow(["string", e.source_file, e.context, e.string_id, "", e.value])
    for e in inv.xml_entries:
        writer.writerow(["xml", e.source_file, e.root, e.element_path, e.attribute, e.value])


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def main(argv: list[str] | None = None) -> int:
    """Run inventory from CLI arguments."""
    parser = argparse.ArgumentParser(description="Inventory beta ExampleLanguage strings and schema")
    parser.add_argument("example_dir", type=Path, help="Path to ExampleLanguage/ directory")
    parser.add_argument(
        "--format",
        choices=["json", "csv", "summary"],
        default="summary",
        help="Output format (default: summary)",
    )
    parser.add_argument("-o", "--output", type=Path, help="Output file (default: stdout)")
    args = parser.parse_args(argv)

    if not args.example_dir.is_dir():
        print(f"Error: {args.example_dir} is not a directory", file=sys.stderr)  # noqa: T201
        return 1

    inv = build_inventory(args.example_dir)

    if args.format == "json":
        text = inventory_to_json(inv)
        if args.output:
            args.output.write_text(text, encoding="utf-8")
        else:
            print(text)  # noqa: T201
    elif args.format == "csv":
        if args.output:
            with args.output.open("w", newline="", encoding="utf-8") as f:
                inventory_to_csv(inv, f)
        else:
            inventory_to_csv(inv, sys.stdout)
    else:
        # Summary
        print(f"ExampleLanguage directory: {args.example_dir}")  # noqa: T201
        print(f"Files found: {len(inv.files_found)}")  # noqa: T201
        for f in inv.files_found:
            print(f"  - {f}")  # noqa: T201
        if inv.files_missing:
            print(f"Files missing: {len(inv.files_missing)}")  # noqa: T201
            for f in inv.files_missing:
                print(f"  - {f}")  # noqa: T201
        print(f"String entries: {len(inv.string_entries)}")  # noqa: T201
        print(f"XML entries: {len(inv.xml_entries)}")  # noqa: T201
        print(f"\n[LanguageXml] document roots ({len(LANGUAGE_XML_ROOTS)}):")  # noqa: T201
        for r in LANGUAGE_XML_ROOTS:
            print(f"  {r['root']:25s} ({r['source']}) - {r['description']}")  # noqa: T201

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
