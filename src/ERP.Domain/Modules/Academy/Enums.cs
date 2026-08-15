namespace ERP.Domain.Modules.Academy;

/// <summary>The sport a player is enrolled in.</summary>
public enum Sport
{
    Football = 0,
    Basketball = 1,
    Swimming = 2,
    Karate = 3,
    Tennis = 4,
    Volleyball = 5,
    Gymnastics = 6
}

/// <summary>Training level, which decides the group a player trains with.</summary>
public enum PlayerLevel
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2
}

public enum Gender
{
    Male = 0,
    Female = 1
}
