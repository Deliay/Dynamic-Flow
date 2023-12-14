namespace DynamicFlow.Application.Abstraction;

public struct DueTo
{
    public bool IsTBD { get; set; }

    public DateTime Actual { get; set; }

    public DueTo()
    {
        IsTBD = true;
        Actual = default;
    }

    public DueTo(DateTime actual)
    {
        IsTBD = false;
        Actual = actual;
    }

    public readonly static DueTo TBD = new();
}