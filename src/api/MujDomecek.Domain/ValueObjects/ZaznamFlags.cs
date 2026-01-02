namespace MujDomecek.Domain.ValueObjects;

[Flags]
public enum ZaznamFlags
{
    None = 0,
    MissingPhoto = 1 << 0,
    Todo = 1 << 1,
    Waiting = 1 << 2,
    Important = 1 << 3,
    Warranty = 1 << 4,
    Favorite = 1 << 5
}
