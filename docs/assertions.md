# Assertions Reference

Complete API reference for DraftSpec's `expect()` assertion system.

## Overview

DraftSpec uses Jest-style assertions with automatic expression capture for clear error messages:

```csharp
expect(calculator.Add("1,2")).toBe(3);
// If this fails, you see:
// Expected calculator.Add("1,2") to be 3, but was 5
```

The expression `calculator.Add("1,2")` is captured automatically using `[CallerArgumentExpression]`.

---

## Creating Expectations

### expect(value)

Creates an expectation for a value. The return type depends on the input:

| Input Type | Returns | Available Assertions |
|------------|---------|---------------------|
| `bool` | `BoolExpectation` | `toBeTrue()`, `toBeFalse()` |
| `string` | `StringExpectation` | `toContain()`, `toStartWith()`, `toEndWith()` |
| `Action` | `ActionExpectation` | `toThrow<T>()`, `toNotThrow()` |
| `T[]`, `List<T>`, `IList<T>` | `CollectionExpectation<T>` | `toContain()`, `toHaveCount()`, `toBeEmpty()` |
| Any other `T` | `Expectation<T>` | `toBe()`, `toBeNull()`, `toBeGreaterThan()` |

```csharp
expect(42)                  // Expectation<int>
expect(true)                // BoolExpectation
expect("hello")             // StringExpectation
expect(() => DoSomething()) // ActionExpectation
expect(new[] { 1, 2, 3 })   // CollectionExpectation<int>
```

---

## General Assertions

Available on `Expectation<T>` for any type:

### toBe(expected)

Assert exact equality using `Equals()`.

```csharp
expect(2 + 2).toBe(4);
expect(user.Name).toBe("Alice");
```

### toBeNull()

Assert the value is null.

```csharp
expect(result.Error).toBeNull();
```

### toNotBeNull()

Assert the value is not null.

```csharp
expect(user).toNotBeNull();
```

---

## Comparison Assertions

For types implementing `IComparable<T>`:

### toBeGreaterThan(expected)

Assert the value is greater than expected.

```csharp
expect(items.Count).toBeGreaterThan(0);
```

### toBeLessThan(expected)

Assert the value is less than expected.

```csharp
expect(errorCount).toBeLessThan(10);
```

### toBeAtLeast(expected)

Assert the value is greater than or equal to expected.

```csharp
expect(score).toBeAtLeast(70);  // >= 70
```

### toBeAtMost(expected)

Assert the value is less than or equal to expected.

```csharp
expect(retryCount).toBeAtMost(3);  // <= 3
```

### toBeInRange(min, max)

Assert the value is within the range (inclusive).

```csharp
expect(percentage).toBeInRange(0, 100);
```

### toBeCloseTo(expected, tolerance)

Assert the value is within tolerance of expected. Works with numeric types.

```csharp
expect(calculatedPrice).toBeCloseTo(19.99, 0.01);
expect(pi).toBeCloseTo(3.14159, 0.00001);
```

---

## Boolean Assertions

Available on `BoolExpectation`:

### toBeTrue()

Assert the value is true.

```csharp
expect(user.IsActive).toBeTrue();
expect(list.Any()).toBeTrue();
```

### toBeFalse()

Assert the value is false.

```csharp
expect(errors.Any()).toBeFalse();
expect(isDeleted).toBeFalse();
```

### toBe(expected)

Assert equality (for consistency).

```csharp
expect(flag).toBe(true);
```

---

## String Assertions

Available on `StringExpectation`:

### toBe(expected)

Assert exact string equality.

```csharp
expect(greeting).toBe("Hello, World!");
```

### toContain(substring)

Assert the string contains a substring.

```csharp
expect(email).toContain("@");
expect(errorMessage).toContain("not found");
```

### toStartWith(prefix)

Assert the string starts with a prefix.

```csharp
expect(url).toStartWith("https://");
expect(className).toStartWith("Test");
```

### toEndWith(suffix)

Assert the string ends with a suffix.

```csharp
expect(filename).toEndWith(".txt");
expect(response).toEndWith("OK");
```

### toBeNullOrEmpty()

Assert the string is null or empty.

```csharp
expect(optionalField).toBeNullOrEmpty();
```

### toBeNull()

Assert the string is null.

```csharp
expect(deletedUser.Email).toBeNull();
```

---

## Collection Assertions

Available on `CollectionExpectation<T>`:

### toContain(item)

Assert the collection contains an item.

```csharp
expect(roles).toContain("admin");
expect(numbers).toContain(42);
```

### toNotContain(item)

Assert the collection does not contain an item.

```csharp
expect(activeUsers).toNotContain(deletedUser);
```

### toContainAll(items...)

Assert the collection contains all specified items.

```csharp
expect(permissions).toContainAll("read", "write", "execute");
```

### toHaveCount(count)

Assert the collection has exactly the specified count.

```csharp
expect(results).toHaveCount(3);
expect(emptyList).toHaveCount(0);
```

### toBeEmpty()

Assert the collection is empty.

```csharp
expect(errors).toBeEmpty();
```

### toNotBeEmpty()

Assert the collection is not empty.

```csharp
expect(users).toNotBeEmpty();
```

### toBe(expected)

Assert the collection equals the expected sequence (order matters).

```csharp
expect(sorted).toBe(1, 2, 3);
expect(names).toBe(new[] { "Alice", "Bob", "Charlie" });
```

---

## Exception Assertions

Available on `ActionExpectation`:

### toThrow\<TException\>()

Assert the action throws a specific exception type. Returns the exception for further inspection.

```csharp
expect(() => Divide(1, 0)).toThrow<DivideByZeroException>();

// Inspect the exception
var ex = expect(() => ParseInt("abc")).toThrow<FormatException>();
expect(ex.Message).toContain("Input string");
```

### toThrow()

Assert the action throws any exception. Returns the exception.

```csharp
var ex = expect(() => riskyOperation()).toThrow();
expect(ex.Message).toContain("failed");
```

### toNotThrow()

Assert the action completes without throwing.

```csharp
expect(() => SafeOperation()).toNotThrow();
expect(() => Validate(validInput)).toNotThrow();
```

---

## Custom Matchers

Create custom assertions via extension methods. Each expectation type exposes `Actual` and `Expression` for building matchers:

```csharp
public static class DateExpectationExtensions
{
    public static void toBeAfter(this Expectation<DateTime> exp, DateTime other)
    {
        if (exp.Actual <= other)
            throw new AssertionException(
                $"Expected {exp.Expression} to be after {other}, but was {exp.Actual}");
    }

    public static void toBeBefore(this Expectation<DateTime> exp, DateTime other)
    {
        if (exp.Actual >= other)
            throw new AssertionException(
                $"Expected {exp.Expression} to be before {other}, but was {exp.Actual}");
    }

    public static void toBeToday(this Expectation<DateTime> exp)
    {
        if (exp.Actual.Date != DateTime.Today)
            throw new AssertionException(
                $"Expected {exp.Expression} to be today ({DateTime.Today:d}), but was {exp.Actual:d}");
    }
}
```

Usage:
```csharp
expect(order.CreatedAt).toBeAfter(DateTime.Now.AddDays(-7));
expect(subscription.ExpiresAt).toBeBefore(DateTime.Now.AddYears(1));
expect(lastLogin).toBeToday();
```

### Available Properties

| Expectation Type | Properties |
|-----------------|------------|
| `Expectation<T>` | `Actual` (T), `Expression` (string?) |
| `BoolExpectation` | `Actual` (bool), `Expression` (string?) |
| `StringExpectation` | `Actual` (string?), `Expression` (string?) |
| `ActionExpectation` | `Action` (Action), `Expression` (string?) |
| `CollectionExpectation<T>` | `Actual` (IEnumerable\<T\>), `Expression` (string?) |

---

## Error Messages

DraftSpec provides clear, informative error messages:

```
Expected calculator.Add("1,2") to be 5, but was 3

Expected user.Email to contain "@", but was "invalid"

Expected items to have count 3, but was 5

Expected () => Divide(1, 0) to throw ArgumentException, but threw DivideByZeroException: Attempted to divide by zero.

Expected roles to contain "admin", but it did not. Contents: ["user", "guest"]
```

---

## See Also

- **[DSL Reference](dsl-reference.md)** - describe/it/hooks API
- **[Configuration](configuration.md)** - Custom matchers via plugins
