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
        # Color markup {{X|text}} is normalised to {{X|...}} for comparison
        result = extract_placeholders("Hello {{W|text}} world")
        assert "{{W|...}}" in result
        assert "{{W|text}}" not in result

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


class TestOverlayPlaceholderAdded:
    """Tests for placeholder_added detection."""

    def test_added_placeholder_reported(self, tmp_path: Path) -> None:
        source = '<strings><string Context="" ID="x" Value="hello"/></strings>'
        overlay = '<strings><string Context="" ID="x" Value="hello =extra="/></strings>'
        src_path = _write(tmp_path, "source.xml", source)
        ovl_path = _write(tmp_path, "overlay.xml", overlay)
        issues = check_overlay_placeholders(ovl_path, src_path)
        assert any(i.check == "placeholder_added" for i in issues)
        assert any(i.severity == Severity.WARNING for i in issues)


class TestOverlayRootTagMismatch:
    """Tests for root tag consistency check."""

    def test_root_tag_mismatch_reported(self, tmp_path: Path) -> None:
        source = '<strings><string Context="" ID="x" Value="hi"/></strings>'
        overlay = '<mutations><string Context="" ID="x" Value="hi"/></mutations>'
        src_path = _write(tmp_path, "source.xml", source)
        ovl_path = _write(tmp_path, "overlay.xml", overlay)
        issues = check_overlay_placeholders(ovl_path, src_path)
        assert any(i.check == "root_tag_mismatch" for i in issues)


class TestWhitespaceTextFallback:
    """Tests for whitespace-only elem.text falling back to Value attribute."""

    def test_whitespace_text_uses_value_attr(self, tmp_path: Path) -> None:
        source = '<strings><string Context="" ID="x" Value="=name=">   </string></strings>'
        overlay = '<strings><string Context="" ID="x" Value="hello">   </string></strings>'
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

    def test_missing_source_file_reported(self, tmp_path: Path) -> None:
        ovl = _write(tmp_path, "Strings.jp.xml", '<strings><string Context="" ID="x" Value="hi"/></strings>')
        source_dir = tmp_path / "sources"
        source_dir.mkdir()
        result = validate_file(ovl, source_dir=source_dir)
        assert any(i.check == "source_not_found" for i in result.issues)


class TestOverlayOnlyKeyReported:
    """Finding 2: overlay-only (Context, ID) pairs are reported as WARNING."""

    def test_overlay_only_key_reported(self, tmp_path: Path) -> None:
        source = '<strings><string Context="" ID="x" Value="hello"/></strings>'
        overlay = (
            "<strings>"
            '<string Context="" ID="x" Value="こんにちは"/>'
            '<string Context="" ID="extra" Value="余分"/>'
            "</strings>"
        )
        src_path = _write(tmp_path, "source.xml", source)
        ovl_path = _write(tmp_path, "overlay.xml", overlay)
        issues = check_overlay_placeholders(ovl_path, src_path)
        assert any(i.check == "overlay_key_not_in_source" for i in issues)
        assert any(i.severity == Severity.WARNING for i in issues)


class TestUnclosedTokenIsError:
    """Finding 3: unclosed =token detects as ERROR, not WARNING."""

    def test_unclosed_token_is_error(self, tmp_path: Path) -> None:
        xml = '<strings><string ID="x" Value="=unclosed"/></strings>'
        path = _write(tmp_path, "bad.xml", xml)
        issues = check_placeholders_in_file(path)
        token_issues = [i for i in issues if i.check == "token_unclosed"]
        assert token_issues, "expected at least one token_unclosed issue"
        assert all(i.severity == Severity.ERROR for i in token_issues)


class TestAmbiguousPositionalMatchSkipped:
    """Positional fallback returns None when sibling counts differ."""

    def test_ambiguous_positional_match_skipped(self, tmp_path: Path) -> None:
        """Source has 3 keyless <item> siblings; overlay has only 1."""
        source_xml = "<root><item Value='a'/><item Value='b'/><item Value='c'/></root>"
        overlay_xml = "<root><item Value='x'/></root>"
        src_path = _write(tmp_path, "source.xml", source_xml)
        ovl_path = _write(tmp_path, "overlay.xml", overlay_xml)
        issues = check_overlay_placeholders(ovl_path, src_path)
        assert not any(i.severity == Severity.ERROR for i in issues)


class TestTokenUnclosedFalsePositives:
    """Regression tests for token_unclosed false positives."""

    def test_double_equals_not_flagged(self, tmp_path: Path) -> None:
        """Requires='OptionFoo==Yes' should not trigger token_unclosed."""
        xml = '<options><option Requires="OptionFoo==Yes" ID="x" Value="test"/></options>'
        path = _write(tmp_path, "test.xml", xml)
        issues = check_placeholders_in_file(path)
        assert not any(i.check == "token_unclosed" for i in issues)

    def test_colon_token_recognized(self, tmp_path: Path) -> None:
        """[=commandKey:Accept=] should be a valid closed token."""
        xml = '<strings><string ID="x" Value="Press [=commandKey:Accept=]"/></strings>'
        path = _write(tmp_path, "test.xml", xml)
        issues = check_placeholders_in_file(path)
        assert not any(i.check == "token_unclosed" for i in issues)


class TestMarkupNormalization:
    """Regression tests for {{X|text}} markup normalization."""

    def test_color_markup_translated_text_passes(self, tmp_path: Path) -> None:
        """{{W|English}} vs {{W|日本語}} should pass (same color code)."""
        source = '<strings><string Context="" ID="x" Value="{{W|hello}}"/></strings>'
        overlay = '<strings><string Context="" ID="x" Value="{{W|こんにちは}}"/></strings>'
        src = _write(tmp_path, "src.xml", source)
        ovl = _write(tmp_path, "ovl.xml", overlay)
        issues = check_overlay_placeholders(ovl, src)
        assert not any(i.check == "placeholder_missing" for i in issues)

    def test_inner_token_still_checked(self, tmp_path: Path) -> None:
        """{{W|=name=}} vs {{W|こんにちは}} should report missing =name=."""
        source = '<strings><string Context="" ID="x" Value="{{W|=name=}}"/></strings>'
        overlay = '<strings><string Context="" ID="x" Value="{{W|こんにちは}}"/></strings>'
        src = _write(tmp_path, "src.xml", source)
        ovl = _write(tmp_path, "ovl.xml", overlay)
        issues = check_overlay_placeholders(ovl, src)
        assert any(i.check == "placeholder_missing" for i in issues)

    def test_bare_template_var_still_opaque(self) -> None:
        """{{foo}} without | should be treated as opaque placeholder."""
        result = extract_placeholders("hello {{foo}} world")
        assert "{{foo}}" in result

    def test_color_markup_normalized(self) -> None:
        """{{W|text}} should normalize to {{W|...}}."""
        result = extract_placeholders("{{W|Aggressive stance}}")
        assert "{{W|...}}" in result
        assert "{{W|Aggressive stance}}" not in result
