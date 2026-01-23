namespace PatientSyncHealth.Domain.Enums;

public enum ExaminationFrequency
{
    Monthly = 1,
    Quarterly = 3,
    SemiAnnually = 6,
    Annually = 12,
    Biannually = 24
}

public static class ExaminationFrequencyExtensions
{
    public static int GetMonths(this ExaminationFrequency frequency) => (int)frequency;

    public static DateTime CalculateNextExaminationDate(this ExaminationFrequency frequency, DateTime lastExaminationDate)
    {
        return lastExaminationDate.AddMonths(frequency.GetMonths());
    }
}
