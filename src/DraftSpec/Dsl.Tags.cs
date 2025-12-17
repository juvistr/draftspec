using DraftSpec.Internal;

namespace DraftSpec;

public static partial class Dsl
{
    private static readonly AsyncLocal<List<string>?> CurrentTagsLocal = new();

    /// <summary>
    /// Current tags in scope. Specs created within a tag() block inherit these tags.
    /// </summary>
    internal static List<string>? CurrentTags
    {
        get => CurrentTagsLocal.Value;
        set => CurrentTagsLocal.Value = value;
    }

    /// <summary>
    /// Tag specs within a block. Nested tags accumulate.
    /// </summary>
    /// <example>
    /// tag("slow", () => {
    ///     it("takes time", () => { ... }); // has tag "slow"
    /// });
    /// </example>
    public static void tag(string tagName, Action body)
    {
        var previousTags = CurrentTags;
        var newTags = previousTags is null
            ? new List<string> { tagName }
            : [.. previousTags, tagName];
        CurrentTags = newTags;
        try
        {
            body();
        }
        finally
        {
            CurrentTags = previousTags;
        }
    }

    /// <summary>
    /// Tag specs within a block with multiple tags.
    /// </summary>
    /// <example>
    /// tags(["slow", "integration"], () => {
    ///     it("connects to database", () => { ... }); // has both tags
    /// });
    /// </example>
    public static void tags(string[] tagNames, Action body)
    {
        var previousTags = CurrentTags;
        var newTags = previousTags is null
            ? tagNames.ToList()
            : [.. previousTags, .. tagNames];
        CurrentTags = newTags;
        try
        {
            body();
        }
        finally
        {
            CurrentTags = previousTags;
        }
    }
}