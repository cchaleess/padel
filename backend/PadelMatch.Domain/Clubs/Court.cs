namespace PadelMatch.Domain.Clubs;

public sealed class Court
{
    public Guid Id { get; private set; }
    public Guid ClubId { get; private set; }
    public string Name { get; private set; } = null!;

    private Court()
    {
    }

    public static Court Create(Guid clubId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        return new Court
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = name
        };
    }
}
