"""Tests for validate_assets module."""

from pathlib import Path

from scripts.validate_assets import (
    Severity,
    check_overlay_placeholders,
    check_placeholders_in_file,
    check_utf8_no_bom,
    check_xml_wellformedness,
    extract_placeholders,
    validate_file,
)


def _write(tmp_path: Path, name: str, content: str, *, encoding: str = "utf-8") -> Path:
    p = tmp_path / name
    p.write_text(content, encoding=encoding)
    return p


def _write_bytes(tmp_path: Path, name: str, data: bytes) -> Path:
    p = tmp_path / name
    p.write_bytes(data)
    return p


class TestExtractPlaceholders:
    """Tests for extract_placeholders."""

    def test_template_vars(self) -> None:
        assert "{{W|text}}" in extract_placeholders("Hello {{W|text}} world")

    def test_color_codes(self) -> None:
        result = extract_placeholders("&Ytext&R")
        assert "&R" in result
        assert "&Y" in result

    def test_replacement_tokens(self) -> None:
        assert "=subject.name=" in extract_placeholders("The =subject.name= attacks")

    def test_double_ampersand_not_counted_as_color(self) -> None:
        result = extract_placeholders("AT&&T")
        assert "&&" in result
        assert "&T" not in result

    def test_double_caret_not_counted_as_format(self) -> None:
        result = extract_placeholders("x^^y")
        assert "^^" in result
        assert "^y" not in result

    def test_empty_string(self) -> None:
        assert extract_placeholders("") == []

    def test_plain_text(self) -> None:
        assert extract_placeholders("Hello world") == []


class TestCheckUtf8NoBom:
    """Tests for check_utf8_no_bom."""

    def test_valid_utf8(self, tmp_path: Path) -> None:
        path = _write(tmp_path, "ok.xml", "<root/>")
        assert check_utf8_no_bom(path) == []

    def test_bom_detected(self, tmp_path: Path) -> None:
        path = _write_bytes(tmp_path, "bom.xml", b"\xef\xbb\xbf<root/>")
        issues = check_utf8_no_bom(path)
        assert len(issues) == 1
        assert issues[0].check == "utf8_bom"
        assert issues[0].severity == Severity.ERROR

    def test_invalid_encoding(self, tmp_path: Path) -> None:
        path = _write_bytes(tmp_path, "bad.xml", b"<root>\xff\xfe</root>")
        issues = check_utf8_no_bom(path)
        assert any(i.check == "utf8_decode" for i in issues)


class TestCheckXmlWellformedness:
    """Tests for check_xml_wellformedness."""

    def test_valid_xml(self, tmp_path: Path) -> None:
        path = _write(tmp_path, "ok.xml", "<root><child/></root>")
        assert check_xml_wellformedness(path) == []

    def test_malformed_xml(self, tmp_path: Path) -> None:
        path = _write(tmp_path, "bad.xml", "<root><unclosed")
        issues = check_xml_wellformedness(path)
        assert len(issues) == 1
        assert issues[0].severity == Severity.ERROR


class TestCheckPlaceholdersInFile:
    """Tests for check_placeholders_in_file."""

    def test_balanced_template_vars(self, tmp_path: Path) -> None:
        xml = '<strings><string ID="x" Value="{{W|ok}}"/></strings>'
        path = _write(tmp_path, "ok.xml", xml)
        assert check_placeholders_in_file(path) == []

    def test_unbalanced_template_vars(self, tmp_path: Path) -> None:
        xml = '<strings><string ID="x" Value="{{W|broken"/></strings>'
        path = _write(tmp_path, "bad.xml", xml)
        issues = check_placeholders_in_file(path)
        assert any(i.check == "placeholder_balance" for i in issues)


class TestCheckOverlayPlaceholders:
    """Tests for check_overlay_placeholders."""

    def test_matching_placeholders_pass(self, tmp_path: Path) -> None:
        source = '<strings><string Context="" ID="x" Value="&amp;Yhello =name="/></strings>'
        overlay = '<strings><string Context="" ID="x" Value="&amp;Yこんにちは =name="/></strings>'
        src_path = _write(tmp_path, "source.xml", source)
        ovl_path = _write(tmp_path, "overlay.xml", overlay)
        issues = check_overlay_placeholders(ovl_path, src_path)
        assert len(issues) == 0

    def test_missing_placeholder_reported(self, tmp_path: Path) -> None:
        source = '<strings><string Context="" ID="x" Value="{{W|hello}} =name="/></strings>'
        overlay = '<strings><string Context="" ID="x" Value="こんにちは"/></strings>'
        src_path = _write(tmp_path, "source.xml", source)
        ovl_path = _write(tmp_path, "overlay.xml", overlay)
        issues = check_overlay_placeholders(ovl_path, src_path)
        assert any(i.check == "placeholder_missing" for i in issues)


class TestValidateFile:
    """Integration tests for validate_file."""

    def test_valid_file_passes(self, tmp_path: Path) -> None:
        path = _write(tmp_path, "ok.xml", '<strings><string Context="" ID="x" Value="hello"/></strings>')
        result = validate_file(path)
        assert result.ok

    def test_multiple_issues_collected(self, tmp_path: Path) -> None:
        path = _write_bytes(tmp_path, "bad.xml", b"\xef\xbb\xbf<root><unclosed")
        result = validate_file(path)
        assert result.error_count >= 2  # BOM + parse error
