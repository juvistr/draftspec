namespace DraftSpec.Formatters.Html;

/// <summary>
/// Options for customizing HTML output.
/// </summary>
public class HtmlOptions
{
    /// <summary>
    /// URL to a CSS stylesheet. Defaults to Simple.css CDN.
    /// </summary>
    public string CssUrl { get; set; } = "https://cdnjs.cloudflare.com/ajax/libs/simpledotcss/2.3.7/simple.min.css";

    /// <summary>
    /// Page title. Defaults to "Spec Results".
    /// </summary>
    public string Title { get; set; } = "Spec Results";

    /// <summary>
    /// Additional CSS to include inline.
    /// </summary>
    public string? CustomCss { get; set; }
}
