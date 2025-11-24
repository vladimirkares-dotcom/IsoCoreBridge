namespace IsoCore.Domain;

public class CalcProfile
{
    public double AreaM2 { get; set; }
    public double PricePerM2 { get; set; }
    public double ReservePercent { get; set; }

    public double BasePrice => AreaM2 * PricePerM2;
    public double ReservedPrice => BasePrice * (1 + ReservePercent / 100);
}
