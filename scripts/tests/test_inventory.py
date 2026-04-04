"""Tests for inventory_beta_strings module."""

import io
import json
from pathlib import Path

from scripts.inventory_beta_strings import (
    Inventory,
    build_inventory,
    extract_strings_from_file,
    extract_xml_entries,
    inventory_to_csv,
    inventory_to_json,
)


def _write_xml(tmp_path: Path, name: str, content: str) -> Path:
    p = tmp_path / name
    p.write_text(content, encoding="utf-8")
    return p


class TestExtractStrings:
    """Tests for extract_strings_from_file."""

    def test_extracts_context_id_value(self, tmp_path: Path) -> None:
        xml = '<strings><string Context="UI" ID="Ok" Value="OK"/></strings>'
        path = _write_xml(tmp_path, "Strings.example.xml", xml)
        entries = extract_strings_from_file(path)
        assert len(entries) == 1
        assert entries[0].context == "UI"
        assert entries[0].string_id == "Ok"
        assert entries[0].value == "OK"

    def test_strips_triangle_prefix(self, tmp_path: Path) -> None:
        xml = '<strings><string Context="" ID="greeting" Value="\u25b6Hello"/></strings>'
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_strings_from_file(path)
        assert entries[0].value == "Hello"

    def test_empty_id_skipped(self, tmp_path: Path) -> None:
        xml = '<strings><string Context="X" ID="" Value="skip"/></strings>'
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_strings_from_file(path)
        assert len(entries) == 0

    def test_non_strings_root_returns_empty(self, tmp_path: Path) -> None:
        xml = "<mutations><mutation Name='test'/></mutations>"
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_strings_from_file(path)
        assert len(entries) == 0

    def test_malformed_xml_returns_empty(self, tmp_path: Path) -> None:
        path = _write_xml(tmp_path, "bad.xml", "<strings><unclosed")
        entries = extract_strings_from_file(path)
        assert len(entries) == 0


class TestExtractXmlEntries:
    """Tests for extract_xml_entries."""

    def test_extracts_triangle_marked_attributes(self, tmp_path: Path) -> None:
        xml = '<mutations><mutation Name="\u25b6Fire" Description="\u25b6Burns things"/></mutations>'
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_xml_entries(path)
        assert len(entries) == 2
        assert entries[0].attribute == "Name"
        assert entries[0].value == "Fire"

    def test_ignores_non_marked_attributes(self, tmp_path: Path) -> None:
        xml = '<mutations><mutation Name="Fire" Load="always"/></mutations>'
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_xml_entries(path)
        assert len(entries) == 0

    def test_extracts_text_content(self, tmp_path: Path) -> None:
        xml = "<help><topic>\u25b6Help text here</topic></help>"
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_xml_entries(path)
        assert len(entries) == 1
        assert entries[0].attribute == "(text)"
        assert entries[0].value == "Help text here"


class TestElementPathUniqueness:
    """Regression tests: element_path must be unique across parents and siblings."""

    def test_different_parent_keys_produce_distinct_paths(self, tmp_path: Path) -> None:
        xml = (
            "<quests>"
            '  <quest ID="A"><step Name="\u25b6Step A"/></quest>'
            '  <quest ID="B"><step Name="\u25b6Step B"/></quest>'
            "</quests>"
        )
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_xml_entries(path)
        paths = [e.element_path for e in entries]
        assert len(paths) == len(set(paths)), f"Duplicate paths: {paths}"
        # Parent qualifier must propagate
        assert any("quest[ID=A]" in p for p in paths)
        assert any("quest[ID=B]" in p for p in paths)

    def test_keyless_siblings_get_positional_index(self, tmp_path: Path) -> None:
        xml = '<items>  <item><name Name="\u25b6Sword"/></item>  <item><name Name="\u25b6Shield"/></item></items>'
        path = _write_xml(tmp_path, "test.xml", xml)
        entries = extract_xml_entries(path)
        paths = [e.element_path for e in entries]
        assert len(paths) == len(set(paths)), f"Duplicate paths: {paths}"
        # Items should be disambiguated by index
        assert any("item[0]" in p for p in paths)
        assert any("item[1]" in p for p in paths)


class TestBuildInventory:
    """Tests for build_inventory."""

    def test_finds_strings_files(self, tmp_path: Path) -> None:
        _write_xml(tmp_path, "Strings.example.xml", '<strings><string Context="" ID="hi" Value="Hi"/></strings>')
        inv = build_inventory(tmp_path)
        assert "Strings.example.xml" in inv.files_found
        assert len(inv.string_entries) == 1

    def test_reports_missing_files(self, tmp_path: Path) -> None:
        inv = build_inventory(tmp_path)
        assert "Strings.example.xml" in inv.files_missing

    def test_scans_non_strings_example_files(self, tmp_path: Path) -> None:
        _write_xml(
            tmp_path,
            "Mutations.example.xml",
            '<mutations><mutation Name="\u25b6Fire"/></mutations>',
        )
        inv = build_inventory(tmp_path)
        assert "Mutations.example.xml" in inv.files_found
        assert len(inv.xml_entries) == 1


class TestOutputFormats:
    """Tests for serialization."""

    def test_json_output_is_valid(self, tmp_path: Path) -> None:
        _write_xml(tmp_path, "Strings.example.xml", '<strings><string Context="" ID="x" Value="y"/></strings>')
        inv = build_inventory(tmp_path)

        data = json.loads(inventory_to_json(inv))
        assert "string_entries" in data
        assert "schema_roots" in data

    def test_empty_inventory_serializes(self) -> None:
        inv = Inventory()
        text = inventory_to_json(inv)
        assert '"string_entries": []' in text

    def test_csv_output_has_expected_columns(self, tmp_path: Path) -> None:
        _write_xml(tmp_path, "Strings.example.xml", '<strings><string Context="C" ID="x" Value="y"/></strings>')
        inv = build_inventory(tmp_path)
        buf = io.StringIO()
        inventory_to_csv(inv, buf)
        buf.seek(0)
        lines = buf.readlines()
        assert len(lines) >= 2  # header + at least one entry
        assert "type" in lines[0]
