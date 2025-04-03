using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EuroGen.Models;

public class Draw
{
    [Name("boule_1")]
    [Index(0)]
    public int FirstNumber { get; set; }

    [Name("boule_2")]
    [Index(1)]
    public int SecondNumber { get; set; }

    [Name("boule_3")]
    [Index(2)]
    public int ThirdNumber { get; set; }

    [Name("boule_4")]
    [Index(3)]
    public int FourthNumber { get; set; }

    [Name("boule_5")]
    [Index(4)]
    public int FifthNumber { get; set; }

    [Name("etoile_1")]
    [Index(5)]
    public int FirstStar { get; set; }

    [Name("etoile_2")]
    [Index(6)]
    public int SecondStar { get; set; }

    [DataType(DataType.Date)]
    [Key]
    [Name("date_de_tirage")]
    [Index(7)]
    [Format("dd/MM/yyyy", "dd/MM/yy", "yyyyMMdd")]
    public DateTime DrawDate { get; set; }
}