using System.ComponentModel;

namespace MyPdfApi.Models;

public enum CoverageType
{
    [Description("Roubo e Furto")]
    RouboEFurto = 1,

    [Description("Basico")]
    Basico = 2,

    [Description("Ampliado")]
    Ampliado = 3,

    [Description("Completo")]
    Completo = 4,

    [Description("Master")]
    Master = 5,

    [Description("Exclusivo")]
    Exclusivo = 6
}