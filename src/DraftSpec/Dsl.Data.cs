namespace DraftSpec;

public static partial class Dsl
{
    /// <summary>
    /// Generate specs from a collection of data items.
    /// Each item in the collection is passed to the factory function to create specs.
    /// </summary>
    /// <typeparam name="T">The type of data items</typeparam>
    /// <param name="data">Collection of test data</param>
    /// <param name="specFactory">Function that receives each data item and defines specs</param>
    /// <example>
    /// <code>
    /// withData([
    ///     new { input = "hello", expected = 5 },
    ///     new { input = "world", expected = 5 },
    ///     new { input = "", expected = 0 }
    /// ], data => {
    ///     it($"returns {data.expected} for '{data.input}'", () => {
    ///         expect(data.input.Length).toBe(data.expected);
    ///     });
    /// });
    /// </code>
    /// </example>
    public static void withData<T>(IEnumerable<T> data, Action<T> specFactory)
    {
        foreach (var item in data)
            specFactory(item);
    }

    /// <summary>
    /// Generate specs from a collection of 2-tuples.
    /// </summary>
    /// <example>
    /// <code>
    /// withData([
    ///     ("hello", 5),
    ///     ("world", 5),
    ///     ("", 0)
    /// ], (input, expected) => {
    ///     it($"returns {expected} for '{input}'", () => {
    ///         expect(input.Length).toBe(expected);
    ///     });
    /// });
    /// </code>
    /// </example>
    public static void withData<T1, T2>(
        IEnumerable<(T1, T2)> data,
        Action<T1, T2> specFactory)
    {
        foreach (var (item1, item2) in data)
            specFactory(item1, item2);
    }

    /// <summary>
    /// Generate specs from a collection of 3-tuples.
    /// </summary>
    /// <example>
    /// <code>
    /// withData([
    ///     (1, 1, 2),
    ///     (2, 3, 5),
    ///     (-1, 1, 0)
    /// ], (a, b, expected) => {
    ///     it($"adds {a} + {b} = {expected}", () => {
    ///         expect(a + b).toBe(expected);
    ///     });
    /// });
    /// </code>
    /// </example>
    public static void withData<T1, T2, T3>(
        IEnumerable<(T1, T2, T3)> data,
        Action<T1, T2, T3> specFactory)
    {
        foreach (var (item1, item2, item3) in data)
            specFactory(item1, item2, item3);
    }

    /// <summary>
    /// Generate specs from a collection of 4-tuples.
    /// </summary>
    public static void withData<T1, T2, T3, T4>(
        IEnumerable<(T1, T2, T3, T4)> data,
        Action<T1, T2, T3, T4> specFactory)
    {
        foreach (var (item1, item2, item3, item4) in data)
            specFactory(item1, item2, item3, item4);
    }

    /// <summary>
    /// Generate specs from a collection of 5-tuples.
    /// </summary>
    public static void withData<T1, T2, T3, T4, T5>(
        IEnumerable<(T1, T2, T3, T4, T5)> data,
        Action<T1, T2, T3, T4, T5> specFactory)
    {
        foreach (var (item1, item2, item3, item4, item5) in data)
            specFactory(item1, item2, item3, item4, item5);
    }

    /// <summary>
    /// Generate specs from a collection of 6-tuples.
    /// </summary>
    public static void withData<T1, T2, T3, T4, T5, T6>(
        IEnumerable<(T1, T2, T3, T4, T5, T6)> data,
        Action<T1, T2, T3, T4, T5, T6> specFactory)
    {
        foreach (var (item1, item2, item3, item4, item5, item6) in data)
            specFactory(item1, item2, item3, item4, item5, item6);
    }

    /// <summary>
    /// Generate specs from a dictionary where keys are used as test case names.
    /// </summary>
    /// <typeparam name="T">The type of data values</typeparam>
    /// <param name="data">Dictionary mapping test case names to data</param>
    /// <param name="specFactory">Function that receives the name and data to define specs</param>
    /// <example>
    /// <code>
    /// withData(new Dictionary&lt;string, (int, int, int)&gt; {
    ///     ["positive numbers"] = (1, 2, 3),
    ///     ["with zero"] = (0, 5, 5),
    ///     ["negative result"] = (1, -5, -4)
    /// }, (name, data) => {
    ///     it(name, () => {
    ///         expect(data.Item1 + data.Item2).toBe(data.Item3);
    ///     });
    /// });
    /// </code>
    /// </example>
    public static void withData<T>(
        IReadOnlyDictionary<string, T> data,
        Action<string, T> specFactory)
    {
        foreach (var (name, value) in data)
            specFactory(name, value);
    }
}
