using System;

[Flags]
public enum TeamMask
{
    Player = 1 << 0,
    Enemy = 1 << 1,
}