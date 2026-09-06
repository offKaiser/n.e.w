public enum TeamId
{
    Blue,
    Red,
    Neutral
}

public interface ITeamMember
{
    TeamId TeamId { get; }
}
