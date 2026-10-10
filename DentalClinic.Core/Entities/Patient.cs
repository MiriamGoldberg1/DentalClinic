namespace DentalClinic.Core.Entities;

/// <summary>מטופל במרפאה. Appointments הוא היסטוריית התורים שהזמין (רק התורים שלו).</summary>
public class Patient : Person
{
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
//פרטים אישיים של הפצינט- בהורשה ממחלקת Person 