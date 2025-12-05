namespace IsoCore.Domain;

public enum BuildingStructureType
{
    Unknown = 0,
    Most = 1,
    Propustek = 2,
    Nadjezd = 3,
    Jine = 4
}

public enum BuildingCoverType
{
    Unknown = 0,
    Presypany = 1,
    Nepresypany = 2
}

public enum SurfacePrepType
{
    Unknown = 0,
    Penetrak = 1,
    Epoxid = 2
}

public enum BuildingObjectStatus
{
    Draft = 0,
    Planned = 1,
    InProgress = 2,
    Completed = 3
}
