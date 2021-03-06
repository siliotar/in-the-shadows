public enum LevelInfo : int
{
    Inactive = 0,
    Active = 1,
    Solved = 2
}

public enum GameMode : int
{
    None = 0,
    Normal = 1,
    Test = 2
}

public static class Player
{
    public static LevelInfo[] Levels;
    public static GameMode mode;
    public static int SaveSlot;
}

[System.Serializable]
public class PlayerData
{
    public LevelInfo[] Levels;
    public GameMode mode;
}
