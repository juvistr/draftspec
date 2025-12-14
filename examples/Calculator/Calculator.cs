namespace Calculator;

public class StringCalculator
{
    public int Add(string numbers)
    {
        if (string.IsNullOrEmpty(numbers))
            return 0;

        var delimiters = new List<char> { ',', '\n' };

        // Check for custom delimiter: //[delimiter]\n[numbers]
        if (numbers.StartsWith("//"))
        {
            var delimiterEnd = numbers.IndexOf('\n');
            var customDelimiter = numbers[2..delimiterEnd];
            delimiters.Add(customDelimiter[0]);
            numbers = numbers[(delimiterEnd + 1)..];
        }

        var parts = numbers.Split(delimiters.ToArray(), StringSplitOptions.RemoveEmptyEntries);
        var values = parts.Select(int.Parse).ToList();

        var negatives = values.Where(v => v < 0).ToList();
        if (negatives.Count > 0)
            throw new ArgumentException($"Negatives not allowed: {string.Join(", ", negatives)}");

        return values.Sum();
    }
}
