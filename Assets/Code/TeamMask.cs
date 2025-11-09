using System;

[Flags]
public enum TeamMask
{
    Player = 0,
    Enemy = 1 << 0,
}