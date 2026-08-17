namespace AdminFlow.Budget.Domain.CostCenters;

public sealed class CostCenter
{
    public CostCenter(string code, string name)
    {
        Id = Guid.NewGuid();
        Code = NormalizeRequired(code, nameof(code));
        Name = NormalizeRequired(name, nameof(name));
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Name { get; }

    private static string NormalizeRequired(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", parameterName);
        }

        return value.Trim();
    }
}
