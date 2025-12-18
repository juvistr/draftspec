using System.Globalization;
using System.Text;
using System.Xml;

namespace DraftSpec.Formatters.JUnit;

/// <summary>
/// Formats spec reports as JUnit XML for CI/CD integration.
/// Compatible with Jenkins, Azure DevOps, GitHub Actions, and other CI systems.
/// </summary>
/// <remarks>
/// Generates XML conforming to the JUnit XML format:
/// - Root element: &lt;testsuites&gt; containing overall summary
/// - Each context becomes a &lt;testsuite&gt;
/// - Each spec becomes a &lt;testcase&gt;
/// - Failed specs include &lt;failure&gt; element with error message
/// - Skipped/pending specs include &lt;skipped&gt; element
/// </remarks>
public class JUnitFormatter : IFormatter
{
    /// <summary>
    /// The file extension for JUnit XML output.
    /// </summary>
    public string FileExtension => ".xml";

    /// <summary>
    /// Formats the spec report as JUnit XML.
    /// </summary>
    /// <param name="report">The spec report to format.</param>
    /// <returns>A JUnit XML-formatted string.</returns>
    public string Format(SpecReport report)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new StringWriter();
        using (var writer = XmlWriter.Create(stringWriter, settings))
        {
            writer.WriteStartDocument();

            // Root testsuites element with overall summary
            writer.WriteStartElement("testsuites");
            writer.WriteAttributeString("tests", report.Summary.Total.ToString());
            writer.WriteAttributeString("failures", report.Summary.Failed.ToString());
            writer.WriteAttributeString("errors", "0");
            writer.WriteAttributeString("skipped", (report.Summary.Skipped + report.Summary.Pending).ToString());
            writer.WriteAttributeString("time", FormatDuration(report.Summary.DurationMs));

            // Write each context as a testsuite
            foreach (var context in report.Contexts)
            {
                WriteTestSuite(writer, context, "");
            }

            writer.WriteEndElement(); // testsuites
            writer.WriteEndDocument();
        }

        return stringWriter.ToString();
    }

    private void WriteTestSuite(XmlWriter writer, SpecContextReport context, string parentPath)
    {
        var suiteName = string.IsNullOrEmpty(parentPath)
            ? context.Description
            : $"{parentPath}.{context.Description}";

        var (tests, failures, skipped, duration) = CalculateSuiteStats(context);

        writer.WriteStartElement("testsuite");
        writer.WriteAttributeString("name", suiteName);
        writer.WriteAttributeString("tests", tests.ToString());
        writer.WriteAttributeString("failures", failures.ToString());
        writer.WriteAttributeString("errors", "0");
        writer.WriteAttributeString("skipped", skipped.ToString());
        writer.WriteAttributeString("time", FormatDuration(duration));

        // Write specs as testcases
        foreach (var spec in context.Specs)
        {
            WriteTestCase(writer, spec, suiteName);
        }

        writer.WriteEndElement(); // testsuite

        // Write nested contexts as separate testsuites
        foreach (var child in context.Contexts)
        {
            WriteTestSuite(writer, child, suiteName);
        }
    }

    private void WriteTestCase(XmlWriter writer, SpecResultReport spec, string className)
    {
        writer.WriteStartElement("testcase");
        writer.WriteAttributeString("classname", className);
        writer.WriteAttributeString("name", spec.Description);
        writer.WriteAttributeString("time", FormatDuration(spec.DurationMs ?? 0));

        if (spec.Failed)
        {
            writer.WriteStartElement("failure");
            writer.WriteAttributeString("message", GetFirstLine(spec.Error) ?? "Assertion failed");
            writer.WriteAttributeString("type", "AssertionError");

            if (!string.IsNullOrEmpty(spec.Error))
            {
                writer.WriteString(spec.Error);
            }

            writer.WriteEndElement(); // failure
        }
        else if (spec.Skipped || spec.Pending)
        {
            writer.WriteStartElement("skipped");
            if (spec.Pending)
            {
                writer.WriteAttributeString("message", "Pending spec - not yet implemented");
            }
            writer.WriteEndElement(); // skipped
        }

        writer.WriteEndElement(); // testcase
    }

    private static (int tests, int failures, int skipped, double duration) CalculateSuiteStats(SpecContextReport context)
    {
        var tests = context.Specs.Count;
        var failures = context.Specs.Count(s => s.Failed);
        var skipped = context.Specs.Count(s => s.Skipped || s.Pending);
        var duration = context.Specs.Sum(s => s.DurationMs ?? 0);

        return (tests, failures, skipped, duration);
    }

    private static string FormatDuration(double milliseconds)
    {
        var seconds = milliseconds / 1000.0;
        return seconds.ToString("F3", CultureInfo.InvariantCulture);
    }

    private static string? GetFirstLine(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var newlineIndex = text.IndexOf('\n');
        return newlineIndex >= 0 ? text[..newlineIndex].TrimEnd('\r') : text;
    }
}
