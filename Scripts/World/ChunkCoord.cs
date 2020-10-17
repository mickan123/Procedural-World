using System;

public struct ChunkCoord : IEquatable<ChunkCoord>
{
    public int x;
    public int y;

    public ChunkCoord(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool Equals(ChunkCoord other)
    {
        return Equals(other, this);
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        var other = (ChunkCoord)obj;

        return this.x == other.x && this.y == other.y;
    }

    public override int GetHashCode()
    {
        var calculation = x + y;
        return calculation.GetHashCode();
    }

    public override string ToString()
    {
        return this.x + "," + this.y;
    }


    public static bool operator ==(ChunkCoord coord1, ChunkCoord coord2)
    {
        return coord1.Equals(coord2);
    }

    public static bool operator !=(ChunkCoord coord1, ChunkCoord coord2)
    {
        return !coord1.Equals(coord2);
    }
}