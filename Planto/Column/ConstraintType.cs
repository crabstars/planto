namespace Planto.Column;

internal enum ConstraintType
{
    ForeignKey,
    PrimaryKey,
    Unique,
    Check, // not supported right now: warning log when detecting
}