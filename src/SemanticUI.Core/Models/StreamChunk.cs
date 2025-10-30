namespace SemanticUI.Core.Models;

public class StreamChunk
{
    public ChunkType Type { get; set; }
    public string? Content { get; set; }
    public UIDefinition? UIDefinition { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum ChunkType
{
    Text,
    UIComponent,
    Metadata,
    Error
}
