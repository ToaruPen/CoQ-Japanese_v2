using System;
using System.Runtime.Serialization;

namespace QudJP.Corpus;

/// <summary>
/// DataContract POCO for the Japanese corpus JSON format.
/// Used by <c>DataContractJsonSerializer</c> (net48-compatible).
/// </summary>
/// <remarks>
/// Expected JSON structure:
/// <code>
/// {
///   "order": 2,
///   "sentences": [
///     "形態素1 形態素2 形態素3 .",
///     "形態素4 形態素5 ."
///   ]
/// }
/// </code>
/// Each sentence is morpheme-segmented (space-delimited) and ends with " ." (space + ASCII period).
/// </remarks>
[DataContract]
internal sealed class JapaneseCorpusDocument
{
    [DataMember(Name = "order")]
    public int Order { get; set; }

    [DataMember(Name = "sentences")]
    public string[] Sentences { get; set; } = Array.Empty<string>();
}
