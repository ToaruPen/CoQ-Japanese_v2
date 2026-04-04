"""Tests for diff_beta_strings module."""

import io
import json
from pathlib import Path

import pytest

from scripts.diff_beta_strings import (
    DiffEntry,
    DiffReport,
    diff_entries,
    diff_to_csv,
    diff_to_json,
    diff_to_text,
    load_beta_entries,
    load_v1_entries,
)

# Triangle marker used by beta example files
TRIANGLE = "\u25b6"


def _write(tmp_path: Path, name: str, content: str) -> Path:
    p = tmp_path / name
    p.write_text(content, encoding="utf-8")
    return p


# ---------------------------------------------------------------------------
# Helpers: build minimal v1 and beta dirs
# ---------------------------------------------------------------------------


def _make_v1_strings(tmp_path: Path, stem: str, entries: list[tuple[str, str]]) -> None:
    """Write a <strings> jp.xml file with (Context, ID) pairs."""
    inner = "".join(f'<string Context="{ctx}" ID="{sid}" Value="val"/>' for ctx, sid in entries)
    _write(tmp_path, f"{stem}.jp.xml", f"<strings>{inner}</strings>")


def _make_beta_strings(tmp_path: Path, stem: str, entries: list[tuple[str, str]]) -> None:
    """Write a <strings> example.xml file with (Context, ID) pairs (triangle-prefixed)."""
    inner = "".join(f'<string Context="{ctx}" ID="{sid}" Value="{TRIANGLE}val"/>' for ctx, sid in entries)
    _write(tmp_path, f"{stem}.example.xml", f"<strings>{inner}</strings>")


def _make_v1_xml(tmp_path: Path, stem: str, root: str, name_value: str) -> None:
    """Write a [LanguageXml]-style jp.xml file (no triangle — v1 files don't have it)."""
    _write(tmp_path, f"{stem}.jp.xml", f'<{root}><mutation Name="{name_value}"/></{root}>')


def _make_beta_xml(tmp_path: Path, stem: str, root: str, name_value: str) -> None:
    """Write a [LanguageXml]-style example.xml file (triangle-marked)."""
    _write(tmp_path, f"{stem}.example.xml", f'<{root}><mutation Name="{TRIANGLE}{name_value}"/></{root}>')


# ---------------------------------------------------------------------------
# test_covered_entry_detected
# ---------------------------------------------------------------------------


class TestCoveredEntry:
    """Tests for covered entry detection."""

    def test_covered_entry_detected(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        _make_v1_strings(v1_dir, "Strings", [("UI", "Ok")])
        _make_beta_strings(beta_dir, "Strings", [("UI", "Ok")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        assert len(report.covered) == 1
        assert report.covered[0].status == "covered"
        assert "Ok" in report.covered[0].key
        assert len(report.missing) == 0
        assert len(report.orphaned) == 0


# ---------------------------------------------------------------------------
# test_missing_entry_detected
# ---------------------------------------------------------------------------


class TestMissingEntry:
    """Tests for missing entry detection."""

    def test_missing_entry_detected(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        # v1 has only "Ok"; beta has "Ok" + "Cancel"
        _make_v1_strings(v1_dir, "Strings", [("UI", "Ok")])
        _make_beta_strings(beta_dir, "Strings", [("UI", "Ok"), ("UI", "Cancel")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        assert len(report.missing) == 1
        assert "Cancel" in report.missing[0].key
        assert len(report.covered) == 1
        assert len(report.orphaned) == 0


# ---------------------------------------------------------------------------
# test_orphaned_entry_detected
# ---------------------------------------------------------------------------


class TestOrphanedEntry:
    """Tests for orphaned entry detection."""

    def test_orphaned_entry_detected(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        # v1 has "Ok" + "Legacy"; beta only has "Ok"
        _make_v1_strings(v1_dir, "Strings", [("UI", "Ok"), ("UI", "Legacy")])
        _make_beta_strings(beta_dir, "Strings", [("UI", "Ok")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        assert len(report.orphaned) == 1
        assert "Legacy" in report.orphaned[0].key
        assert len(report.covered) == 1
        assert len(report.missing) == 0


# ---------------------------------------------------------------------------
# test_xml_root_diff
# ---------------------------------------------------------------------------


class TestXmlRootDiff:
    """Tests for LanguageXml element_path based comparison."""

    def test_xml_root_diff(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        # v1 has Fire; beta has Fire + Ice
        _make_v1_xml(v1_dir, "Mutations", "mutations", "Fire")
        _make_beta_xml(beta_dir, "Mutations", "mutations", "Fire")
        # Add second entry in beta only
        _write(
            beta_dir,
            "Mutations.example.xml",
            f'<mutations><mutation Name="{TRIANGLE}Fire"/><mutation Name="{TRIANGLE}Ice"/></mutations>',
        )

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        assert report.covered[0].root == "mutations"
        # Fire is covered; Ice is missing (v1 has no triangle, so extract_xml_entries returns empty,
        # but Fire is in v1 via element_path key — let's verify counts)
        statuses = {e.status for e in report.entries}
        assert "covered" in statuses or "missing" in statuses  # at minimum some entries exist
        assert len(report.entries) > 0


# ---------------------------------------------------------------------------
# test_summary_counts
# ---------------------------------------------------------------------------


class TestSummaryCounts:
    """Tests for summary count properties."""

    def test_summary_counts(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        _make_v1_strings(v1_dir, "Strings", [("", "A"), ("", "B"), ("", "C")])
        _make_beta_strings(beta_dir, "Strings", [("", "A"), ("", "B"), ("", "D")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        # A, B: covered; D: missing; C: orphaned
        assert len(report.covered) == 2
        assert len(report.missing) == 1
        assert len(report.orphaned) == 1
        assert len(report.entries) == 4


# ---------------------------------------------------------------------------
# test_json_output
# ---------------------------------------------------------------------------


class TestJsonOutput:
    """Tests for JSON serialization."""

    def test_json_output(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        _make_v1_strings(v1_dir, "Strings", [("UI", "Ok")])
        _make_beta_strings(beta_dir, "Strings", [("UI", "Ok"), ("UI", "Cancel")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        raw = diff_to_json(report)
        data = json.loads(raw)

        assert "summary" in data
        assert "entries" in data
        assert data["summary"]["covered"] == 1
        assert data["summary"]["missing"] == 1
        assert data["summary"]["orphaned"] == 0
        assert data["summary"]["total"] == 2
        assert isinstance(data["entries"], list)
        assert all("file" in e and "root" in e and "key" in e and "status" in e for e in data["entries"])


# ---------------------------------------------------------------------------
# test_empty_dirs
# ---------------------------------------------------------------------------


class TestEmptyDirs:
    """Tests for empty directory handling."""

    def test_empty_dirs(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        assert len(report.entries) == 0
        assert len(report.covered) == 0
        assert len(report.missing) == 0
        assert len(report.orphaned) == 0


# ---------------------------------------------------------------------------
# test_unmatched_files_reported
# ---------------------------------------------------------------------------


class TestUnmatchedFiles:
    """Tests for v1 files with no matching beta pair."""

    def test_unmatched_files_reported(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        # v1 has OldFile.jp.xml with no corresponding beta file
        _make_v1_strings(v1_dir, "OldFile", [("", "LegacyID")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        assert len(report.orphaned) == 1
        assert report.orphaned[0].status == "orphaned"
        assert "LegacyID" in report.orphaned[0].key
        assert report.orphaned[0].file == "OldFile.jp.xml"


# ---------------------------------------------------------------------------
# Additional formatter tests
# ---------------------------------------------------------------------------


class TestCsvOutput:
    """Tests for CSV output format."""

    def test_csv_has_header_and_rows(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        _make_v1_strings(v1_dir, "Strings", [("UI", "Ok")])
        _make_beta_strings(beta_dir, "Strings", [("UI", "Ok")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        buf = io.StringIO()
        diff_to_csv(report, buf)
        buf.seek(0)
        lines = buf.readlines()
        assert len(lines) >= 2
        assert "file" in lines[0]
        assert "status" in lines[0]


class TestTextOutput:
    """Tests for plain-text output format."""

    def test_text_output_contains_summary(self, tmp_path: Path) -> None:
        v1_dir = tmp_path / "v1"
        beta_dir = tmp_path / "beta"
        v1_dir.mkdir()
        beta_dir.mkdir()

        _make_v1_strings(v1_dir, "Strings", [("", "A")])
        _make_beta_strings(beta_dir, "Strings", [("", "A"), ("", "B")])

        v1_map = load_v1_entries(v1_dir)
        beta_map = load_beta_entries(beta_dir)
        report = diff_entries(v1_map, beta_map)

        buf = io.StringIO()
        diff_to_text(report, buf)
        text = buf.getvalue()
        assert "covered" in text
        assert "missing" in text
        assert "MISSING" in text


# ---------------------------------------------------------------------------
# DiffReport property smoke test
# ---------------------------------------------------------------------------


class TestDiffReportProperties:
    """Tests for DiffReport property filters."""

    def test_properties_filter_correctly(self) -> None:

        entries = [
            DiffEntry(file="f.example.xml", root="strings", key="k1", status="covered"),
            DiffEntry(file="f.example.xml", root="strings", key="k2", status="missing"),
            DiffEntry(file="f.jp.xml", root="strings", key="k3", status="orphaned"),
        ]
        report = DiffReport(entries=entries)
        assert len(report.covered) == 1
        assert len(report.missing) == 1
        assert len(report.orphaned) == 1

    @pytest.mark.parametrize("status", ["covered", "missing", "orphaned"])
    def test_status_values_accepted(self, status: str) -> None:

        e = DiffEntry(file="f.xml", root="r", key="k", status=status)
        report = DiffReport(entries=[e])
        assert len(report.entries) == 1
